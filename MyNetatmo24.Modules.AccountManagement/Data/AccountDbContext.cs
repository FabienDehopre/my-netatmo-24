using Microsoft.EntityFrameworkCore;
using MyNetatmo24.SharedKernel.Infrastructure;

namespace MyNetatmo24.Modules.AccountManagement.Data;

public class AccountDbContext(DbContextOptions<AccountDbContext> options, TimeProvider timeProvider) : ModuleDbContext(options, timeProvider)
{
    public override string Schema => "accountmamangement";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
