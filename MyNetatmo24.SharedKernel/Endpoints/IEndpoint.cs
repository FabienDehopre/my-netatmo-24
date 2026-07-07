using Microsoft.AspNetCore.Routing;

namespace MyNetatmo24.SharedKernel.Endpoints;

/// <summary>
/// Defines a contract for configuring an endpoint in the application.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Method used to configure the endpoint route.
    /// </summary>
    /// <param name="builder">The endpoint route builder.</param>
    void Configure(IEndpointRouteBuilder builder);
}
