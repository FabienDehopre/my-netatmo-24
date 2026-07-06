using FluentResults;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.SharedKernel.Tests.Results;

public class CachedResultTests
{
    [Test]
    public async Task Constructor_ExposesWrappedResult()
    {
        var inner = Result.Ok(42);

        var cached = new CachedResult<int>(inner);

        await Assert.That(cached.Result).IsEqualTo(inner);
        await Assert.That(cached.Result.Value).IsEqualTo(42);
    }
}
