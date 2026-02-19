using MyNetatmo24.Modules.AccountManagement.Auth0.Dtos;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.Auth0;

public interface IUserInfoService
{
    Task<Result<UserInfo>> GetUserInfoAsync(CancellationToken cancellationToken);
}
