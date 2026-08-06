using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Validation;
using MyNetatmo24.Modules.AccountManagement.Application.Validation;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Endpoints;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class Register
{
    /// <summary>
    /// How many registrations a single client may attempt per <see cref="RateLimitWindow"/>.
    /// </summary>
    internal const int RateLimitPermitLimit = 5;

    /// <summary>
    /// The window over which <see cref="RateLimitPermitLimit"/> is counted.
    /// </summary>
    internal static TimeSpan RateLimitWindow => TimeSpan.FromMinutes(1);

    /// <summary>
    /// The name of the rate-limiting policy guarding this endpoint.
    /// </summary>
    private const string RateLimitPolicyName = "registration";

    /// <param name="Email">
    /// The e-mail address the identity is created with. Required.
    /// </param>
    /// <param name="Password">
    /// The password of the new identity. Required, forwarded to the identity provider and never stored locally.
    /// </param>
    /// <param name="PasswordConfirmation">
    /// A repeat of <paramref name="Password"/>, so that a typo cannot silently lock the person out. Required.
    /// </param>
    /// <param name="Nickname">
    /// The display name the person picked. Required.
    /// </param>
    /// <param name="GivenName">
    /// The person's given name. Required.
    /// </param>
    /// <param name="FamilyName">
    /// The person's family name. Required.
    /// </param>
    /// <param name="AvatarUrl">
    /// An optional URL pointing at the person's avatar image.
    /// </param>
    // AvatarUrl stays a string so that a malformed URL is reported as a validation error naming the
    // field, instead of failing deserialization with an opaque 400.
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "The raw string is validated and reported per field.")]
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "The raw string is validated and reported per field.")]
#pragma warning disable ASP0029 // Microsoft.Extensions.Validation is [Experimental] in .NET 10.
    [ValidatableType]
#pragma warning restore ASP0029
    public sealed record RegistrationDto(
        [property: Required, EmailAddress] string Email,
        [property: Required] string Password,
        // The password itself is never judged locally: its strength is the identity provider's policy,
        // and duplicating the rules here would only let the two drift apart.
        [property: Required, Compare(nameof(RegistrationDto.Password))] string PasswordConfirmation,
        [property: Required, ProfileText] string Nickname,
        [property: Required, ProfileText] string GivenName,
        [property: Required, ProfileText] string FamilyName,
        [property: AvatarUrl] string? AvatarUrl);

    public static void Configure(IEndpointRouteBuilder builder)
    {
        builder
            .MapPost("register", HandleAsync)
            // The module's only anonymous endpoint: signing up cannot require the session it creates.
            .AllowAnonymous()
            // Anonymous and backed by an external tenant: without a limit, a bot could fill the
            // identity provider with junk identities and verification e-mails.
            .RequireRateLimiting(RateLimitPolicyName)
            .WithName("Register")
            .WithSummary("Registers a new identity for a prospective user.")
            .WithDescription("This endpoint creates the identity of a prospective user with the submitted e-mail, " +
                             "password and profile, and triggers the verification e-mail. " +
                             "It creates no account: the account is provisioned on the first authenticated call. " +
                             "If the submitted data is invalid, a 400 Bad Request response is returned. " +
                             "If the identity is created, a 204 No Content response is returned.")
            .ProducesWithDescription(StatusCodes.Status204NoContent, "The identity was created and a verification e-mail was sent.")
            .ProducesValidationProblemWithDescription("The submitted registration data is invalid, or the password does not satisfy the identity provider's policy.")
            .ProducesWithDescription(StatusCodes.Status409Conflict, "The e-mail address is already registered.")
            .ProducesWithDescription(StatusCodes.Status429TooManyRequests, "Too many registrations were attempted from this client; try again later.")
            .ProducesProblemWithDescription(StatusCodes.Status502BadGateway, "The identity provider could not be reached, so no identity was created.");
    }

    /// <summary>
    /// Registers the rate-limiting policy <see cref="Configure"/> asks for. It lives next to the endpoint
    /// it guards rather than in the module's wiring, because every number in it - the budget, the window,
    /// what counts as one client - is part of this endpoint's contract and nothing else's.
    /// </summary>
    /// <param name="options">The application's rate-limiter options.</param>
    internal static void ConfigureRateLimiting(RateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(RateLimitPolicyName, GetPartitionFor);
    }

    /// <summary>
    /// The rate-limiting partition of the calling client: the address the gateway observed, so that
    /// one abusive client cannot spend everybody else's budget.
    /// </summary>
    internal static string GetClientPartitionKey(HttpContext context)
    {
        // The endpoint is only reachable through the gateway, and YARP overwrites X-Forwarded-For
        // rather than appending to it, so the single entry it carries is the address the gateway
        // saw. Falling back to the connection address keeps the limiter meaningful when the
        // endpoint is reached directly.
        //
        // The header is trusted as it arrives: nothing here checks that the hop it came from is the
        // gateway. Through the gateway that is sound, but the API service is also directly addressable
        // on the internal network, and a caller reaching it that way can rotate its own partition and
        // spend an unbounded budget. Closing that off means UseForwardedHeaders with an explicit
        // KnownProxies list, which is a deliberate follow-up rather than part of this endpoint.
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        var clientAddress = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrEmpty(clientAddress)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : clientAddress;
    }

    public static async Task<Results<NoContent, Conflict, ValidationProblem, ProblemHttpResult>> HandleAsync(
        [FromBody, NotNull] RegistrationDto registration,
        [FromServices, NotNull] IRegistrationService registrationService,
        CancellationToken ct)
    {
        var result = await registrationService.RegisterAsync(ToRegistrationRequest(registration), ct);
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        return result.Reasons.OfType<EndpointError>().FirstOrDefault() switch
        {
            { StatusCode: StatusCodes.Status409Conflict } => TypedResults.Conflict(),
            // The password policy is the only thing the seam can reject with a 400: everything else it
            // could disagree about was already settled by validation before the call. Reporting the
            // failure against Password is therefore exact, but it stops being so the moment the seam
            // learns a second 400 - match on the reason rather than the status code if it ever does.
            { StatusCode: StatusCodes.Status400BadRequest } weakPassword =>
                TypedResults.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [nameof(RegistrationDto.Password)] =
                        [weakPassword.GetPasswordPolicyMessage() ?? weakPassword.Message]
                }),
            { StatusCode: StatusCodes.Status502BadGateway } unavailable =>
                TypedResults.Problem(unavailable.Message, statusCode: StatusCodes.Status502BadGateway),
            _ => throw new InvalidOperationException("Unexpected error while registering an identity.")
        };
    }

    private static RateLimitPartition<string> GetPartitionFor(HttpContext httpContext) =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = RateLimitPermitLimit,
                Window = RateLimitWindow,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

    /// <summary>
    /// Normalizes the submitted data on its way to the seam: profile values are trimmed, and a blank
    /// avatar URL becomes no avatar at all.
    /// </summary>
    private static RegistrationRequest ToRegistrationRequest(RegistrationDto registration) =>
        new(
            registration.Email.Trim(),
            registration.Password,
            registration.Nickname.Trim(),
            registration.GivenName.Trim(),
            registration.FamilyName.Trim(),
            Uri.TryCreate(registration.AvatarUrl?.Trim(), UriKind.Absolute, out var avatarUrl) ? avatarUrl : null);
}
