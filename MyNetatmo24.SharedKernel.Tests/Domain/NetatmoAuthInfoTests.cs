using MyNetatmo24.SharedKernel.Domain;

namespace MyNetatmo24.SharedKernel.Tests.Domain;

public class NetatmoAuthInfoTests
{
    [Test]
    public async Task From_WithValidValues_SetsTokensAndComputesExpiry()
    {
        var before = DateTimeOffset.UtcNow;

        var info = NetatmoAuthInfo.From("access-token", "refresh-token", 3600);

        await Assert.That(info.AccessToken).IsEqualTo("access-token");
        await Assert.That(info.RefreshToken).IsEqualTo("refresh-token");
        await Assert.That(info.ExpiresAt).IsGreaterThanOrEqualTo(before.AddSeconds(3600));
        await Assert.That(info.ExpiresAt).IsLessThanOrEqualTo(DateTimeOffset.UtcNow.AddSeconds(3600));
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task From_WithInvalidAccessToken_Throws(string? accessToken)
    {
        await Assert.That(() => NetatmoAuthInfo.From(accessToken, "refresh", 3600)).Throws<ArgumentException>();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task From_WithInvalidRefreshToken_Throws(string? refreshToken)
    {
        await Assert.That(() => NetatmoAuthInfo.From("access", refreshToken, 3600)).Throws<ArgumentException>();
    }

    [Test]
    public async Task From_WithNullExpiresIn_ThrowsArgumentNullException()
    {
        await Assert.That(() => NetatmoAuthInfo.From("access", "refresh", null)).Throws<ArgumentNullException>();
    }
}
