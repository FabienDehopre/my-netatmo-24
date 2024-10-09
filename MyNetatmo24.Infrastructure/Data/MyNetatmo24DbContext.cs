using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.ApplicationCore.Entities;

namespace MyNetatmo24.Infrastructure.Data;

public class MyNetatmo24DbContext(DbContextOptions<MyNetatmo24DbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}