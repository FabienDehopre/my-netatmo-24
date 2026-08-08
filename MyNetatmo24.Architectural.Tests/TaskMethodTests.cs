using ArchUnitNET.Fluent;
using ArchUnitNET.TUnit;
using MyNetatmo24.Architectural.Tests.Conditions;

namespace MyNetatmo24.Architectural.Tests;

internal sealed class TaskMethodTests : ArchitecturalBaseTest
{
    [Test]
    [Retry(3)]
    public void Methods_returning_Task_have_CancellationToken_as_last_parameter()
    {
        ArchRuleDefinition.MethodMembers()
            .That()
            .HaveReturnType(typeof(Task), typeof(Task<>), typeof(ValueTask), typeof(ValueTask<>))
            .And()
            .HaveFullNameContaining("MyNetatmo24.")
            .And()
            .DoNotHaveFullNameContaining("Test")
            .And()
            // Task-composition extensions: they await a Task the caller already started and forward no work of their own.
            .DoNotHaveFullNameContaining("MyNetatmo24.SharedKernel.Results.ResultExtensions")
            .Should()
            .FollowCustomCondition(new LastParameterOfTypeCondition<CancellationToken>())
            .Check(Architecture);
    }
}
