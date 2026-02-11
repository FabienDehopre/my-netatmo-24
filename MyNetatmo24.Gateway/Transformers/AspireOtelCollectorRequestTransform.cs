using Yarp.ReverseProxy.Transforms;
using MyNetatmo24.SharedKernel.Logging;

namespace MyNetatmo24.Gateway.Transformers;

internal sealed class AspireOtelCollectorRequestTransform(IConfiguration configuration, ILogger<AspireOtelCollectorRequestTransform> logger) : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context.HttpContext.Request.Path == "/v1/traces")
        {
            var headers = configuration["OTEL_EXPORTER_OTLP_HEADERS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            foreach (var header in headers)
            {
                if (header.TryDeconstruct(out var headerName, out var headerValue))
                {
                    logger.LogAppendAspireOtelCollectorHeaders(headerName);
                    context.ProxyRequest.Headers.Remove(headerName);
                    context.ProxyRequest.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
        }

        return ValueTask.CompletedTask;
    }
}

internal static class HeaderSplitDeconstruct
{
    public static bool TryDeconstruct(this string header, out string headerName, out string headerValue)
    {
        headerName = string.Empty;
        headerValue = string.Empty;

        var parts = header.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        headerName = parts[0];
        headerValue = parts[1];
        return true;
    }
}
