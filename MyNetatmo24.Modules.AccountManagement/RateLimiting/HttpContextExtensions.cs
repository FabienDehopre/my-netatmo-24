using System.Net;
using Microsoft.AspNetCore.Http;

namespace MyNetatmo24.Modules.AccountManagement.RateLimiting;

/// <summary>
/// Answers which client a request really came from, knowing that every request reaches this
/// application through the Gateway proxy, whose own address is the same for everyone.
/// </summary>
internal static class HttpContextExtensions
{
    /// <summary>
    /// The header the proxy chain records the client addresses in, oldest first.
    /// </summary>
    public const string ForwardedForHeaderName = "X-Forwarded-For";

    /// <summary>
    /// The address stood in for every client that cannot be identified at all.
    /// </summary>
    public const string UnknownClientIp = "unknown";

    extension(HttpContext context)
    {
        /// <summary>
        /// Gets the address of the client behind the request, or <see cref="UnknownClientIp"/> when
        /// neither the forwarded header nor the connection names one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only the <em>last</em> forwarded address is read. The Gateway replaces the header with
        /// the address it actually saw, so there is normally a single entry; reading the last one
        /// holds just as well if the chain is ever set to append, and either way a client that
        /// sends its own <c>X-Forwarded-For</c> can only prepend values it never gets partitioned
        /// on. An address that cannot be read falls back to the connection, which lumps such
        /// requests into the single partition of the proxy instead of handing out a fresh budget
        /// per made-up value.
        /// </para>
        /// <para>
        /// This trusts the Gateway to be the hop in front of this application. Put a CDN or an
        /// ingress before the Gateway and it is the forwarded-headers configuration of the Gateway
        /// that has to name the real client, not this.
        /// </para>
        /// </remarks>
        public string ForwardedClientIp
        {
            get
            {
                ArgumentNullException.ThrowIfNull(context);

                var forwardedFor = context.Request.Headers[ForwardedForHeaderName].ToString();
                var lastSeparator = forwardedFor.LastIndexOf(',');
                var lastForwarded = lastSeparator < 0 ? forwardedFor : forwardedFor[(lastSeparator + 1)..];
                if (TryParseAddress(lastForwarded.Trim(), out var forwardedAddress))
                {
                    return Canonicalize(forwardedAddress);
                }

                var connectionAddress = context.Connection.RemoteIpAddress;
                return connectionAddress is null ? UnknownClientIp : Canonicalize(connectionAddress);
            }
        }
    }

    /// <summary>
    /// Reads an address that a proxy may have written with or without its ephemeral port.
    /// </summary>
    /// <param name="value">The forwarded value to read.</param>
    /// <param name="address">The address that was read, when there was one.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> names an address.</returns>
    private static bool TryParseAddress(string value, out IPAddress address)
    {
        if (IPAddress.TryParse(value, out var parsed))
        {
            address = parsed;
            return true;
        }

        if (IPEndPoint.TryParse(value, out var endPoint))
        {
            address = endPoint.Address;
            return true;
        }

        address = IPAddress.None;
        return false;
    }

    /// <summary>
    /// Collapses the two spellings a dual-stack socket reports an IPv4 peer under, so that one
    /// client never holds two budgets.
    /// </summary>
    /// <param name="address">The address to spell canonically.</param>
    /// <returns>The canonical spelling of <paramref name="address"/>.</returns>
    private static string Canonicalize(IPAddress address) =>
        (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
}
