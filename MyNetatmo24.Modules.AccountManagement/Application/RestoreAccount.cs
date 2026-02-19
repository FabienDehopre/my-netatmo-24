using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Data;
using MyNetatmo24.Modules.AccountManagement.Domain;
using MyNetatmo24.SharedKernel.Domain;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.Messages;
using Wolverine.EntityFrameworkCore;

namespace MyNetatmo24.Modules.AccountManagement.Application;

public static class RestoreAccount
{
    public sealed class EndpointSummary : Summary<Endpoint>
    {
        public EndpointSummary()
        {
            Summary = "Restores the authenticated user's account if it was previously marked as deleted.";
            Description =
                "This endpoint restores the authenticated user's account if it was previously marked as deleted. " +
                "If the user is not authenticated, a 401 Unauthorized response is returned. " +
                "If the user's account cannot be found, a 404 Not Found response is returned. " +
                "If the user's account is successfully restored, a 204 No Content response is returned.";
            Response(StatusCodes.Status204NoContent,
                "The user's account was successfully restored.");
            Response(StatusCodes.Status401Unauthorized,
                "The user is not authenticated, so their account cannot be restored.");
            Response(StatusCodes.Status404NotFound,
                "The user's account could not be found, so it cannot be restored.");
        }
    }

    public sealed class Endpoint(IDbContextOutbox<AccountDbContext> outbox)
        : Ep.NoReq.Res<Results<NoContent, UnauthorizedHttpResult, NotFound>>
    {
        private readonly IDbContextOutbox<AccountDbContext> _outbox = outbox.ThrowIfNull();

        public override void Configure() => Post("/account/restore");

        public override async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> ExecuteAsync(CancellationToken ct)
        {
            var auth0Id = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(auth0Id))
            {
                return TypedResults.Unauthorized();
            }

            var existingAccount = await _outbox.DbContext
                .Set<Account>()
                .IgnoreQueryFilters([Constants.SoftDeleteFilter])
                .SingleOrDefaultAsync(a => a.Auth0Id == auth0Id, ct);
            if (existingAccount is null)
            {
                return TypedResults.NotFound();
            }

            ((ISoftDelete)existingAccount).Undo();
            await _outbox.PublishAsync(new AccountRestored(existingAccount.Id));
            await _outbox.SaveChangesAndFlushMessagesAsync(ct);

            return TypedResults.NoContent();
        }
    }
}
