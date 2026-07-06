using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;

/// <summary>
/// Builds a real, in-memory <see cref="HybridCache"/> instance (L1 only) for use as a
/// deterministic collaborator in tests.
/// </summary>
internal static class HybridCacheFactory
{
    public static HybridCache Create()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }
}
