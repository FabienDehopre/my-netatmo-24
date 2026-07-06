using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;

internal static class TestClaims
{
    /// <summary>Configures the request context with an authenticated user whose name is <paramref name="auth0Id"/>.</summary>
    public static void Authenticated(DefaultHttpContext ctx, string auth0Id) =>
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, auth0Id)], "Test"));

    /// <summary>Configures the request context with an anonymous (unauthenticated) user.</summary>
    public static void Anonymous(DefaultHttpContext ctx) =>
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity());
}
