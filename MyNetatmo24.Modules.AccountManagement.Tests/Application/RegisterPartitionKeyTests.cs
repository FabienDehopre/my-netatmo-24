using System.Net;
using Microsoft.AspNetCore.Http;
using MyNetatmo24.Modules.AccountManagement.Application;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application;

public class RegisterPartitionKeyTests
{
    private static DefaultHttpContext ContextFor(string? forwardedFor, string? remoteIp)
    {
        var context = new DefaultHttpContext();
        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        if (remoteIp is not null)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        }

        return context;
    }

    [Test]
    public async Task GetClientPartitionKey_PrefersTheForwardedAddress()
    {
        var key = Register.GetClientPartitionKey(ContextFor("203.0.113.7", "10.0.0.1"));

        await Assert.That(key).IsEqualTo("203.0.113.7");
    }

    [Test]
    public async Task GetClientPartitionKey_WithSeveralForwardedAddresses_TakesTheFirst()
    {
        // YARP overwrites the header, so a list can only come from a caller trying to shift partitions;
        // the gateway's own value is the first one.
        var key = Register.GetClientPartitionKey(ContextFor("203.0.113.7, 198.51.100.9", "10.0.0.1"));

        await Assert.That(key).IsEqualTo("203.0.113.7");
    }

    [Test]
    public async Task GetClientPartitionKey_WithoutForwardedAddress_FallsBackToTheConnection()
    {
        var key = Register.GetClientPartitionKey(ContextFor(forwardedFor: null, "10.0.0.1"));

        await Assert.That(key).IsEqualTo("10.0.0.1");
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(",")]
    public async Task GetClientPartitionKey_WithABlankForwardedAddress_FallsBackToTheConnection(string forwardedFor)
    {
        var key = Register.GetClientPartitionKey(ContextFor(forwardedFor, "10.0.0.1"));

        await Assert.That(key).IsEqualTo("10.0.0.1");
    }

    [Test]
    public async Task GetClientPartitionKey_WithNothingToGoOn_IsStillAPartition()
    {
        var key = Register.GetClientPartitionKey(ContextFor(forwardedFor: null, remoteIp: null));

        await Assert.That(key).IsEqualTo("unknown");
    }
}
