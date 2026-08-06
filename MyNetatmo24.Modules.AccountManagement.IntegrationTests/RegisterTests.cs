using ApiServiceSDK.Models.MyNetatmo24.Modules.AccountManagement.Application.Register;
using Microsoft.Kiota.Abstractions;
using MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests;

public class RegisterTests : AccountApiIntegrationTest
{
    private const string Password = "sup3r-s3cret-passphrase";

    private static RegistrationDto ValidRegistration() => new()
    {
        Email = "jane@example.com",
        Password = Password,
        PasswordConfirmation = Password,
        Nickname = "janey",
        GivenName = "Jane",
        FamilyName = "Doe",
        AvatarUrl = "https://example.com/avatar.png",
    };

    [Test]
    public async Task Register_WhenNotAuthenticated_ReturnsNoContent()
    {
        var apiClient = CreateAnonymousApiClient();

        await apiClient.Account.Register.PostAsync(ValidRegistration());

        await Assert.That(RegistrationService.CallCount).IsEqualTo(1);
    }

    [Test]
    public async Task Register_ForwardsTheSubmittedRegistrationDataToTheRegistrationService()
    {
        var apiClient = CreateAnonymousApiClient();

        await apiClient.Account.Register.PostAsync(ValidRegistration());

        var request = RegistrationService.LastRequest;
        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Email).IsEqualTo("jane@example.com");
        await Assert.That(request.Password).IsEqualTo(Password);
        await Assert.That(request.Nickname).IsEqualTo("janey");
        await Assert.That(request.GivenName).IsEqualTo("Jane");
        await Assert.That(request.FamilyName).IsEqualTo("Doe");
        await Assert.That(request.AvatarUrl?.ToString()).IsEqualTo("https://example.com/avatar.png");
    }

    [Test]
    public async Task Register_WithoutAvatarUrl_IsAccepted()
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.AvatarUrl = null;

        await apiClient.Account.Register.PostAsync(registration);

        await Assert.That(RegistrationService.LastRequest?.AvatarUrl).IsNull();
    }

    [Test]
    [Arguments(nameof(RegistrationDto.Email))]
    [Arguments(nameof(RegistrationDto.Password))]
    [Arguments(nameof(RegistrationDto.PasswordConfirmation))]
    [Arguments(nameof(RegistrationDto.Nickname))]
    [Arguments(nameof(RegistrationDto.GivenName))]
    [Arguments(nameof(RegistrationDto.FamilyName))]
    public async Task Register_WhenARequiredFieldIsMissing_ReturnsBadRequest(string missingField)
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        ClearField(registration, missingField);

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(registration)).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(RegistrationService.CallCount).IsEqualTo(0);
    }

    /// <summary>
    /// Blanks one field of an otherwise valid registration. Reflection rather than a per-field switch,
    /// so that a field added to the contract needs nothing here to be covered.
    /// </summary>
    private static void ClearField(RegistrationDto registration, string field)
    {
        var property = typeof(RegistrationDto).GetProperty(field) ??
                       throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown registration field.");

        property.SetValue(registration, null);
    }
}
