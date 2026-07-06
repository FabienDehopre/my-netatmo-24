using Microsoft.Extensions.Caching.Hybrid;

namespace MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;

/// <summary>
/// A <see cref="HybridCache"/> test double that always invokes the factory and never stores
/// anything. It isolates a unit under test from real caching and, crucially, from the
/// serialization round-trip a real cache performs — so cached values keep full fidelity.
/// </summary>
internal sealed class PassThroughHybridCache : HybridCache
{
    public override ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default) =>
        factory(state, cancellationToken);

    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    public override ValueTask RemoveByTagAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
