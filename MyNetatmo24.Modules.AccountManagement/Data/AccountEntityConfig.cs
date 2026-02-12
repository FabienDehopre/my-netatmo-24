using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyNetatmo24.Modules.AccountManagement.Domain;

namespace MyNetatmo24.Modules.AccountManagement.Data;

internal sealed class AccountEntityConfig : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(p => p.Auth0Id).IsUnique();

        builder.ComplexProperty(p => p.Name, o =>
        {
            o.Property(p => p.FirstName).HasMaxLength(255);
            o.Property(p => p.LastName).HasMaxLength(255);
        });

        builder.ComplexProperty(p => p.NetatmoAuthInfo, o =>
        {
            o.Property(p => p.AccessToken).HasMaxLength(50);
            o.Property(p => p.RefreshToken).HasMaxLength(50);
        });
    }
}
