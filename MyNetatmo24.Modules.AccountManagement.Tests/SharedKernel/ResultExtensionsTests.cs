using FluentResults;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.Tests.SharedKernel;

public class ResultExtensionsTests
{
    [Test]
    public async Task BindWith_WhenWithResultIsTheFailedChainHead_DoesNotDuplicateItsReason()
    {
        var error = new Error("boom");
        var head = Result.Fail<string>(error);
        // Propagate the head's error down the chain, then feed the same head back in as withResult.
        var chain = Task.FromResult(head.Bind(value => Result.Ok(value)));

        var result = await chain.BindWith(head, (a, b) => Result.Ok(a + b));

        await Assert.That(result.IsFailed).IsTrue();
        await Assert.That(result.Reasons.Count).IsEqualTo(1);
        await Assert.That(result.Reasons).Contains(error);
    }

    [Test]
    public async Task BindWith_WhenChainAndWithResultCarryDistinctErrors_KeepsBoth()
    {
        var chainError = new Error("chain");
        var withError = new Error("with");
        var chain = Task.FromResult(Result.Fail<string>(chainError));

        var result = await chain.BindWith(Result.Fail<string>(withError), (a, b) => Result.Ok(a + b));

        await Assert.That(result.IsFailed).IsTrue();
        await Assert.That(result.Reasons).Contains(chainError).And.Contains(withError);
    }

    [Test]
    public async Task BindWith_WhenBothSucceed_AppliesBindAndPropagatesValue()
    {
        var chain = Task.FromResult(Result.Ok("a"));

        var result = await chain.BindWith(Result.Ok("b"), (a, b) => Result.Ok(a + b));

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo("ab");
    }
}
