using Microsoft.Kiota.Abstractions;
using MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests;

public class RestoreAccountTests : AccountApiIntegrationTest
{
    [Test]
    public async Task RestoreAccount_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var apiClient = CreateAnonymousApiClient();

        var exception = await Assert.That(() => apiClient.Account.Restore.PostAsync()).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task RestoreAccount_WhenAccountDoesNotExist_ReturnsNotFound()
    {
        var apiClient = CreateAuthenticatedApiClient();

        var exception = await Assert.That(() => apiClient.Account.Restore.PostAsync()).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task RestoreAccount_WhenAccountIsSoftDeleted_ClearsDeletionDate()
    {
        await SeedAccountAsync(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
        var apiClient = CreateAuthenticatedApiClient();

        await apiClient.Account.Restore.PostAsync();

        var account = await FindAccountAsync();
        await Assert.That(account!.DeletedAt).IsNull();
    }

    [Test]
    public async Task RestoreAccount_WhenAccountIsNotDeleted_IsIdempotent()
    {
        var seeded = await SeedAccountAsync();
        var apiClient = CreateAuthenticatedApiClient();

        await apiClient.Account.Restore.PostAsync();

        var account = await FindAccountAsync();
        await Assert.That(account!.Id).IsEqualTo(seeded.Id);
        await Assert.That(account.DeletedAt).IsNull();
    }
}
