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
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Endpoints;
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

    public static void Configure(IEndpointRouteBuilder builder)
    {
        builder
            .MapPost("ensure", HandleAsync)
            .WithName("EnsureAccount")
            .WithSummary("Ensures that the authenticated user has an account.")
            .WithDescription("This endpoint checks if the authenticated user already has an account. " +
                             "If they do, a 204 No Content response is returned. " +
                             "If they don't, a new account is created for them and a 201 Created response is returned. " +
                             "If the user's information cannot be retrieved from Auth0, a 404 Not Found response is returned. " +
                             "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                             "If an account exists but is marked as deleted, a 409 Conflict response is returned.")
            .ProducesWithDescription(StatusCodes.Status204NoContent, "The authenticated user already has an account.")
            .ProducesWithDescription(StatusCodes.Status401Unauthorized, "The user is not authenticated, so an account cannot be ensured.")
            .ProducesWithDescription(StatusCodes.Status404NotFound, "The user's information could not be retrieved from Auth0, so an account could not be created.")
            .ProducesWithDescription<UserDeletedDto>(StatusCodes.Status409Conflict, "An account for the user already exists but is marked as deleted.");
    }

    public static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, Conflict<UserDeletedDto>>>
        HandleAsync(
            [FromServices, NotNull] IHttpContextAccessor httpContextAccessor,
            [FromServices, NotNull] IDbContextOutbox<AccountDbContext> outbox,
            [FromServices, NotNull] IQueryable<Account> accounts,
            [FromServices, NotNull] IUserInfoService userInfoService,
            CancellationToken ct)
    {
        var auth0IdResult = GetAuth0Id(httpContextAccessor.HttpContext?.User);
        var result = await auth0IdResult
            .Bind(auth0Id => GetExistingAccount(accounts, auth0Id, ct))
            .Bind(() => userInfoService.GetUserInfoAsync(ct))
            .BindWith(auth0IdResult, (userInfo, auth0Id) => CreateAccount(auth0Id, userInfo))
            .Bind(newAccount => SaveAccountAndPublishEvent(outbox, newAccount, ct))
            .Compensate(r => r.Reasons.Any(e => e.IsUserExistsError()) ? Result.Ok() : r);
        return result switch
        {
            { IsSuccess: true } => TypedResults.NoContent(),
            { IsSuccess: false } => result.Reasons.OfType<EndpointError>().SingleOrDefault() switch
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

    private static Result<string> GetAuth0Id(ClaimsPrincipal? user) =>
        user?.Identity?.Name is { } auth0Id && !string.IsNullOrWhiteSpace(auth0Id)
            ? auth0Id
            : Errors.UserNotAuthenticated;

    private static async Task<Result> GetExistingAccount(IQueryable<Account> accounts, string auth0Id, CancellationToken ct)
    {
        var existingAccount = await accounts.IgnoreQueryFilters([Constants.SoftDeleteFilter]).SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
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

    private static Result<Account> CreateAccount(string auth0Id, UserInfoDto userInfo)
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

    private static async Task<Result> SaveAccountAndPublishEvent(IDbContextOutbox<AccountDbContext> outbox, Account account, CancellationToken ct)
    {
        await outbox.DbContext.AddAsync(account, ct);
        await outbox.PublishAsync(new AccountCreated(account.Id, account.Name.FirstName, account.Name.LastName));
        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Ok();
    }
}
