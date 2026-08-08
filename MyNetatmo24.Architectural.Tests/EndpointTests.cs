using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.TUnit;

namespace MyNetatmo24.Architectural.Tests;

internal sealed class EndpointTests : ArchitecturalBaseTest
{
    [Test]
    [Retry(3)]
    public void Endpoints_are_static()
    {
        ArchRuleDefinition.Classes()
            .That()
            .Are(Endpoints)
            .Should()
            .BeAbstract()
            .AndShould()
            .BeSealed()
            .Because("Endpoints are static classes: they are stateless and only expose their configuration and handler as static methods")
            .Check(Architecture);
    }

    [Test]
    [Retry(3)]
    public void Endpoints_expose_a_Configure_method()
    {
        ArchRuleDefinition.Classes()
            .That()
            .Are(Endpoints)
            .Should()
            .FollowCustomCondition(
                clazz => clazz.GetMethodMembers().Any(m => m.NameStartsWith("Configure")),
                "have a Configure method",
                "The endpoint needs a Configure method to map itself on the route builder.")
            .Check(Architecture);
    }

    [Test]
    [Retry(3)]
    public void Endpoints_expose_a_HandleAsync_method()
    {
        ArchRuleDefinition.Classes()
            .That()
            .Are(Endpoints)
            .Should()
            .FollowCustomCondition(
                clazz => clazz.GetMethodMembers().Any(m => m.NameStartsWith("HandleAsync")),
                "have a HandleAsync method",
                "The endpoint needs a HandleAsync method to process requests.")
            .Check(Architecture);
    }

    [Test]
    [Retry(3)]
    public void Endpoint_contracts_are_nested_immutable_sealed_records_named_Dto()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreNestedIn(Endpoints)
            .Should()
            .HaveNameEndingWith("Dto")
            .AndShould()
            .BeRecord()
            .AndShould()
            .BeImmutable()
            .AndShould()
            .BeSealed()
            .Because("Request and response contracts are immutable data transfer objects nested in their endpoint")
            .Check(Architecture);
    }
}
