using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Assembly = System.Reflection.Assembly;

namespace MyNetatmo24.Architectural.Tests;

/// <remarks>
///     Reusable sets of types are exposed as <see cref="IObjectProvider{T}" /> (built with <c>.As(...)</c>), never as a
///     <c>Given*Conjunction</c>. The fluent conjunction objects are mutable builders: sharing one between rules makes the
///     conditions of every rule accumulate on it, so the tests would check each other's conditions (and race when run in
///     parallel). An <see cref="IObjectProvider{T}" /> is only used to select the types a rule applies to, so it is safe
///     to share.
/// </remarks>
public abstract class ArchitecturalBaseTest
{
    protected static readonly Assembly AccountManagementAssembly = typeof(Modules.AccountManagement.AccountManagementModule).Assembly;
    protected static readonly Assembly SharedKernelAssembly = typeof(SharedKernel.Modules.IModule).Assembly;

    protected static readonly IObjectProvider<IType> DomainLayer =
        ArchRuleDefinition.Types().That().HaveFullNameContaining(".Domain").As("Domain Layer");
    protected static readonly IObjectProvider<IType> ApplicationLayer =
        ArchRuleDefinition.Types().That().HaveFullNameContaining(".Application").As("Application Layer");
    protected static readonly IObjectProvider<IType> HandlersLayer =
        ArchRuleDefinition.Types().That().HaveFullNameContaining(".Handlers").As("Handlers Layer");
    protected static readonly IObjectProvider<IType> InfrastructureLayer =
        ArchRuleDefinition.Types().That().HaveFullNameContaining(".Data").As("Infrastructure Layer");
    protected static readonly IObjectProvider<IType> SharedKernelLayer =
        ArchRuleDefinition.Types().That().ResideInAssembly("MyNetatmo24.SharedKernel").As("Shared Kernel Layer");
    protected static readonly IObjectProvider<IType> TestLayers =
        ArchRuleDefinition.Types().That().HaveFullNameContaining(".Tests").Or().HaveFullNameContaining(".IntegrationTests").As("Test Layers");

    // Declared after TestLayers because static field initializers run in declaration order.
    protected static readonly IObjectProvider<IType> Endpoints =
        ArchRuleDefinition.Classes().That().Are(ApplicationLayer).And().AreNotNested().And().AreNot(TestLayers).As("Endpoints");

    protected static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            AccountManagementAssembly,
            SharedKernelAssembly)
        .Build();
}
