using System.Diagnostics.CodeAnalysis;

namespace MyNetatmo24.Modules.AccountManagement.Domain;

public readonly record struct NetatmoAuthInfo(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt)
{
    public static NetatmoAuthInfo From([NotNull] string? accessToken, [NotNull] string? refreshToken,
        [NotNull] int? expiresIn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        if (!expiresIn.HasValue)
        {
            throw new ArgumentNullException(nameof(expiresIn));
        }

        return new NetatmoAuthInfo(accessToken, refreshToken, DateTimeOffset.UtcNow.AddSeconds(expiresIn.Value));
    }
}
