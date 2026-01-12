using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AIntern.Data;

namespace AIntern.Data.Tests;

/// <summary>
/// Factory for creating in-memory SQLite DbContext instances for testing.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory SQLite DbContext for testing.
    /// Each call creates a fresh database.
    /// </summary>
    public static AInternDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new AInternDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Creates a new in-memory SQLite DbContext with a shared connection.
    /// Use this when you need multiple contexts sharing the same database.
    /// </summary>
    public static AInternDbContext CreateWithConnection(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AInternDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

/// <summary>
/// Mock IDbContextFactory for testing repository classes that require factory injection.
/// Creates a new context with a shared SQLite connection for each CreateDbContext call.
/// </summary>
public sealed class TestDbContextFactoryWrapper : IDbContextFactory<AInternDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public TestDbContextFactoryWrapper()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Create initial schema
        using var context = CreateDbContext();
    }

    public AInternDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AInternDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public Task<AInternDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Close();
            _connection.Dispose();
            _disposed = true;
        }
    }
}
