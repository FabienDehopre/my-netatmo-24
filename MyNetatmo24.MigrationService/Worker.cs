using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.SharedKernel.Infrastructure;

namespace MyNetatmo24.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime applicationLifetime) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider.ThrowIfNull();
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime.ThrowIfNull();

    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();

            await RunMigrationsAsync(dbContext, stoppingToken);
            await SeedDataAsync(dbContext, stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        _applicationLifetime.StopApplication();
    }

    private async Task RunMigrationsAsync(AccountDbContext dbContext, CancellationToken stoppingToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails
            await dbContext.Database.MigrateAsync(stoppingToken);
        });
    }

    private async Task SeedDataAsync(AccountDbContext dbContext, CancellationToken stoppingToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);

            // TODO: Add seeding logic here, e.g.:
            await dbContext.SaveChangesAsync(stoppingToken);
            await transaction.CommitAsync(stoppingToken);
        });
    }
}
