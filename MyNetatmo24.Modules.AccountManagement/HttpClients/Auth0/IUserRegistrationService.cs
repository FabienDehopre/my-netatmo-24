using FluentResults;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// Creates the identity of a prospective user with the identity provider.
/// </summary>
/// <remarks>
/// Everything the identity provider needs — its SDK, its credentials and its error shapes — stays
/// below this seam so the Registration endpoint can be exercised without any outbound call.
/// Registration deliberately creates no <see cref="Domain.Account"/>: the account keeps its single
/// creation path on the first authenticated call (see <c>docs/adr/0001-registration-creates-identity-only.md</c>).
/// </remarks>
public interface IUserRegistrationService
{
    /// <summary>
    /// Creates the identity described by <paramref name="registration"/>.
    /// </summary>
    /// <param name="registration">The profile and credentials of the prospective user.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A successful result when the identity was created; a failed result otherwise.</returns>
    Task<Result> RegisterAsync(RegistrationData registration, CancellationToken cancellationToken);
}
