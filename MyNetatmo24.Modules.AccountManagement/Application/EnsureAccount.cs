using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Auth0;
using MyNetatmo24.Modules.AccountManagement.Auth0.Dtos;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Messages;
using MyNetatmo24.SharedKernel.Results;
using MyNetatmo24.SharedKernel.StronglyTypedIds;
using Wolverine.EntityFrameworkCore;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class EnsureAccount
{
    /// <param name="DeletedAt">
    /// The date and time when the account was marked as deleted.
    /// </param>
    public sealed record ConflictResponse(DateTimeOffset DeletedAt);

    public sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Ensures that the authenticated user has an account.";
            Description =
                    "This endpoint checks if the authenticated user already has an account. " +
                    "If they do, a 204 No Content response is returned. " +
                    "If they don't, a new account is created for them and a 201 Created response is returned. " +
                    "If the user's information cannot be retrieved from Auth0, a 404 Not Found response is returned. " +
                    "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                    "If an account exists but is marked as deleted, a 409 Conflict response is returned.";
            Response(StatusCodes.Status201Created,
                "A new account was created for the user.");
            Response(StatusCodes.Status204NoContent, "The user already has an account.");
            Response(StatusCodes.Status401Unauthorized,
                "The user is not authenticated, so an account cannot be ensured.");
            Response(StatusCodes.Status404NotFound,
                "The user's information could not be retrieved from Auth0, so an account could not be created.");
            Response<ConflictResponse>(StatusCodes.Status409Conflict,
                "An account for the user already exists but is marked as deleted.");
        }
    }

    public sealed class Endpoint(
        IDbContextOutbox<AccountDbContext> outbox,
        IQueryable<Account> accounts,
        IUserInfoService userInfoService)
        : Ep.NoReq.Res<Results<Created, NoContent, UnauthorizedHttpResult, NotFound, Conflict<ConflictResponse>>>
    {
        private readonly IQueryable<Account> _accounts = accounts.ThrowIfNull();
        private readonly IDbContextOutbox<AccountDbContext> _outbox = outbox.ThrowIfNull();
        private readonly IUserInfoService _userInfoService = userInfoService.ThrowIfNull();

        public override void Configure() => Post("/account/ensure");

        public override async Task<Results<Created, NoContent, UnauthorizedHttpResult, NotFound, Conflict<ConflictResponse>>>
            ExecuteAsync(CancellationToken ct)
        {
            var auth0Id = User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(auth0Id))
            {
                return TypedResults.Unauthorized();
            }

            var existingAccount = await _accounts.SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
            if (existingAccount is not null)
            {
                if (existingAccount.DeletedAt.HasValue)
                {
                    return TypedResults.Conflict(new ConflictResponse(existingAccount.DeletedAt.Value));
                }

                return TypedResults.NoContent();
            }

            var result = await _userInfoService.GetUserInfoAsync(ct);
            return result switch
            {
                { IsSuccess: true } userInfoResult => await CreateUser(userInfoResult.Value, auth0Id, ct),
                { IsFailure: true, Error: NotFoundError } => TypedResults.NotFound(),
                _ => throw new InvalidOperationException("Unexpected error while retrieving user info.")
            };
        }

        private async Task<Created> CreateUser(UserInfo userInfo, string auth0Id, CancellationToken ct)
        {
            var newAccount = Account.Create(
                AccountId.New(),
                auth0Id,
                userInfo.Nickname,
                FullName.From(userInfo.GivenName, userInfo.FamilyName));

            if (userInfo.Picture is not null)
            {
                newAccount.SetAvatarUrl(userInfo.Picture);
            }

            await _outbox.DbContext.AddAsync(newAccount, ct);
            await _outbox.PublishAsync(new AccountCreated(newAccount.Id, newAccount.Name.FirstName,
                newAccount.Name.LastName));
            await _outbox.SaveChangesAndFlushMessagesAsync(ct);

            return TypedResults.Created(new Uri("/api/account/me"));
        }
    }
}
