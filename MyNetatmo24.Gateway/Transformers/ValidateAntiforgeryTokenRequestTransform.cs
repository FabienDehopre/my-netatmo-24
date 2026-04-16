using Microsoft.AspNetCore.Antiforgery;
using MyNetatmo24.SharedKernel.Logging;
using Yarp.ReverseProxy.Transforms;

namespace MyNetatmo24.Gateway.Transformers;

internal sealed class ValidateAntiforgeryTokenRequestTransform(
    IAntiforgery antiforgery,
    ILogger<ValidateAntiforgeryTokenRequestTransform> logger) : RequestTransform
{
    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context.HttpContext.Request.Method == HttpMethod.Get.Method ||
            context.HttpContext.Request.Method == HttpMethod.Head.Method ||
            context.HttpContext.Request.Method == HttpMethod.Options.Method ||
            context.HttpContext.Request.Method == HttpMethod.Trace.Method)
        {
            return;
        }

        // Skip antiforgery validation for protobuf payloads because these requests do not
        // use the standard form or header-based token flow expected by ASP.NET Core antiforgery,
        // which would make valid protobuf requests fail validation.
        if (context.HttpContext.Request.Headers.ContentType.Contains("application/x-protobuf"))
        {
            return;
        }

        logger.LogValidatingAntiforgeryToken(context.HttpContext.Request.Path.Value);

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException ex)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            logger.LogAntiforgeryValidationFailed(ex, context.HttpContext.Request.Path.Value);
        }
    }
}
