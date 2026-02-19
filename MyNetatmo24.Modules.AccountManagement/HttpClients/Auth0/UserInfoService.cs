using System.Net.Http.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

public class UserInfoService(HttpClient httpClient, ILogger<UserInfoService> logger) : IUserInfoService
{
    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();
    private readonly ILogger _logger = logger.ThrowIfNull();

    public async Task<Result<UserInfoDto>> GetUserInfoAsync(CancellationToken cancellationToken)
    {
        var userInfo = await _httpClient.GetFromJsonAsync<UserInfoDto>("userinfo", cancellationToken);
        if (userInfo is null)
        {
            _logger.LogCannotRetrieveUserInfo();
            return Errors.UserInfoNotFound;
        }

        return userInfo;
    }
}
