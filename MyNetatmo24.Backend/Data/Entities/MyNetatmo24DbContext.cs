using Microsoft.EntityFrameworkCore;

namespace MyNetatmo24.Backend.Data.Entities;

public partial class MyNetatmo24DbContext : DbContext
{
    public MyNetatmo24DbContext()
    {
    }

    public MyNetatmo24DbContext(DbContextOptions<MyNetatmo24DbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired();
            entity.Property(e => e.Auth0UserId)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.NetatmoAccessToken)
                .HasMaxLength(255);
            entity.Property(e => e.NetatmoRefreshToken)
                .HasMaxLength(255);
            entity.HasIndex(e => e.Auth0UserId).IsUnique();
            entity.ToTable("Users");
        });
        
        OnModelCreatingPartial(modelBuilder);
    }
    
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}