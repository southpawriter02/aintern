using AIntern.Core.Entities;
using AIntern.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIntern.Data.Tests;

/// <summary>
/// Unit tests for the <see cref="SystemPromptRepository"/> class.
/// Verifies CRUD operations, soft delete, hard delete, and category management.
/// </summary>
/// <remarks>
/// <para>
/// These tests use SQLite in-memory databases to verify:
/// </para>
/// <list type="bullet">
///   <item><description>Repository instantiation and constructor validation</description></item>
///   <item><description>System prompt CRUD operations</description></item>
///   <item><description>Soft delete functionality (IsActive flag)</description></item>
///   <item><description>Hard delete with built-in protection</description></item>
///   <item><description>Category-based queries</description></item>
/// </list>
/// </remarks>
public class SystemPromptRepositoryTests : IDisposable
{
    #region Test Infrastructure

    /// <summary>
    /// SQLite connection kept open for in-memory database lifetime.
    /// </summary>
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Database context for repository operations.
    /// </summary>
    private readonly AInternDbContext _context;

    /// <summary>
    /// Repository under test.
    /// </summary>
    private readonly SystemPromptRepository _repository;

    /// <summary>
    /// Initializes test infrastructure with an in-memory SQLite database.
    /// </summary>
    public SystemPromptRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        _context.Database.EnsureCreated();

        _repository = new SystemPromptRepository(_context);
    }

    /// <summary>
    /// Disposes of the test infrastructure.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region CRUD Tests

    /// <summary>
    /// Verifies that CreateAsync generates a new ID when the entity has an empty GUID.
    /// </summary>
    [Fact]
    public async Task CreateAsync_GeneratesId_WhenIdIsEmpty()
    {
        // Arrange
        var prompt = new SystemPromptEntity
        {
            Name = "Test Prompt",
            Content = "You are a test assistant."
        };

        // Act
        var result = await _repository.CreateAsync(prompt);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    /// <summary>
    /// Verifies that GetAllActiveAsync excludes inactive prompts.
    /// </summary>
    [Fact]
    public async Task GetAllActiveAsync_ExcludesInactive()
    {
        // Arrange
        await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Active Prompt",
            Content = "Active content"
        });

        var inactive = await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Inactive Prompt",
            Content = "Inactive content"
        });

        await _repository.DeleteAsync(inactive.Id);

        // Act
        var results = await _repository.GetAllActiveAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("Active Prompt", results[0].Name);
    }

    #endregion

    #region Soft Delete Tests

    /// <summary>
    /// Verifies that DeleteAsync sets IsActive to false (soft delete).
    /// </summary>
    [Fact]
    public async Task DeleteAsync_SetsIsActiveFalse()
    {
        // Arrange
        var prompt = await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Deletable Prompt",
            Content = "Test content"
        });

        // Act
        await _repository.DeleteAsync(prompt.Id);

        // Assert
        var deleted = await _context.SystemPrompts
            .AsNoTracking()
            .FirstAsync(p => p.Id == prompt.Id);

        Assert.False(deleted.IsActive);
    }

    /// <summary>
    /// Verifies that RestoreAsync sets IsActive to true.
    /// </summary>
    [Fact]
    public async Task RestoreAsync_SetsIsActiveTrue()
    {
        // Arrange
        var prompt = await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Restorable Prompt",
            Content = "Test content"
        });

        await _repository.DeleteAsync(prompt.Id);

        // Act
        await _repository.RestoreAsync(prompt.Id);

        // Assert
        var restored = await _context.SystemPrompts
            .AsNoTracking()
            .FirstAsync(p => p.Id == prompt.Id);

        Assert.True(restored.IsActive);
    }

    #endregion

    #region Hard Delete Tests

    /// <summary>
    /// Verifies that HardDeleteAsync does not delete built-in prompts.
    /// </summary>
    [Fact]
    public async Task HardDeleteAsync_FailsForBuiltInPrompts()
    {
        // Arrange
        var builtIn = new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = "Built-In Prompt",
            Content = "Built-in content",
            IsBuiltIn = true
        };

        _context.SystemPrompts.Add(builtIn);
        await _context.SaveChangesAsync();

        // Act
        await _repository.HardDeleteAsync(builtIn.Id);

        // Assert - prompt should still exist
        var stillExists = await _context.SystemPrompts
            .AnyAsync(p => p.Id == builtIn.Id);

        Assert.True(stillExists);
    }

    /// <summary>
    /// Verifies that HardDeleteAsync removes user-created prompts.
    /// </summary>
    [Fact]
    public async Task HardDeleteAsync_RemovesUserCreatedPrompts()
    {
        // Arrange
        var userCreated = await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "User Prompt",
            Content = "User content",
            IsBuiltIn = false
        });

        // Act
        await _repository.HardDeleteAsync(userCreated.Id);

        // Assert
        var deleted = await _context.SystemPrompts
            .AnyAsync(p => p.Id == userCreated.Id);

        Assert.False(deleted);
    }

    #endregion

    #region Category Tests

    /// <summary>
    /// Verifies that GetCategoriesAsync returns distinct categories.
    /// </summary>
    [Fact]
    public async Task GetCategoriesAsync_ReturnsDistinctCategories()
    {
        // Arrange
        await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Prompt 1",
            Content = "Content 1",
            Category = "Coding"
        });

        await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Prompt 2",
            Content = "Content 2",
            Category = "Writing"
        });

        await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Prompt 3",
            Content = "Content 3",
            Category = "Coding"
        });

        // Act
        var categories = await _repository.GetCategoriesAsync();

        // Assert
        Assert.Equal(2, categories.Count);
        Assert.Contains("Coding", categories);
        Assert.Contains("Writing", categories);
    }

    /// <summary>
    /// Verifies that GetByCategoryAsync filters by category correctly.
    /// </summary>
    [Fact]
    public async Task GetByCategoryAsync_FiltersCorrectly()
    {
        // Arrange
        await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Coding Prompt 1",
            Content = "Content 1",
            Category = "Coding"
        });

        await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Writing Prompt",
            Content = "Content 2",
            Category = "Writing"
        });

        await _repository.CreateAsync(new SystemPromptEntity
        {
            Name = "Coding Prompt 2",
            Content = "Content 3",
            Category = "Coding"
        });

        // Act
        var codingPrompts = await _repository.GetByCategoryAsync("Coding");

        // Assert
        Assert.Equal(2, codingPrompts.Count);
        Assert.All(codingPrompts, p => Assert.Equal("Coding", p.Category));
    }

    #endregion
}
