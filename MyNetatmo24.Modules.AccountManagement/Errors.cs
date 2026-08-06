using FluentResults;
using Microsoft.AspNetCore.Http;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement;

internal static class Errors
{
    private const string DeletedAtName = "DeletedAt";
    private const string UserExistsMarker = "UserExists";

    public static readonly Error UserNotAuthenticated = new EndpointError(StatusCodes.Status401Unauthorized, "The user is not authenticated.");
    public static readonly Error UserExists = new EndpointError(StatusCodes.Status204NoContent, "The user already exists.").WithMetadata(UserExistsMarker, true);
    public static readonly Error UserInfoNotFound = new EndpointError(StatusCodes.Status404NotFound, "Failed to retrieve user info from Auth0.");
    public static readonly Error AccountNotFound = new EndpointError(StatusCodes.Status404NotFound, "The user's account could not be found.");
    public static Error UserDeleted(DateTimeOffset deletedAt) => new EndpointError(StatusCodes.Status409Conflict, $"The user's account was deleted at {deletedAt}.").WithMetadata(DeletedAtName, deletedAt);

    // Answering 409 tells an anonymous caller which e-mail addresses are registered. That is a
    // deliberate trade-off for a personal-scale application: telling someone "you already have an
    // identity, log in instead" beats a vague error, and the tenant is not a target worth enumerating.
    public static readonly Error EmailAlreadyRegistered = new EndpointError(StatusCodes.Status409Conflict, "The e-mail address is already registered.");
    public static readonly Error IdentityProviderUnavailable = new EndpointError(StatusCodes.Status502BadGateway, "The identity provider could not be reached.");

    /// <summary>
    /// The identity provider rejected the password. Its own policy message is the error's message, so
    /// the person is told what a valid password looks like rather than being left to guess.
    /// </summary>
    public static Error PasswordTooWeak(string policyMessage) =>
        new EndpointError(StatusCodes.Status400BadRequest, policyMessage);

    extension(IReason reason)
    {
        public bool IsUserExistsError() => reason is IError error && error.Metadata.TryGetValue(UserExistsMarker, out var value) && value is true;
        public DateTimeOffset? GetDeletedAt() => reason is IError error && error.Metadata.TryGetValue(DeletedAtName, out var value) && value is DateTimeOffset deletedAt ? deletedAt : null;
        public bool IsUserInfoNotFound() => reason is EndpointError { StatusCode: StatusCodes.Status404NotFound };
        public bool IsUserNotAuthenticated() => reason is EndpointError { StatusCode: StatusCodes.Status401Unauthorized };
    }
}
