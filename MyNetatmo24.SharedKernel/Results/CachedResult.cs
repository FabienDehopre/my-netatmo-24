using FluentResults;

namespace MyNetatmo24.SharedKernel.Results;

/// <summary>
/// Wraps a successful Result so that HybridCache can distinguish
/// between "not cached yet" (null) and "cached success" (this type).
/// HybridCache does not store null, which is how failures are excluded.
/// </summary>
public sealed class CachedResult<T>(Result<T> result)
{
    public Result<T> Result { get; } = result;
}
