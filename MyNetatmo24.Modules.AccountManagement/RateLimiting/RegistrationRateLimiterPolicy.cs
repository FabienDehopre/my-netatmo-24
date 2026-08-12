using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace MyNetatmo24.Modules.AccountManagement.RateLimiting;

/// <summary>
/// Keeps a single client from flooding the identity provider with junk identities through
/// Registration, the only anonymous endpoint of the module.
/// </summary>
/// <remarks>
/// The window is deliberately small: registering is a once-in-a-lifetime act for a legitimate
/// prospective user, who never notices the limit even after a few validation failures. It counts
/// per forwarded client address, so the address of the Gateway the requests all share is never
/// what a budget belongs to.
/// </remarks>
internal sealed class RegistrationRateLimiterPolicy : IRateLimiterPolicy<string>
{
    /// <summary>
    /// The name Registration attaches this policy to its route under.
    /// </summary>
    public const string Name = "account-registration";

    /// <summary>
    /// The number of registration attempts a single client may make per <see cref="Window"/>.
    /// </summary>
    public const int PermitLimit = 5;

    /// <summary>
    /// The length of the window the <see cref="PermitLimit"/> attempts are counted over.
    /// </summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <inheritdoc/>
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; } = (context, _) =>
    {
        // The rejected client is told when its budget comes back rather than being left to guess,
        // and the status code itself is the 429 the host configures for every rejection.
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds))
                .ToString(CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    };

    /// <inheritdoc/>
    public RateLimitPartition<string> GetPartition(HttpContext httpContext) =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.ForwardedClientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = PermitLimit,
                Window = Window,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                // Registration is interactive: a prospective user is better told to slow down than
                // left waiting on a queued request.
                QueueLimit = 0
            });
}
