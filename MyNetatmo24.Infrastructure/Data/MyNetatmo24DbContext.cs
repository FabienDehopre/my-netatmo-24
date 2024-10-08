using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.ApplicationCore.Entities;

namespace MyNetatmo24.Infrastructure.Data;

public class MyNetatmo24DbContext : DbContext
{
    #pragma warning disable CS8618 // Required by Entity Framework
    public MyNetatmo24DbContext(DbContextOptions<MyNetatmo24DbContext> options) : base(options) {}
    
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}