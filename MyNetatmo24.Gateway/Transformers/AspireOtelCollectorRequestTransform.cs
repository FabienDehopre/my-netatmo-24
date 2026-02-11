using Yarp.ReverseProxy.Transforms;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Gateway.Transformers;

internal sealed class AspireOtelCollectorRequestTransform(IConfiguration configuration, ILogger<AspireOtelCollectorRequestTransform> logger) : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context.HttpContext.Request.Path == "/v1/traces")
        {
            var headers = configuration["OTEL_EXPORTER_OTLP_HEADERS"]?.Split(',') ?? [];
            foreach (var header in headers)
            {
                var (headerName, headerValue) = header;
                logger.LogAppendAspireOtelCollectorHeaders(headerName, headerValue);
                context.ProxyRequest.Headers.Add(headerName, headerValue);
            }
        }

        return ValueTask.CompletedTask;
    }
}

internal static class HeaderSplitDeconstruct
{
    public static void Deconstruct(this string header, out string headerName, out string headerValue)
    {
        var parts = header.Split('=');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid header: {header}");
        }

        headerName = parts[0];
        headerValue = parts[1];
    }
}
