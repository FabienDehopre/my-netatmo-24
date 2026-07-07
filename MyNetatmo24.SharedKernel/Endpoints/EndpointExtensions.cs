using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace MyNetatmo24.SharedKernel.Endpoints;

public static class EndpointExtensions
{
    extension(IEndpointRouteBuilder endpointRouteBuilder)
    {
        public void RegisterAssemblyEndpoints()
        {
            var assembly = Assembly.GetCallingAssembly();
            var endpointType = typeof(IEndpoint);
            var endpoints = assembly.GetTypes()
                .Where(type => endpointType.IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false })
                .Select(type => (IEndpoint?)Activator.CreateInstance(type));
            foreach (var endpoint in endpoints)
            {
                endpoint?.Configure(endpointRouteBuilder);
            }
        }
    }
}
