var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MyNetatmo24_Backend>("Backend");

builder.Build().Run();