using Microsoft.AspNetCore.Http;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.SharedKernel.Tests.Results;

public class FastEndpointsErrorTests
{
    [Test]
    public async Task Constructor_StoresStatusCodeAndMessage()
    {
        var error = new FastEndpointsError(StatusCodes.Status409Conflict, "conflict");

        await Assert.That(error.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
        await Assert.That(error.Message).IsEqualTo("conflict");
    }

    [Test]
    public async Task StatusCode_IsExposedThroughMetadata()
    {
        var error = new FastEndpointsError(StatusCodes.Status404NotFound, "missing");

        await Assert.That(error.Metadata).ContainsKey("StatusCode");
        await Assert.That(error.Metadata["StatusCode"]).IsEqualTo(StatusCodes.Status404NotFound);
    }
}
