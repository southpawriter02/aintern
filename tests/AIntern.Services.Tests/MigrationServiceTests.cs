using System.Text.Json;
using AIntern.Core.Entities;
using AIntern.Core.Models;
using AIntern.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AIntern.Services.Tests;

/// <summary>
/// Testable subclass of DatabasePathResolver that allows specifying a custom AppDataDirectory.
/// </summary>
/// <remarks>
/// Inherits from <see cref="DatabasePathResolver"/> and uses the protected constructor
/// to specify a custom test directory.
/// </remarks>
internal sealed class TestableDatabasePathResolver : DatabasePathResolver
{
    /// <summary>
    /// Initializes a new instance with a custom app data directory for testing.
    /// </summary>
    /// <param name="testDirectory">The test directory to use as AppDataDirectory.</param>
    public TestableDatabasePathResolver(string testDirectory)
        : base(null, testDirectory)
    {
    }
}

/// <summary>
/// Unit and integration tests for <see cref="MigrationService"/> (v0.2.5d).
/// Tests migration detection, backup, version stamping, and legacy settings parsing.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor validation for null dependencies</description></item>
///   <item><description>IsMigrationRequiredAsync detection logic</description></item>
///   <item><description>GetCurrentVersionAsync version retrieval</description></item>
///   <item><description>MigrateIfNeededAsync full migration flow</description></item>
///   <item><description>Legacy settings backup and parsing</description></item>
///   <item><description>Version stamping in database</description></item>
/// </list>
/// <para>
/// Integration tests use SQLite in-memory databases and temporary files.
/// </para>
/// <para>Added in v0.2.5d.</para>
/// </remarks>
public class MigrationServiceTests : IDisposable
{
    #region Test Infrastructure

    /// <summary>
    /// SQLite connection kept open for in-memory database lifetime.
    /// </summary>
    private SqliteConnection? _connection;

    /// <summary>
    /// Mock logger for the migration service.
    /// </summary>
    private readonly Mock<ILogger<MigrationService>> _mockLogger;

    /// <summary>
    /// Testable path resolver that uses a temporary directory.
    /// </summary>
    private readonly TestableDatabasePathResolver _testPathResolver;

    /// <summary>
    /// Temporary directory for test files.
    /// </summary>
    private readonly string _testDirectory;

    public MigrationServiceTests()
    {
        _mockLogger = new Mock<ILogger<MigrationService>>();

        // Create a temporary directory for test files
        _testDirectory = Path.Combine(Path.GetTempPath(), $"MigrationServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        // Create a testable path resolver with the test directory
        _testPathResolver = new TestableDatabasePathResolver(_testDirectory);
    }

    /// <summary>
    /// Creates an in-memory SQLite DbContext.
    /// </summary>
    private async Task<AInternDbContext> CreateInMemoryContextAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        await context.Database.EnsureCreatedAsync();

        return context;
    }

    /// <summary>
    /// Creates a MigrationService with test dependencies.
    /// </summary>
    private MigrationService CreateService(AInternDbContext context)
    {
        return new MigrationService(context, _testPathResolver, _mockLogger.Object);
    }

    /// <summary>
    /// Creates a legacy settings.json file in the test directory.
    /// </summary>
    /// <param name="lastModelPath">Optional last model path.</param>
    /// <param name="contextSize">Optional context size (default 8192).</param>
    /// <param name="gpuLayers">Optional GPU layers (default 35).</param>
    /// <param name="temperature">Optional temperature (default 0.8).</param>
    /// <param name="theme">Optional theme (default "Light").</param>
    private void CreateLegacySettingsFile(
        string? lastModelPath = "/path/to/model.gguf",
        uint contextSize = 8192,
        int gpuLayers = 35,
        float temperature = 0.8f,
        string theme = "Light")
    {
        var settings = new
        {
            lastModelPath,
            defaultContextSize = contextSize,
            defaultGpuLayers = gpuLayers,
            temperature,
            theme
        };

        var json = JsonSerializer.Serialize(settings);
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        File.WriteAllText(settingsPath, json);
    }

    /// <summary>
    /// Removes the legacy settings file if it exists.
    /// </summary>
    private void RemoveLegacySettingsFile()
    {
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();

        // Clean up temporary directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures in tests
            }
        }

        GC.SuppressFinalize(this);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor throws when DbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MigrationService(null!, _testPathResolver, _mockLogger.Object));
    }

    /// <summary>
    /// Verifies constructor throws when path resolver is null.
    /// </summary>
    [Fact]
    public async Task Constructor_NullPathResolver_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MigrationService(context, null!, _mockLogger.Object));
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public async Task Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MigrationService(context, _testPathResolver, null!));
    }

    /// <summary>
    /// Verifies constructor succeeds with valid dependencies.
    /// </summary>
    [Fact]
    public async Task Constructor_ValidDependencies_Succeeds()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();

        // Act
        var service = CreateService(context);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region IsMigrationRequiredAsync Tests

    /// <summary>
    /// Verifies IsMigrationRequiredAsync returns false when no legacy settings file exists.
    /// </summary>
    [Fact]
    public async Task IsMigrationRequiredAsync_NoSettingsFile_ReturnsFalse()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        RemoveLegacySettingsFile();

        // Act
        var result = await service.IsMigrationRequiredAsync();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies IsMigrationRequiredAsync returns true when legacy settings exist and no version is stamped.
    /// </summary>
    [Fact]
    public async Task IsMigrationRequiredAsync_HasSettingsAndNoVersion_ReturnsTrue()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        CreateLegacySettingsFile();

        // Act
        var result = await service.IsMigrationRequiredAsync();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies IsMigrationRequiredAsync returns false when version is already current.
    /// </summary>
    [Fact]
    public async Task IsMigrationRequiredAsync_VersionCurrent_ReturnsFalse()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();

        // Stamp current version
        context.AppVersions.Add(new AppVersionEntity
        {
            Major = MigrationService.CurrentVersion.Major,
            Minor = MigrationService.CurrentVersion.Minor,
            Patch = MigrationService.CurrentVersion.Build,
            MigratedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        CreateLegacySettingsFile();

        // Act
        var result = await service.IsMigrationRequiredAsync();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetCurrentVersionAsync Tests

    /// <summary>
    /// Verifies GetCurrentVersionAsync returns legacy version when no version is stored.
    /// </summary>
    [Fact]
    public async Task GetCurrentVersionAsync_NoVersion_ReturnsLegacy()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Act
        var version = await service.GetCurrentVersionAsync();

        // Assert
        Assert.Equal(MigrationService.LegacyVersion, version);
    }

    /// <summary>
    /// Verifies GetCurrentVersionAsync returns the stored version.
    /// </summary>
    [Fact]
    public async Task GetCurrentVersionAsync_HasVersion_ReturnsStored()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var expectedVersion = new Version(0, 2, 0);

        context.AppVersions.Add(new AppVersionEntity
        {
            Major = 0,
            Minor = 2,
            Patch = 0,
            MigratedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var version = await service.GetCurrentVersionAsync();

        // Assert
        Assert.Equal(expectedVersion, version);
    }

    /// <summary>
    /// Verifies GetCurrentVersionAsync returns the most recent version when multiple exist.
    /// </summary>
    [Fact]
    public async Task GetCurrentVersionAsync_MultipleVersions_ReturnsMostRecent()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();

        context.AppVersions.Add(new AppVersionEntity
        {
            Major = 0,
            Minor = 1,
            Patch = 0,
            MigratedAt = DateTime.UtcNow.AddHours(-2)
        });
        context.AppVersions.Add(new AppVersionEntity
        {
            Major = 0,
            Minor = 2,
            Patch = 0,
            MigratedAt = DateTime.UtcNow.AddHours(-1)
        });
        context.AppVersions.Add(new AppVersionEntity
        {
            Major = 0,
            Minor = 3,
            Patch = 0,
            MigratedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var version = await service.GetCurrentVersionAsync();

        // Assert
        Assert.Equal(new Version(0, 3, 0), version);
    }

    #endregion

    #region MigrateIfNeededAsync Tests

    /// <summary>
    /// Verifies MigrateIfNeededAsync returns success with no steps when no migration needed.
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_NoMigrationNeeded_ReturnsSuccess()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        RemoveLegacySettingsFile();

        // Act
        var result = await service.MigrateIfNeededAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.MigrationSteps);
        Assert.Contains("No migration required", result.MigrationSteps[0]);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies MigrateIfNeededAsync creates backup of legacy settings.
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_CreatesBackup()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        CreateLegacySettingsFile();

        var backupPath = Path.Combine(_testDirectory, "settings.v1.json.bak");
        Assert.False(File.Exists(backupPath)); // Sanity check

        // Act
        await service.MigrateIfNeededAsync();

        // Assert
        Assert.True(File.Exists(backupPath));
    }

    /// <summary>
    /// Verifies MigrateIfNeededAsync stamps the version in database.
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_StampsVersion()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        CreateLegacySettingsFile();

        Assert.Empty(await context.AppVersions.ToListAsync()); // Sanity check

        // Act
        await service.MigrateIfNeededAsync();

        // Assert
        var versions = await context.AppVersions.ToListAsync();
        Assert.Single(versions);
        Assert.Equal(MigrationService.CurrentVersion.Major, versions[0].Major);
        Assert.Equal(MigrationService.CurrentVersion.Minor, versions[0].Minor);
    }

    /// <summary>
    /// Verifies MigrateIfNeededAsync reports correct migration steps.
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_ReportsSteps()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        CreateLegacySettingsFile();

        // Act
        var result = await service.MigrateIfNeededAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.MigrationSteps.Count >= 3); // Starting, backup, version stamp
        Assert.Contains(result.MigrationSteps, s => s.Contains("Starting migration"));
        Assert.Contains(result.MigrationSteps, s => s.Contains("Backed up"));
        Assert.Contains(result.MigrationSteps, s => s.Contains("Stamped version"));
    }

    /// <summary>
    /// Verifies MigrateIfNeededAsync correctly reports from and to versions.
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_ReportsVersionTransition()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        CreateLegacySettingsFile();

        // Act
        var result = await service.MigrateIfNeededAsync();

        // Assert
        Assert.Equal(MigrationService.LegacyVersion, result.FromVersion);
        Assert.Equal(MigrationService.CurrentVersion, result.ToVersion);
    }

    /// <summary>
    /// Verifies MigrateIfNeededAsync is idempotent (can be called multiple times).
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_IsIdempotent()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        CreateLegacySettingsFile();

        // Act - Call twice
        var result1 = await service.MigrateIfNeededAsync();
        var result2 = await service.MigrateIfNeededAsync();

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);

        // Second call should be a no-op
        Assert.Contains("No migration required", result2.MigrationSteps[0]);

        // Should only have one version record
        var versions = await context.AppVersions.ToListAsync();
        Assert.Single(versions);
    }

    #endregion

    #region Legacy Settings Parsing Tests

    /// <summary>
    /// Verifies migration handles valid legacy settings JSON.
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_ValidJson_Succeeds()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        CreateLegacySettingsFile(
            lastModelPath: "/custom/path.gguf",
            temperature: 0.5f,
            theme: "Dark");

        // Act
        var result = await service.MigrateIfNeededAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Contains(result.MigrationSteps, s => s.Contains("Read legacy settings"));
    }

    /// <summary>
    /// Verifies migration handles invalid JSON gracefully.
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_InvalidJson_StillSucceeds()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Write invalid JSON
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        File.WriteAllText(settingsPath, "{ invalid json }");

        // Act
        var result = await service.MigrateIfNeededAsync();

        // Assert - Migration should still succeed (just skip parsing)
        Assert.True(result.Success);
    }

    /// <summary>
    /// Verifies migration handles empty settings file.
    /// </summary>
    [Fact]
    public async Task MigrateIfNeededAsync_EmptyFile_StillSucceeds()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        // Write empty file
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        File.WriteAllText(settingsPath, "");

        // Act
        var result = await service.MigrateIfNeededAsync();

        // Assert - Migration should still succeed
        Assert.True(result.Success);
    }

    #endregion

    #region Cancellation Tests

    /// <summary>
    /// Verifies IsMigrationRequiredAsync respects cancellation.
    /// </summary>
    [Fact]
    public async Task IsMigrationRequiredAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);
        CreateLegacySettingsFile();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await service.IsMigrationRequiredAsync(cts.Token));
    }

    /// <summary>
    /// Verifies GetCurrentVersionAsync respects cancellation.
    /// </summary>
    [Fact]
    public async Task GetCurrentVersionAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var context = await CreateInMemoryContextAsync();
        var service = CreateService(context);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await service.GetCurrentVersionAsync(cts.Token));
    }

    #endregion

    #region MigrationResult Tests

    /// <summary>
    /// Verifies MigrationResult.NoMigrationNeeded creates correct result.
    /// </summary>
    [Fact]
    public void MigrationResult_NoMigrationNeeded_CreatesCorrectResult()
    {
        // Arrange
        var version = new Version(0, 2, 0);

        // Act
        var result = MigrationResult.NoMigrationNeeded(version);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(version, result.FromVersion);
        Assert.Equal(version, result.ToVersion);
        Assert.Single(result.MigrationSteps);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies MigrationResult.Succeeded creates correct result.
    /// </summary>
    [Fact]
    public void MigrationResult_Succeeded_CreatesCorrectResult()
    {
        // Arrange
        var from = new Version(0, 1, 0);
        var to = new Version(0, 2, 0);
        var steps = new List<string> { "Step 1", "Step 2" };

        // Act
        var result = MigrationResult.Succeeded(from, to, steps);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(from, result.FromVersion);
        Assert.Equal(to, result.ToVersion);
        Assert.Equal(2, result.MigrationSteps.Count);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies MigrationResult.Failed creates correct result.
    /// </summary>
    [Fact]
    public void MigrationResult_Failed_CreatesCorrectResult()
    {
        // Arrange
        var from = new Version(0, 1, 0);
        var to = new Version(0, 2, 0);
        var steps = new List<string> { "Step 1" };
        var error = "Test error message";

        // Act
        var result = MigrationResult.Failed(from, to, steps, error);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(from, result.FromVersion);
        Assert.Equal(to, result.ToVersion);
        Assert.Single(result.MigrationSteps);
        Assert.Equal(error, result.ErrorMessage);
    }

    /// <summary>
    /// Verifies MigrationResult.Failed throws for null error message.
    /// </summary>
    [Fact]
    public void MigrationResult_Failed_NullError_ThrowsArgumentException()
    {
        // Arrange
        var from = new Version(0, 1, 0);
        var to = new Version(0, 2, 0);
        var steps = new List<string>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MigrationResult.Failed(from, to, steps, null!));
    }

    /// <summary>
    /// Verifies MigrationResult.Failed throws for whitespace error message.
    /// </summary>
    [Fact]
    public void MigrationResult_Failed_WhitespaceError_ThrowsArgumentException()
    {
        // Arrange
        var from = new Version(0, 1, 0);
        var to = new Version(0, 2, 0);
        var steps = new List<string>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            MigrationResult.Failed(from, to, steps, "   "));
    }

    #endregion
}
