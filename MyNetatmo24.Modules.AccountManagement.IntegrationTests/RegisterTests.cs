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

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("Ada Lovelace-King and then some more characters than fifty")]
    public async Task Register_WithAnUnusableProfileValue_ReturnsBadRequest(string value)
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.Nickname = value;

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(registration)).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(RegistrationService.CallCount).IsEqualTo(0);
    }

    [Test]
    [Arguments(0x0000)] // Cc: null
    [Arguments(0x200B)] // Cf: zero-width space
    public async Task Register_WithControlOrFormatCharactersInAProfileValue_ReturnsBadRequest(int codePoint)
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.GivenName = "Ja" + char.ConvertFromUtf32(codePoint) + "ne";

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(registration)).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(RegistrationService.CallCount).IsEqualTo(0);
    }

    [Test]
    public async Task Register_WithNonLatinScriptProfileValues_IsAccepted()
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.Nickname = "ковалевская";
        registration.GivenName = "София";
        registration.FamilyName = "Ковалевская";

        await apiClient.Account.Register.PostAsync(registration);

        await Assert.That(RegistrationService.LastRequest?.FamilyName).IsEqualTo("Ковалевская");
    }

    [Test]
    public async Task Register_TrimsTheProfileValuesBeforeTheyReachTheRegistrationService()
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.Nickname = "  janey  ";
        registration.GivenName = "  Jane  ";
        registration.FamilyName = "  Doe  ";

        await apiClient.Account.Register.PostAsync(registration);

        var request = RegistrationService.LastRequest;
        await Assert.That(request!.Nickname).IsEqualTo("janey");
        await Assert.That(request.GivenName).IsEqualTo("Jane");
        await Assert.That(request.FamilyName).IsEqualTo("Doe");
    }

    [Test]
    public async Task Register_WhenThePasswordConfirmationDiffers_ReturnsBadRequest()
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.PasswordConfirmation = Password + "-typo";

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(registration)).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(RegistrationService.CallCount).IsEqualTo(0);
    }

    [Test]
    [Arguments("not-an-email")]
    [Arguments("jane@")]
    public async Task Register_WithAMalformedEmail_ReturnsBadRequest(string email)
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.Email = email;

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(registration)).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(RegistrationService.CallCount).IsEqualTo(0);
    }

    [Test]
    [Arguments("http://example.com/avatar.png")]
    [Arguments("/avatar.png")]
    [Arguments("javascript:alert(1)")]
    public async Task Register_WithAnAvatarUrlThatIsNotAbsoluteHttps_ReturnsBadRequest(string value)
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.AvatarUrl = value;

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(registration)).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(RegistrationService.CallCount).IsEqualTo(0);
    }

    [Test]
    public async Task Register_WithAnOverlongAvatarUrl_ReturnsBadRequest()
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.AvatarUrl = "https://example.com/" + new string('a', 2048);

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(registration)).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(RegistrationService.CallCount).IsEqualTo(0);
    }

    [Test]
    public async Task Register_WithABlankAvatarUrl_IsAcceptedAsNoAvatar()
    {
        var apiClient = CreateAnonymousApiClient();
        var registration = ValidRegistration();
        registration.AvatarUrl = "   ";

        await apiClient.Account.Register.PostAsync(registration);

        await Assert.That(RegistrationService.LastRequest?.AvatarUrl).IsNull();
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
