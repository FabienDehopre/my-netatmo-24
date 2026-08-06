using FluentResults;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

/// <summary>
/// Replaces the identity-provider-backed <see cref="IRegistrationService"/> so tests never make
/// outbound HTTP calls. Each test owns its instance, can switch the canned result before invoking the
/// endpoint, and can inspect what the seam was asked to do.
/// </summary>
public sealed class FakeRegistrationService : IRegistrationService
{
    private Result _result = Result.Ok();

    /// <summary>
    /// The registration data of the last call, or <see langword="null"/> when the seam was never reached.
    /// </summary>
    public RegistrationRequest? LastRequest { get; private set; }

    public int CallCount { get; private set; }

    public void SetResult(Result result) => _result = result;

    public Task<Result> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        CallCount++;
        return Task.FromResult(_result);
    }
}
