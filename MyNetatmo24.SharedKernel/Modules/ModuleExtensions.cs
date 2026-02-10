using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace MyNetatmo24.SharedKernel.Modules;

public static class ModuleExtensions
{
    private static readonly IModule[] s_modules = DiscoverModules();

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

    private static IModule[] DiscoverModules()
    {
        return [.. Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
            .Where(filePath => Path.GetFileName(filePath).StartsWith("MyNetatmo24.", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)
            .SelectMany(assembly => assembly.GetTypes()
                .Where(type => typeof(IModule).IsAssignableFrom(type) &&
                               type is { IsInterface: false, IsAbstract: false }))
            .Select(type => (IModule)Activator.CreateInstance(type)!)
            .ToList()
            .AsReadOnly()];
    }
}
