using FluentResults;
using Microsoft.Extensions.Logging;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// Placeholder implementation of <see cref="IRegistrationService"/> that accepts every registration
/// without creating anything. It keeps the endpoint wired end to end until the Auth0 Management API
/// adapter replaces it.
/// </summary>
public sealed class StubRegistrationService(ILogger<StubRegistrationService> logger) : IRegistrationService
{
    private readonly ILogger _logger = logger.ThrowIfNull();

    public Task<Result> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Deliberately logs nothing about the submitted data: it carries credentials and personal data.
        _logger.LogRegistrationNotWiredToIdentityProvider();
        return Task.FromResult(Result.Ok());
    }
}
