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
    public sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Ensures that the authenticated user has an account.";
            Description =
                "This endpoint checks if the authenticated user already has an account. If they do, it returns a 200 OK response." +
                "If they don't, it attempts to create a new account using the user's information from Auth0." +
                "If the account creation is successful, it returns a 201 Created response. If the user's information cannot be retrieved from Auth0, it returns a 404 Not Found response." +
                "If the user is not authenticated, it returns a 401 Unauthorized response.";
            Response(StatusCodes.Status200OK, "The user already has an account.");
            Response(StatusCodes.Status201Created,
                "A new account was created for the user.");
            Response(StatusCodes.Status404NotFound,
                "The user's information could not be retrieved from Auth0, so an account could not be created.");
            Response(StatusCodes.Status401Unauthorized,
                "The user is not authenticated, so an account cannot be ensured.");
        }
    }

    public sealed class Endpoint(
        IDbContextOutbox<AccountDbContext> outbox,
        IQueryable<Account> accounts,
        IUserInfoService userInfoService)
        : Ep.NoReq.Res<Results<Ok, Created, NotFound, UnauthorizedHttpResult>>
    {
        private readonly IQueryable<Account> _accounts = accounts.ThrowIfNull();
        private readonly IDbContextOutbox<AccountDbContext> _outbox = outbox.ThrowIfNull();
        private readonly IUserInfoService _userInfoService = userInfoService.ThrowIfNull();

        public override void Configure()
        {
            Post("/account/ensure");
        }

        public override async Task<Results<Ok, Created, NotFound, UnauthorizedHttpResult>>
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
                return TypedResults.Ok();
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
