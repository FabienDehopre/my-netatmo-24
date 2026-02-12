using MyNetatmo24.SharedKernel.Domain;
using MyNetatmo24.SharedKernel.StronglyTypedIds;

namespace MyNetatmo24.Modules.AccountManagement.Domain;

public class Account : ISoftDelete
{
    public AccountId Id { get; init; }

    public string? Auth0Id { get; private set; }

    public string? NickName { get; private set; }

    public FullName Name { get; private set; }

    public Uri? AvatarUrl { get; private set; }

    public NetatmoAuthInfo? NetatmoAuthInfo { get; private set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public static Account Create(AccountId id, string auth0Id, string nickName, FullName fullName)
    {
        return new Account
        {
            Id = id,
            Auth0Id = auth0Id,
            NickName = nickName,
            Name = fullName,
        };
    }

    public void SetAvatarUrl(Uri avatarUrl)
    {
        AvatarUrl = avatarUrl;
    }

    public void SetNetatmoAuthInfo(NetatmoAuthInfo authInfo)
    {
        NetatmoAuthInfo = authInfo;
    }
}
