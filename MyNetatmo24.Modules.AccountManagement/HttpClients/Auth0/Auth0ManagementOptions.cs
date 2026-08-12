namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// The settings the Registration endpoint needs to reach the Management API of the identity
/// provider on its own behalf, as opposed to on behalf of a signed-in user.
/// </summary>
/// <remarks>
/// The credentials belong to a dedicated machine-to-machine application authorized for the
/// Management API with the <c>create:users</c> scope, and reach the process as
/// <c>Auth0__Management__ClientId</c> and <c>Auth0__Management__ClientSecret</c>. They are absent
/// on a host that has none provisioned, which is what <see cref="StubUserRegistrationService"/>
/// exists for.
/// </remarks>
public sealed class Auth0ManagementOptions
{
    /// <summary>
    /// The configuration section these settings are bound from.
    /// </summary>
    public const string SectionName = "Auth0:Management";

    /// <summary>
    /// The database connection every identity is created in when nothing else is configured. It is
    /// the name Auth0 gives the database connection of a tenant it creates.
    /// </summary>
    public const string DefaultDatabaseConnectionName = "Username-Password-Authentication";

    /// <summary>
    /// Gets or sets the canonical domain of the Auth0 tenant, without a scheme, or
    /// <see langword="null"/> when the Management API is not reachable from this host.
    /// </summary>
    /// <remarks>
    /// This is deliberately not the custom login domain the rest of the application talks to: the
    /// Management API is a resource server identified by the canonical tenant domain, and that is
    /// the audience the machine-to-machine grant is authorized for.
    /// </remarks>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the client id of the machine-to-machine application, or <see langword="null"/>
    /// when no such application is provisioned for this host.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret of the machine-to-machine application, or
    /// <see langword="null"/> when no such application is provisioned for this host.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the name of the Auth0 database connection the identities are created in.
    /// </summary>
    public string DatabaseConnectionName { get; set; } = DefaultDatabaseConnectionName;

    /// <summary>
    /// Gets a value indicating whether this host knows a tenant and holds machine-to-machine
    /// credentials for it, and can therefore create identities for real.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Domain) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
