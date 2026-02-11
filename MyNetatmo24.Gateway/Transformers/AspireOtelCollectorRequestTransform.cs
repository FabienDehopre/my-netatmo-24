using System.Diagnostics.CodeAnalysis;
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
                if (TryParse(header, out var headerName, out var headerValue))
                {
                    logger.LogAppendAspireOtelCollectorHeaders(headerName);
                    context.ProxyRequest.Headers.Remove(headerName);
                    context.ProxyRequest.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    private static bool TryParse(string header, [NotNullWhen(true)] out string? headerName, [NotNullWhen(true)] out string? headerValue)
    {
        headerName = null;
        headerValue = null;

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

