using System.Net;
using System.Net.Http.Json;
using ApiServiceSDK.Models.MyNetatmo24.Modules.AccountManagement.Application.Registration;
using MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;
using ValidationProblem = ApiServiceSDK.Models.Microsoft.AspNetCore.Http.HttpValidationProblemDetails;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests;

public class RegistrationTests : AccountApiIntegrationTest
{
    [Test]
    public async Task Registration_WhenNotAuthenticated_ForwardsTheRegistrationAndSucceeds()
    {
        var apiClient = CreateAnonymousApiClient();

        await apiClient.Account.Register.PostAsync(ValidRegistration());

        var registration = UserRegistrationService.LastRegistration;
        await Assert.That(registration).IsNotNull();
        await Assert.That(registration!.Email).IsEqualTo("jane.doe@example.com");
        await Assert.That(registration.Password).IsEqualTo("s3cr3t-p4ssw0rd");
        await Assert.That(registration.Nickname).IsEqualTo("janie");
        await Assert.That(registration.GivenName).IsEqualTo("Jane");
        await Assert.That(registration.FamilyName).IsEqualTo("Doe");
        await Assert.That(registration.AvatarUrl).IsEqualTo(new Uri("https://example.com/jane.png"));
    }

    [Test]
    public async Task Registration_WhenNotAuthenticated_ReturnsNoContent()
    {
        // The generated SDK swallows the success status code, so the 204 of the contract is pinned
        // with a raw client instead.
        using var httpClient = Factory.CreateClient();
        var body = ValidRegistration();

        using var response = await httpClient.PostAsJsonAsync(
            "account/register",
            new
            {
                email = body.Email,
                password = body.Password,
                passwordConfirmation = body.PasswordConfirmation,
                nickname = body.Nickname,
                givenName = body.GivenName,
                familyName = body.FamilyName,
                avatarUrl = body.AvatarUrl
            });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Registration_WithoutAvatarUrl_Succeeds()
    {
        var apiClient = CreateAnonymousApiClient();
        var body = ValidRegistration();
        body.AvatarUrl = null;

        await apiClient.Account.Register.PostAsync(body);

        await Assert.That(UserRegistrationService.LastRegistration!.AvatarUrl).IsNull();
    }

    [Test]
    public async Task Registration_WhenRequiredFieldIsMissing_ReturnsValidationProblem()
    {
        var apiClient = CreateAnonymousApiClient();
        var body = ValidRegistration();
        body.Nickname = null;

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(body))
            .Throws<ValidationProblem>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(exception.Errors!.AdditionalData).ContainsKey(nameof(RegistrationRequestDto.Nickname));
        await Assert.That(UserRegistrationService.Registrations).IsEmpty();
    }

    private static RegistrationRequestDto ValidRegistration() => new()
    {
        Email = "jane.doe@example.com",
        Password = "s3cr3t-p4ssw0rd",
        PasswordConfirmation = "s3cr3t-p4ssw0rd",
        Nickname = "janie",
        GivenName = "Jane",
        FamilyName = "Doe",
        AvatarUrl = "https://example.com/jane.png"
    };
}
