using MartinCostello.OpenApi;
using Microsoft.AspNetCore.Builder;
using  Microsoft.AspNetCore.Http;

namespace MyNetatmo24.SharedKernel.Endpoints;

public static class RouteHandlerBuilderExtensions
{
    extension(RouteHandlerBuilder builder)
    {
        public RouteHandlerBuilder Produces(
            int statusCode,
            string description,
            string? contentType = null,
            params string[] additionalContentTypes)
        {
            return builder.Produces(statusCode, contentType: contentType, additionalContentTypes: additionalContentTypes)
                .ProducesOpenApiResponse(statusCode, description);
        }

        public RouteHandlerBuilder Produces<T>(
            int statusCode,
            string description,
            string? contentType = null,
            params string[] additionalContentTypes)
        {
            return builder.Produces<T>(statusCode, contentType, additionalContentTypes)
                .ProducesOpenApiResponse(statusCode, description);
        }
    }
}
