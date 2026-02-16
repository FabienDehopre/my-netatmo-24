var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

// builder.AddNpgsqlDbContext<AccountDbContext>("my-netatmo24-db");
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString(Constants.DatabaseName)));
builder.EnrichNpgsqlDbContext<AccountDbContext>(config => config.DisableRetry = true);

var host = builder.Build();
host.Run();
