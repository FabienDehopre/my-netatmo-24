using System.Text;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// The profile and credentials a prospective user submits during Registration, as the
/// <see cref="IUserRegistrationService"/> seam consumes them.
/// </summary>
/// <param name="Email">
/// The e-mail address the identity is created for. It is also the login name.
/// </param>
/// <param name="Password">
/// The password chosen by the prospective user. It is only forwarded to the identity provider,
/// never stored or otherwise processed by this application.
/// </param>
/// <param name="Nickname">
/// The display name chosen by the prospective user. Not unique.
/// </param>
/// <param name="GivenName">
/// The given name of the prospective user.
/// </param>
/// <param name="FamilyName">
/// The family name of the prospective user.
/// </param>
/// <param name="AvatarUrl">
/// The optional URL of the avatar picture of the prospective user.
/// </param>
public sealed record RegistrationData(
    string Email,
    string Password,
    string Nickname,
    string GivenName,
    string FamilyName,
    Uri? AvatarUrl)
{
    /// <summary>
    /// Suppresses the compiler-generated member printing, which would put the plaintext password
    /// into every log line, exception dump or debugger view this record reaches.
    /// </summary>
    /// <param name="_">The builder the record would have printed its members into.</param>
    /// <returns>Always <see langword="false"/>, so nothing is printed between the braces.</returns>
    private bool PrintMembers(StringBuilder _) => false;
}
