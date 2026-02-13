using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Migrations;

internal class DbInitializer(IServiceProvider serviceProvider, ILogger<DbInitializer> logger)
    : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider.ThrowIfNull();
    private readonly ILogger<DbInitializer> _logger = logger.ThrowIfNull();
    public const string ActivitySourceName = "Migrations";

    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

        using var activity = _activitySource.StartActivity("Initializing database", ActivityKind.Client);
        return InitializeDatabaseAsync(dbContext, stoppingToken);
    }

    public async Task InitializeDatabaseAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(dbContext.Database.MigrateAsync, cancellationToken);

        await SeedAsync(dbContext, cancellationToken);

        _logger.LogDatabaseInitializationCompleted(sw.ElapsedMilliseconds);
    }

    private async Task SeedAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogSeedingDatabase();

        // Add seeding logic here if needed
    }
}
