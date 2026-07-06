using FluentResults;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Data;

public class AccountCacheTests
{
    private static readonly MyAccount.UserInfoDto s_userInfo = new("johnny", "John", "Doe", null);

    [Test]
    public async Task GetOrCreateAccountAsync_WhenFactorySucceeds_ReturnsCachedResult()
    {
        var cache = HybridCacheFactory.Create();

        var result = await cache.GetOrCreateAccountAsync(
            "auth0|success",
            _ => Task.FromResult(Result.Ok(s_userInfo)),
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task GetOrCreateAccountAsync_WhenFactoryFails_ReturnsNull()
    {
        var cache = HybridCacheFactory.Create();

        var result = await cache.GetOrCreateAccountAsync(
            "auth0|failure",
            _ => Task.FromResult(Result.Fail<MyAccount.UserInfoDto>("not found")),
            CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOrCreateAccountAsync_CachesSuccessfulResult_FactoryInvokedOnce()
    {
        var cache = HybridCacheFactory.Create();
        var invocations = 0;

        Task<Result<MyAccount.UserInfoDto>> Factory(CancellationToken _)
        {
            invocations++;
            return Task.FromResult(Result.Ok(s_userInfo));
        }

        await cache.GetOrCreateAccountAsync("auth0|cached", Factory, CancellationToken.None);
        await cache.GetOrCreateAccountAsync("auth0|cached", Factory, CancellationToken.None);

        await Assert.That(invocations).IsEqualTo(1);
    }

    [Test]
    public async Task GetOrCreateAccountAsync_WithNullCache_Throws()
    {
        await Assert.That(() =>
                ((Microsoft.Extensions.Caching.Hybrid.HybridCache)null!).GetOrCreateAccountAsync(
                    "auth0|x",
                    _ => Task.FromResult(Result.Ok(s_userInfo)),
                    CancellationToken.None))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetOrCreateAccountAsync_WithNullFactory_Throws()
    {
        var cache = HybridCacheFactory.Create();

        await Assert.That(() => cache.GetOrCreateAccountAsync("auth0|x", null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
    }
}
