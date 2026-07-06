using FastEndpoints;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
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
    public sealed record UserDeletedDto(DateTimeOffset DeletedAt);

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
            // Response(StatusCodes.Status201Created, "A new account was created for the user.");
            Response(StatusCodes.Status204NoContent, "The user already has an account.");
            Response(StatusCodes.Status401Unauthorized,
                "The user is not authenticated, so an account cannot be ensured.");
            Response(StatusCodes.Status404NotFound,
                "The user's information could not be retrieved from Auth0, so an account could not be created.");
            Response<UserDeletedDto>(StatusCodes.Status409Conflict,
                "An account for the user already exists but is marked as deleted.");
        }
    }

    public sealed class Endpoint(
        IDbContextOutbox<AccountDbContext> outbox,
        IQueryable<Account> accounts,
        IUserInfoService userInfoService)
        : Ep.NoReq.Res<Results<NoContent, UnauthorizedHttpResult, NotFound, Conflict<UserDeletedDto>>>
    {
        private readonly IQueryable<Account> _accounts = accounts.ThrowIfNull();
        private readonly IDbContextOutbox<AccountDbContext> _outbox = outbox.ThrowIfNull();
        private readonly IUserInfoService _userInfoService = userInfoService.ThrowIfNull();

        public override void Configure() => Post("/account/ensure");

        public override async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, Conflict<UserDeletedDto>>>
            ExecuteAsync(CancellationToken ct)
        {
            var auth0IdResult = GetAuth0Id();
            var result = await auth0IdResult
                .Bind(auth0Id => GetExistingAccount(auth0Id, ct))
                .Bind(() => _userInfoService.GetUserInfoAsync(ct))
                .BindWith(auth0IdResult, (userInfo, auth0Id) => CreateAccount(auth0Id, userInfo))
                .Bind(newAccount => SaveAccountAndPublishEvent(newAccount, ct))
                .Compensate(r => r.Reasons.Any(e => e.IsUserExistsError()) ? Result.Ok() : r);
            return result switch
            {
                { IsSuccess: true } => TypedResults.NoContent(),
                { IsSuccess: false } => result.Reasons.OfType<FastEndpointsError>().SingleOrDefault() switch
                {
                    { StatusCode: 401 } => TypedResults.Unauthorized(),
                    { StatusCode: 404 } => TypedResults.NotFound(),
                    { StatusCode: 409 } conflictError
                        when conflictError.GetDeletedAt() is { } deletedAt =>
                        TypedResults.Conflict(new UserDeletedDto(deletedAt)),
                    _ => throw new InvalidOperationException("Unexpected error while ensuring account.")
                }
            };
        }

        private Result<string> GetAuth0Id() =>
            User.Identity?.Name is { } auth0Id && !string.IsNullOrWhiteSpace(auth0Id)
                ? auth0Id
                : Errors.UserNotAuthenticated;

        private async Task<Result> GetExistingAccount(string auth0Id, CancellationToken ct)
        {
            var existingAccount = await _accounts.IgnoreQueryFilters([Constants.SoftDeleteFilter]).SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
            if (existingAccount is null)
            {
                return Result.Ok();
            }

            if (existingAccount.DeletedAt.HasValue)
            {
                return Errors.UserDeleted(existingAccount.DeletedAt.Value);
            }

            return Errors.UserExists;
        }

        private Result<Account> CreateAccount(string auth0Id, UserInfoDto userInfo)
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

            return newAccount;
        }

        private async Task<Result> SaveAccountAndPublishEvent(Account account, CancellationToken ct)
        {
            await _outbox.DbContext.AddAsync(account, ct);
            await _outbox.PublishAsync(new AccountCreated(account.Id, account.Name.FirstName,
                account.Name.LastName));
            await _outbox.SaveChangesAndFlushMessagesAsync(ct);
            return Result.Ok();
        }
    }
}
