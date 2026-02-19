using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace MyNetatmo24.SharedKernel.FastEndpoints;

public static class SendExtensions
{
    extension(IResponseSender sender)
    {
        public async Task ConflictAsync<TResponse>(TResponse response, CancellationToken ct)
            where TResponse : notnull
        {
            await sender.HttpContext.Response.SendAsync(response, StatusCodes.Status409Conflict, cancellation: ct).ConfigureAwait(false);
        }
    }
}
