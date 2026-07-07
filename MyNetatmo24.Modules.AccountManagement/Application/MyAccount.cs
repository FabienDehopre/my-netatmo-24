using System.Security.Claims;
using FluentResults;
using FluentResults.Extensions;
using MartinCostello.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Endpoints;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public sealed class MyAccount(
    IHttpContextAccessor httpContextAccessor,
    IQueryable<Account> accounts,
    [FromKeyedServices("Account")] HybridCache cache)
    : EndpointWithoutRequest<Results<Ok<MyAccount.UserInfoDto>, UnauthorizedHttpResult, NotFound>>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor.ThrowIfNull();
    private readonly IQueryable<Account> _accounts = accounts.ThrowIfNull();
    private readonly HybridCache _cache = cache.ThrowIfNull();

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

    public override void Configure(IEndpointRouteBuilder builder)
    {
        builder
            .MapGet("me", InvokeAsync)
            .WithName("MyAccount")
            .WithSummary("Gets the authenticated user's account information.")
            .WithDescription("This endpoint retrieves the account information of the authenticated user. " +
                             "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                             "If the user's account cannot be found, a 404 Not Found response is returned. " +
                             "If the user's account is found, a 200 OK response is returned with the account information.")
            .Produces<UserInfoDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesOpenApiResponse(StatusCodes.Status200OK, "he user's account information was successfully retrieved.")
            .ProducesOpenApiResponse(StatusCodes.Status401Unauthorized, "The user is not authenticated, so their account information cannot be retrieved.")
            .ProducesOpenApiResponse(StatusCodes.Status404NotFound, "The user's account could not be found, so their account information cannot be retrieved.");
    }

    public override async Task<Results<Ok<UserInfoDto>, UnauthorizedHttpResult, NotFound>> InvokeAsync(CancellationToken ct)
    {
        var auth0IdResult = GetAuth0Id(_httpContextAccessor.HttpContext?.User);
        if (auth0IdResult.IsFailed)
        {
            return TypedResults.Unauthorized();
        }

        var auth0Id = auth0IdResult.Value;
        var cached = await _cache.GetOrCreateAccountAsync(
            auth0Id,
            async cancel => await GetExistingAccount(auth0Id, cancel)
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

    private Result<string> GetAuth0Id(ClaimsPrincipal? user) =>
        user?.Identity?.Name is { } auth0Id && !string.IsNullOrWhiteSpace(auth0Id)
            ? auth0Id
            : Errors.UserNotAuthenticated;

    private async Task<Result<Account>> GetExistingAccount(string auth0Id, CancellationToken ct)
    {
        var account = await _accounts.SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
        return account is not null
            ? account
            : Errors.AccountNotFound;
    }
}
