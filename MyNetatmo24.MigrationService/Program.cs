using MyNetatmo24.MigrationService;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<AccountDbContext>("my-netatmo24-db");

var host = builder.Build();
host.Run();
