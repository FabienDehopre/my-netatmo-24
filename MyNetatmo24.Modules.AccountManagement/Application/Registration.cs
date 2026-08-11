using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Endpoints;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class Registration
{
    /// <param name="Email">
    /// The e-mail address of the prospective user, which is required and also acts as their login name.
    /// </param>
    /// <param name="Password">
    /// The password chosen by the prospective user, which is required. Its strength is checked by the
    /// identity provider, not by this application.
    /// </param>
    /// <param name="PasswordConfirmation">
    /// A second copy of the password, which is required. It is collected so that a typo cannot
    /// silently lock the prospective user out of the identity they just created; the check that it
    /// matches <paramref name="Password"/> is not implemented yet.
    /// </param>
    /// <param name="Nickname">
    /// The display name chosen by the prospective user, which is required and is not unique.
    /// </param>
    /// <param name="GivenName">
    /// The given name of the prospective user, which is required.
    /// </param>
    /// <param name="FamilyName">
    /// The family name of the prospective user, which is required.
    /// </param>
    /// <param name="AvatarUrl">
    /// The URL of the avatar picture of the prospective user, which is optional.
    /// </param>
    public sealed record RegistrationRequestDto(
        [property: Required] string Email,
        [property: Required] string Password,
        [property: Required] string PasswordConfirmation,
        [property: Required] string Nickname,
        [property: Required] string GivenName,
        [property: Required] string FamilyName,
        Uri? AvatarUrl = null)
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
            .WithName("Registration")
            .WithSummary("Registers a prospective user with the identity provider.")
            .WithDescription("This endpoint creates the identity of a prospective user from their e-mail address, " +
                             "password and profile, and triggers the verification of their e-mail address. " +
                             "It creates no account: the account is provisioned on the first authenticated call. " +
                             "It requires no authentication. " +
                             "If the registration succeeds, a 204 No Content response is returned. " +
                             "If the submitted registration is invalid, a 400 Bad Request response is returned.")
            .ProducesWithDescription(StatusCodes.Status204NoContent, "The identity of the prospective user was successfully created.")
            .ProducesWithDescription<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "The submitted registration is invalid, so no identity was created.", "application/problem+json");
    }

    public static async Task<NoContent> HandleAsync(
        [FromBody, NotNull] RegistrationRequestDto registration,
        [FromServices, NotNull] IUserRegistrationService userRegistrationService,
        CancellationToken ct)
    {
        var result = await userRegistrationService.RegisterAsync(ToRegistrationData(registration), ct);
        return result switch
        {
            { IsSuccess: true } => TypedResults.NoContent(),

            // The failure outcomes of the seam (e-mail already registered, password rejected by the
            // identity provider policy, provider unavailable) get their HTTP mapping in a follow-up
            // story; no implementation of the seam reports them yet.
            _ => throw new InvalidOperationException("The user registration service reported a failure that has no HTTP mapping yet.")
        };
    }

    private static RegistrationData ToRegistrationData(RegistrationRequestDto registration) =>
        new(registration.Email,
            registration.Password,
            registration.Nickname,
            registration.GivenName,
            registration.FamilyName,
            registration.AvatarUrl);
}
