using MyNetatmo24.SharedKernel.Infrastructure;
using TUnit.AspNetCore;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

public abstract class AccountApiIntegrationTest : WebApplicationTest<AccountApiWebApplicationFactory, Program>, IAsyncDisposable
{
    private string? _databaseName;
    private string? _databaseConnectionString;

    protected override async Task SetupAsync()
    {
        _databaseName = GetIsolatedName("MyNetatmo24");
        _databaseConnectionString = await GlobalFactory.CreateIsolatedDatabaseAsync(_databaseName);
    }

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting($"ConnectionStrings:{Constants.DatabaseName}", _databaseConnectionString);
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{Constants.DatabaseName}"] = _databaseConnectionString,
            });
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (_databaseName is not null)
        {
            await GlobalFactory.DropIsolatedDatabaseAsync(_databaseName);
        }
    }
}
