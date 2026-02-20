using FluentResults;
using Microsoft.Extensions.Caching.Hybrid;
using MyNetatmo24.Modules.AccountManagement.Application;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Results;

namespace MyNetatmo24.Modules.AccountManagement.Data;

internal static class AccountCache
{
    private static readonly IReadOnlyList<string> s_accountsTags = ["accounts"];

    internal static async Task<CachedResult<MyAccount.UserInfoDto>?> GetOrCreateAccountAsync(
        this HybridCache cache,
        string auth0Id,
        Func<CancellationToken, Task<Result<MyAccount.UserInfoDto>>> factory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(auth0Id);
        ArgumentNullException.ThrowIfNull(factory);

        var cachedResult = await cache.GetOrCreateAsync(
            $"user-info-{auth0Id}",
            async token =>
            {
                var result = await factory(token);
                return result.IsSuccess ? new CachedResult<MyAccount.UserInfoDto>(result) : null;
            },
            tags: s_accountsTags,
            cancellationToken: cancellationToken);

        return cachedResult;
    }
}
