using FluentResults;
using Microsoft.Extensions.Logging;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// Placeholder implementation of <see cref="IUserRegistrationService"/> that accepts every
/// registration without creating anything.
/// </summary>
/// <remarks>
/// It exists so the Registration endpoint can be wired end to end before the Auth0 Management API
/// adapter lands, and logs a warning on every call so a deployment that still runs it is visible.
/// </remarks>
public sealed class StubUserRegistrationService(ILogger<StubUserRegistrationService> logger) : IUserRegistrationService
{
    private readonly ILogger _logger = logger.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Result> RegisterAsync(RegistrationData registration, CancellationToken cancellationToken)
    {
        _logger.LogStubbedUserRegistration();
        return Task.FromResult(Result.Ok());
    }
}
