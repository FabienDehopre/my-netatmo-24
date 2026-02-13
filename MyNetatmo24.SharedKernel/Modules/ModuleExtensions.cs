using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;

namespace MyNetatmo24.SharedKernel.Modules;

#pragma warning disable CA1708
public static class ModuleExtensions
#pragma warning restore CA1708
{
    private static readonly IModule[] s_modules = DiscoverModules();

    private static IModule[] DiscoverModules() =>
    [
        .. Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
            .Where(filePath => Path.GetFileName(filePath).StartsWith("MyNetatmo24.", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)
            .SelectMany(assembly => assembly.GetTypes()
                .Where(type => typeof(IModule).IsAssignableFrom(type) &&
                               type is { IsInterface: false, IsAbstract: false }))
            .Select(type => (IModule)Activator.CreateInstance(type)!)
            .ToList()
            .AsReadOnly()
    ];

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddModules()
        {
            foreach (var module in s_modules)
            {
                module.AddModule(builder);
            }

            return builder;
        }
    }

    extension(EndpointDiscoveryOptions options)
    {
        public EndpointDiscoveryOptions AddEndpointsAssemblies()
        {
            options.Assemblies = s_modules.Select(m => m.GetType().Assembly).ToArray();
            return options;
        }
    }
}
