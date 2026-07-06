using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyNetatmo24.Modules.AccountManagement.Data;

namespace MyNetatmo24.Modules.AccountManagement.Tests.TestSupport;

/// <summary>
/// A self-contained <see cref="AccountDbContext"/> backed by an in-memory SQLite database.
/// This acts as a fast, isolated database test double: the relational configuration
/// (complex properties, the named soft-delete query filter and Vogen value converters)
/// behaves exactly as it does against PostgreSQL, unlike the EF Core in-memory provider.
/// </summary>
internal sealed class TestAccountDbContext : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestAccountDbContext(SqliteConnection connection, AccountDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public AccountDbContext Context { get; }

    public static async Task<TestAccountDbContext> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AccountDbContext(options, TimeProvider.System);
        await context.Database.EnsureCreatedAsync();

        return new TestAccountDbContext(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
