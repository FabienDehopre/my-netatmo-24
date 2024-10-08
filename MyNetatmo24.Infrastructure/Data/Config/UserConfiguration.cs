using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyNetatmo24.ApplicationCore.Entities;
using MyNetatmo24.Infrastructure.Data.Generators;

namespace MyNetatmo24.Infrastructure.Data.Config;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .IsRequired()
            .HasValueGenerator<SnowflakeValueGenerator>();
        builder.Property(e => e.Auth0UserId)
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(e => e.NetatmoAccessToken)
            .HasMaxLength(255);
        builder.Property(e => e.NetatmoRefreshToken)
            .HasMaxLength(255);
        builder.Property(p => p.Inserted)
            .ValueGeneratedOnAdd();
        builder.Property(p => p.LastUpdated)
            .ValueGeneratedOnAddOrUpdate();
        builder.HasIndex(e => e.Auth0UserId).IsUnique();
        builder.ToTable("Users");
    }
}