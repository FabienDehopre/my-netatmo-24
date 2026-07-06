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
        var error = (FastEndpointsError)Errors.UserNotAuthenticated;

        await Assert.That(error.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task AccountNotFound_HasNotFoundStatusCode()
    {
        var error = (FastEndpointsError)Errors.AccountNotFound;

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
        await Assert.That(((FastEndpointsError)reason).StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
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
}
