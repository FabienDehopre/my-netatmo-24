using Microsoft.Extensions.Logging;

namespace MyNetatmo24.SharedKernel.Logging;

public static partial class LoggerExtensions
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Forwarding telemetry for {ResourceName} to the collector")]
    public static partial void LogForwardingTelemetry(
        this ILogger logger,
        string resourceName);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Appending header {HeaderName} with value {HeaderValue} to request for Aspire Open Telemetry collector")]
    public static partial void LogAppendAspireOtelCollectorHeaders(
        this ILogger logger,
        string headerName,
        string headerValue);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Information,
        Message = "XSRF token added to response for request path: {RequestPath}")]
    public static partial void LogXsrfTokenAdded(
        this ILogger logger,
        string? requestPath);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Information,
        Message = "Validating antiforgery token for request path: {RequestPath}")]
    public static partial void LogValidatingAntiforgeryToken(
        this ILogger logger,
        string? requestPath);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Information,
        Message = "Adding bearer token to request headers for request path: {RequestPath}")]
    public static partial void LogAddingBearerToken(
        this ILogger logger,
        string? requestPath);

    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Warning,
        Message = "No {ResourceType} resource found")]
    public static partial void LogResourceNotFound(
        this ILogger logger,
        string resourceType);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "No {EndpointName} endpoint for the collector")]
    public static partial void LogEndpointNotFound(
        this ILogger logger,
        string endpointName);

    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Error,
        Message = "Antiforgery token validation failed for request path: {RequestPath}")]
    public static partial void LogAntiforgeryValidationFailed(
        this ILogger logger,
        Exception ex,
        string? requestPath);

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Error,
        Message = "Could not get access token: {GetUserAccessTokenError} for request path: {RequestPath}. {Error}")]
    public static partial void LogAccessTokenFailed(
        this ILogger logger,
        string? getUserAccessTokenError,
        string? requestPath,
        string? error);
}
