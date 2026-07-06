using System.Net;
using System.Net.Http.Headers;

namespace MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="HttpMessageHandler"/> that returns a pre-canned response,
/// letting an <see cref="HttpClient"/> be used as a test double without a network call.
/// </summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string? jsonBody) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(statusCode);
        if (jsonBody is not null)
        {
            response.Content = new StringContent(jsonBody);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        return Task.FromResult(response);
    }
}
