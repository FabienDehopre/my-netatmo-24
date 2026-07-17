using Microsoft.Kiota.Abstractions;
using MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests;

public class MyAccountTests : AccountApiIntegrationTest
{
    [Test]
    public async Task MyAccount_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var apiClient = CreateAnonymousApiClient();

        var exception = await Assert.That(() => apiClient.Account.Me.GetAsync()).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task MyAccount_WhenAccountDoesNotExist_ReturnsNotFound()
    {
        var apiClient = CreateAuthenticatedApiClient();

        var exception = await Assert.That(() => apiClient.Account.Me.GetAsync()).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task MyAccount_WhenAccountExists_ReturnsUserInfo()
    {
        await SeedAccountAsync();
        var apiClient = CreateAuthenticatedApiClient();

        var userInfo = await apiClient.Account.Me.GetAsync();

        await Verify(userInfo);
    }

    [Test]
    public async Task MyAccount_WhenAccountIsSoftDeleted_ReturnsNotFound()
    {
        await SeedAccountAsync(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
        var apiClient = CreateAuthenticatedApiClient();

        var exception = await Assert.That(() => apiClient.Account.Me.GetAsync()).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task MyAccount_WhenCalledTwice_ServesSecondCallFromCache()
    {
        await SeedAccountAsync();
        var apiClient = CreateAuthenticatedApiClient();

        var first = await apiClient.Account.Me.GetAsync();

        // Soft-delete the account behind the cache's back; an uncached second call would return 404.
        await UpdateAccountAsync(account => account.DeletedAt = DateTimeOffset.UtcNow);

        var second = await apiClient.Account.Me.GetAsync();
        await Assert.That(second!.Nickname).IsEqualTo(first!.Nickname);
    }
}
