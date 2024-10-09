using MyNetatmo24.Infrastructure.Data;
using MyNetatmo24.MigrationService;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));
builder.AddNpgsqlDbContext<MyNetatmo24DbContext>("my-netatmo-24-db");

var host = builder.Build();
host.Run();