using Microsoft.AspNetCore.Builder;

namespace MyNetatmo24.SharedKernel.Modules;

public interface IModule
{
    WebApplicationBuilder AddModule(WebApplicationBuilder builder);
    // WebApplication UseModule(WebApplication app);
}
