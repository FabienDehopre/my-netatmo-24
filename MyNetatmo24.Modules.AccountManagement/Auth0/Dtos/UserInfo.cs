using System.Text.Json.Serialization;

namespace MyNetatmo24.Modules.AccountManagement.Auth0.Dtos;

public record UserInfo(
    string Nickname,
    Uri? Picture,
    [property: JsonPropertyName("auth0.given_name")] string GivenName,
    [property: JsonPropertyName("auth0.family_name")] string FamilyName);
