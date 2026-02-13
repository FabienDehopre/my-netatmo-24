using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Infrastructure;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class MyAccount
{
    /// <param name="Nickname">
    /// The user's nickname, which may be null if the user has not set a nickname.
    /// </param>
    /// <param name="FirstName">
    /// The user's first name, which is required and cannot be null or empty.
    /// </param>
    /// <param name="LastName">
    /// The user's last name, which is required and cannot be null or empty.
    /// </param>
    /// <param name="AvatarUrl">
    /// The URL of the user's avatar, which may be null if the user has not set an avatar.
    /// </param>
    public sealed record Response(string? Nickname, string FirstName, string LastName, Uri? AvatarUrl);

    public sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Gets the authenticated user's account information.";
            Description =
                "This endpoint retrieves the account information of the authenticated user. " +
                "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                "If the user's account cannot be found, a 404 Not Found response is returned. " +
                "If the user's account is found, a 200 OK response is returned with the account information.";
            Response<Response>(StatusCodes.Status200OK, "The user's account information was successfully retrieved.");
            Response(StatusCodes.Status401Unauthorized,
                "The user is not authenticated, so their account information cannot be retrieved.");
            Response(StatusCodes.Status404NotFound,
                "The user's account could not be found, so their account information cannot be retrieved.");
        }
    }

    public sealed class Endpoint(IQueryable<Account> accounts)
        : Ep.NoReq.Res<Results<Ok<Response>, UnauthorizedHttpResult, NotFound>>
    {
        private readonly IQueryable<Account> _accounts = accounts.ThrowIfNull();

        public override void Configure() => Get("/account/me");

        public override async Task<Results<Ok<Response>, UnauthorizedHttpResult, NotFound>> ExecuteAsync(
            CancellationToken ct)
        {
            var auth0Id = User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(auth0Id))
            {
                return TypedResults.Unauthorized();
            }

            var account = await _accounts.SingleOrDefaultAsync(a => a.Auth0Id == auth0Id && a.DeletedAt == null, ct);
            if (account is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(new Response(account.NickName, account.Name.FirstName, account.Name.LastName,
                account.AvatarUrl));
        }
    }
}
