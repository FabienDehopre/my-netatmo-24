using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using NSubstitute;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application;

public class RegistrationTests
{
    private static Registration.RegistrationRequestDto ValidRequest(string? avatarUrl = null) =>
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

        await Assert.That(response.Result).IsTypeOf<NoContent>();
    }

    [Test]
    public async Task HandleAsync_ForwardsTheSubmittedRegistration()
    {
        var service = RegistrationServiceReturning(Result.Ok());
        var avatarUrl = new Uri("https://example.com/jane.png");

        await Registration.HandleAsync(ValidRequest(avatarUrl.OriginalString), service, CancellationToken.None);

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
    public async Task HandleAsync_ForwardsTheProfileValuesTrimmed()
    {
        var service = RegistrationServiceReturning(Result.Ok());
        var request = new Registration.RegistrationRequestDto(
            "  jane.doe@example.com  ",
            "  s3cr3t-p4ssw0rd  ",
            "  s3cr3t-p4ssw0rd  ",
            "  janie  ",
            "\tJane\t",
            " Doe\n");

        await Registration.HandleAsync(request, service, CancellationToken.None);

        await service.Received(1).RegisterAsync(
            Arg.Is<RegistrationData>(data =>
                data.Email == "jane.doe@example.com" &&
                data.Nickname == "janie" &&
                data.GivenName == "Jane" &&
                data.FamilyName == "Doe"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ForwardsThePasswordUntouched()
    {
        // Whitespace is significant in a password: trimming one would create an identity the
        // person cannot log into with what they typed.
        var service = RegistrationServiceReturning(Result.Ok());
        var request = new Registration.RegistrationRequestDto(
            "jane.doe@example.com",
            "  s3cr3t-p4ssw0rd  ",
            "  s3cr3t-p4ssw0rd  ",
            "janie",
            "Jane",
            "Doe");

        await Registration.HandleAsync(request, service, CancellationToken.None);

        await service.Received(1).RegisterAsync(
            Arg.Is<RegistrationData>(data => data.Password == "  s3cr3t-p4ssw0rd  "),
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
    public async Task HandleAsync_WhenEmailIsAlreadyRegistered_ReturnsConflict()
    {
        var service = RegistrationServiceReturning(Result.Fail(Errors.EmailAlreadyRegistered));

        var response = await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None);

        var problem = (ProblemHttpResult)response.Result;
        await Assert.That(problem.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
    }

    [Test]
    public async Task HandleAsync_WhenPasswordIsTooWeak_ReturnsValidationProblemCarryingThePolicyMessage()
    {
        const string policyMessage = "Password is too common, and must contain at least 8 characters.";
        var service = RegistrationServiceReturning(Result.Fail(Errors.PasswordTooWeak(policyMessage)));

        var response = await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None);

        var validationProblem = (ValidationProblem)response.Result;
        await Assert.That(validationProblem.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(validationProblem.ProblemDetails.Errors[nameof(Registration.RegistrationRequestDto.Password)])
            .Contains(policyMessage);
    }

    [Test]
    public async Task HandleAsync_WhenPasswordIsTooWeak_DoesNotLeakThePolicyMessageIntoOtherFields()
    {
        var service = RegistrationServiceReturning(Result.Fail(Errors.PasswordTooWeak("too weak")));

        var response = await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None);

        var validationProblem = (ValidationProblem)response.Result;
        await Assert.That(validationProblem.ProblemDetails.Errors.Keys)
            .HasSingleItem()
            .And.Contains(nameof(Registration.RegistrationRequestDto.Password));
    }

    [Test]
    public async Task HandleAsync_WhenIdentityProviderIsUnavailable_ReturnsBadGateway()
    {
        var service = RegistrationServiceReturning(Result.Fail(Errors.IdentityProviderUnavailable));

        var response = await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None);

        var problem = (ProblemHttpResult)response.Result;
        await Assert.That(problem.StatusCode).IsEqualTo(StatusCodes.Status502BadGateway);
    }

    [Test]
    public async Task HandleAsync_WhenRegistrationFailsWithAnotherErrorOfTheSameStatusCode_Throws()
    {
        // UserDeleted is a 409 of the same catalogue: the mapping must recognize the outcome, not
        // merely the status code that happens to be attached to it.
        var service = RegistrationServiceReturning(Result.Fail(Errors.UserDeleted(DateTimeOffset.UtcNow)));

        await Assert.That(async () => await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task HandleAsync_WhenRegistrationFailsWithAnUnmappedError_Throws()
    {
        // A failure the catalogue does not describe is a defect, not an outcome of the contract;
        // the handler must not silently report success for it.
        var service = RegistrationServiceReturning(Result.Fail("boom"));

        await Assert.That(async () => await Registration.HandleAsync(ValidRequest(), service, CancellationToken.None))
            .Throws<InvalidOperationException>();
    }
}
