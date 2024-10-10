using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// var cache = builder.AddRedis("cache");

var dbUser = builder.AddParameter("db-user", secret: true);
var dbPass = builder.AddParameter("db-pass", secret: true);
var db = builder.AddPostgres("postgres", dbUser, dbPass)
    .WithDataVolume()
    .AddDatabase("my-netatmo-24-db");

var backend = builder.AddProject<Projects.MyNetatmo24_Backend>("backend")
    .WithReference(db);

builder.AddProject<Projects.MyNetatmo24_MigrationService>("migrations")
    .WithReference(db);

var frontend = builder.AddNpmApp("frontend", "../MyNetatmo24.Frontend")
    .WithReference(backend)
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"] ??
                    builder.Configuration["AppHost:DefaultLaunchProfileName"]; // work around https://github.com/dotnet/aspire/issues/5093
if (builder.Environment.IsDevelopment() && launchProfile == "https")
{
    // Disable TLS certificate validation in development, see https://github.com/dotnet/aspire/issues/3324 for more details.
    frontend.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0");
}

builder.Build().Run();