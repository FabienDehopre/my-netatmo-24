using Auth0.ManagementApi;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;

/// <summary>
/// Creates the identity of a prospective user with the Auth0 Management API.
/// </summary>
/// <remarks>
/// This is the production implementation of <see cref="IUserRegistrationService"/> and the only
/// place the Auth0 SDK, its credentials and its error shapes are visible. It is a pass-through: it
/// translates a <see cref="RegistrationData"/> into one create-user call and that call's answer
/// back into the outcomes of the seam, and holds no logic of its own worth testing.
/// </remarks>
/// <param name="managementApi">The Management API of the identity provider.</param>
/// <param name="options">The settings the Management API is reached with.</param>
/// <param name="logger">The logger the unexpected refusals are reported to.</param>
public sealed class Auth0UserRegistrationService(
    IManagementApiClient managementApi,
    IOptions<Auth0ManagementOptions> options,
    ILogger<Auth0UserRegistrationService> logger) : IUserRegistrationService
{
    private readonly IManagementApiClient _managementApi = managementApi.ThrowIfNull();
    private readonly Auth0ManagementOptions _options = options.ThrowIfNull().Value;
    private readonly ILogger _logger = logger.ThrowIfNull();

    /// <inheritdoc/>
    public async Task<Result> RegisterAsync(RegistrationData registration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var request = new CreateUserRequestContent
        {
            Connection = _options.DatabaseConnectionName,
            Email = registration.Email,
            Password = registration.Password,
            // The profile is written as root attributes rather than as user metadata, because that
            // is where the /userinfo read of the first authenticated call looks for it when it
            // provisions the account (see docs/adr/0001-registration-creates-identity-only.md).
            Nickname = registration.Nickname,
            GivenName = registration.GivenName,
            FamilyName = registration.FamilyName,
            Picture = registration.AvatarUrl?.AbsoluteUri,
            // The prospective user has not proven yet that the address is theirs, and asking Auth0
            // to verify it is what sends them the verification e-mail.
            EmailVerified = false,
            VerifyEmail = true,
        };

        try
        {
            await _managementApi.Users.CreateAsync(request, cancellationToken: cancellationToken);
            return Result.Ok();
        }
        catch (ManagementApiException conflict) when (conflict.StatusCode == StatusCodes.Status409Conflict)
        {
            // Creating a user is the only thing this service asks of Auth0, so the only conflict it
            // can answer with is that the e-mail address already identifies someone.
            return Result.Fail(Errors.EmailAlreadyRegistered);
        }
        catch (ManagementApiException refusal)
            when (refusal.StatusCode == StatusCodes.Status400BadRequest && IsPasswordPolicyRefusal(refusal))
        {
            return Result.Fail(Errors.PasswordTooWeak(ToPolicyMessage(refusal.Message)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller gave up, which says nothing about the identity provider.
            throw;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception failure)
        {
            // Every other way this can end -- a Management API error the outcomes have no room for,
            // a machine-to-machine token that could not be obtained, a timeout, a broken
            // connection -- leaves whether the identity was created unknown, which is exactly what
            // the provider-unavailable outcome reports. The detail is only useful to an operator,
            // so it goes to the log rather than to the prospective user.
            _logger.LogUserRegistrationFailed(failure);
            return Result.Fail(Errors.IdentityProviderUnavailable);
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Tells whether a rejected create-user call was rejected by the password policy, as opposed to
    /// by anything else Auth0 validates on the request.
    /// </summary>
    /// <param name="refusal">The rejection Auth0 answered the create-user call with.</param>
    /// <returns>
    /// <see langword="true"/> when the password policy refused the password; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Auth0 reports every password rule it enforces -- strength, dictionary, personal information
    /// and history -- as a message opening with the name of the rule that refused
    /// (<c>PasswordStrengthError</c>, <c>PasswordDictionaryError</c>, <c>PasswordNoUserInfoError</c>
    /// and <c>PasswordHistoryError</c>), which is the one thing they have in common. The message is
    /// all there is to go on: the tenant answers a refused password with
    /// <c>{"statusCode":400,"error":"Bad Request","message":"PasswordStrengthError: Password is too
    /// weak"}</c>, carrying no machine-readable code to tell it apart from the other things a
    /// create-user call can be rejected over.
    /// </remarks>
    private static bool IsPasswordPolicyRefusal(ManagementApiException refusal) =>
        refusal.Message.StartsWith("Password", StringComparison.Ordinal);

    /// <summary>
    /// Turns the message of a password refusal into the message the prospective user reads.
    /// </summary>
    /// <param name="message">The message Auth0 refused the password with.</param>
    /// <returns>The wording of <paramref name="message"/>, without the name of the refusing rule.</returns>
    /// <remarks>
    /// The wording is Auth0's, so that the password policy has a single source of truth; only the
    /// name of the rule it opens with is dropped, since <c>"PasswordStrengthError: Password is too
    /// weak"</c> names an SDK error class at someone who is only trying to pick a password.
    /// </remarks>
    private static string ToPolicyMessage(string message)
    {
        var separator = message.IndexOf(": ", StringComparison.Ordinal);
        return separator < 0 ? message : message[(separator + 2)..];
    }
}
