using FluentResults;
using Microsoft.AspNetCore.Http;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using NSubstitute;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application;

public class RegisterTests
{
    private const string Email = "jane@example.com";
    private const string Password = "sup3r-s3cret-passphrase";
    private const string Nickname = "janey";
    private const string GivenName = "Jane";
    private const string FamilyName = "Doe";
    private const string AvatarUrl = "https://example.com/avatar.png";

    private static Register.RegistrationDto ValidDto() =>
        new(Email, Password, Password, Nickname, GivenName, FamilyName, AvatarUrl);

    private static IRegistrationService SucceedingService()
    {
        var service = Substitute.For<IRegistrationService>();
        service.RegisterAsync(Arg.Any<RegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        return service;
    }

    [Test]
    public async Task HandleAsync_WhenRegistrationSucceeds_ReturnsNoContent()
    {
        var service = SucceedingService();

        var response = await Register.HandleAsync(ValidDto(), service, CancellationToken.None);

        await Assert.That(response).IsNotNull();
        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status204NoContent);
    }

    [Test]
    public async Task HandleAsync_ForwardsSubmittedRegistrationDataToTheService()
    {
        var service = SucceedingService();

        await Register.HandleAsync(ValidDto(), service, CancellationToken.None);

        await service.Received(1).RegisterAsync(
            Arg.Is<RegistrationRequest>(r =>
                r.Email == Email &&
                r.Password == Password &&
                r.Nickname == Nickname &&
                r.GivenName == GivenName &&
                r.FamilyName == FamilyName &&
                r.AvatarUrl != null && r.AvatarUrl.ToString() == AvatarUrl),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_WhenAvatarUrlIsOmitted_ForwardsNullAvatarUrl()
    {
        var service = SucceedingService();
        var dto = ValidDto() with { AvatarUrl = null };

        await Register.HandleAsync(dto, service, CancellationToken.None);

        await service.Received(1).RegisterAsync(
            Arg.Is<RegistrationRequest>(r => r.AvatarUrl == null),
            Arg.Any<CancellationToken>());
    }
}
