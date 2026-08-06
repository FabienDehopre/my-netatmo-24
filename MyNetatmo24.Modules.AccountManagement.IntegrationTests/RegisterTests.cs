using System.Net.Http.Json;
using ApiServiceSDK.Models.MyNetatmo24.Modules.AccountManagement.Application.Register;
using Microsoft.Kiota.Abstractions;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests;

public class RegisterTests : AccountApiIntegrationTest
{
    private const string Password = "sup3r-s3cret-passphrase";
    private const string ForwardedForHeaderName = "X-Forwarded-For";

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

    /// <summary>
    /// The same valid registration as <see cref="ValidRegistration"/>, projected onto the wire so that a
    /// test can mutate one field before it is serialized. The values come from the one fixture; only the
    /// JSON names are restated, and those are what these tests are about.
    /// </summary>
    private static Dictionary<string, object?> ValidRegistrationPayload()
    {
        var registration = ValidRegistration();

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["email"] = registration.Email,
            ["password"] = registration.Password,
            ["passwordConfirmation"] = registration.PasswordConfirmation,
            ["nickname"] = registration.Nickname,
            ["givenName"] = registration.GivenName,
            ["familyName"] = registration.FamilyName,
            ["avatarUrl"] = registration.AvatarUrl,
        };
    }

    /// <summary>
    /// Posts a registration whose payload has been mutated, and hands back the raw answer. The generated
    /// SDK surfaces the status code but discards the body, and the body is where the validation errors
    /// name the field they are about.
    /// </summary>
    private async Task<(int StatusCode, string Body)> PostRegistrationAsync(Action<Dictionary<string, object?>> mutate)
    {
        var payload = ValidRegistrationPayload();
        mutate(payload);

        using var httpClient = Factory.CreateClient();
        using var response = await httpClient.PostAsJsonAsync(new Uri("account/register", UriKind.Relative), payload);
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }

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
    public async Task Register_WhenTheEmailIsAlreadyRegistered_ReturnsConflict()
    {
        RegistrationService.SetEmailAlreadyRegistered();
        var apiClient = CreateAnonymousApiClient();

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(ValidRegistration())).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status409Conflict);
    }

    [Test]
    public async Task Register_WhenThePasswordFailsTheProviderPolicy_ReturnsBadRequestCarryingThePolicyMessage()
    {
        const string policyMessage = "Password is too common; pick at least 12 characters.";
        RegistrationService.SetPasswordTooWeak(policyMessage);

        var (statusCode, body) = await PostRegistrationAsync(_ => { });

        await Assert.That(statusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(body).Contains(policyMessage);
    }

    [Test]
    public async Task Register_WhenTheIdentityProviderIsUnavailable_ReturnsBadGateway()
    {
        RegistrationService.SetUnavailable();
        var apiClient = CreateAnonymousApiClient();

        var exception = await Assert.That(() => apiClient.Account.Register.PostAsync(ValidRegistration())).Throws<ApiException>();

        await Assert.That(exception!.ResponseStatusCode).IsEqualTo(StatusCodes.Status502BadGateway);
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

    [Test]
    [Arguments("nickname", "")]
    [Arguments("nickname", "   ")]
    [Arguments("givenName", "Ada Lovelace-King and then some more characters than fifty")]
    [Arguments("familyName", "")]
    [Arguments("email", "not-an-email")]
    [Arguments("avatarUrl", "http://example.com/avatar.png")]
    [Arguments("avatarUrl", "/avatar.png")]
    public async Task Register_WhenAFieldIsRejected_TheProblemDetailsNamesIt(string field, string value)
    {
        var (statusCode, body) = await PostRegistrationAsync(payload => payload[field] = value);

        await Assert.That(statusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(body).Contains(field, StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task Register_WhenAProfileValueCarriesControlCharacters_TheProblemDetailsNamesIt()
    {
        var (statusCode, body) = await PostRegistrationAsync(payload =>
            payload["nickname"] = "Ja" + char.ConvertFromUtf32(0x200B) + "ne");

        await Assert.That(statusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(body).Contains("nickname", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task Register_WhenThePasswordConfirmationDiffers_TheProblemDetailsNamesIt()
    {
        var (statusCode, body) = await PostRegistrationAsync(payload =>
            payload["passwordConfirmation"] = Password + "-typo");

        await Assert.That(statusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(body).Contains("passwordConfirmation", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task Register_WhenBurstingPastTheRateLimit_ReturnsTooManyRequests()
    {
        using var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add(ForwardedForHeaderName, "203.0.113.7");

        var statusCodes = await PostRepeatedlyAsync(httpClient, Register.RateLimitPermitLimit + 1);

        await Assert.That(statusCodes.Take(Register.RateLimitPermitLimit))
            .All().Satisfy(code => code.IsEqualTo(StatusCodes.Status204NoContent));
        await Assert.That(statusCodes[^1]).IsEqualTo(StatusCodes.Status429TooManyRequests);
    }

    [Test]
    public async Task Register_ThrottlesEachForwardedClientSeparately()
    {
        using var throttled = Factory.CreateClient();
        throttled.DefaultRequestHeaders.Add(ForwardedForHeaderName, "203.0.113.7");
        await PostRepeatedlyAsync(throttled, Register.RateLimitPermitLimit + 1);

        using var other = Factory.CreateClient();
        other.DefaultRequestHeaders.Add(ForwardedForHeaderName, "198.51.100.9");
        var statusCodes = await PostRepeatedlyAsync(other, 1);

        await Assert.That(statusCodes[0]).IsEqualTo(StatusCodes.Status204NoContent);
    }

    [Test]
    public async Task Register_IgnoresTheProxysOwnAddressWhenPartitioning()
    {
        // Both clients share a connection address; only the forwarded address tells them apart.
        using var first = Factory.CreateClient();
        first.DefaultRequestHeaders.Add(ForwardedForHeaderName, "203.0.113.7");
        using var second = Factory.CreateClient();
        second.DefaultRequestHeaders.Add(ForwardedForHeaderName, "198.51.100.9");

        await PostRepeatedlyAsync(first, Register.RateLimitPermitLimit);
        var statusCodes = await PostRepeatedlyAsync(second, Register.RateLimitPermitLimit);

        await Assert.That(statusCodes).All().Satisfy(code => code.IsEqualTo(StatusCodes.Status204NoContent));
    }

    [Test]
    public async Task OtherEndpoints_AreNotRateLimited()
    {
        await SeedAccountAsync();
        using var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add(AccountApiAuthenticationHandler.Auth0IdHeaderName, Auth0Id);
        httpClient.DefaultRequestHeaders.Add(ForwardedForHeaderName, "203.0.113.7");

        var statusCodes = new List<int>();
        for (var i = 0; i < Register.RateLimitPermitLimit + 2; i++)
        {
            using var response = await httpClient.GetAsync(new Uri("account/me", UriKind.Relative));
            statusCodes.Add((int)response.StatusCode);
        }

        await Assert.That(statusCodes).All().Satisfy(code => code.IsEqualTo(StatusCodes.Status200OK));
    }

    private static async Task<List<int>> PostRepeatedlyAsync(HttpClient httpClient, int count)
    {
        var statusCodes = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            using var response = await httpClient.PostAsJsonAsync(
                new Uri("account/register", UriKind.Relative),
                ValidRegistrationPayload());
            statusCodes.Add((int)response.StatusCode);
        }

        return statusCodes;
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
