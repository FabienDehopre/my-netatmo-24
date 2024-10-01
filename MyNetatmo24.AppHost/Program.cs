using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// var cache = builder.AddRedis("cache");

var backend = builder.AddProject<Projects.MyNetatmo24_Backend>("backend");
    //.WithReference(keycloak);

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