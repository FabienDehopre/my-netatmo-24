using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using MyNetatmo24.Modules.AccountManagement.RateLimiting;

namespace MyNetatmo24.Modules.AccountManagement.Tests.RateLimiting;

public class RegistrationRateLimiterPolicyTests
{
    [Test]
    public async Task GetPartition_PartitionsOnTheForwardedClientAddress()
    {
        var policy = new RegistrationRateLimiterPolicy();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.1, 203.0.113.7";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");

        var partition = policy.GetPartition(context);

        // The proxy address the connection reports is the same for every prospective user, so
        // partitioning on it would give the whole internet a single shared budget.
        await Assert.That(partition.PartitionKey).IsEqualTo("203.0.113.7");
    }

    [Test]
    public async Task GetPartition_ForTwoRequestsOfTheSameClient_ReturnsTheSamePartition()
    {
        var policy = new RegistrationRateLimiterPolicy();

        var first = policy.GetPartition(ContextFrom("203.0.113.7"));
        var second = policy.GetPartition(ContextFrom("203.0.113.7"));

        await Assert.That(first.PartitionKey).IsEqualTo(second.PartitionKey);
    }

    [Test]
    public async Task GetPartition_ForTwoRequestsOfDifferentClients_ReturnsDifferentPartitions()
    {
        var policy = new RegistrationRateLimiterPolicy();

        var first = policy.GetPartition(ContextFrom("203.0.113.7"));
        var second = policy.GetPartition(ContextFrom("203.0.113.8"));

        await Assert.That(first.PartitionKey).IsNotEqualTo(second.PartitionKey);
    }

    [Test]
    public async Task OnRejected_TellsTheClientWhenToRetry()
    {
        var policy = new RegistrationRateLimiterPolicy();
        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
        using var granted = limiter.AttemptAcquire();
        using var rejected = limiter.AttemptAcquire();
        var httpContext = new DefaultHttpContext();

        await policy.OnRejected!(new OnRejectedContext { HttpContext = httpContext, Lease = rejected }, CancellationToken.None);

        var retryAfter = httpContext.Response.Headers.RetryAfter.ToString();
        await Assert.That(int.TryParse(retryAfter, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)).IsTrue();
        await Assert.That(seconds).IsGreaterThan(0).And.IsLessThanOrEqualTo(60);
    }

    [Test]
    public async Task GetPartition_WithNullContext_Throws()
    {
        var policy = new RegistrationRateLimiterPolicy();

        await Assert.That(() => policy.GetPartition(null!)).Throws<ArgumentNullException>();
    }

    private static DefaultHttpContext ContextFrom(string clientIp)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = clientIp;
        return context;
    }
}
