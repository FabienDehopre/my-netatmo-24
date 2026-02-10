var builder = DistributedApplication.CreateBuilder(args);

var openIdConnectSettingsClientId = builder.AddParameter("OpenIdConnectSettingsClientId", secret: false);
var openIdConnectSettingsClientSecret = builder.AddParameter("OpenIdConnectSettingsClientSecret", secret: true);

var openTelemetryCollector = builder.AddOpenTelemetryCollector("../config/otel.yml");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
postgres
    .WithPgWeb(p => p.WithParentRelationship(postgres));

var database = postgres.AddDatabase("my-netatmo24-db");

var redis = builder.AddRedis("cache")
    .WithDataVolume();
redis
    .WithRedisInsight(p => p.WithParentRelationship(redis));

// var migrations = builder.AddProject<Projects.MyNetatmo24_Migrations>("migrations")
//     .WithReference(database)
//     .WithReference(redis)
//     .WaitFor(database)
//     .WithParentRelationship(database);
//
// if (builder.Environment.IsDevelopment())
// {
//     migrations.WithHttpCommand(path: "/reset-db", displayName: "Reset Database", commandOptions: new HttpCommandOptions
//     {
//         IconName = "DatabaseLightning",
//         ConfirmationMessage = "Are you sure you want to reset the database?",
//     });
//
//     migrations.WithHttpCommand(path: "/reseed-db", displayName: "Reseed Database", commandOptions: new HttpCommandOptions
//     {
//         IconName = "DatabaseLightning",
//         ConfirmationMessage = "Are you sure you want to reseed the database?",
//     });
// }

var apiService = builder.AddProject<Projects.MyNetatmo24_ApiService>("apiservice")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("Auth0__ClientId", openIdConnectSettingsClientId)
    .WithEnvironment("Auth0__ClientSecret", openIdConnectSettingsClientSecret)
    .WaitFor(database)
    // .WaitFor(migrations)
    .WaitFor(redis)
    .WithUrlForEndpoint("http", u => u.DisplayText = "API Documentation")
    .WithUrlForEndpoint("https", u => u.DisplayText = "API Documentation");

var frontend = builder.AddViteApp("angular-frontend", "../MyNetatmo24.Frontend")
    .WithPnpm(install: true, installArgs: ["--frozen-lockfile"])
    .PublishAsDockerFile(configure: resource =>
    {
        resource.WithDockerfile("../", stage: "frontend-app");
    });

var gateway = builder.AddProject<Projects.MyNetatmo24_Gateway>("gateway")
    .WithReference(apiService)
    .WithReference(frontend)
    .WithReference(openTelemetryCollector.Resource.HTTPEndpoint)
    .WithEnvironment("Auth0__ClientId", openIdConnectSettingsClientId)
    .WithEnvironment("Auth0__ClientSecret", openIdConnectSettingsClientSecret)
    .WaitFor(apiService)
    .WaitFor(frontend)
    .WaitFor(openTelemetryCollector)
    .WithUrlForEndpoint("http", u => u.DisplayText = "Open Application")
    .WithUrlForEndpoint("https", u => u.DisplayText = "Open Application")
    .WithExternalHttpEndpoints();

apiService.WithParentRelationship(gateway);
frontend.WithParentRelationship(gateway);

// if (builder.Environment.IsDevelopment())
// {
//     var playwright = builder
//         .AddJavaScriptApp("playwright", "../Sandbox.EndToEndTests")
//         .WithPnpm(install: false)
//         .WithRunScript("test")
//         .WithExplicitStart()
//         .WithPlaywrightRepeatCommand()
//         .ExcludeFromManifest()
//         .WithEnvironment("ASPIRE", "true")
//         .WithReference(gateway)
//         .WithReference(migrations)
//         .WithParentRelationship(angularApplication);
// }

// var dabServer = builder
//     .AddContainer("data-api", image: "azure-databases/data-api-builder", tag: "1.7.83-rc")
//     .WithImageRegistry("mcr.microsoft.com")
//     .WithBindMount(source: new FileInfo("dab-config.json").FullName, target: "/App/dab-config.json", isReadOnly: true)
//     .WithHttpEndpoint(port: 5000, targetPort: 5000)
//     .WithUrls(context =>
//     {
//         context.Urls.Clear();
//         context.Urls.Add(new() { Url = "/graphql", DisplayText = "Nitro", Endpoint = context.GetEndpoint("http") });
//         context.Urls.Add(new() { Url = "/swagger", DisplayText = "Swagger", Endpoint = context.GetEndpoint("http") });
//         context.Urls.Add(new() { Url = "/", DisplayText = "Health", Endpoint = context.GetEndpoint("http") });
//     })
//     .WithOtlpExporter()
//     .WithHttpHealthCheck("/")
//     .WithEnvironment("DAB_ENVIRONMENT", builder.Environment.IsDevelopment() ? "development" : "production")
//     .WithEnvironment("ConnectionString", database)
//     .WaitFor(database)
//     .WaitFor(migrations);

builder.AddDockerComposeEnvironment("MyNetatmo24")
    .WithDashboard(false);

#pragma warning disable ASPIREAZURE001
builder.AddAzureEnvironment();
#pragma warning restore ASPIREAZURE001

await builder.Build().RunAsync();
