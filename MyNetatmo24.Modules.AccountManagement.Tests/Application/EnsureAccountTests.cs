using FastEndpoints;
using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;
using MyNetatmo24.SharedKernel.Messages;
using MyNetatmo24.SharedKernel.StronglyTypedIds;
using NSubstitute;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application;

public class EnsureAccountTests
{
    private const string Auth0Id = "auth0|123";

    private static readonly UserInfoDto s_auth0UserInfo = new(
        "johnny",
        new Uri("https://example.com/avatar.png"),
        "John",
        "Doe");

    private static IDbContextOutbox<AccountDbContext> OutboxFor(AccountDbContext context)
    {
        var outbox = Substitute.For<IDbContextOutbox<AccountDbContext>>();
        outbox.DbContext.Returns(context);
        return outbox;
    }

    private static Account SeededAccount()
    {
        return Account.Create(AccountId.New(), Auth0Id, "johnny", FullName.From("John", "Doe"));
    }

private static EnsureAccountArgs Arrange(AccountDbContext context)
{
    var outbox = OutboxFor(context);
    var userInfoService = Substitute.For<IUserInfoService>();
    return new EnsureAccountArgs(outbox, context.Set<Account>().AsNoTracking(), userInfoService);
}

private sealed record EnsureAccountArgs(
    IDbContextOutbox<AccountDbContext> Outbox,
    IQueryable<Account> Accounts,
    IUserInfoService UserInfoService);

    [Test]
    public async Task ExecuteAsync_WhenAccountAlreadyExists_ReturnsNoContentWithoutCreating()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        db.Context.Add(SeededAccount());
        await db.Context.SaveChangesAsync();
        var args = Arrange(db.Context);

        var endpoint = Factory.Create<EnsureAccount.Endpoint>(
            ctx => TestClaims.Authenticated(ctx, Auth0Id),
            args.Outbox, args.Accounts, args.UserInfoService);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Result is NoContent).IsTrue();
        await args.UserInfoService.DidNotReceive().GetUserInfoAsync(Arg.Any<CancellationToken>());
        await args.Outbox.DidNotReceive().PublishAsync(Arg.Any<AccountCreated>());
    }

    [Test]
    public async Task ExecuteAsync_WhenAccountIsNew_CreatesAccountAndPublishesEvent()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        var args = Arrange(db.Context);
        args.UserInfoService.GetUserInfoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(s_auth0UserInfo)));

        var endpoint = Factory.Create<EnsureAccount.Endpoint>(
            ctx => TestClaims.Authenticated(ctx, Auth0Id),
            args.Outbox, args.Accounts, args.UserInfoService);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Result is NoContent).IsTrue();
        await args.Outbox.Received(1).PublishAsync(
            Arg.Is<AccountCreated>(m => m.FirstName == "John" && m.LastName == "Doe"));
        await args.Outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        var args = Arrange(db.Context);

        var endpoint = Factory.Create<EnsureAccount.Endpoint>(
            TestClaims.Anonymous,
            args.Outbox, args.Accounts, args.UserInfoService);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Result is UnauthorizedHttpResult).IsTrue();
        await args.UserInfoService.DidNotReceive().GetUserInfoAsync(Arg.Any<CancellationToken>());
        await args.Outbox.DidNotReceive().PublishAsync(Arg.Any<AccountCreated>());
    }

    [Test]
    public async Task ExecuteAsync_WhenAuth0UserInfoUnavailable_ReturnsNotFound()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        var args = Arrange(db.Context);
        args.UserInfoService.GetUserInfoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<UserInfoDto>(Errors.UserInfoNotFound)));

        var endpoint = Factory.Create<EnsureAccount.Endpoint>(
            ctx => TestClaims.Authenticated(ctx, Auth0Id),
            args.Outbox, args.Accounts, args.UserInfoService);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Result is NotFound).IsTrue();
        await args.Outbox.DidNotReceive().PublishAsync(Arg.Any<AccountCreated>());
    }

    [Test]
    public async Task ExecuteAsync_WhenAccountIsSoftDeleted_ReturnsConflictWithDeletedAt()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        var deletedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var account = SeededAccount();
        account.DeletedAt = deletedAt;
        db.Context.Add(account);
        await db.Context.SaveChangesAsync();
        var args = Arrange(db.Context);

        var endpoint = Factory.Create<EnsureAccount.Endpoint>(
            ctx => TestClaims.Authenticated(ctx, Auth0Id),
            args.Outbox, args.Accounts, args.UserInfoService);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Result is Conflict<EnsureAccount.UserDeletedDto>).IsTrue();
        var conflict = (Conflict<EnsureAccount.UserDeletedDto>)response.Result;
        await Assert.That(conflict.Value!.DeletedAt).IsEqualTo(deletedAt);
    }
}
