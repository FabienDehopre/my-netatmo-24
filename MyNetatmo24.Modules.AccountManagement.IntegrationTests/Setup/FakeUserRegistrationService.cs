using FluentResults;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

/// <summary>
/// Replaces the identity-provider-backed <see cref="IUserRegistrationService"/> so tests never make
/// outbound HTTP calls, and records what the endpoint asked it to register.
/// Each test owns its instance.
/// </summary>
public sealed class FakeUserRegistrationService : IUserRegistrationService
{
    private readonly List<RegistrationData> _registrations = [];

    /// <summary>
    /// The registrations the endpoint forwarded to this service, in call order.
    /// </summary>
    public IReadOnlyList<RegistrationData> Registrations => _registrations;

    /// <summary>
    /// The single registration the endpoint forwarded, or <c>null</c> when it forwarded none.
    /// </summary>
    public RegistrationData? LastRegistration => _registrations.Count > 0 ? _registrations[^1] : null;

    public Task<Result> RegisterAsync(RegistrationData registration, CancellationToken cancellationToken)
    {
        _registrations.Add(registration);
        return Task.FromResult(Result.Ok());
    }
}
