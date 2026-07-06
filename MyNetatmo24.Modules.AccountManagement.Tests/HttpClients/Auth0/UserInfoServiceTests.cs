using System.Net;
using Microsoft.Extensions.Logging;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;
using NSubstitute;

namespace MyNetatmo24.Modules.AccountManagement.Tests.HttpClients.Auth0;

public class UserInfoServiceTests
{
    private static UserInfoService CreateService(HttpStatusCode statusCode, string? jsonBody, out ILogger<UserInfoService> logger)
    {
        logger = Substitute.For<ILogger<UserInfoService>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode, jsonBody))
        {
            BaseAddress = new Uri("https://tenant.auth0.com/"),
        };
        return new UserInfoService(httpClient, logger);
    }

    [Test]
    public async Task GetUserInfoAsync_WithValidResponse_ReturnsUserInfo()
    {
        const string json = """
        {
            "nickname": "johnny",
            "picture": "https://example.com/avatar.png",
            "auth0.given_name": "John",
            "auth0.family_name": "Doe"
        }
        """;
        var service = CreateService(HttpStatusCode.OK, json, out _);

        var result = await service.GetUserInfoAsync(CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Nickname).IsEqualTo("johnny");
        await Assert.That(result.Value.GivenName).IsEqualTo("John");
        await Assert.That(result.Value.FamilyName).IsEqualTo("Doe");
        await Assert.That(result.Value.Picture).IsEqualTo(new Uri("https://example.com/avatar.png"));
    }

    [Test]
    public async Task GetUserInfoAsync_WithNullBody_ReturnsUserInfoNotFoundError()
    {
        var service = CreateService(HttpStatusCode.OK, "null", out var logger);

        var result = await service.GetUserInfoAsync(CancellationToken.None);

        await Assert.That(result.IsFailed).IsTrue();
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
