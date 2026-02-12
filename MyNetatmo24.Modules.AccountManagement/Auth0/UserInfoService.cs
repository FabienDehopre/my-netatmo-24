using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MyNetatmo24.Modules.AccountManagement.Auth0.Dtos;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Logging;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.Auth0;

public class UserInfoService(HttpClient httpClient, ILogger<UserInfoService> logger) : IUserInfoService
{
    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();
    private readonly ILogger _logger = logger.ThrowIfNull();

    public async Task<Result<UserInfo>> GetUserInfoAsync(CancellationToken cancellationToken)
    {

        var userInfo = await _httpClient.GetFromJsonAsync<UserInfo>("userinfo", cancellationToken);
        if (userInfo is null)
        {
            _logger.LogCannotRetrieveUserInfo();
            return Error.NotFound("UserInfo.NotFound", "Failed to retrieve user info from Auth0.");
        }

        return userInfo;
    }
}
