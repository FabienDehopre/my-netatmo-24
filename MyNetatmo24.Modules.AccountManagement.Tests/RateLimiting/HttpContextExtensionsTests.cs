using System.Net;
using Microsoft.AspNetCore.Http;
using MyNetatmo24.Modules.AccountManagement.RateLimiting;

namespace MyNetatmo24.Modules.AccountManagement.Tests.RateLimiting;

public class HttpContextExtensionsTests
{
    private static DefaultHttpContext ContextWith(string? forwardedFor = null, string? remoteIp = "192.0.2.10")
    {
        var context = new DefaultHttpContext();
        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        context.Connection.RemoteIpAddress = remoteIp is null ? null : IPAddress.Parse(remoteIp);
        return context;
    }

    [Test]
    public async Task ForwardedClientIp_WithoutForwardedHeader_UsesTheAddressOfTheConnection()
    {
        var context = ContextWith(remoteIp: "192.0.2.10");

        await Assert.That(context.ForwardedClientIp).IsEqualTo("192.0.2.10");
    }

    [Test]
    public async Task ForwardedClientIp_WithASingleForwardedAddress_UsesIt()
    {
        var context = ContextWith("203.0.113.7");

        await Assert.That(context.ForwardedClientIp).IsEqualTo("203.0.113.7");
    }

    [Test]
    public async Task ForwardedClientIp_WithSeveralForwardedAddresses_UsesTheLastOne()
    {
        // Everything left of the last entry was already in the header when the proxy appended what
        // it actually saw, so only the last entry is trustworthy.
        var context = ContextWith("198.51.100.1, 203.0.113.7");

        await Assert.That(context.ForwardedClientIp).IsEqualTo("203.0.113.7");
    }

    [Test]
    public async Task ForwardedClientIp_WithSeveralForwardedHeaders_UsesTheLastAddressOfTheLastHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = new[] { "198.51.100.1", "192.0.2.4, 203.0.113.7" };
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");

        await Assert.That(context.ForwardedClientIp).IsEqualTo("203.0.113.7");
    }

    [Test]
    [Arguments("  203.0.113.7  ")]
    [Arguments("203.0.113.7:41234")]
    public async Task ForwardedClientIp_WithASurroundedOrPortedForwardedAddress_UsesTheAddressAlone(string forwardedFor)
    {
        var context = ContextWith(forwardedFor);

        await Assert.That(context.ForwardedClientIp).IsEqualTo("203.0.113.7");
    }

    [Test]
    [Arguments("2001:db8::7")]
    [Arguments("[2001:db8::7]:41234")]
    public async Task ForwardedClientIp_WithAForwardedIPv6Address_UsesTheAddressAlone(string forwardedFor)
    {
        var context = ContextWith(forwardedFor);

        await Assert.That(context.ForwardedClientIp).IsEqualTo("2001:db8::7");
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("not-an-address")]
    [Arguments("198.51.100.1, ")]
    public async Task ForwardedClientIp_WithAnUnusableForwardedAddress_FallsBackToTheConnection(string forwardedFor)
    {
        // Falling back to the connection lumps every such request into the single partition of the
        // proxy rather than handing out a fresh budget per made-up value.
        var context = ContextWith(forwardedFor, remoteIp: "192.0.2.10");

        await Assert.That(context.ForwardedClientIp).IsEqualTo("192.0.2.10");
    }

    [Test]
    public async Task ForwardedClientIp_WithAnIPv4MappedConnectionAddress_UsesItsIPv4Form()
    {
        // Kestrel reports IPv4 peers as ::ffff:192.0.2.10 on a dual-stack socket; the two forms
        // must not be two budgets for the same client.
        var context = ContextWith(remoteIp: "::ffff:192.0.2.10");

        await Assert.That(context.ForwardedClientIp).IsEqualTo("192.0.2.10");
    }

    [Test]
    public async Task ForwardedClientIp_WithAnIPv4MappedForwardedAddress_UsesItsIPv4Form()
    {
        var context = ContextWith("::ffff:203.0.113.7");

        await Assert.That(context.ForwardedClientIp).IsEqualTo("203.0.113.7");
    }

    [Test]
    public async Task ForwardedClientIp_WithoutAnyKnownAddress_UsesTheSharedUnknownPartition()
    {
        var context = ContextWith(remoteIp: null);

        await Assert.That(context.ForwardedClientIp).IsEqualTo("unknown");
    }

    [Test]
    public async Task ForwardedClientIp_WithNullContext_Throws()
    {
        await Assert.That(() => ((HttpContext)null!).ForwardedClientIp).Throws<ArgumentNullException>();
    }
}
