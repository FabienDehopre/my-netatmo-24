using System.Text.Json.Serialization;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

public record UserInfoDto(
    string Nickname,
    Uri? Picture,
    [property: JsonPropertyName("auth0.given_name")] string GivenName,
    [property: JsonPropertyName("auth0.family_name")] string FamilyName);
