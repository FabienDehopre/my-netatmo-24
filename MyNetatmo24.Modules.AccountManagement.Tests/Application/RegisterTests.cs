using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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

        await Assert.That(response.Result is NoContent).IsTrue();
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

    private static IRegistrationService FailingService(Error error)
    {
        var service = Substitute.For<IRegistrationService>();
        service.RegisterAsync(Arg.Any<RegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        return service;
    }

    [Test]
    public async Task HandleAsync_WhenTheEmailIsAlreadyRegistered_ReturnsConflict()
    {
        var service = FailingService(Errors.EmailAlreadyRegistered);

        var response = await Register.HandleAsync(ValidDto(), service, CancellationToken.None);

        await Assert.That(response.Result is Conflict).IsTrue();
    }

    [Test]
    public async Task HandleAsync_WhenThePasswordIsTooWeak_ReturnsValidationProblemCarryingThePolicyMessage()
    {
        const string policyMessage = "Password is too common; pick at least 12 characters.";
        var service = FailingService(Errors.PasswordTooWeak(policyMessage));

        var response = await Register.HandleAsync(ValidDto(), service, CancellationToken.None);

        var problem = response.Result as ValidationProblem;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.ProblemDetails.Errors[nameof(Register.RegistrationDto.Password)])
            .Contains(policyMessage);
    }

    [Test]
    public async Task HandleAsync_WhenTheIdentityProviderIsUnavailable_ReturnsBadGateway()
    {
        var service = FailingService(Errors.IdentityProviderUnavailable);

        var response = await Register.HandleAsync(ValidDto(), service, CancellationToken.None);

        var problem = response.Result as ProblemHttpResult;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.StatusCode).IsEqualTo(StatusCodes.Status502BadGateway);
    }

    [Test]
    public async Task HandleAsync_WithAnUnmappedError_Throws()
    {
        var service = FailingService(Errors.AccountNotFound);

        await Assert.That(async () => { await Register.HandleAsync(ValidDto(), service, CancellationToken.None); })
            .Throws<InvalidOperationException>();
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
