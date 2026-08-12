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
    private Result _outcome = Result.Ok();

    /// <summary>
    /// The registrations the endpoint forwarded to this service, in call order.
    /// </summary>
    public IReadOnlyList<RegistrationData> Registrations => _registrations;

    /// <summary>
    /// The single registration the endpoint forwarded, or <c>null</c> when it forwarded none.
    /// </summary>
    public RegistrationData? LastRegistration => _registrations.Count > 0 ? _registrations[^1] : null;

    /// <summary>
    /// Makes every following registration fail because the e-mail address already identifies someone.
    /// </summary>
    public void RejectEmailAsAlreadyRegistered() => _outcome = Result.Fail(Errors.EmailAlreadyRegistered);

    /// <summary>
    /// Makes every following registration fail because the password policy refused the password.
    /// </summary>
    /// <param name="policyMessage">The policy message the identity provider would have worded.</param>
    public void RejectPasswordAsTooWeak(string policyMessage) => _outcome = Result.Fail(Errors.PasswordTooWeak(policyMessage));

    /// <summary>
    /// Makes every following registration fail because the identity provider cannot be reached.
    /// </summary>
    public void ReportIdentityProviderAsUnavailable() => _outcome = Result.Fail(Errors.IdentityProviderUnavailable);

    /// <summary>
    /// Makes every following registration fail with an outcome the endpoint has no mapping for.
    /// </summary>
    public void FailUnexpectedly() => _outcome = Result.Fail("boom");

    public Task<Result> RegisterAsync(RegistrationData registration, CancellationToken cancellationToken)
    {
        _registrations.Add(registration);
        return Task.FromResult(_outcome);
    }
}
