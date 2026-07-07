using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;
using MyNetatmo24.SharedKernel.StronglyTypedIds;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application;

public class MyAccountTests
{
    private const string Auth0Id = "auth0|123";

    private static Account SeededAccount(string auth0Id = Auth0Id)
    {
        var account = Account.Create(AccountId.New(), auth0Id, "johnny", FullName.From("John", "Doe"));
        account.SetAvatarUrl(new Uri("https://example.com/avatar.png"));
        return account;
    }

    [Test]
    public async Task InvokeAsync_WhenAccountExists_ReturnsOkWithUserInfo()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        db.Context.Add(SeededAccount());
        await db.Context.SaveChangesAsync();

        var endpoint = new MyAccount(
            TestClaims.Authenticated(Auth0Id),
            db.Context.Set<Account>().AsNoTracking(),
            new PassThroughHybridCache());

        var response = await endpoint.HandleAsync(CancellationToken.None);

        await Assert.That(response.Result is Ok<MyAccount.UserInfoDto>).IsTrue();
        var ok = (Ok<MyAccount.UserInfoDto>)response.Result;
        await Assert.That(ok.Value!.Nickname).IsEqualTo("johnny");
        await Assert.That(ok.Value.FirstName).IsEqualTo("John");
        await Assert.That(ok.Value.LastName).IsEqualTo("Doe");
    }

    [Test]
    public async Task InvokeAsync_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        await using var db = await TestAccountDbContext.CreateAsync();

        var endpoint = new MyAccount(
            TestClaims.Anonymous(),
            db.Context.Set<Account>().AsNoTracking(),
            new PassThroughHybridCache());

        var response = await endpoint.HandleAsync(CancellationToken.None);

        await Assert.That(response.Result is UnauthorizedHttpResult).IsTrue();
    }

    [Test]
    public async Task InvokeAsync_WhenAccountMissing_ReturnsNotFound()
    {
        await using var db = await TestAccountDbContext.CreateAsync();

        var endpoint = new MyAccount(
            TestClaims.Authenticated(Auth0Id),
            db.Context.Set<Account>().AsNoTracking(),
            new PassThroughHybridCache());

        var response = await endpoint.HandleAsync(CancellationToken.None);

        await Assert.That(response.Result is NotFound).IsTrue();
    }

    [Test]
    public async Task InvokeAsync_WhenAccountSoftDeleted_ReturnsNotFound()
    {
        await using var db = await TestAccountDbContext.CreateAsync();
        var account = SeededAccount();
        account.DeletedAt = DateTimeOffset.UtcNow;
        db.Context.Add(account);
        await db.Context.SaveChangesAsync();

        var endpoint = new MyAccount(
            TestClaims.Authenticated(Auth0Id),
            db.Context.Set<Account>().AsNoTracking(),
            new PassThroughHybridCache());

        var response = await endpoint.HandleAsync(CancellationToken.None);

        await Assert.That(response.Result is NotFound).IsTrue();
    }
}
