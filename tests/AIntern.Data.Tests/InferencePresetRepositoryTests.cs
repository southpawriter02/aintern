using AIntern.Core.Entities;
using AIntern.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIntern.Data.Tests;

/// <summary>
/// Unit tests for the <see cref="InferencePresetRepository"/> class.
/// Verifies CRUD operations, default handling, and duplication functionality.
/// </summary>
/// <remarks>
/// <para>
/// These tests use SQLite in-memory databases to verify:
/// </para>
/// <list type="bullet">
///   <item><description>Repository instantiation and constructor validation</description></item>
///   <item><description>Inference preset CRUD operations</description></item>
///   <item><description>Built-in preset protection</description></item>
///   <item><description>Default preset management</description></item>
///   <item><description>Preset duplication</description></item>
/// </list>
/// </remarks>
public class InferencePresetRepositoryTests : IDisposable
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
    private readonly InferencePresetRepository _repository;

    /// <summary>
    /// Initializes test infrastructure with an in-memory SQLite database.
    /// </summary>
    public InferencePresetRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        _context.Database.EnsureCreated();

        _repository = new InferencePresetRepository(_context);
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
        var preset = new InferencePresetEntity
        {
            Name = "Test Preset"
        };

        // Act
        var result = await _repository.CreateAsync(preset);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    /// <summary>
    /// Verifies that DeleteAsync does not delete built-in presets.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ProtectsBuiltInPresets()
    {
        // Arrange
        var builtIn = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Built-In Preset",
            IsBuiltIn = true
        };

        _context.InferencePresets.Add(builtIn);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(builtIn.Id);

        // Assert - preset should still exist
        var stillExists = await _context.InferencePresets
            .AnyAsync(p => p.Id == builtIn.Id);

        Assert.True(stillExists);
    }

    #endregion

    #region Default Handling Tests

    /// <summary>
    /// Verifies that SetAsDefaultAsync clears the previous default.
    /// </summary>
    [Fact]
    public async Task SetAsDefaultAsync_ClearsPreviousDefault()
    {
        // Arrange
        var preset1 = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Preset 1",
            IsDefault = true
        });

        var preset2 = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Preset 2"
        });

        // Act
        await _repository.SetAsDefaultAsync(preset2.Id);

        // Assert
        var updatedPreset1 = await _context.InferencePresets
            .AsNoTracking()
            .FirstAsync(p => p.Id == preset1.Id);

        var updatedPreset2 = await _context.InferencePresets
            .AsNoTracking()
            .FirstAsync(p => p.Id == preset2.Id);

        Assert.False(updatedPreset1.IsDefault);
        Assert.True(updatedPreset2.IsDefault);
    }

    /// <summary>
    /// Verifies that DeleteAsync reassigns the default when deleting the default preset.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ReassignsDefault_WhenDeletingDefault()
    {
        // Arrange
        var preset1 = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Preset 1",
            IsDefault = true,
            IsBuiltIn = false
        });

        var preset2 = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Preset 2",
            IsBuiltIn = false
        });

        // Act
        await _repository.DeleteAsync(preset1.Id);

        // Assert
        var remainingDefault = await _repository.GetDefaultAsync();

        Assert.NotNull(remainingDefault);
        Assert.Equal(preset2.Id, remainingDefault.Id);
    }

    #endregion

    #region Duplication Tests

    /// <summary>
    /// Verifies that DuplicateAsync copies all parameter values.
    /// </summary>
    [Fact]
    public async Task DuplicateAsync_CopiesAllParameters()
    {
        // Arrange
        var source = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Source Preset",
            Description = "A test preset",
            Temperature = 0.8f,
            TopP = 0.95f,
            TopK = 50,
            RepeatPenalty = 1.2f,
            MaxTokens = 4096,
            ContextSize = 8192
        });

        // Act
        var duplicate = await _repository.DuplicateAsync(source.Id, "Duplicated Preset");

        // Assert
        Assert.NotNull(duplicate);
        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.Equal("Duplicated Preset", duplicate.Name);
        Assert.Equal(source.Description, duplicate.Description);
        Assert.Equal(source.Temperature, duplicate.Temperature);
        Assert.Equal(source.TopP, duplicate.TopP);
        Assert.Equal(source.TopK, duplicate.TopK);
        Assert.Equal(source.RepeatPenalty, duplicate.RepeatPenalty);
        Assert.Equal(source.MaxTokens, duplicate.MaxTokens);
        Assert.Equal(source.ContextSize, duplicate.ContextSize);
    }

    /// <summary>
    /// Verifies that DuplicateAsync sets IsBuiltIn to false on the duplicate.
    /// </summary>
    [Fact]
    public async Task DuplicateAsync_SetsIsBuiltInFalse()
    {
        // Arrange
        var builtIn = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Built-In Preset",
            IsBuiltIn = true,
            IsDefault = true
        };

        _context.InferencePresets.Add(builtIn);
        await _context.SaveChangesAsync();

        // Act
        var duplicate = await _repository.DuplicateAsync(builtIn.Id, "Custom Preset");

        // Assert
        Assert.NotNull(duplicate);
        Assert.False(duplicate.IsBuiltIn);
        Assert.False(duplicate.IsDefault);
    }

    #endregion
}
