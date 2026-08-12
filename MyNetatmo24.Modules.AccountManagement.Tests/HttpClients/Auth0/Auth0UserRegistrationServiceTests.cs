using Auth0.ManagementApi;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyNetatmo24.Modules.AccountManagement.Tests.HttpClients.Auth0;

public class Auth0UserRegistrationServiceTests
{
    private const string ConnectionName = "Some-Database-Connection";

    private static readonly RegistrationData s_registration = new(
        "jane.doe@example.com",
        "s3cr3t-p4ssw0rd",
        "janie",
        "Jane",
        "Doe",
        new Uri("https://example.com/avatar.png"));

    [Test]
    public async Task RegisterAsync_WhenAuth0CreatesTheIdentity_Succeeds()
    {
        var service = CreateService(out var users, out _);
        users.CreateAsync(Arg.Any<CreateUserRequestContent>(), Arg.Any<RequestOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Created());

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task RegisterAsync_AsksAuth0ToCreateTheIdentityAndVerifyTheEmail()
    {
        var service = CreateService(out var users, out _);
        CreateUserRequestContent? request = null;
        users.CreateAsync(Arg.Any<CreateUserRequestContent>(), Arg.Any<RequestOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                request = call.Arg<CreateUserRequestContent>();
                return Created();
            });

        await service.RegisterAsync(s_registration, CancellationToken.None);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Connection).IsEqualTo(ConnectionName);
        await Assert.That(request.Email).IsEqualTo("jane.doe@example.com");
        await Assert.That(request.Password).IsEqualTo("s3cr3t-p4ssw0rd");
        await Assert.That(request.Nickname).IsEqualTo("janie");
        await Assert.That(request.GivenName).IsEqualTo("Jane");
        await Assert.That(request.FamilyName).IsEqualTo("Doe");
        await Assert.That(request.Picture).IsEqualTo("https://example.com/avatar.png");
        await Assert.That(request.EmailVerified).IsNotNull().And.IsFalse();
        await Assert.That(request.VerifyEmail).IsNotNull().And.IsTrue();
    }

    [Test]
    public async Task RegisterAsync_WithoutAnAvatar_SendsNoPicture()
    {
        var service = CreateService(out var users, out _);
        CreateUserRequestContent? request = null;
        users.CreateAsync(Arg.Any<CreateUserRequestContent>(), Arg.Any<RequestOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                request = call.Arg<CreateUserRequestContent>();
                return Created();
            });

        await service.RegisterAsync(s_registration with { AvatarUrl = null }, CancellationToken.None);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Picture).IsNull();
    }

    [Test]
    public async Task RegisterAsync_WhenAuth0AnswersConflict_ReportsTheEmailAsAlreadyRegistered()
    {
        var service = CreateService(out var users, out var logger);
        Refuse(users, new ManagementApiException("The user already exists.", 409, null!, null!, null!));

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await Assert.That(result.IsFailed).IsTrue();
        await Assert.That(result.Errors[0].IsEmailAlreadyRegistered()).IsTrue();
        NothingLogged(logger);
    }

    [Test]
    [Arguments("PasswordStrengthError: Password is too weak", "Password is too weak")]
    [Arguments("PasswordDictionaryError: Password is too common", "Password is too common")]
    [Arguments("PasswordNoUserInfoError: Password contains user information", "Password contains user information")]
    [Arguments("PasswordHistoryError: Password has previously been used", "Password has previously been used")]
    [Arguments("PasswordStrengthError", "PasswordStrengthError")]
    public async Task RegisterAsync_WhenThePasswordPolicyRefuses_CarriesItsWordingThrough(string message, string expectedPolicyMessage)
    {
        var service = CreateService(out var users, out var logger);
        Refuse(users, new ManagementApiException(message, 400, null!, null!, null!));

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await Assert.That(result.IsFailed).IsTrue();
        await Assert.That(result.Errors[0].GetPasswordPolicyMessage()).IsEqualTo(expectedPolicyMessage);
        NothingLogged(logger);
    }

    [Test]
    public async Task RegisterAsync_WhenAuth0RejectsTheRequestForAnotherReason_ReportsTheProviderAsUnavailable()
    {
        var service = CreateService(out var users, out var logger);
        Refuse(users, new ManagementApiException("Payload validation error: 'Object didn't pass validation'.", 400, null!, null!, null!));

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await AssertUnavailableAndLogged(result, logger);
    }

    [Test]
    public async Task RegisterAsync_WhenAuth0AnswersAnUnmappedError_ReportsTheProviderAsUnavailable()
    {
        var service = CreateService(out var users, out var logger);
        Refuse(users, new ManagementApiException("Too Many Requests", 429, null!, null!, null!));

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await AssertUnavailableAndLogged(result, logger);
    }

    [Test]
    public async Task RegisterAsync_WhenAuth0CannotBeReached_ReportsTheProviderAsUnavailable()
    {
        var service = CreateService(out var users, out var logger);
        Refuse(users, new HttpRequestException("No such host is known."));

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await AssertUnavailableAndLogged(result, logger);
    }

    [Test]
    public async Task RegisterAsync_WhenTheTokenRequestFails_ReportsTheProviderAsUnavailable()
    {
        var service = CreateService(out var users, out var logger);
        users.CreateAsync(Arg.Any<CreateUserRequestContent>(), Arg.Any<RequestOptions?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Could not obtain a machine-to-machine token."));

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await AssertUnavailableAndLogged(result, logger);
    }

    [Test]
    public async Task RegisterAsync_WhenTheCallTimesOut_ReportsTheProviderAsUnavailable()
    {
        // A timeout is an OperationCanceledException that the caller did not ask for, so it is a
        // failure of the identity provider rather than a caller giving up.
        var service = CreateService(out var users, out var logger);
        Refuse(users, new OperationCanceledException("The request timed out."));

        var result = await service.RegisterAsync(s_registration, CancellationToken.None);

        await AssertUnavailableAndLogged(result, logger);
    }

    [Test]
    public async Task RegisterAsync_WhenTheCallerGivesUp_RethrowsTheCancellation()
    {
        var service = CreateService(out var users, out var logger);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        Refuse(users, new OperationCanceledException(cancellation.Token));

        await Assert.That(async () => await service.RegisterAsync(s_registration, cancellation.Token))
            .Throws<OperationCanceledException>();
        NothingLogged(logger);
    }

    [Test]
    public async Task RegisterAsync_WithoutRegistrationData_Throws()
    {
        var service = CreateService(out _, out _);

        await Assert.That(async () => await service.RegisterAsync(null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
    }

    private static Auth0UserRegistrationService CreateService(
        out IUsersClient users,
        out ILogger<Auth0UserRegistrationService> logger)
    {
        users = Substitute.For<IUsersClient>();
        var managementApi = Substitute.For<IManagementApiClient>();
        managementApi.Users.Returns(users);
        logger = Substitute.For<ILogger<Auth0UserRegistrationService>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var options = Options.Create(new Auth0ManagementOptions
        {
            Domain = "tenant.eu.auth0.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            DatabaseConnectionName = ConnectionName,
        });
        return new Auth0UserRegistrationService(managementApi, options, logger);
    }

    private static void Refuse(IUsersClient users, Exception failure) =>
        users.CreateAsync(Arg.Any<CreateUserRequestContent>(), Arg.Any<RequestOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new WithRawResponseTask<CreateUserResponseContent>(
                Task.FromException<WithRawResponse<CreateUserResponseContent>>(failure)));

    private static WithRawResponseTask<CreateUserResponseContent> Created() =>
        new(Task.FromResult(new WithRawResponse<CreateUserResponseContent>
        {
            Data = new CreateUserResponseContent { UserId = "auth0|1234567890" },
            RawResponse = null!,
        }));

    private static async Task AssertUnavailableAndLogged(Result result, ILogger<Auth0UserRegistrationService> logger)
    {
        await Assert.That(result.IsFailed).IsTrue();
        await Assert.That(result.Errors[0].IsIdentityProviderUnavailable()).IsTrue();
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // The level is not known statically here, which is what CA1873 warns about; nothing is logged
    // by this call in the first place.
#pragma warning disable CA1873
    private static void NothingLogged(ILogger<Auth0UserRegistrationService> logger) =>
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CA1873
}
