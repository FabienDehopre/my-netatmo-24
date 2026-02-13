using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyNetatmo24.Migrations;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.ServiceDefaults;
using MyNetatmo24.SharedKernel.Infrastructure;
using Respawn;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(DbInitializer.ActivitySourceName));
builder.Services.AddDbContext<AccountDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString(Constants.DatabaseName), builder =>
    {
        builder.EnableRetryOnFailure();
        builder.MigrationsAssembly(typeof(DbInitializer).Assembly.GetName().Name);
    });
});

builder.Services.AddSingleton<DbInitializer>();
builder.Services.AddHostedService<DbInitializer>();
builder.Services.AddHealthChecks()
    .AddCheck<DbInitializerHealthCheck>("DbInitializer", null);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapPost("/reset-db", async (AccountDbContext dbContext, DbInitializer dbInitializer, CancellationToken cancellationToken) =>
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbInitializer.InitializeDatabaseAsync(dbContext, cancellationToken);
    });

    app.MapPost("/reseed-db", async (AccountDbContext dbContext, DbInitializer dbInitializer, CancellationToken cancellationToken) =>
    {
        await using var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToExclude = ["public"],
            DbAdapter = DbAdapter.Postgres
        });
        await respawner.ResetAsync(connection);
        await dbInitializer.InitializeDatabaseAsync(dbContext, cancellationToken);
    });
}

app.MapDefaultEndpoints();

await app.RunAsync();
