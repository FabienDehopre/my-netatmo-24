using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using NSubstitute;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application;

public class RegistrationTests
{
    private static Registration.RegistrationRequestDto ValidRequest(Uri? avatarUrl = null) =>
        new("jane.doe@example.com", "s3cr3t-p4ssw0rd", "s3cr3t-p4ssw0rd", "janie", "Jane", "Doe", avatarUrl);

    private static IUserRegistrationService RegistrationServiceReturning(Result result)
    {
        var service = Substitute.For<IUserRegistrationService>();
        service.RegisterAsync(Arg.Any<RegistrationData>(), Arg.Any<CancellationToken>()).Returns(result);
        return service;
    }

    [Test]
    public async Task HandleAsync_WhenRegistrationSucceeds_ReturnsNoContent()
    {
        var service = RegistrationServiceReturning(Result.Ok());

        var response = await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None);

        await Assert.That(response).IsTypeOf<NoContent>();
    }

    [Test]
    public async Task HandleAsync_ForwardsTheSubmittedRegistration()
    {
        var service = RegistrationServiceReturning(Result.Ok());
        var avatarUrl = new Uri("https://example.com/jane.png");

        await Registration.HandleAsync(ValidRequest(avatarUrl), service, CancellationToken.None);

        await service.Received(1).RegisterAsync(
            Arg.Is<RegistrationData>(data =>
                data.Email == "jane.doe@example.com" &&
                data.Password == "s3cr3t-p4ssw0rd" &&
                data.Nickname == "janie" &&
                data.GivenName == "Jane" &&
                data.FamilyName == "Doe" &&
                data.AvatarUrl == avatarUrl),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_WithoutAvatarUrl_ForwardsNoAvatarUrl()
    {
        var service = RegistrationServiceReturning(Result.Ok());

        await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None);

        await service.Received(1).RegisterAsync(
            Arg.Is<RegistrationData>(data => data.AvatarUrl == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RegistrationRequestDto_ToString_DoesNotLeakThePassword()
    {
        var text = ValidRequest().ToString();

        await Assert.That(text).DoesNotContain("s3cr3t-p4ssw0rd");
        await Assert.That(text).DoesNotContain("jane.doe@example.com");
    }

    [Test]
    public async Task HandleAsync_WhenRegistrationFails_Throws()
    {
        // The failure outcomes of the seam have no HTTP mapping yet; the handler must not
        // silently report success for them.
        var service = RegistrationServiceReturning(Result.Fail("boom"));

        await Assert.That(async () => await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None))
            .Throws<InvalidOperationException>();
    }
}
