using Microsoft.Extensions.Logging;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using NSubstitute;

namespace MyNetatmo24.Modules.AccountManagement.Tests.HttpClients.Auth0;

public class StubUserRegistrationServiceTests
{
    private static readonly RegistrationData s_registration = new(
        "jane.doe@example.com",
        "s3cr3t-p4ssw0rd",
        "janie",
        "Jane",
        "Doe",
        null);

    [Test]
    public async Task RegistrationData_ToString_DoesNotLeakThePassword()
    {
        var text = s_registration.ToString();

        await Assert.That(text).DoesNotContain("s3cr3t-p4ssw0rd");
        await Assert.That(text).DoesNotContain("jane.doe@example.com");
    }

    [Test]
    public async Task RegisterAsync_SucceedsAndWarnsThatNoIdentityWasCreated()
    {
        var logger = Substitute.For<ILogger<StubUserRegistrationService>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var service = new StubUserRegistrationService(logger);

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
