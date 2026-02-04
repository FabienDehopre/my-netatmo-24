var builder = DistributedApplication.CreateBuilder(args);

// var cache = builder.AddRedis("cache");

var backend = builder.AddProject<Projects.MyNetatmo24_Backend>("backend");

var frontend = builder.AddJavaScriptApp("frontend", "../MyNetatmo24.Frontend")
    .WithReference(backend)
    .WithPnpm(install: true, installArgs: ["--frozen-lockfile"])
    .WithRunScript("start")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithEnvironment("APPLICATION", "MyNetatmo24.Frontend")
    .PublishAsDockerFile(configure: resource =>
    {
        resource.WithDockerfile("../", stage: "frontend-app");
    });

builder.Build().Run();
