// extern alias migrations;
using System.Diagnostics.CodeAnalysis;
using ApiServiceSDK;
using Microsoft.EntityFrameworkCore;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using MyNetatmo24.Modules.AccountManagement.Data;
using Npgsql;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace MyNetatmo24.Modules.AccountManagement.IntegrationTests.Setup;

[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "PostgreSQL database identifiers cannot be parameterized; database names are internally generated and quoted.")]
public sealed class AccountApiWebApplicationFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    private const string TemplateDatabaseName = "my-netatmo24-api-service-template";

    private readonly Lock _templateInitializationLock = new();
    private Task? _templateInitializationTask;
    private bool _templateInitialized;

    [ClassDataSource<DatabaseContainer>(Shared = SharedType.PerTestSession)]
    public DatabaseContainer Database { get; init; } = null!;

    [ClassDataSource<CacheContainer>(Shared = SharedType.PerTestSession)]
    public CacheContainer Cache { get; init; } = null!;

    private string UnpooledMaintenanceDatabaseConnectionString
        => BuildConnectionString("postgres", disablePooling: true);

    private string UnpooledTemplateDatabaseConnectionString
        => BuildConnectionString(TemplateDatabaseName, disablePooling: true);

    public async Task InitializeAsync()
        => await EnsureTemplateInitializedAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // TODO
    }

    public static ApiClient CreateApiClient(HttpClient httpClient)
    {
        var authProvider = new AnonymousAuthenticationProvider();
        using var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        return new ApiClient(adapter);
    }

    internal async Task<string> CreateIsolatedDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        await EnsureTemplateInitializedAsync(cancellationToken);

        await using var connection = new NpgsqlConnection(UnpooledMaintenanceDatabaseConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)} TEMPLATE {QuoteIdentifier(TemplateDatabaseName)};";
        await command.ExecuteNonQueryAsync(cancellationToken);

        return BuildConnectionString(databaseName);
    }

    internal async Task DropIsolatedDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(UnpooledMaintenanceDatabaseConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)};";
        await dropCommand.ExecuteNonQueryAsync(cancellationToken);

        NpgsqlConnection.ClearAllPools();
    }

    private Task EnsureTemplateInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_templateInitialized)
        {
            return Task.CompletedTask;
        }

        lock (_templateInitializationLock)
        {
            _templateInitializationTask ??= InitializeTemplateAsync(cancellationToken);
            return _templateInitializationTask;
        }
    }

    private async Task InitializeTemplateAsync(CancellationToken cancellationToken)
    {
        await EnsureTemplateDatabaseAsync(cancellationToken);
        await MigrateAsync(UnpooledTemplateDatabaseConnectionString, cancellationToken);
        NpgsqlConnection.ClearAllPools();
        _templateInitialized = true;
    }

    private async Task EnsureTemplateDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(UnpooledMaintenanceDatabaseConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(TemplateDatabaseName)} WITH (FORCE);";
        await dropCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(TemplateDatabaseName)};";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken)
    {
        // var options = new DbContextOptionsBuilder<AccountDbContext>()
        //     .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(migrations.Program).Assembly))
        //     .Options;
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(AccountDbContext).Assembly))
            .Options;

        await using var dbContext = new AccountDbContext(options, TimeProvider.System);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private string BuildConnectionString(string databaseName, bool disablePooling = false)
    {
        var builder = new NpgsqlConnectionStringBuilder(Database.Container.GetConnectionString())
        {
            Database = databaseName, Pooling = !disablePooling
        };

        return builder.ConnectionString;
    }

    private static string QuoteIdentifier(string identifier)
    {
        return '"' + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
    }
}
