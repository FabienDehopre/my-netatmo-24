using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyNetatmo24.SharedKernel.Infrastructure;

namespace MyNetatmo24.Migrations;

internal class DbInitializerHealthCheck(DbInitializer dbInitializer) : IHealthCheck
{
    private readonly DbInitializer _dbInitializer = dbInitializer.ThrowIfNull();

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var task = _dbInitializer.ExecuteTask;
        return task switch
        {
            null => Task.FromResult(HealthCheckResult.Healthy()),
            { IsCompletedSuccessfully: true } => Task.FromResult(HealthCheckResult.Healthy()),
            { IsFaulted: true } => Task.FromResult(HealthCheckResult.Unhealthy(task.Exception?.InnerException?.Message, task.Exception)),
            { IsCanceled: true } => Task.FromResult(HealthCheckResult.Unhealthy("Database initialization was canceled")),
            _ => Task.FromResult(HealthCheckResult.Degraded("Database initialization is still in progress"))
        };
    }
}
