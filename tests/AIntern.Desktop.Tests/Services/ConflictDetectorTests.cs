using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Services;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.Services;

/// <summary>
/// Unit tests for v0.4.3e ConflictDetector.
/// </summary>
public class ConflictDetectorTests : IDisposable
{
    private readonly Mock<IFileSystemService> _fileSystemMock = new();
    private readonly ConflictDetector _detector;
    private readonly string _testPath = "/test/file.cs";
    private readonly string _tempDir;

    public ConflictDetectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"conflict_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _detector = new ConflictDetector(_fileSystemMock.Object);
    }

    public void Dispose()
    {
        _detector.Dispose();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_NullFileSystem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ConflictDetector(null!));
    }

    [Fact]
    public void SnapshotCount_InitiallyZero()
    {
        Assert.Equal(0, _detector.SnapshotCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TakeSnapshotAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TakeSnapshotAsync_ExistingFile_CapturesState()
    {
        // Use temp file since TakeSnapshotAsync uses FileInfo
        var tempFile = Path.Combine(_tempDir, "test.cs");
        await File.WriteAllTextAsync(tempFile, "content");

        _fileSystemMock.Setup(f => f.FileExistsAsync(tempFile)).ReturnsAsync(true);
        _fileSystemMock.Setup(f => f.ReadFileAsync(tempFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync("content");

        await _detector.TakeSnapshotAsync(tempFile);

        Assert.Equal(1, _detector.SnapshotCount);
        Assert.True(_detector.HasSnapshot(tempFile));
    }

    [Fact]
    public async Task TakeSnapshotAsync_NonExistentFile_CapturesNonExistence()
    {
        _fileSystemMock.Setup(f => f.FileExistsAsync(_testPath)).ReturnsAsync(false);

        await _detector.TakeSnapshotAsync(_testPath);

        Assert.True(_detector.HasSnapshot(_testPath));
    }

    [Fact]
    public async Task TakeSnapshotAsync_NullPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _detector.TakeSnapshotAsync(null!));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CheckConflictAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CheckConflictAsync_NoSnapshot_ReturnsNoSnapshot()
    {
        var result = await _detector.CheckConflictAsync(_testPath);

        Assert.False(result.HasConflict);
        Assert.Equal(ConflictReason.NoSnapshot, result.Reason);
    }

    [Fact]
    public async Task CheckConflictAsync_FileUnchanged_ReturnsNoConflict()
    {
        const string content = "test content";
        var tempFile = Path.Combine(_tempDir, "unchanged.cs");
        await File.WriteAllTextAsync(tempFile, content);

        _fileSystemMock.Setup(f => f.FileExistsAsync(tempFile)).ReturnsAsync(true);
        _fileSystemMock.Setup(f => f.ReadFileAsync(tempFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        await _detector.TakeSnapshotAsync(tempFile);
        var result = await _detector.CheckConflictAsync(tempFile);

        Assert.False(result.HasConflict);
        Assert.Equal(ConflictReason.None, result.Reason);
    }

    [Fact]
    public async Task CheckConflictAsync_ContentModified_ReturnsConflict()
    {
        var tempFile = Path.Combine(_tempDir, "modified.cs");
        await File.WriteAllTextAsync(tempFile, "original content");

        _fileSystemMock.Setup(f => f.FileExistsAsync(tempFile)).ReturnsAsync(true);
        _fileSystemMock.SetupSequence(f => f.ReadFileAsync(tempFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync("original content")
            .ReturnsAsync("modified content");

        await _detector.TakeSnapshotAsync(tempFile);
        var result = await _detector.CheckConflictAsync(tempFile);

        Assert.True(result.HasConflict);
        Assert.Equal(ConflictReason.ContentModified, result.Reason);
    }

    [Fact]
    public async Task CheckConflictAsync_FileCreated_ReturnsConflict()
    {
        _fileSystemMock.SetupSequence(f => f.FileExistsAsync(_testPath))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        await _detector.TakeSnapshotAsync(_testPath);
        var result = await _detector.CheckConflictAsync(_testPath);

        Assert.True(result.HasConflict);
        Assert.Equal(ConflictReason.FileCreated, result.Reason);
    }

    [Fact]
    public async Task CheckConflictAsync_FileDeleted_ReturnsConflict()
    {
        var tempFile = Path.Combine(_tempDir, "deleted.cs");
        await File.WriteAllTextAsync(tempFile, "content");

        _fileSystemMock.SetupSequence(f => f.FileExistsAsync(tempFile))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        _fileSystemMock.Setup(f => f.ReadFileAsync(tempFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync("content");

        await _detector.TakeSnapshotAsync(tempFile);
        var result = await _detector.CheckConflictAsync(tempFile);

        Assert.True(result.HasConflict);
        Assert.Equal(ConflictReason.FileDeleted, result.Reason);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Snapshot Management Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ClearSnapshot_RemovesSnapshot()
    {
        _fileSystemMock.Setup(f => f.FileExistsAsync(_testPath)).ReturnsAsync(false);

        await _detector.TakeSnapshotAsync(_testPath);
        Assert.True(_detector.HasSnapshot(_testPath));

        _detector.ClearSnapshot(_testPath);
        Assert.False(_detector.HasSnapshot(_testPath));
    }

    [Fact]
    public async Task ClearAllSnapshots_RemovesAll()
    {
        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        await _detector.TakeSnapshotAsync("/file1.cs");
        await _detector.TakeSnapshotAsync("/file2.cs");
        Assert.Equal(2, _detector.SnapshotCount);

        _detector.ClearAllSnapshots();
        Assert.Equal(0, _detector.SnapshotCount);
    }

    [Fact]
    public async Task GetSnapshotTime_ReturnsTime()
    {
        _fileSystemMock.Setup(f => f.FileExistsAsync(_testPath)).ReturnsAsync(false);

        await _detector.TakeSnapshotAsync(_testPath);
        var time = _detector.GetSnapshotTime(_testPath);

        Assert.NotNull(time);
        Assert.True((DateTime.UtcNow - time.Value).TotalSeconds < 5);
    }

    [Fact]
    public void GetSnapshotTime_NoSnapshot_ReturnsNull()
    {
        var time = _detector.GetSnapshotTime(_testPath);
        Assert.Null(time);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ConflictInfo Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConflictInfo_NoConflict_HasExpectedValues()
    {
        var info = ConflictInfo.NoConflict();
        Assert.False(info.HasConflict);
        Assert.Equal(ConflictReason.None, info.Reason);
    }

    [Fact]
    public void ConflictInfo_ContentWasModified_HasExpectedValues()
    {
        var now = DateTime.UtcNow;
        var info = ConflictInfo.ContentWasModified(now, now.AddMinutes(-5));

        Assert.True(info.HasConflict);
        Assert.Equal(ConflictReason.ContentModified, info.Reason);
        Assert.NotNull(info.Message);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var detector = new ConflictDetector(_fileSystemMock.Object);
        detector.Dispose();
        detector.Dispose(); // Should not throw
    }
}
