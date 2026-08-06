namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// The registration data of a prospective user, expressed at domain level: no identity-provider
/// concepts leak through this seam.
/// </summary>
/// <param name="Email">The e-mail address the identity is created with.</param>
/// <param name="Password">The password, forwarded to the identity provider and never stored locally.</param>
/// <param name="Nickname">The display name the person picked.</param>
/// <param name="GivenName">The person's given name.</param>
/// <param name="FamilyName">The person's family name.</param>
/// <param name="AvatarUrl">An optional URL pointing at the person's avatar image.</param>
public sealed record RegistrationRequest(
    string Email,
    string Password,
    string Nickname,
    string GivenName,
    string FamilyName,
    Uri? AvatarUrl);
