using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyNetatmo24.SharedKernel.Domain;

namespace MyNetatmo24.SharedKernel.Infrastructure;

public class SoftDeleteInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is null)
        {
            return ValueTask.FromResult(result);
        }

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry is not { State: EntityState.Deleted, Entity: ISoftDelete entity })
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entity.Delete(timeProvider);
        }

        return ValueTask.FromResult(result);
    }
}
