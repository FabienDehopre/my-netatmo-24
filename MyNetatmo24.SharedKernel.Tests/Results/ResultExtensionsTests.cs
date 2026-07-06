using FluentResults;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.SharedKernel.Tests.Results;

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

    [Test]
    public async Task BindWith_WhenChainFails_DoesNotInvokeBind()
    {
        var invoked = false;
        var chain = Task.FromResult(Result.Fail<string>("boom"));

        var result = await chain.BindWith(Result.Ok("b"), (a, b) =>
        {
            invoked = true;
            return Result.Ok(a + b);
        });

        await Assert.That(invoked).IsFalse();
        await Assert.That(result.IsFailed).IsTrue();
    }

    [Test]
    public async Task BindWith_WithNullWithResult_Throws()
    {
        var chain = Task.FromResult(Result.Ok("a"));

        await Assert.That(async () => await chain.BindWith<string, string, string>(null!, (a, b) => Result.Ok(a + b)))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task BindWith_WithNullBind_Throws()
    {
        var chain = Task.FromResult(Result.Ok("a"));

        await Assert.That(async () => await chain.BindWith<string, string, string>(Result.Ok("b"), null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Compensate_WhenResultSucceeds_ReturnsSameResultWithoutCompensating()
    {
        var invoked = false;
        var chain = Task.FromResult(Result.Ok());

        var result = await chain.Compensate(r =>
        {
            invoked = true;
            return Result.Ok();
        });

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(invoked).IsFalse();
    }

    [Test]
    public async Task Compensate_WhenResultFails_ReturnsCompensatorResult()
    {
        var chain = Task.FromResult(Result.Fail("boom"));

        var result = await chain.Compensate(_ => Result.Ok());

        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task Compensate_WhenCompensatorReturnsFailure_PropagatesIt()
    {
        var chain = Task.FromResult(Result.Fail("boom"));

        var result = await chain.Compensate(r => r);

        await Assert.That(result.IsFailed).IsTrue();
    }

    [Test]
    public async Task Compensate_WithNullCompensator_Throws()
    {
        var chain = Task.FromResult(Result.Fail("boom"));

        await Assert.That(async () => await chain.Compensate(null!)).Throws<ArgumentNullException>();
    }
}
