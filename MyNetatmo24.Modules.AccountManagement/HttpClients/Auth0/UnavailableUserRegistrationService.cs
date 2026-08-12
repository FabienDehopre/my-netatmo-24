using FluentResults;
using Microsoft.Extensions.Logging;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// Implementation of <see cref="IUserRegistrationService"/> for a deployed host that holds no
/// credentials for the identity provider, and can therefore create no identity at all.
/// </summary>
/// <remarks>
/// It exists so that missing credentials fail loudly rather than quietly: answering the prospective
/// user that registration is unavailable is true, whereas the success
/// <see cref="StubUserRegistrationService"/> answers would send them off to wait for a verification
/// e-mail that no one is ever going to send. The stub keeps that liberty on a developer machine,
/// where nobody is waiting for an e-mail.
/// </remarks>
public sealed class UnavailableUserRegistrationService(ILogger<UnavailableUserRegistrationService> logger)
    : IUserRegistrationService
{
    private readonly ILogger _logger = logger.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Result> RegisterAsync(RegistrationData registration, CancellationToken cancellationToken)
    {
        _logger.LogUserRegistrationNotConfigured();
        return Task.FromResult(Result.Fail(Errors.IdentityProviderUnavailable));
    }
}
