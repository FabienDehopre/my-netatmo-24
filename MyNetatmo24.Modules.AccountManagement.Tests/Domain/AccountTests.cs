using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Domain;
using MyNetatmo24.SharedKernel.StronglyTypedIds;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Domain;

public class AccountTests
{
    [Test]
    public async Task Create_SetsAllProvidedProperties()
    {
        var id = AccountId.New();
        var name = FullName.From("John", "Doe");

        var account = Account.Create(id, "auth0|123", "johnny", name);

        await Assert.That(account.Id).IsEqualTo(id);
        await Assert.That(account.Auth0Id).IsEqualTo("auth0|123");
        await Assert.That(account.NickName).IsEqualTo("johnny");
        await Assert.That(account.Name).IsEqualTo(name);
    }

    [Test]
    public async Task Create_LeavesOptionalPropertiesUnset()
    {
        var account = Account.Create(AccountId.New(), "auth0|123", "johnny", FullName.From("John", "Doe"));

        await Assert.That(account.AvatarUrl).IsNull();
        await Assert.That(account.NetatmoAuthInfo).IsNull();
        await Assert.That(account.DeletedAt).IsNull();
    }

    [Test]
    public async Task SetAvatarUrl_UpdatesAvatarUrl()
    {
        var account = Account.Create(AccountId.New(), "auth0|123", "johnny", FullName.From("John", "Doe"));
        var avatar = new Uri("https://example.com/avatar.png");

        account.SetAvatarUrl(avatar);

        await Assert.That(account.AvatarUrl).IsEqualTo(avatar);
    }

    [Test]
    public async Task SetNetatmoAuthInfo_UpdatesNetatmoAuthInfo()
    {
        var account = Account.Create(AccountId.New(), "auth0|123", "johnny", FullName.From("John", "Doe"));
        var authInfo = NetatmoAuthInfo.From("access", "refresh", 3600);

        account.SetNetatmoAuthInfo(authInfo);

        await Assert.That(account.NetatmoAuthInfo).IsEqualTo(authInfo);
    }
}
