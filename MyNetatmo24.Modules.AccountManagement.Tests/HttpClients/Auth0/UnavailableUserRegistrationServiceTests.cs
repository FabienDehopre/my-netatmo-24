using Microsoft.Extensions.Logging;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using NSubstitute;

namespace MyNetatmo24.Modules.AccountManagement.Tests.HttpClients.Auth0;

public class UnavailableUserRegistrationServiceTests
{
    private static readonly RegistrationData s_registration = new(
        "jane.doe@example.com",
        "s3cr3t-p4ssw0rd",
        "janie",
        "Jane",
        "Doe",
        null);

    [Test]
    public async Task RegisterAsync_ReportsTheIdentityProviderAsUnavailable()
    {
        var service = new UnavailableUserRegistrationService(CreateLogger());

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await Assert.That(result.IsFailed).IsTrue();
        await Assert.That(result.Errors[0].IsIdentityProviderUnavailable()).IsTrue();
    }

    [Test]
    public async Task RegisterAsync_WarnsThatTheHostHoldsNoCredentials()
    {
        var logger = CreateLogger();
        var service = new UnavailableUserRegistrationService(logger);

        await service.RegisterAsync(s_registration, CancellationToken.None);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task Constructor_WithoutALogger_Throws()
    {
        await Assert.That(() => new UnavailableUserRegistrationService(null!)).Throws<ArgumentNullException>();
    }

    private static ILogger<UnavailableUserRegistrationService> CreateLogger()
    {
        var logger = Substitute.For<ILogger<UnavailableUserRegistrationService>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return logger;
    }
}
