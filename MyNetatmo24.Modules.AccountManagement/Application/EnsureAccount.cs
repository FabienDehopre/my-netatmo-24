using FastEndpoints;
using FluentResults;
using FluentResults.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.FastEndpoints;
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
    [UsedImplicitly]
    public sealed record ConflictResponse(DateTimeOffset DeletedAt);

    public sealed class Endpoint(
        IDbContextOutbox<AccountDbContext> outbox,
        IQueryable<Account> accounts,
        IUserInfoService userInfoService)
        : Ep.NoReq.Res<Results<NoContent, UnauthorizedHttpResult, NotFound, Conflict<ConflictResponse>>>
    {
        private readonly IQueryable<Account> _accounts = accounts.ThrowIfNull();
        private readonly IDbContextOutbox<AccountDbContext> _outbox = outbox.ThrowIfNull();
        private readonly IUserInfoService _userInfoService = userInfoService.ThrowIfNull();

        public override void Configure()
        {
            Post("/account/ensure");
            DontAutoSendResponse();
            Description(desc =>
                desc.WithName("EnsureAccount")
                    .WithSummary("Ensures that the authenticated user has an account.")
                    .WithDescription("This endpoint checks if the authenticated user already has an account. " +
                                     "If they do, a 204 No Content response is returned. " +
                                     "If they don't, a new account is created for them and a 201 Created response is returned. " +
                                     "If the user's information cannot be retrieved from Auth0, a 404 Not Found response is returned. " +
                                     "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                                     "If an account exists but is marked as deleted, a 409 Conflict response is returned."));
                    // .Produces(StatusCodes.Status201Created)
                    // .Produces(StatusCodes.Status204NoContent)
                    // .Produces(StatusCodes.Status401Unauthorized)
                    // .Produces(StatusCodes.Status404NotFound)
                    // .Produces<ConflictResponse>(StatusCodes.Status409Conflict));
            Summary(summary =>
            {
                // summary.Response(StatusCodes.Status201Created,
                //     "A new account was created for the user.");
                summary.Response(StatusCodes.Status204NoContent, "The user already has an account.");
                summary.Response(StatusCodes.Status401Unauthorized,
                    "The user is not authenticated, so an account cannot be ensured.");
                summary.Response(StatusCodes.Status404NotFound,
                    "The user's information could not be retrieved from Auth0, so an account could not be created.");
                summary.Response<ConflictResponse>(StatusCodes.Status409Conflict,
                    "An account for the user already exists but is marked as deleted.");
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var auth0IdResult = GetAuth0Id();
            var result = await auth0IdResult
                .Bind(auth0Id => GetExistingAccount(auth0Id, ct))
                .Bind(() => _userInfoService.GetUserInfoAsync(ct))
                .BindWith(auth0IdResult, (userInfo, auth0Id) => CreateAccount(auth0Id, userInfo))
                .Bind(newAccount => SaveAccountAndPublishEvent(newAccount, ct))
                .Compensate(r => r.Reasons.Any(e => e.IsUserExistsError()) ? Result.Ok() : r);
            switch (result)
            {
                case { IsSuccess: true }:
                    await Send.NoContentAsync(ct);
                    break;
                case { IsSuccess: false }:
                    switch (result.Reasons.OfType<FastEndpointsError>().SingleOrDefault())
                    {
                        case { StatusCode: 401 }:
                            await Send.UnauthorizedAsync(ct);
                            break;
                        case { StatusCode: 404 }:
                            await Send.NotFoundAsync(ct);
                            break;
                        case { StatusCode: 409, Metadata: { } metadata }
                            when metadata.TryGetValue("DeletedAt", out var deletedAtValue) && deletedAtValue is DateTimeOffset deletedAt:
                            await Send.ConflictAsync(new ConflictResponse(deletedAt), ct);
                            break;
                    }
                    break;
            }
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

        // public override async Task<Results<Created, NoContent, UnauthorizedHttpResult, NotFound, Conflict<ConflictResponse>>>
        //     ExecuteAsync(CancellationToken ct)
        // {
        //     var auth0Id = User.Identity?.Name;
        //     if (string.IsNullOrWhiteSpace(auth0Id))
        //     {
        //         return TypedResults.Unauthorized();
        //     }
        //
        //     var existingAccount = await _accounts.SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
        //     if (existingAccount is not null)
        //     {
        //         if (existingAccount.DeletedAt.HasValue)
        //         {
        //             return TypedResults.Conflict(new ConflictResponse(existingAccount.DeletedAt.Value));
        //         }
        //
        //         return TypedResults.NoContent();
        //     }
        //
        //     var result = await _userInfoService.GetUserInfoAsync(ct);
        //     return result switch
        //     {
        //         { IsSuccess: true } userInfoResult => await CreateUser(userInfoResult.Value, auth0Id, ct),
        //         { IsFailure: true, Error: NotFoundError } => TypedResults.NotFound(),
        //         _ => throw new InvalidOperationException("Unexpected error while retrieving user info.")
        //     };
        // }
        //
        // private async Task<Created> CreateUser(UserInfo userInfo, string auth0Id, CancellationToken ct)
        // {
        //     var newAccount = Account.Create(
        //         AccountId.New(),
        //         auth0Id,
        //         userInfo.Nickname,
        //         FullName.From(userInfo.GivenName, userInfo.FamilyName));
        //
        //     if (userInfo.Picture is not null)
        //     {
        //         newAccount.SetAvatarUrl(userInfo.Picture);
        //     }
        //
        //     await _outbox.DbContext.AddAsync(newAccount, ct);
        //     await _outbox.PublishAsync(new AccountCreated(newAccount.Id, newAccount.Name.FirstName,
        //         newAccount.Name.LastName));
        //     await _outbox.SaveChangesAndFlushMessagesAsync(ct);
        //
        //     return TypedResults.Created(new Uri("/api/account/me"));
        // }
    }
}
