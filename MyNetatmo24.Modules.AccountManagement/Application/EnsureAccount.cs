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
    public class Endpoint(
        IDbContextOutbox<AccountDbContext> outbox,
        IQueryable<Account> accounts,
        IUserInfoService userInfoService)
        : Ep.NoReq.Res<Results<Ok<AccountId>, Created<AccountId>, NotFound, UnauthorizedHttpResult>>
    {
        private readonly IQueryable<Account> _accounts = accounts.ThrowIfNull();
        private readonly IDbContextOutbox<AccountDbContext> _outbox = outbox.ThrowIfNull();
        private readonly IUserInfoService _userInfoService = userInfoService.ThrowIfNull();

        public override void Configure()
        {
            Post("/account/ensure");
            Policies(Constants.Policies.Authenticated);
        }

        public override async Task<Results<Ok<AccountId>, Created<AccountId>, NotFound, UnauthorizedHttpResult>>
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
                return TypedResults.Ok(existingAccount.Id);
            }

            var result = await _userInfoService.GetUserInfoAsync(ct);
            return result switch
            {
                { IsSuccess: true } userInfoResult => await CreateUser(userInfoResult.Value, auth0Id, ct),
                { IsFailure: true, Error: NotFoundError } => TypedResults.NotFound(),
                _ => throw new InvalidOperationException("Unexpected error while retrieving user info.")
            };
        }

        private async Task<Created<AccountId>> CreateUser(UserInfo userInfo, string auth0Id, CancellationToken ct)
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

            return TypedResults.Created("/api/account/me", newAccount.Id);
        }
    }
}
