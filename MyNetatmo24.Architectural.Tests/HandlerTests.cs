using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.TUnit;

namespace MyNetatmo24.Architectural.Tests;

internal sealed class HandlerTests : ArchitecturalBaseTest
{
    [Test]
    [Retry(3)]
    public void Handlers_reside_in_handler_namespace_with_a_Handle_method()
    {
        ArchRuleDefinition.Classes()
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreNot(TestLayers)
            .Should()
            .Be(HandlersLayer)
            .Because("All handlers must reside in the Handlers namespace")
            .AndShould()
            .FollowCustomCondition(
                clazz => clazz.Members.Any(m => m.NameStartsWith("Handle")),
                "handler must have a Handle method",
                "The handler needs to implement a Handle method to process messages.")
            // No Wolverine message handler exists yet: keep the rule green until the first one lands, instead of failing
            // on the empty set that ArchUnitNET rejects by default.
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }
}
