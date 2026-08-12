using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.Modules.AccountManagement.RateLimiting;
using MyNetatmo24.Modules.AccountManagement.Validation;
using MyNetatmo24.SharedKernel.Endpoints;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class Registration
{
    /// <summary>
    /// The maximum number of characters a profile text value may contain once trimmed.
    /// </summary>
    private const int MaximumProfileTextLength = 50;

    /// <summary>
    /// The maximum number of characters the avatar URL may contain.
    /// </summary>
    private const int MaximumAvatarUrlLength = 2048;

    /// <param name="Email">
    /// The e-mail address of the prospective user, which is required and also acts as their login name.
    /// </param>
    /// <param name="Password">
    /// The password chosen by the prospective user, which is required. Its strength is checked by the identity provider, not by this application.
    /// </param>
    /// <param name="PasswordConfirmation">
    /// A second copy of the password, which is required and must equal <paramref name="Password"/>. It is collected so that a typo cannot silently lock the prospective user out of the identity they just created.
    /// </param>
    /// <param name="Nickname">
    /// The display name chosen by the prospective user, which is required and is not unique. It is trimmed, must not be empty, must be at most 50 characters long and must contain no Unicode control or format character; every script is accepted.
    /// </param>
    /// <param name="GivenName">
    /// The given name of the prospective user, which is required. It is trimmed, must not be empty, must be at most 50 characters long and must contain no Unicode control or format character; every script is accepted.
    /// </param>
    /// <param name="FamilyName">
    /// The family name of the prospective user, which is required. It is trimmed, must not be empty, must be at most 50 characters long and must contain no Unicode control or format character; every script is accepted.
    /// </param>
    /// <param name="AvatarUrl">
    /// The URL of the avatar picture of the prospective user, which is optional. When present it must be an absolute https URL of at most 2048 characters.
    /// </param>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "The avatar URL is carried as text on purpose, so that text no URI parser accepts is rejected by validation naming the field rather than by the body reader.")]
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "The avatar URL is carried as text on purpose, so that text no URI parser accepts is rejected by validation naming the field rather than by the body reader.")]
    public sealed record RegistrationRequestDto(
        [property: Required, RegularExpression(@".*\S.*$", ErrorMessage = "The {0} field must not be empty.")] string Email,
        [property: Required] string Password,
        [property: Required, Compare(nameof(RegistrationRequestDto.Password))] string PasswordConfirmation,
        [property: Required, ProfileText(MaximumProfileTextLength)] string Nickname,
        [property: Required, ProfileText(MaximumProfileTextLength)] string GivenName,
        [property: Required, ProfileText(MaximumProfileTextLength)] string FamilyName,
        // No [Url] next to it: its looser rule (any fully-qualified http, https or ftp URL) would
        // only add a second, overlapping message to the field the person has to correct.
        [property: AbsoluteHttpsUrl(MaximumAvatarUrlLength)] string? AvatarUrl = null)
    {
        /// <summary>
        /// Suppresses the compiler-generated member printing, which would put the plaintext
        /// password into every log line, exception dump or debugger view this record reaches.
        /// </summary>
        /// <param name="_">The builder the record would have printed its members into.</param>
        /// <returns>Always <see langword="false"/>, so nothing is printed between the braces.</returns>
        private bool PrintMembers(StringBuilder _) => false;
    }

    public static void Configure(IEndpointRouteBuilder builder)
    {
        builder
            .MapPost("register", HandleAsync)
            // The only anonymous endpoint of the module: Registration cannot require the very
            // session it exists to enable. It opts out of the group's RequireAuthorization().
            .AllowAnonymous()
            // Anonymous and identity-creating, so it is also the only endpoint a bot has any reason
            // to flood. No other endpoint of the module carries the policy.
            .RequireRateLimiting(RegistrationRateLimiterPolicy.Name)
            .WithName("Registration")
            .WithSummary("Registers a prospective user with the identity provider.")
            .WithDescription("This endpoint creates the identity of a prospective user from their e-mail address, " +
                             "password and profile, and triggers the verification of their e-mail address. " +
                             "It creates no account: the account is provisioned on the first authenticated call. " +
                             "It requires no authentication. " +
                             "If the registration succeeds, a 204 No Content response is returned. " +
                             "If the submitted registration is invalid, or if the identity provider refuses the " +
                             "password as too weak, a 400 Bad Request response is returned. " +
                             "If the e-mail address is already registered, a 409 Conflict response is returned. " +
                             "If too many registrations are attempted from the same client, a 429 Too Many Requests " +
                             "response is returned. " +
                             "If the identity provider is unavailable, a 502 Bad Gateway response is returned.")
            .ProducesWithDescription(StatusCodes.Status204NoContent, "The identity of the prospective user was successfully created.")
            .ProducesWithDescription<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "The submitted registration is invalid, or its password does not satisfy the password policy of the identity provider, so no identity was created.", "application/problem+json")
            .ProducesWithDescription<ProblemDetails>(StatusCodes.Status409Conflict, "The e-mail address is already registered, so no second identity was created for it.", "application/problem+json")
            .ProducesWithDescription<ProblemDetails>(StatusCodes.Status429TooManyRequests, "Too many registrations were attempted from the same client, so this one was not forwarded to the identity provider. The Retry-After header tells when the next one will be.", "application/problem+json")
            .ProducesWithDescription<ProblemDetails>(StatusCodes.Status502BadGateway, "The identity provider could not be reached, so whether an identity was created is unknown.", "application/problem+json");
    }

    public static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> HandleAsync(
        [FromBody, NotNull] RegistrationRequestDto registration,
        [FromServices, NotNull] IUserRegistrationService userRegistrationService,
        CancellationToken ct)
    {
        var result = await userRegistrationService.RegisterAsync(ToRegistrationData(registration), ct);
        return result switch
        {
            { IsSuccess: true } => TypedResults.NoContent(),
            // The outcomes are told apart by their identity in the catalogue rather than by their
            // status code, which several unrelated errors of the module share.
            { IsSuccess: false } => result.Reasons.OfType<EndpointError>().SingleOrDefault() switch
            {
                // Answering 409 tells anyone who asks whether an e-mail address is registered here.
                // That enumeration is accepted on purpose: for an application at this scale, letting
                // someone who already has an identity go straight to logging in is worth more than
                // hiding the membership of an address behind a fake success.
                { } emailTaken when emailTaken.IsEmailAlreadyRegistered() => TypedResults.Problem(
                    detail: emailTaken.Message,
                    statusCode: emailTaken.StatusCode,
                    title: "E-mail address already registered"),

                // The password policy lives with the identity provider, so its verdict is reported
                // as a validation failure on the field the prospective user has to correct, in the
                // same shape as the locally-checked ones.
                { } weakPassword when weakPassword.GetPasswordPolicyMessage() is { } policyMessage =>
                    TypedResults.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        [nameof(RegistrationRequestDto.Password)] = [policyMessage]
                    }),

                { } unavailable when unavailable.IsIdentityProviderUnavailable() => TypedResults.Problem(
                    detail: unavailable.Message,
                    statusCode: unavailable.StatusCode,
                    title: "Identity provider unavailable"),

                _ => throw new InvalidOperationException("The user registration service reported a failure that has no HTTP mapping.")
            }
        };
    }

    /// <summary>
    /// Normalizes the submitted registration into the form the seam consumes: every value the
    /// prospective user typed for display is trimmed, since surrounding whitespace is a typing
    /// accident and the identity provider would otherwise store it forever, and the avatar URL is
    /// parsed. The password is the exception -- its whitespace is part of the credential.
    /// </summary>
    /// <param name="registration">
    /// The registration as it was submitted, which validation has already accepted.
    /// </param>
    /// <returns>The normalized registration.</returns>
    private static RegistrationData ToRegistrationData(RegistrationRequestDto registration) =>
        new(registration.Email.Trim(),
            registration.Password,
            registration.Nickname.Trim(),
            registration.GivenName.Trim(),
            registration.FamilyName.Trim(),
            // Validation has already parsed this exact text as an absolute URL, so it cannot throw
            // here for a request that reached the handler.
            registration.AvatarUrl is null ? null : new Uri(registration.AvatarUrl, UriKind.Absolute));
}
