using System.Net;
using Auth0.Core.Exceptions;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Users;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// Creates identities through the Auth0 Management API. The nickname, given name, family name and
/// picture are written as root attributes because that is where the first-login account provisioning
/// reads them back from - they are the contract between the two, see ADR-0001.
/// </summary>
public sealed class Auth0RegistrationService : IRegistrationService, IDisposable
{
    private readonly ManagementClient _client;
    private readonly Auth0RegistrationOptions _options;
    private readonly ILogger _logger;

    // An explicit constructor rather than the primary-constructor style its siblings use: the management
    // client is built out of the resolved options, and a field initializer cannot read another field.
    public Auth0RegistrationService(
        IOptions<Auth0RegistrationOptions> options,
        ILogger<Auth0RegistrationService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
        _client = new ManagementClient(new ManagementClientOptions
        {
            Domain = _options.Domain,
            TokenProvider = new ClientCredentialsTokenProvider(
                _options.Domain,
                _options.ManagementClientId,
                _options.ManagementClientSecret),
        });
    }

    public async Task<Result> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await _client.Users.CreateAsync(
                new CreateUserRequestContent
                {
                    Connection = _options.DatabaseConnectionName,
                    Email = request.Email,
                    Password = request.Password,
                    Nickname = request.Nickname,
                    GivenName = request.GivenName,
                    FamilyName = request.FamilyName,
                    Picture = request.AvatarUrl?.ToString(),
                    // The pair that makes Auth0 send its verification e-mail.
                    EmailVerified = false,
                    VerifyEmail = true,
                },
                cancellationToken: cancellationToken);

            return Result.Ok();
        }
        catch (ErrorApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return Errors.EmailAlreadyRegistered;
        }
        catch (ErrorApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest && IsPasswordRejection(ex))
        {
            return Errors.PasswordTooWeak(ex.ApiError?.Message ?? "The password does not satisfy the password policy.");
        }
        catch (ApiException ex)
        {
            // Anything else - a revoked M2M application, a throttled tenant, a malformed request -
            // is ours to fix, not the prospective user's to act on.
            _logger.LogIdentityCreationFailed(ex);
            return Errors.IdentityProviderUnavailable;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogIdentityCreationFailed(ex);
            return Errors.IdentityProviderUnavailable;
        }
    }

    public void Dispose() => _client.Dispose();

    private static bool IsPasswordRejection(ErrorApiException exception)
    {
        // Auth0 reports every password-policy failure with one of these error codes; the human-readable
        // message that travels with them is the policy text shown to the person.
        var errorCode = exception.ApiError?.ErrorCode;
        return string.Equals(errorCode, "invalid_password", StringComparison.Ordinal) ||
               string.Equals(errorCode, "PasswordStrengthError", StringComparison.Ordinal) ||
               string.Equals(errorCode, "PasswordDictionaryError", StringComparison.Ordinal) ||
               string.Equals(errorCode, "PasswordNoUserInfoError", StringComparison.Ordinal) ||
               string.Equals(errorCode, "PasswordHistoryError", StringComparison.Ordinal);
    }
}
