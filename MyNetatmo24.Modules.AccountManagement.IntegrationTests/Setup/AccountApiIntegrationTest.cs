using System.Net;
using ApiServiceSDK;
using Microsoft.EntityFrameworkCore;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.Modules.AccountManagement.HttpClients.Auth0;
using MyNetatmo24.Modules.AccountManagement.RateLimiting;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.StronglyTypedIds;
using TUnit.AspNetCore;
using TUnit.AspNetCore.Extensions;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

public abstract class AccountApiIntegrationTest : WebApplicationTest<AccountApiWebApplicationFactory, Program>, IAsyncDisposable
{
    /// <summary>
    /// Hands out the forwarded client address of each test. The rate limiter of the application
    /// counts per client address and its partitions outlive a single test, so two tests sharing an
    /// address would spend each other's budget.
    /// </summary>
    private static int s_nextClientIp;

    private readonly List<IDisposable> _disposables = [];
    private string? _databaseName;
    private string? _databaseConnectionString;

    /// <summary>
    /// The Auth0 user id of the authenticated user for this test. Unique per test so that
    /// database rows and FusionCache entries (keyed by auth0 id in the session-shared Redis)
    /// never leak between tests.
    /// </summary>
    protected string Auth0Id { get; private set; } = null!;

    /// <summary>
    /// The client address every request of this test claims to come from, as the Gateway proxy
    /// would forward it. Unique per test.
    /// </summary>
    protected string ClientIp { get; private set; } = null!;

    protected FakeUserInfoService UserInfoService { get; } = new();

    protected FakeUserRegistrationService UserRegistrationService { get; } = new();

    protected override async Task SetupAsync()
    {
        Auth0Id = GetIsolatedName("auth0");
        ClientIp = NextClientIp();
        _databaseName = GetIsolatedName("MyNetatmo24");
        _databaseConnectionString = await GlobalFactory.CreateIsolatedDatabaseAsync(_databaseName);
    }

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting($"ConnectionStrings:{Constants.DatabaseName}", _databaseConnectionString);
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ReplaceService<IUserInfoService>(UserInfoService);
        services.ReplaceService<IUserRegistrationService>(UserRegistrationService);
    }

    protected ApiClient CreateAuthenticatedApiClient()
    {
        var httpClient = CreateHttpClient();
        httpClient.DefaultRequestHeaders.Add(AccountApiAuthenticationHandler.Auth0IdHeaderName, Auth0Id);
        return CreateApiClient(httpClient);
    }

    protected ApiClient CreateAnonymousApiClient() => CreateApiClient(CreateHttpClient());

    /// <summary>
    /// Creates a raw client whose requests carry the forwarded client address of this test, the
    /// way the Gateway proxy sets it.
    /// </summary>
    /// <param name="clientIp">
    /// The address to claim instead of the one of this test, for tests that need to be two clients.
    /// </param>
    /// <returns>The client. The caller owns it.</returns>
    protected HttpClient CreateHttpClient(string? clientIp = null)
    {
        var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add(HttpContextExtensions.ForwardedForHeaderName, clientIp ?? ClientIp);
        return httpClient;
    }

    protected async Task<Account> SeedAccountAsync(DateTimeOffset? deletedAt = null)
    {
        var account = Account.Create(
            AccountId.New(),
            Auth0Id,
            FakeUserInfoService.DefaultUserInfo.Nickname,
            FullName.From(
                FakeUserInfoService.DefaultUserInfo.GivenName,
                FakeUserInfoService.DefaultUserInfo.FamilyName));
        account.SetAvatarUrl(FakeUserInfoService.DefaultUserInfo.Picture!);
        account.DeletedAt = deletedAt;

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        dbContext.Add(account);
        await dbContext.SaveChangesAsync();

        return account;
    }

    protected async Task<Account?> FindAccountAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        return await dbContext.Set<Account>()
            .AsNoTracking()
            .IgnoreQueryFilters([Constants.SoftDeleteFilter])
            .SingleOrDefaultAsync(a => a.Auth0Id == Auth0Id);
    }

    protected async Task UpdateAccountAsync(Action<Account> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        var account = await dbContext.Set<Account>()
            .IgnoreQueryFilters([Constants.SoftDeleteFilter])
            .SingleAsync(a => a.Auth0Id == Auth0Id);
        mutate(account);
        await dbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        if (_databaseName is not null)
        {
            await GlobalFactory.DropIsolatedDatabaseAsync(_databaseName);
        }
    }

    /// <summary>
    /// Hands out the next unused client address, for a test that needs to be more than one client.
    /// </summary>
    /// <returns>An address no other test uses.</returns>
    protected static string NextClientIp()
    {
        var index = Interlocked.Increment(ref s_nextClientIp);
        return new IPAddress([10, (byte)(index >> 16), (byte)(index >> 8), (byte)index]).ToString();
    }

    private ApiClient CreateApiClient(HttpClient httpClient)
    {
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
        _disposables.Add(adapter);
        return new ApiClient(adapter);
    }
}
