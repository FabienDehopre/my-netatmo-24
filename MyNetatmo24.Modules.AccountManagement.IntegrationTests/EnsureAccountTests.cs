using ApiServiceSDK.Models.Modules.AccountManagement.Application.EnsureAccount;
using Microsoft.Kiota.Abstractions;
using MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests;

public class EnsureAccountTests : AccountApiIntegrationTest
{
    [Test]
    public async Task EnsureAccount_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var apiClient = CreateAnonymousApiClient();

        var exception = await Assert.That(() => apiClient.Account.Ensure.PostAsync()).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task EnsureAccount_WhenUserHasNoAccount_CreatesAccountFromAuth0UserInfo()
    {
        var apiClient = CreateAuthenticatedApiClient();

        await apiClient.Account.Ensure.PostAsync();

        var account = await FindAccountAsync();
        await Assert.That(account).IsNotNull();
        await Assert.That(account!.Auth0Id).IsEqualTo(Auth0Id);
        await Assert.That(account.NickName).IsEqualTo(FakeUserInfoService.DefaultUserInfo.Nickname);
        await Assert.That(account.Name.FirstName).IsEqualTo(FakeUserInfoService.DefaultUserInfo.GivenName);
        await Assert.That(account.Name.LastName).IsEqualTo(FakeUserInfoService.DefaultUserInfo.FamilyName);
        await Assert.That(account.AvatarUrl).IsEqualTo(FakeUserInfoService.DefaultUserInfo.Picture);
        await Assert.That(account.DeletedAt).IsNull();
    }

    [Test]
    public async Task EnsureAccount_WhenAccountAlreadyExists_SucceedsWithoutCreatingDuplicate()
    {
        var seeded = await SeedAccountAsync();
        var apiClient = CreateAuthenticatedApiClient();

        await apiClient.Account.Ensure.PostAsync();

        // FindAccountAsync uses SingleOrDefaultAsync, so a duplicate row would make it throw.
        var account = await FindAccountAsync();
        await Assert.That(account!.Id).IsEqualTo(seeded.Id);
    }

    [Test]
    public async Task EnsureAccount_WhenUserInfoCannotBeRetrieved_ReturnsNotFound()
    {
        UserInfoService.SetUnavailable();
        var apiClient = CreateAuthenticatedApiClient();

        var exception = await Assert.That(() => apiClient.Account.Ensure.PostAsync()).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status404NotFound);
        await Assert.That(await FindAccountAsync()).IsNull();
    }

    [Test]
    public async Task EnsureAccount_WhenAccountIsSoftDeleted_ReturnsConflictWithDeletionDate()
    {
        var deletedAt = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        await SeedAccountAsync(deletedAt);
        var apiClient = CreateAuthenticatedApiClient();

        var exception = await Assert.That(() => apiClient.Account.Ensure.PostAsync()).Throws<UserDeletedDto>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(exception.DeletedAt).IsEqualTo(deletedAt);
    }
}
