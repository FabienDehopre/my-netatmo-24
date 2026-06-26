using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MyNetatmo24.Gateway.TestModule;

internal static class TestModule
{
    extension(IEndpointRouteBuilder builder)
    {
        internal IEndpointRouteBuilder MapTestAuthEndpoints()
        {
            builder.MapGet("test-login", async (
                TestLoginRequest request,
                IHttpContextAccessor httpContextAccessor,
                IConfiguration configuration) =>
            {
                var domain = configuration["Auth0:Domain"];
                var clientId = configuration["Auth0:ClientId"];
                var clientSecret = configuration["Auth0:ClientSecret"];
                var audience = configuration["Auth0:Audience"];

                ArgumentException.ThrowIfNullOrWhiteSpace(domain);
                ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
                ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
                ArgumentException.ThrowIfNullOrWhiteSpace(audience);

                using var httpClient = new HttpClient();
                var tokenResponse = await httpClient.PostAsync(
                    new Uri($"https://{domain}/oauth/token"),
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "password",
                        ["username"] = request.Username,
                        ["password"] = request.Password,
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["audience"] = audience,
                        ["scope"] = "openid"
                    }));

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    return Results.Unauthorized();
                }

                var tokens = await tokenResponse.Content.ReadFromJsonAsync<Auth0TokenResponse>();
                if (tokens is null)
                {
                    return Results.Unauthorized();
                }

                // Parse id_token for claims (same as OIDC middleware does)
                var handler = new JsonWebTokenHandler();
                var jwt = handler.ReadJsonWebToken(tokens.IdToken);

                // Optionally call userinfo to get full claims (matches GetClaimsFromUserInfoEndpoint = true)
                var userInfoResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                    $"https://{domain}/userinfo")
                {
                    Headers =
                    {
                        Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken)
                    }
                });
                var userInfoClaims = userInfoResponse.IsSuccessStatusCode
                    ? ParseUserInfoClaims(await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>())
                    : [];

                var claims = jwt.Claims
                    .Concat(userInfoClaims)
                    .DistinctBy(c => (c.Type, c.Value))
                    .ToList();

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn);
                var authProperties = new AuthenticationProperties();
                authProperties.StoreTokens([
                    new() { Name = "access_token", Value = tokens.AccessToken },
                    new() { Name = "id_token", Value = tokens.IdToken },
                    new() { Name = "refresh_token", Value = tokens.RefreshToken },
                    new() { Name = "token_type", Value = tokens.TokenType },
                    new() { Name = "expires_at", Value = expiresAt.ToString("o") },
                ]);

                Debug.Assert(httpContextAccessor.HttpContext != null, "httpContextAccessor.HttpContext != null");
                await httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
                return Results.Ok();
            });

            return builder;
        }
    }

    private static Claim[] ParseUserInfoClaims(JsonElement jsonElement)
    {
        var claims = new List<Claim>();
        foreach (var property in jsonElement.EnumerateObject())
        {
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.String:
                    claims.Add(new Claim(property.Name, property.Value.GetString()!));
                    break;
                case JsonValueKind.True:
                    claims.Add(new Claim(property.Name, "true"));
                    break;
                case JsonValueKind.False:
                    claims.Add(new Claim(property.Name, "false"));
                    break;
                case JsonValueKind.Number:
                    claims.Add(new Claim(property.Name, property.Value.GetRawText()));
                    break;
                case JsonValueKind.Array:
                    foreach (var item in property.Value.EnumerateArray())
                        claims.Add(new Claim(property.Name, item.ValueKind == JsonValueKind.String
                            ? item.GetString()!
                            : item.GetRawText()));
                    break;
                case JsonValueKind.Object:
                    claims.Add(new Claim(property.Name, property.Value.GetRawText()));
                    break;
            }
        }
        return [.. claims];
    }
}
