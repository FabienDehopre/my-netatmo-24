using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;
using MyNetatmo24.SharedKernel.Messages;
using MyNetatmo24.SharedKernel.StronglyTypedIds;
using NSubstitute;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application;

public class RestoreAccountTests
{
    private const string Auth0Id = "auth0|123";

    private static IDbContextOutbox<AccountDbContext> OutboxFor(AccountDbContext context)
    {
        var outbox = Substitute.For<IDbContextOutbox<AccountDbContext>>();
        outbox.DbContext.Returns(context);
        return outbox;
    }

    [Test]
    public async Task ExecuteAsync_WhenAccountIsDeleted_RestoresAndPublishesEvent()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        var account = Account.Create(AccountId.New(), Auth0Id, "johnny", FullName.From("John", "Doe"));
        account.DeletedAt = DateTimeOffset.UtcNow;
        db.Context.Add(account);
        await db.Context.SaveChangesAsync();
        var outbox = OutboxFor(db.Context);

        var endpoint = Factory.Create<RestoreAccount.Endpoint>(
            ctx => TestClaims.Authenticated(ctx, Auth0Id),
            outbox);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Result is NoContent).IsTrue();
        await Assert.That(account.DeletedAt).IsNull();
        await outbox.Received(1).PublishAsync(Arg.Is<AccountRestored>(m => m.Id == account.Id));
        await outbox.Received(1).SaveChangesAndFlushMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        var outbox = OutboxFor(db.Context);

        var endpoint = Factory.Create<RestoreAccount.Endpoint>(TestClaims.Anonymous, outbox);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Result is UnauthorizedHttpResult).IsTrue();
        await outbox.DidNotReceive().PublishAsync(Arg.Any<AccountRestored>());
    }

    [Test]
    public async Task ExecuteAsync_WhenAccountMissing_ReturnsNotFound()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        var outbox = OutboxFor(db.Context);

        var endpoint = Factory.Create<RestoreAccount.Endpoint>(
            ctx => TestClaims.Authenticated(ctx, Auth0Id),
            outbox);

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        await Assert.That(response.Result is NotFound).IsTrue();
        await outbox.DidNotReceive().PublishAsync(Arg.Any<AccountRestored>());
    }
}
