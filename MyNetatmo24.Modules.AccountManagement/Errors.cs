using FluentResults;
using Microsoft.AspNetCore.Http;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement;

internal static class Errors
{
    public static readonly Error UserNotAuthenticated = new UserNotAuthenticatedError();
    public static readonly Error UserExists = new UserExistsError();
    public static readonly Error UserInfoNotFound = new UserInfoNotFoundError();
    public static Error UserDeleted(DateTimeOffset deletedAt) => new UserDeletedError(deletedAt);

    private sealed class UserNotAuthenticatedError()
        : FastEndpointsError(StatusCodes.Status401Unauthorized, "The user is not authenticated.");

    private sealed class UserDeletedError : FastEndpointsError
    {
        private const string DeletedAtName = "DeletedAt";

        public DateTimeOffset? DeletedAt => Metadata.TryGetValue(DeletedAtName, out var value) && value is DateTimeOffset deletedAt ? deletedAt : null;

        public UserDeletedError(DateTimeOffset deletedAt) : base(StatusCodes.Status409Conflict, $"The user was deleted at {deletedAt}.")
        {
            Metadata.Add(DeletedAtName, deletedAt);
        }
    }

    private sealed class UserInfoNotFoundError()
        : FastEndpointsError(StatusCodes.Status404NotFound, "Failed to retrieve user info from Auth0.");

    private sealed class UserExistsError()
        : FastEndpointsError(StatusCodes.Status204NoContent, "The user already exists.");

    extension(IReason reason)
    {
        public bool IsUserExistsError() => reason is UserExistsError;
    }
}
