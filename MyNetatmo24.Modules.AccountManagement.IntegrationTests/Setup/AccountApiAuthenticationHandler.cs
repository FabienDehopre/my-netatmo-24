using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

public class AccountApiAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string Auth0IdHeaderName = "X-Test-Auth0Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var auth0Id = Request.Headers[Auth0IdHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(auth0Id))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, auth0Id),
            new Claim(ClaimTypes.NameIdentifier, auth0Id),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
