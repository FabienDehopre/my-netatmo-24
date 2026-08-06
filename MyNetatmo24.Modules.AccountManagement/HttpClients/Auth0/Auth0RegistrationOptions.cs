namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// What the module needs to create identities in the Auth0 tenant. The credentials belong to a
/// dedicated machine-to-machine application authorized for the Management API with the single
/// <c>create:users</c> scope - it can create identities and nothing else.
/// </summary>
public sealed class Auth0RegistrationOptions
{
    /// <summary>
    /// The configuration section these options are bound to.
    /// </summary>
    public const string SectionName = "Auth0";

    /// <summary>
    /// The Auth0 tenant domain, for example <c>auth.example.com</c>.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// The client id of the machine-to-machine application.
    /// </summary>
    public string ManagementClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret of the machine-to-machine application.
    /// </summary>
    public string ManagementClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The database connection new identities are created in.
    /// </summary>
    public string DatabaseConnectionName { get; set; } = "Username-Password-Authentication";

    /// <summary>
    /// Whether everything needed to authenticate against the Management API is present: the tenant to
    /// call and both halves of the machine-to-machine credentials.
    /// </summary>
    public bool HasManagementCredentials =>
        !string.IsNullOrWhiteSpace(Domain) &&
        !string.IsNullOrWhiteSpace(ManagementClientId) &&
        !string.IsNullOrWhiteSpace(ManagementClientSecret);
}
