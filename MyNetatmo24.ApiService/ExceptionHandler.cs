using Microsoft.AspNetCore.Diagnostics;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace MyNetatmo24.ApiService;

internal sealed class ExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment environment) : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService.ThrowIfNull();
    private readonly IHostEnvironment _environment = environment.ThrowIfNull();

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var statusCode = exception switch
        {
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            Exception = exception,
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Detail = _environment.IsDevelopment() ? exception.Message : null,
            }
        });
    }
}
