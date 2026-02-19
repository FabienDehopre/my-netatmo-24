using FluentResults;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

public interface IUserInfoService
{
    Task<Result<UserInfoDto>> GetUserInfoAsync(CancellationToken cancellationToken);
}
