using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;

internal static class TestClaims
{
    /// <summary>An <see cref="IHttpContextAccessor"/> whose user is authenticated with the name <paramref name="auth0Id"/>.</summary>
    public static IHttpContextAccessor Authenticated(string auth0Id) =>
        Accessor(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, auth0Id)], "Test")));

    /// <summary>An <see cref="IHttpContextAccessor"/> whose user is anonymous (unauthenticated).</summary>
    public static IHttpContextAccessor Anonymous() =>
        Accessor(new ClaimsPrincipal(new ClaimsIdentity()));

    private static HttpContextAccessor Accessor(ClaimsPrincipal user) =>
        new()
        { HttpContext = new DefaultHttpContext { User = user } };
}
