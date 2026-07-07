using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;

namespace MyNetatmo24.SharedKernel.Endpoints;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class Endpoint<TRequest, TResponse> : IEndpoint
{
    /// <inheritdoc />
    public abstract void Configure(IEndpointRouteBuilder builder);

    /// <summary>
    /// Method used to handle the request and produce a response.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, with a response of type <typeparamref name="TResponse"/>.</returns>
    public abstract Task<TResponse> InvokeAsync(TRequest request, CancellationToken ct);
}

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class EndpointWithoutRequest<TResponse> : IEndpoint
{
    /// <inheritdoc />
    public abstract void Configure(IEndpointRouteBuilder builder);

    /// <summary>
    /// Method used to handle the request and produce a response.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, with a response of type <typeparamref name="TResponse"/>.</returns>
    public abstract Task<TResponse> InvokeAsync(CancellationToken ct);
}
