using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.TUnit;

namespace MyNetatmo24.Architectural.Tests;

internal sealed class DomainLayerReferencesTests : ArchitecturalBaseTest
{
    [Test]
    [Retry(3)]
    public void Domain_does_not_reference_application_or_data()
    {
        ArchRuleDefinition.Types()
            .That()
            .Are(DomainLayer)
            .Should()
            .OnlyDependOn(ArchRuleDefinition.Types().That().HaveFullNameMatching("(Domain|StronglyTypedIds|System|Microsoft\\.CodeCoverage)"))
            .Because("Domain layer must only depend on itself")
            .Check(Architecture);
    }

    [Test]
    [Retry(3)]
    public void Domain_classes_have_empty_ctor()
    {
        ArchRuleDefinition.Classes()
            .That()
            .Are(DomainLayer)
            .Should()
            .FollowCustomCondition(clazz => clazz.GetConstructors().Any(c => !c.Parameters.Any()), "must have a default constructor", "A default constructor needs to be available for EF.")
            .Check(Architecture);
    }
}
