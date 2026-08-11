using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiServiceSDK.Models.MyNetatmo24.Modules.AccountManagement.Application.Registration;
using MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;
using Problem = ApiServiceSDK.Models.Microsoft.AspNetCore.Mvc.ProblemDetails;
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
        using var response = await PostValidRegistrationAsync();

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

    [Test]
    // Empty once trimmed.
    [Arguments(nameof(RegistrationRequestDto.Nickname), "   ")]
    [Arguments(nameof(RegistrationRequestDto.GivenName), "")]
    [Arguments(nameof(RegistrationRequestDto.FamilyName), "\t\n")]
    // Longer than 50 characters once trimmed.
    [Arguments(nameof(RegistrationRequestDto.Nickname), "aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeff")]
    [Arguments(nameof(RegistrationRequestDto.GivenName), "  aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeff  ")]
    [Arguments(nameof(RegistrationRequestDto.FamilyName), "aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeff")]
    // Containing a Unicode control character (Cc).
    [Arguments(nameof(RegistrationRequestDto.Nickname), "jan\u0007ie")]
    [Arguments(nameof(RegistrationRequestDto.GivenName), "Ja\u001bne")]
    // Containing a Unicode format character (Cf).
    [Arguments(nameof(RegistrationRequestDto.FamilyName), "D\u00ado\u200de")]
    [Arguments(nameof(RegistrationRequestDto.Nickname), "ja\u202enie")]
    public async Task Registration_WithInvalidProfileValue_ReturnsValidationProblemNamingTheField(string field, string value)
    {
        var apiClient = CreateAnonymousApiClient();
        var body = ValidRegistration();
        SetProfileValue(body, field, value);

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(body))
            .Throws<ValidationProblem>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(exception.Errors!.AdditionalData).ContainsKey(field);
        await Assert.That(UserRegistrationService.Registrations).IsEmpty();
    }

    [Test]
    public async Task Registration_WhenPasswordConfirmationDiffers_ReturnsValidationProblem()
    {
        var apiClient = CreateAnonymousApiClient();
        var body = ValidRegistration();
        body.PasswordConfirmation = "s3cr3t-p4ssw0rd-typo";

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(body))
            .Throws<ValidationProblem>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(exception.Errors!.AdditionalData).ContainsKey(nameof(RegistrationRequestDto.PasswordConfirmation));
        await Assert.That(UserRegistrationService.Registrations).IsEmpty();
    }

    [Test]
    [Arguments("http://example.com/jane.png")] // Not https.
    [Arguments("ftp://example.com/jane.png")] // Not https.
    [Arguments("/jane.png")] // Not absolute.
    [Arguments("https://exa mple.com/jane.png")] // Not a URL any parser accepts.
    public async Task Registration_WithUnacceptableAvatarUrl_ReturnsValidationProblem(string avatar)
    {
        var apiClient = CreateAnonymousApiClient();
        var body = ValidRegistration();
        body.AvatarUrl = avatar;

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(body))
            .Throws<ValidationProblem>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(exception.Errors!.AdditionalData).ContainsKey(nameof(RegistrationRequestDto.AvatarUrl));
        await Assert.That(UserRegistrationService.Registrations).IsEmpty();
    }

    [Test]
    public async Task Registration_WithTooLongAvatarUrl_ReturnsValidationProblem()
    {
        const string prefix = "https://example.com/";
        var apiClient = CreateAnonymousApiClient();
        var body = ValidRegistration();
        body.AvatarUrl = prefix + new string('a', 2049 - prefix.Length);

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(body))
            .Throws<ValidationProblem>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(exception.Errors!.AdditionalData).ContainsKey(nameof(RegistrationRequestDto.AvatarUrl));
        await Assert.That(UserRegistrationService.Registrations).IsEmpty();
    }

    [Test]
    [Arguments("やまちゃん", "山田", "太郎")]
    [Arguments("Оля", "Ольга", "Иванова")]
    [Arguments("حمودي", "أحمد", "الحسن")]
    public async Task Registration_WithNonLatinScriptName_ForwardsTheRegistration(string nickname, string givenName, string familyName)
    {
        var apiClient = CreateAnonymousApiClient();
        var body = ValidRegistration();
        body.Nickname = nickname;
        body.GivenName = givenName;
        body.FamilyName = familyName;

        await apiClient.Account.Register.PostAsync(body);

        var registration = UserRegistrationService.LastRegistration;
        await Assert.That(registration).IsNotNull();
        await Assert.That(registration!.Nickname).IsEqualTo(nickname);
        await Assert.That(registration.GivenName).IsEqualTo(givenName);
        await Assert.That(registration.FamilyName).IsEqualTo(familyName);
    }

    [Test]
    public async Task Registration_WithSurroundingWhitespace_ForwardsTheTrimmedValues()
    {
        var apiClient = CreateAnonymousApiClient();
        var body = ValidRegistration();
        body.Email = "  jane.doe@example.com  ";
        body.Nickname = "  janie  ";
        body.GivenName = "\tJane\t";
        body.FamilyName = " Doe\n";

        await apiClient.Account.Register.PostAsync(body);

        var registration = UserRegistrationService.LastRegistration;
        await Assert.That(registration).IsNotNull();
        await Assert.That(registration!.Email).IsEqualTo("jane.doe@example.com");
        await Assert.That(registration.Nickname).IsEqualTo("janie");
        await Assert.That(registration.GivenName).IsEqualTo("Jane");
        await Assert.That(registration.FamilyName).IsEqualTo("Doe");
    }

    [Test]
    public async Task Registration_WhenEmailIsAlreadyRegistered_ReturnsConflict()
    {
        var apiClient = CreateAnonymousApiClient();
        UserRegistrationService.RejectEmailAsAlreadyRegistered();

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(ValidRegistration()))
            .Throws<Problem>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(exception.Status).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(exception.Detail).IsNotNullOrEmpty();
    }

    [Test]
    public async Task Registration_WhenPasswordIsTooWeak_ReturnsValidationProblemNamingThePasswordField()
    {
        var apiClient = CreateAnonymousApiClient();
        UserRegistrationService.RejectPasswordAsTooWeak("Password is too common, and must contain at least 8 characters.");

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(ValidRegistration()))
            .Throws<ValidationProblem>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(exception.Errors!.AdditionalData).ContainsKey(nameof(RegistrationRequestDto.Password));
    }

    [Test]
    public async Task Registration_WhenPasswordIsTooWeak_CarriesThePolicyMessageOfTheIdentityProvider()
    {
        // The generated SDK exposes the field names but not their messages, so the policy message
        // the prospective user has to read is pinned on the raw body.
        const string policyMessage = "Password is too common, and must contain at least 8 characters.";
        UserRegistrationService.RejectPasswordAsTooWeak(policyMessage);

        using var response = await PostValidRegistrationAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var passwordErrors = problem
            .GetProperty("errors")
            .GetProperty(nameof(RegistrationRequestDto.Password))
            .EnumerateArray()
            .Select(error => error.GetString())
            .ToList();
        await Assert.That(passwordErrors).Contains(policyMessage);
    }

    [Test]
    public async Task Registration_WhenIdentityProviderIsUnavailable_ReturnsBadGateway()
    {
        var apiClient = CreateAnonymousApiClient();
        UserRegistrationService.ReportIdentityProviderAsUnavailable();

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(ValidRegistration()))
            .Throws<Problem>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status502BadGateway);
        await Assert.That(exception.Status).IsEqualTo(StatusCodes.Status502BadGateway);
        await Assert.That(exception.Detail).IsNotNullOrEmpty();
    }

    [Test]
    public async Task Registration_WhenRegistrationFailsUnexpectedly_ReturnsInternalServerError()
    {
        // A failure outside the three outcomes of the contract is a defect: it must reach the
        // ProblemDetails exception handler rather than be reported as any kind of success.
        UserRegistrationService.FailUnexpectedly();

        using var response = await PostValidRegistrationAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
    }

    private async Task<HttpResponseMessage> PostValidRegistrationAsync()
    {
        using var httpClient = Factory.CreateClient();
        var body = ValidRegistration();

        return await httpClient.PostAsJsonAsync(
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
    }

    private static void SetProfileValue(RegistrationRequestDto body, string field, string value)
    {
        switch (field)
        {
            case nameof(RegistrationRequestDto.Nickname):
                body.Nickname = value;
                break;
            case nameof(RegistrationRequestDto.GivenName):
                body.GivenName = value;
                break;
            case nameof(RegistrationRequestDto.FamilyName):
                body.FamilyName = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown profile field.");
        }
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
