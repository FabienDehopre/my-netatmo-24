using FluentResults;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

/// <summary>
/// Replaces the Auth0-backed <see cref="IUserInfoService"/> so tests never make outbound HTTP calls.
/// Each test owns its instance and can switch the canned result before invoking an endpoint.
/// </summary>
public sealed class FakeUserInfoService : IUserInfoService
{
    public static readonly UserInfoDto DefaultUserInfo = new(
        "johnny",
        new Uri("https://example.com/avatar.png"),
        "John",
        "Doe");

    private Result<UserInfoDto> _result = Result.Ok(DefaultUserInfo);

    public void SetUserInfo(UserInfoDto userInfo) => _result = Result.Ok(userInfo);

    public void SetUnavailable() =>
        _result = new EndpointError(StatusCodes.Status404NotFound, "Failed to retrieve user info from Auth0.");

    public Task<Result<UserInfoDto>> GetUserInfoAsync(CancellationToken cancellationToken) => Task.FromResult(_result);
}
