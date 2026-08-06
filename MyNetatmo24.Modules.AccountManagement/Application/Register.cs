using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Validation;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Endpoints;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class Register
{
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
        [property: Required] string PasswordConfirmation,
        [property: Required] string Nickname,
        [property: Required] string GivenName,
        [property: Required] string FamilyName,
        string? AvatarUrl);

    public static void Configure(IEndpointRouteBuilder builder)
    {
        builder
            .MapPost("register", HandleAsync)
            // The module's only anonymous endpoint: signing up cannot require the session it creates.
            .AllowAnonymous()
            .WithName("Register")
            .WithSummary("Registers a new identity for a prospective user.")
            .WithDescription("This endpoint creates the identity of a prospective user with the submitted e-mail, " +
                             "password and profile, and triggers the verification e-mail. " +
                             "It creates no account: the account is provisioned on the first authenticated call. " +
                             "If the submitted data is invalid, a 400 Bad Request response is returned. " +
                             "If the identity is created, a 204 No Content response is returned.")
            .ProducesWithDescription(StatusCodes.Status204NoContent, "The identity was created and a verification e-mail was sent.")
            .ProducesValidationProblemWithDescription("The submitted registration data is invalid.");
    }

    public static async Task<NoContent> HandleAsync(
        [FromBody, NotNull] RegistrationDto registration,
        [FromServices, NotNull] IRegistrationService registrationService,
        CancellationToken ct)
    {
        await registrationService.RegisterAsync(ToRegistrationRequest(registration), ct);
        return TypedResults.NoContent();
    }

    private static RegistrationRequest ToRegistrationRequest(RegistrationDto registration) =>
        new(
            registration.Email,
            registration.Password,
            registration.Nickname,
            registration.GivenName,
            registration.FamilyName,
            Uri.TryCreate(registration.AvatarUrl, UriKind.Absolute, out var avatarUrl) ? avatarUrl : null);
}
