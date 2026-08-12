using FluentResults;
using Microsoft.AspNetCore.Http;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement;

internal static class Errors
{
    private const string DeletedAtName = "DeletedAt";
    private const string UserExistsMarker = "UserExists";
    private const string PasswordPolicyMessageName = "PasswordPolicyMessage";
    private const string EmailAlreadyRegisteredMarker = "EmailAlreadyRegistered";
    private const string IdentityProviderUnavailableMarker = "IdentityProviderUnavailable";

    public static readonly Error UserNotAuthenticated = new EndpointError(StatusCodes.Status401Unauthorized, "The user is not authenticated.");
    public static readonly Error UserExists = new EndpointError(StatusCodes.Status204NoContent, "The user already exists.").WithMetadata(UserExistsMarker, true);
    public static readonly Error UserInfoNotFound = new EndpointError(StatusCodes.Status404NotFound, "Failed to retrieve user info from Auth0.");
    public static readonly Error AccountNotFound = new EndpointError(StatusCodes.Status404NotFound, "The user's account could not be found.");

    /// <summary>
    /// Reports that the e-mail address of a Registration already identifies someone with the
    /// identity provider, so no second identity can be created for it.
    /// </summary>
    /// <remarks>
    /// The message is worded for the prospective user, since it is the one the endpoint reports.
    /// </remarks>
    public static readonly Error EmailAlreadyRegistered = new EndpointError(StatusCodes.Status409Conflict, "This e-mail address is already registered. Log in instead.")
        .WithMetadata(EmailAlreadyRegisteredMarker, true);

    /// <summary>
    /// Reports that the identity provider could not be reached or failed to answer a Registration,
    /// which says nothing about whether the registration itself was acceptable.
    /// </summary>
    /// <remarks>
    /// The message is worded for the prospective user, since it is the one the endpoint reports.
    /// </remarks>
    public static readonly Error IdentityProviderUnavailable = new EndpointError(StatusCodes.Status502BadGateway, "Registration is temporarily unavailable. Try again later.")
        .WithMetadata(IdentityProviderUnavailableMarker, true);

    public static Error UserDeleted(DateTimeOffset deletedAt) => new EndpointError(StatusCodes.Status409Conflict, $"The user's account was deleted at {deletedAt}.").WithMetadata(DeletedAtName, deletedAt);

    /// <summary>
    /// Reports that the identity provider refused the password of a Registration as too weak.
    /// </summary>
    /// <param name="policyMessage">
    /// The description of the password policy, as the identity provider worded it. It is the only
    /// authority on what a valid password looks like, so it is carried verbatim to the prospective
    /// user instead of being restated locally.
    /// </param>
    /// <returns>The error carrying <paramref name="policyMessage"/>.</returns>
    public static Error PasswordTooWeak(string policyMessage) =>
        new EndpointError(StatusCodes.Status400BadRequest, "The password does not satisfy the password policy of the identity provider.")
            .WithMetadata(PasswordPolicyMessageName, policyMessage);

    extension(IReason reason)
    {
        public bool IsUserExistsError() => reason is IError error && error.Metadata.TryGetValue(UserExistsMarker, out var value) && value is true;
        public DateTimeOffset? GetDeletedAt() => reason is IError error && error.Metadata.TryGetValue(DeletedAtName, out var value) && value is DateTimeOffset deletedAt ? deletedAt : null;
        public bool IsUserInfoNotFound() => reason is EndpointError { StatusCode: StatusCodes.Status404NotFound };
        public bool IsUserNotAuthenticated() => reason is EndpointError { StatusCode: StatusCodes.Status401Unauthorized };
        public string? GetPasswordPolicyMessage() => reason is IError error && error.Metadata.TryGetValue(PasswordPolicyMessageName, out var value) && value is string policyMessage ? policyMessage : null;
        public bool IsEmailAlreadyRegistered() => reason is IError error && error.Metadata.TryGetValue(EmailAlreadyRegisteredMarker, out var value) && value is true;
        public bool IsIdentityProviderUnavailable() => reason is IError error && error.Metadata.TryGetValue(IdentityProviderUnavailableMarker, out var value) && value is true;
    }
}
