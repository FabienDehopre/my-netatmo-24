using FastEndpoints;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Domain;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Messages;
using Wolverine.EntityFrameworkCore;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class RestoreAccount
{
    public sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Restores the authenticated user's account if it was previously marked as deleted.";
            Description =
                "This endpoint restores the authenticated user's account if it was previously marked as deleted. " +
                "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                "If the user's account cannot be found, a 404 Not Found response is returned. " +
                "If the user's account is successfully restored, a 204 No Content response is returned.";
            Response(StatusCodes.Status204NoContent,
                "The user's account was successfully restored.");
            Response(StatusCodes.Status401Unauthorized,
                "The user is not authenticated, so their account cannot be restored.");
            Response(StatusCodes.Status404NotFound,
                "The user's account could not be found, so it cannot be restored.");
        }
    }

    public sealed class Endpoint(IDbContextOutbox<AccountDbContext> outbox)
        : Ep.NoReq.Res<Results<NoContent, UnauthorizedHttpResult, NotFound>>
    {
        private readonly IDbContextOutbox<AccountDbContext> _outbox = outbox.ThrowIfNull();

        public override void Configure() => Post("/account/restore");

        public override async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> ExecuteAsync(CancellationToken ct)
        {
            var result = await GetAuth0Id()
                .Bind(auth0Id => GetExistingAccount(auth0Id, ct))
                .Bind(account => RestoreAccountAndPublishEvent(account, ct));
            return result switch
            {
                { IsSuccess: true } => TypedResults.NoContent(),
                { IsSuccess: false } when result.Reasons.Any(r => r.IsUserInfoNotFound()) => TypedResults.NotFound(),
                { IsSuccess: false } when result.Reasons.Any(r => r.IsUserNotAuthenticated()) => TypedResults.Unauthorized(),
                _ => throw new InvalidOperationException("This should not happen because all possible case have been handled.")
            };
        }

        private Result<string> GetAuth0Id() =>
            User.Identity?.Name is { } auth0Id && !string.IsNullOrWhiteSpace(auth0Id)
                ? auth0Id
                : Errors.UserNotAuthenticated;

        private async Task<Result<Account>> GetExistingAccount(string auth0Id, CancellationToken ct)
        {
            var existingAccount = await _outbox.DbContext
                .Set<Account>()
                .IgnoreQueryFilters([Constants.SoftDeleteFilter])
                .SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
            return existingAccount is not null
                ? existingAccount
                : Errors.UserInfoNotFound;
        }

        private async Task<Result> RestoreAccountAndPublishEvent(Account account, CancellationToken ct)
        {
            ((ISoftDelete)account).Undo();
            await _outbox.PublishAsync(new AccountRestored(account.Id));
            await _outbox.SaveChangesAndFlushMessagesAsync(ct);
            return Result.Ok();
        }
    }
}
