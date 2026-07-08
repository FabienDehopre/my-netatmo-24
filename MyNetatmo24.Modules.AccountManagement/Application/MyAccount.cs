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
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Endpoints;

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
    public sealed record UserInfoDto(string? Nickname, string FirstName, string LastName, Uri? AvatarUrl);

    public static void Configure(IEndpointRouteBuilder builder)
    {
        builder
            .MapGet("me", HandleAsync)
            .WithName("MyAccount")
            .WithSummary("Gets the authenticated user's account information.")
            .WithDescription("This endpoint retrieves the account information of the authenticated user. " +
                             "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                             "If the user's account cannot be found, a 404 Not Found response is returned. " +
                             "If the user's account is found, a 200 OK response is returned with the account information.")
            .ProducesWithDescription<UserInfoDto>(StatusCodes.Status200OK, "The user's account information was successfully retrieved.")
            .ProducesWithDescription(StatusCodes.Status401Unauthorized, "The user is not authenticated, so their account information cannot be retrieved.")
            .ProducesWithDescription(StatusCodes.Status404NotFound, "The user's account could not be found, so their account information cannot be retrieved.");
    }

    public static async Task<Results<Ok<UserInfoDto>, UnauthorizedHttpResult, NotFound>> HandleAsync(
        [FromServices, NotNull] IHttpContextAccessor httpContextAccessor,
        [FromServices, NotNull] IQueryable<Account> accounts,
        [FromKeyedServices("Account"), NotNull] HybridCache cache,
        CancellationToken ct)
    {
        var auth0IdResult = GetAuth0Id(httpContextAccessor.HttpContext?.User);
        if (auth0IdResult.IsFailed)
        {
            return TypedResults.Unauthorized();
        }

        var auth0Id = auth0IdResult.Value;
        var cached = await cache.GetOrCreateAccountAsync(
            auth0Id,
            async cancel => await GetExistingAccount(accounts, auth0Id, cancel)
                .Map(account => new UserInfoDto(
                    account.NickName,
                    account.Name.FirstName,
                    account.Name.LastName,
                    account.AvatarUrl)),
            ct);
        return cached switch
        {
            { Result.IsSuccess: true, Result.Value: { } userInfo } => TypedResults.Ok(userInfo),
            _ => TypedResults.NotFound()
        };
    }

    private static Result<string> GetAuth0Id(ClaimsPrincipal? user) =>
        user?.Identity?.Name is { } auth0Id && !string.IsNullOrWhiteSpace(auth0Id)
            ? auth0Id
            : Errors.UserNotAuthenticated;

    private static async Task<Result<Account>> GetExistingAccount(IQueryable<Account> accounts, string auth0Id, CancellationToken ct)
    {
        var account = await accounts.SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
        return account is not null
            ? account
            : Errors.AccountNotFound;
    }
}
