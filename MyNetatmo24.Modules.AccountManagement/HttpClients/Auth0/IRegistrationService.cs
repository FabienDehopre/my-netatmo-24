using FluentResults;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// Creates the identity of a prospective user. Everything the identity provider needs - credentials,
/// token acquisition, error shapes - lives behind this seam so the module can be tested without
/// any external call.
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Creates the identity described by <paramref name="request"/> and triggers e-mail verification.
    /// </summary>
    /// <param name="request">The registration data of the prospective user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A successful result when the identity was created; a failed result carrying the reason otherwise.</returns>
    Task<Result> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken);
}
