using FluentResults;
using Microsoft.AspNetCore.Http;
using MyNetatmo24.Modules.AccountManagement;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.Tests;

public class ErrorsTests
{
    [Test]
    public async Task UserNotAuthenticated_HasUnauthorizedStatusCode()
    {
        var error = (EndpointError)Errors.UserNotAuthenticated;

        await Assert.That(error.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task AccountNotFound_HasNotFoundStatusCode()
    {
        var error = (EndpointError)Errors.AccountNotFound;

        await Assert.That(error.StatusCode).IsEqualTo(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task UserExists_IsFlaggedAsUserExistsError()
    {
        IReason reason = Errors.UserExists;

        await Assert.That(reason.IsUserExistsError()).IsTrue();
    }

    [Test]
    public async Task IsUserExistsError_ForOtherError_IsFalse()
    {
        IReason reason = Errors.AccountNotFound;

        await Assert.That(reason.IsUserExistsError()).IsFalse();
    }

    [Test]
    public async Task IsUserExistsError_ForSuccessReason_IsFalse()
    {
        IReason reason = new Success("all good");

        await Assert.That(reason.IsUserExistsError()).IsFalse();
    }

    [Test]
    public async Task UserDeleted_CarriesDeletedAtMetadata()
    {
        var deletedAt = DateTimeOffset.UtcNow;

        IReason reason = Errors.UserDeleted(deletedAt);

        await Assert.That(reason.GetDeletedAt()).IsEqualTo(deletedAt);
        await Assert.That(((EndpointError)reason).StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
    }

    [Test]
    public async Task GetDeletedAt_ForErrorWithoutMetadata_IsNull()
    {
        IReason reason = Errors.AccountNotFound;

        await Assert.That(reason.GetDeletedAt()).IsNull();
    }

    [Test]
    public async Task IsUserInfoNotFound_ForNotFoundError_IsTrue()
    {
        IReason reason = Errors.UserInfoNotFound;

        await Assert.That(reason.IsUserInfoNotFound()).IsTrue();
    }

    [Test]
    public async Task IsUserInfoNotFound_ForUnauthorizedError_IsFalse()
    {
        IReason reason = Errors.UserNotAuthenticated;

        await Assert.That(reason.IsUserInfoNotFound()).IsFalse();
    }

    [Test]
    public async Task IsUserNotAuthenticated_ForUnauthorizedError_IsTrue()
    {
        IReason reason = Errors.UserNotAuthenticated;

        await Assert.That(reason.IsUserNotAuthenticated()).IsTrue();
    }

    [Test]
    public async Task IsUserNotAuthenticated_ForNotFoundError_IsFalse()
    {
        IReason reason = Errors.AccountNotFound;

        await Assert.That(reason.IsUserNotAuthenticated()).IsFalse();
    }

    [Test]
    public async Task EmailAlreadyRegistered_IsFlaggedAndHasConflictStatusCode()
    {
        IReason reason = Errors.EmailAlreadyRegistered;

        await Assert.That(reason.IsEmailAlreadyRegistered()).IsTrue();
        await Assert.That(((EndpointError)reason).StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
    }

    [Test]
    public async Task IdentityProviderUnavailable_IsFlaggedAndHasBadGatewayStatusCode()
    {
        IReason reason = Errors.IdentityProviderUnavailable;

        await Assert.That(reason.IsIdentityProviderUnavailable()).IsTrue();
        await Assert.That(((EndpointError)reason).StatusCode).IsEqualTo(StatusCodes.Status502BadGateway);
    }

    [Test]
    public async Task PasswordTooWeak_CarriesThePolicyMessageMetadata()
    {
        const string policyMessage = "Password is too common.";

        IReason reason = Errors.PasswordTooWeak(policyMessage);

        await Assert.That(reason.GetPasswordPolicyMessage()).IsEqualTo(policyMessage);
        await Assert.That(((EndpointError)reason).StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task PasswordTooWeak_KeepsAStableDescriptionOfItsOwn()
    {
        // The policy message is the payload, not the identity of the error: it is worded by the
        // identity provider and changes with its configuration.
        var error = Errors.PasswordTooWeak("Password is too common.");

        await Assert.That(error.Message).DoesNotContain("Password is too common.");
    }

    [Test]
    public async Task GetPasswordPolicyMessage_ForErrorWithoutMetadata_IsNull()
    {
        IReason reason = Errors.EmailAlreadyRegistered;

        await Assert.That(reason.GetPasswordPolicyMessage()).IsNull();
    }

    [Test]
    public async Task TheRegistrationOutcomes_AreToldApartByIdentityNotByStatusCode()
    {
        // UserDeleted shares the 409 of EmailAlreadyRegistered, so a mapping keyed on the status
        // code alone would confuse the two.
        IReason userDeleted = Errors.UserDeleted(DateTimeOffset.UtcNow);

        await Assert.That(((EndpointError)userDeleted).StatusCode)
            .IsEqualTo(((EndpointError)Errors.EmailAlreadyRegistered).StatusCode);
        await Assert.That(userDeleted.IsEmailAlreadyRegistered()).IsFalse();
        await Assert.That(userDeleted.IsIdentityProviderUnavailable()).IsFalse();
    }
}
