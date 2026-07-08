using FluentResults;

namespace MyNetatmo24.SharedKernel.Results;

public sealed class EndpointError : Error
{
    private const string StatusCodeName = "StatusCode";

    public int StatusCode => Metadata.TryGetValue(StatusCodeName, out var value) && value is int code ? code : 500;

    public EndpointError(int statusCode, string message)
        : base(message)
    {
        Metadata.Add(StatusCodeName, statusCode);
    }
}
