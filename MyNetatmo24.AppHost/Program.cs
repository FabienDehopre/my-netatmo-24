var builder = DistributedApplication.CreateBuilder(args);

var openIdConnectSettingsClientId = builder.AddParameter("OpenIdConnectSettingsClientId", false);
var openIdConnectSettingsClientSecret = builder.AddParameter("OpenIdConnectSettingsClientSecret", true);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
postgres.WithPgWeb(p => p.WithParentRelationship(postgres))
    .WithPgAdmin(p => p.WithParentRelationship(postgres));

var database = postgres.AddDatabase(Constants.DatabaseName);

var redis = builder.AddRedis("cache")
    .WithDataVolume();
redis
    .WithRedisInsight(p => p.WithParentRelationship(redis));

var migrations = builder.AddProject<Projects.MyNetatmo24_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database)
    .WithParentRelationship(database);

var apiService = builder.AddProject<Projects.MyNetatmo24_ApiService>("apiservice")
    .WithReference(database)
    .WithReference(redis)
    .WithReference(migrations)
    .WithEnvironment("Auth0__ClientId", openIdConnectSettingsClientId)
    .WithEnvironment("Auth0__ClientSecret", openIdConnectSettingsClientSecret)
    .WaitFor(database)
    .WaitForCompletion(migrations)
    .WaitFor(redis)
    .WithUrlForEndpoint("http", u => u.DisplayText = "API Documentation")
    .WithUrlForEndpoint("https", u => u.DisplayText = "API Documentation");

var frontend = builder.AddViteApp("angular-frontend", "../MyNetatmo24.Frontend")
    .WithPnpm(installArgs: ["--frozen-lockfile"])
    .PublishAsDockerFile(resource => resource.WithDockerfile("../", stage: "frontend-app"));

var gateway = builder.AddProject<Projects.MyNetatmo24_Gateway>("gateway")
    .WithReference(apiService)
    .WithReference(frontend)
    .WithEnvironment("Auth0__ClientId", openIdConnectSettingsClientId)
    .WithEnvironment("Auth0__ClientSecret", openIdConnectSettingsClientSecret)
    .WaitFor(apiService)
    .WaitFor(frontend)
    .WithUrlForEndpoint("http", u => u.DisplayText = "Open Application")
    .WithUrlForEndpoint("https", u => u.DisplayText = "Open Application")
    .WithExternalHttpEndpoints();

if (builder.Environment.IsDevelopment())
{
    var otlpHttpEndpoint = builder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"];
    if (!string.IsNullOrEmpty(otlpHttpEndpoint))
    {
        gateway.WithEnvironment("services__otelcollector__http__0", otlpHttpEndpoint);
    }
}
// else
// {
//     // TODO: set services__otelcollector__http__0 with the actual OTLP endpoint URL in production
// }

apiService.WithParentRelationship(gateway);
frontend.WithParentRelationship(gateway);

// if (builder.Environment.IsDevelopment())
// {
//     var playwright = builder
//         .AddJavaScriptApp("playwright", "../MyNetatmo24.EndToEndTests")
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
//     .WithEnvironment("ConnectionString", container)
//     .WaitFor(container)
//     .WaitFor(migrations);

await builder.Build().RunAsync();
