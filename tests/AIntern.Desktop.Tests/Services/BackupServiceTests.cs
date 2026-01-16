using AIntern.Core.Models;
using AIntern.Services;
using Xunit;

namespace AIntern.Desktop.Tests.Services;

/// <summary>
/// Unit tests for v0.4.3c BackupService.
/// </summary>
public class BackupServiceTests : IDisposable
{
    private readonly string _testBackupDir;
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _testBackupDir = Path.Combine(Path.GetTempPath(), $"aintern_backup_test_{Guid.NewGuid():N}");
        var options = new BackupOptions { CustomBackupDirectory = _testBackupDir };
        _service = new BackupService(options: options);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_testBackupDir))
        {
            try { Directory.Delete(_testBackupDir, true); } catch { }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_CreatesBackupDirectory()
    {
        Assert.True(Directory.Exists(_service.BackupDirectory));
    }

    [Fact]
    public void BackupDirectory_MatchesOptions()
    {
        Assert.Equal(_testBackupDir, _service.BackupDirectory);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CreateBackupAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateBackupAsync_NullPath_ReturnsNull()
    {
        var result = await _service.CreateBackupAsync(null!);
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBackupAsync_NonExistentFile_ReturnsNull()
    {
        var result = await _service.CreateBackupAsync("/nonexistent/file.cs");
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBackupAsync_ValidFile_CreatesBackup()
    {
        var testFile = Path.Combine(_testBackupDir, "test.cs");
        await File.WriteAllTextAsync(testFile, "test content");

        var backupPath = await _service.CreateBackupAsync(testFile);

        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));
        Assert.True(File.Exists(backupPath + ".meta"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RestoreBackupAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RestoreBackupAsync_NonExistentBackup_ReturnsFalse()
    {
        var result = await _service.RestoreBackupAsync("/nonexistent/backup", "/target");
        Assert.False(result);
    }

    [Fact]
    public async Task RestoreBackupAsync_ValidBackup_RestoresContent()
    {
        var testFile = Path.Combine(_testBackupDir, "original.cs");
        var originalContent = "original content";
        await File.WriteAllTextAsync(testFile, originalContent);

        var backupPath = await _service.CreateBackupAsync(testFile);
        Assert.NotNull(backupPath);

        var restorePath = Path.Combine(_testBackupDir, "restored.cs");
        var success = await _service.RestoreBackupAsync(backupPath, restorePath);

        Assert.True(success);
        Assert.Equal(originalContent, await File.ReadAllTextAsync(restorePath));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DeleteBackup Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteBackup_RemovesBackupAndMetadata()
    {
        var testFile = Path.Combine(_testBackupDir, "todelete.cs");
        await File.WriteAllTextAsync(testFile, "content");

        var backupPath = await _service.CreateBackupAsync(testFile);
        Assert.NotNull(backupPath);

        var success = _service.DeleteBackup(backupPath);

        Assert.True(success);
        Assert.False(File.Exists(backupPath));
        Assert.False(File.Exists(backupPath + ".meta"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Query Operations Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetAllBackups_EmptyDir_ReturnsEmptyList()
    {
        var backups = _service.GetAllBackups();
        Assert.Empty(backups);
    }

    [Fact]
    public async Task GetBackupsForFile_ReturnsCorrectBackups()
    {
        var testFile = Path.Combine(_testBackupDir, "multi.cs");
        await File.WriteAllTextAsync(testFile, "v1");
        await _service.CreateBackupAsync(testFile);

        await File.WriteAllTextAsync(testFile, "v2");
        await _service.CreateBackupAsync(testFile);

        var backups = _service.GetBackupsForFile(testFile);
        Assert.Equal(2, backups.Count);
    }

    [Fact]
    public void BackupExists_NonExistent_ReturnsFalse()
    {
        Assert.False(_service.BackupExists("/nonexistent"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Storage Operations Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetTotalBackupSize_NoBackups_ReturnsZero()
    {
        var size = _service.GetTotalBackupSize();
        Assert.Equal(0, size);
    }

    [Fact]
    public void GetStorageInfo_ReturnsValidInfo()
    {
        var info = _service.GetStorageInfo();

        Assert.Equal(_testBackupDir, info.BackupDirectory);
        Assert.Equal(BackupHealthStatus.Healthy, info.HealthStatus);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Cleanup Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CleanupExpiredBackups_NoBackups_ReturnsZero()
    {
        var deleted = _service.CleanupExpiredBackups(TimeSpan.FromDays(1));
        Assert.Equal(0, deleted);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BackupOptions Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void BackupOptions_Default_HasExpectedValues()
    {
        var options = BackupOptions.Default;
        Assert.Equal(10, options.MaxBackupsPerFile);
        Assert.Equal(500 * 1024 * 1024, options.MaxTotalStorageBytes);
        Assert.True(options.ComputeContentHash);
    }

    [Fact]
    public void BackupOptions_Minimal_HasSmallLimits()
    {
        var options = BackupOptions.Minimal;
        Assert.Equal(3, options.MaxBackupsPerFile);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BackupInfo Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void BackupInfo_Age_CalculatesCorrectly()
    {
        var info = new BackupInfo { CreatedAt = DateTime.UtcNow - TimeSpan.FromHours(2) };
        Assert.True(info.Age >= TimeSpan.FromHours(1));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var service = new BackupService(options: new BackupOptions { CustomBackupDirectory = _testBackupDir });
        service.Dispose();
        service.Dispose(); // Should not throw
    }
}
