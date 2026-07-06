using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.SharedKernel.Domain;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.StronglyTypedIds;

namespace MyNetatmo24.SharedKernel.Tests.TestSupport;

/// <summary>A soft-deletable entity keyed by a Vogen value object, used to exercise ModuleDbContext.</summary>
public sealed class TestEntity : ISoftDelete
{
    public AccountId Id { get; init; }

    public string? Name { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Concrete <see cref="ModuleDbContext"/> so the abstract base (schema, soft-delete filters,
/// Vogen conventions) and the <see cref="SoftDeleteInterceptor"/> can be tested against real SQLite.</summary>
public sealed class TestModuleDbContext(DbContextOptions options, TimeProvider timeProvider)
    : ModuleDbContext(options, timeProvider)
{
    public override string Schema => "test";

    public DbSet<TestEntity> Entities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name);
        });

        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>Owns an in-memory SQLite connection plus a <see cref="TestModuleDbContext"/> for one test.</summary>
public sealed class TestModuleDbContextHarness : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestModuleDbContextHarness(SqliteConnection connection, TestModuleDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public TestModuleDbContext Context { get; }

    public static async Task<TestModuleDbContextHarness> CreateAsync(TimeProvider timeProvider)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TestModuleDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestModuleDbContext(options, timeProvider);
        await context.Database.EnsureCreatedAsync();

        return new TestModuleDbContextHarness(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
