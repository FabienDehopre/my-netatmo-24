using FastEndpoints;
using MyNetatmo24.Backend.DTOs.User;

namespace MyNetatmo24.Backend.Endpoints.User;

public class RequireNetatmoAuthorizationEndpoint : Ep.NoReq.Res<RequireNetatmoAuthorizationResponse>
{
    public override void Configure()
    {
        Get("/user/require-netatmo-authorization");
        Policies("IsAuthenticated");
        Description(d => 
            d.Produces<RequireNetatmoAuthorizationResponse>()
                .RequireAuthorization("IsAuthenticated")
                .WithName("IsNetatmoAuthorized"));
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}