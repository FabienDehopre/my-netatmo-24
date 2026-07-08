using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Domain;
using MyNetatmo24.SharedKernel.Endpoints;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Messages;
using Wolverine.EntityFrameworkCore;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class RestoreAccount
{
    public static void Configure(IEndpointRouteBuilder builder)
    {
        builder
            .MapPost("restore", HandleAsync)
            .WithName("RestoreAccount")
            .WithSummary("Restores the authenticated user's account if it was previously marked as deleted.")
            .WithDescription("This endpoint restores the authenticated user's account if it was previously marked as deleted. " +
                             "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                             "If the user's account cannot be found, a 404 Not Found response is returned. " +
                             "If the user's account is successfully restored, a 204 No Content response is returned.")
            .ProducesWithDescription(StatusCodes.Status204NoContent, "The user's account was successfully restored.")
            .ProducesWithDescription(StatusCodes.Status401Unauthorized, "The user is not authenticated, so their account cannot be restored.")
            .ProducesWithDescription(StatusCodes.Status404NotFound, "The user's account could not be found, so it cannot be restored.");
    }

    public static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> HandleAsync(
        [FromServices, NotNull] IHttpContextAccessor httpContextAccessor,
        [FromServices, NotNull] IDbContextOutbox<AccountDbContext> outbox,
        CancellationToken ct)
    {
        var result = await GetAuth0Id(httpContextAccessor.HttpContext?.User)
            .Bind(auth0Id => GetExistingAccount(outbox, auth0Id, ct))
            .Bind(account => RestoreAccountAndPublishEvent(outbox, account, ct));
        return result switch
        {
            { IsSuccess: true } => TypedResults.NoContent(),
            { IsSuccess: false } when result.Reasons.Any(r => r.IsUserInfoNotFound()) => TypedResults.NotFound(),
            { IsSuccess: false } when result.Reasons.Any(r => r.IsUserNotAuthenticated()) => TypedResults.Unauthorized(),
            _ => throw new InvalidOperationException("This should not happen because all possible case have been handled.")
        };
    }

    private static Result<string> GetAuth0Id(ClaimsPrincipal? user) =>
        user?.Identity?.Name is { } auth0Id && !string.IsNullOrWhiteSpace(auth0Id)
            ? auth0Id
            : Errors.UserNotAuthenticated;

    private static async Task<Result<Account>> GetExistingAccount(IDbContextOutbox<AccountDbContext> outbox, string auth0Id, CancellationToken ct)
    {
        var existingAccount = await outbox.DbContext
            .Set<Account>()
            .IgnoreQueryFilters([Constants.SoftDeleteFilter])
            .SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
        return existingAccount is not null
            ? existingAccount
            : Errors.UserInfoNotFound;
    }

    private static async Task<Result> RestoreAccountAndPublishEvent(IDbContextOutbox<AccountDbContext> outbox, Account account, CancellationToken ct)
    {
        ((ISoftDelete)account).Undo();
        await outbox.PublishAsync(new AccountRestored(account.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Ok();
    }
}
