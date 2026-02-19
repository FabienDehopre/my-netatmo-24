using FluentResults;
using Microsoft.AspNetCore.Http;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement;

internal static class Errors
{
    private const string DeletedAtName = "DeletedAt";
    private const string UserExistsMarker = "UserExists";

    public static readonly Error UserNotAuthenticated = new FastEndpointsError(StatusCodes.Status401Unauthorized, "The user is not authenticated.");
    public static readonly Error UserExists = new FastEndpointsError(StatusCodes.Status204NoContent, "The user already exists.").WithMetadata(UserExistsMarker, true);
    public static readonly Error UserInfoNotFound = new FastEndpointsError(StatusCodes.Status404NotFound, "Failed to retrieve user info from Auth0.");
    public static Error UserDeleted(DateTimeOffset deletedAt) => new FastEndpointsError(StatusCodes.Status409Conflict, $"The user was deleted at {deletedAt}.").WithMetadata(DeletedAtName, deletedAt);

    extension(IReason reason)
    {
        public bool IsUserExistsError() => reason is IError error && error.Metadata.TryGetValue(UserExistsMarker, out var value) && value is true;
        public DateTimeOffset? GetDeletedAt() => reason is IError error && error.Metadata.TryGetValue(DeletedAtName, out var value) && value is DateTimeOffset deletedAt ? deletedAt : null;
    }
}
