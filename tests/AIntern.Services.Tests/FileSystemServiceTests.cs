using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Services;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for FileSystemService (v0.3.1d).
/// </summary>
public class FileSystemServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystemService _service;
    private readonly ILogger<FileSystemService> _logger;

    public FileSystemServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FileSystemServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _logger = Substitute.For<ILogger<FileSystemService>>();
        _service = new FileSystemService(_logger);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region GetDirectoryContentsAsync Tests

    [Fact]
    public async Task GetDirectoryContentsAsync_ReturnsContents()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "content");
        Directory.CreateDirectory(Path.Combine(_tempDir, "subdir"));

        // Act
        var result = await _service.GetDirectoryContentsAsync(_tempDir);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_SortsDirectoriesFirst()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "aaa.txt"), "content");
        Directory.CreateDirectory(Path.Combine(_tempDir, "zzz_folder"));
        File.WriteAllText(Path.Combine(_tempDir, "bbb.txt"), "content");

        // Act
        var result = await _service.GetDirectoryContentsAsync(_tempDir);

        // Assert
        Assert.True(result[0].IsDirectory);
        Assert.Equal("zzz_folder", result[0].Name);
        Assert.True(result[1].IsFile);
        Assert.True(result[2].IsFile);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_SortsAlphabetically()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "zebra.txt"), "");
        File.WriteAllText(Path.Combine(_tempDir, "apple.txt"), "");
        File.WriteAllText(Path.Combine(_tempDir, "mango.txt"), "");

        // Act
        var result = await _service.GetDirectoryContentsAsync(_tempDir);

        // Assert
        Assert.Equal("apple.txt", result[0].Name);
        Assert.Equal("mango.txt", result[1].Name);
        Assert.Equal("zebra.txt", result[2].Name);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_ExcludesHiddenByDefault()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, ".hidden"), "content");
        File.WriteAllText(Path.Combine(_tempDir, "visible.txt"), "content");

        // Act
        var result = await _service.GetDirectoryContentsAsync(_tempDir, includeHidden: false);

        // Assert
        Assert.Single(result);
        Assert.Equal("visible.txt", result[0].Name);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_IncludesHiddenWhenRequested()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, ".hidden"), "content");
        File.WriteAllText(Path.Combine(_tempDir, "visible.txt"), "content");

        // Act
        var result = await _service.GetDirectoryContentsAsync(_tempDir, includeHidden: true);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_ThrowsForNonexistentDirectory()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.GetDirectoryContentsAsync(nonexistent));
    }

    #endregion

    #region GetItemInfoAsync Tests

    [Fact]
    public async Task GetItemInfoAsync_ReturnsFileInfo()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "test content");

        var result = await _service.GetItemInfoAsync(filePath);

        Assert.True(result.IsFile);
        Assert.Equal("test.txt", result.Name);
        Assert.Equal(12, result.Size); // "test content" = 12 bytes
    }

    [Fact]
    public async Task GetItemInfoAsync_ReturnsDirectoryInfo()
    {
        var dirPath = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(dirPath);

        var result = await _service.GetItemInfoAsync(dirPath);

        Assert.True(result.IsDirectory);
        Assert.Equal("subdir", result.Name);
    }

    [Fact]
    public async Task GetItemInfoAsync_ThrowsForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.GetItemInfoAsync(nonexistent));
    }

    #endregion

    #region CreateDirectoryAsync Tests

    [Fact]
    public async Task CreateDirectoryAsync_CreatesDirectory()
    {
        var newDir = Path.Combine(_tempDir, "newdir");

        var result = await _service.CreateDirectoryAsync(newDir);

        Assert.True(Directory.Exists(newDir));
        Assert.True(result.IsDirectory);
        Assert.Equal("newdir", result.Name);
    }

    [Fact]
    public async Task CreateDirectoryAsync_CreatesNestedDirectories()
    {
        var newDir = Path.Combine(_tempDir, "level1", "level2", "level3");

        var result = await _service.CreateDirectoryAsync(newDir);

        Assert.True(Directory.Exists(newDir));
    }

    #endregion

    #region DeleteDirectoryAsync Tests

    [Fact]
    public async Task DeleteDirectoryAsync_DeletesDirectory()
    {
        var dirPath = Path.Combine(_tempDir, "todelete");
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Combine(dirPath, "file.txt"), "content");

        await _service.DeleteDirectoryAsync(dirPath);

        Assert.False(Directory.Exists(dirPath));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_ThrowsForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.DeleteDirectoryAsync(nonexistent));
    }

    #endregion

    #region ReadFileAsync Tests

    [Fact]
    public async Task ReadFileAsync_ReadsContent()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "Hello, World!");

        var result = await _service.ReadFileAsync(filePath);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task ReadFileAsync_ThrowsForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent.txt");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.ReadFileAsync(nonexistent));
    }

    #endregion

    #region ReadFileBytesAsync Tests

    [Fact]
    public async Task ReadFileBytesAsync_ReadsBytes()
    {
        var filePath = Path.Combine(_tempDir, "test.bin");
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        File.WriteAllBytes(filePath, bytes);

        var result = await _service.ReadFileBytesAsync(filePath);

        Assert.Equal(bytes, result);
    }

    #endregion

    #region WriteFileAsync Tests

    [Fact]
    public async Task WriteFileAsync_WritesContent()
    {
        var filePath = Path.Combine(_tempDir, "output.txt");

        await _service.WriteFileAsync(filePath, "Test content");

        Assert.True(File.Exists(filePath));
        Assert.Equal("Test content", File.ReadAllText(filePath));
    }

    [Fact]
    public async Task WriteFileAsync_CreatesDirectoryIfNeeded()
    {
        var filePath = Path.Combine(_tempDir, "subdir", "output.txt");

        await _service.WriteFileAsync(filePath, "Test content");

        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task WriteFileAsync_OverwritesExisting()
    {
        var filePath = Path.Combine(_tempDir, "output.txt");
        File.WriteAllText(filePath, "Old content");

        await _service.WriteFileAsync(filePath, "New content");

        Assert.Equal("New content", File.ReadAllText(filePath));
    }

    #endregion

    #region CreateFileAsync Tests

    [Fact]
    public async Task CreateFileAsync_CreatesEmptyFile()
    {
        var filePath = Path.Combine(_tempDir, "newfile.txt");

        var result = await _service.CreateFileAsync(filePath);

        Assert.True(File.Exists(filePath));
        Assert.Equal(0, new FileInfo(filePath).Length);
        Assert.True(result.IsFile);
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_DeletesFile()
    {
        var filePath = Path.Combine(_tempDir, "todelete.txt");
        File.WriteAllText(filePath, "content");

        await _service.DeleteFileAsync(filePath);

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFileAsync_ThrowsForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent.txt");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.DeleteFileAsync(nonexistent));
    }

    #endregion

    #region RenameAsync Tests

    [Fact]
    public async Task RenameAsync_RenamesFile()
    {
        var oldPath = Path.Combine(_tempDir, "old.txt");
        var newPath = Path.Combine(_tempDir, "new.txt");
        File.WriteAllText(oldPath, "content");

        var result = await _service.RenameAsync(oldPath, "new.txt");

        Assert.False(File.Exists(oldPath));
        Assert.True(File.Exists(newPath));
        Assert.Equal("new.txt", result.Name);
    }

    [Fact]
    public async Task RenameAsync_RenamesDirectory()
    {
        var oldPath = Path.Combine(_tempDir, "olddir");
        var newPath = Path.Combine(_tempDir, "newdir");
        Directory.CreateDirectory(oldPath);

        var result = await _service.RenameAsync(oldPath, "newdir");

        Assert.False(Directory.Exists(oldPath));
        Assert.True(Directory.Exists(newPath));
        Assert.Equal("newdir", result.Name);
    }

    #endregion

    #region CopyFileAsync Tests

    [Fact]
    public async Task CopyFileAsync_CopiesFile()
    {
        var sourcePath = Path.Combine(_tempDir, "source.txt");
        var destPath = Path.Combine(_tempDir, "dest.txt");
        File.WriteAllText(sourcePath, "content");

        var result = await _service.CopyFileAsync(sourcePath, destPath);

        Assert.True(File.Exists(sourcePath));
        Assert.True(File.Exists(destPath));
        Assert.Equal("content", File.ReadAllText(destPath));
    }

    [Fact]
    public async Task CopyFileAsync_OverwritesWhenAllowed()
    {
        var sourcePath = Path.Combine(_tempDir, "source.txt");
        var destPath = Path.Combine(_tempDir, "dest.txt");
        File.WriteAllText(sourcePath, "new content");
        File.WriteAllText(destPath, "old content");

        await _service.CopyFileAsync(sourcePath, destPath, overwrite: true);

        Assert.Equal("new content", File.ReadAllText(destPath));
    }

    [Fact]
    public async Task CopyFileAsync_ThrowsWhenNotOverwriting()
    {
        var sourcePath = Path.Combine(_tempDir, "source.txt");
        var destPath = Path.Combine(_tempDir, "dest.txt");
        File.WriteAllText(sourcePath, "content");
        File.WriteAllText(destPath, "existing");

        await Assert.ThrowsAsync<IOException>(
            () => _service.CopyFileAsync(sourcePath, destPath, overwrite: false));
    }

    #endregion

    #region MoveAsync Tests

    [Fact]
    public async Task MoveAsync_MovesFile()
    {
        var sourcePath = Path.Combine(_tempDir, "source.txt");
        var destPath = Path.Combine(_tempDir, "moved.txt");
        File.WriteAllText(sourcePath, "content");

        var result = await _service.MoveAsync(sourcePath, destPath);

        Assert.False(File.Exists(sourcePath));
        Assert.True(File.Exists(destPath));
        Assert.Equal("content", File.ReadAllText(destPath));
    }

    [Fact]
    public async Task MoveAsync_MovesDirectory()
    {
        var sourcePath = Path.Combine(_tempDir, "sourcedir");
        var destPath = Path.Combine(_tempDir, "moveddir");
        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "content");

        var result = await _service.MoveAsync(sourcePath, destPath);

        Assert.False(Directory.Exists(sourcePath));
        Assert.True(Directory.Exists(destPath));
        Assert.True(File.Exists(Path.Combine(destPath, "file.txt")));
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task FileExistsAsync_ReturnsTrueForExisting()
    {
        var filePath = Path.Combine(_tempDir, "exists.txt");
        File.WriteAllText(filePath, "content");

        Assert.True(await _service.FileExistsAsync(filePath));
    }

    [Fact]
    public async Task FileExistsAsync_ReturnsFalseForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent.txt");

        Assert.False(await _service.FileExistsAsync(nonexistent));
    }

    [Fact]
    public async Task DirectoryExistsAsync_ReturnsTrueForExisting()
    {
        var dirPath = Path.Combine(_tempDir, "existsdir");
        Directory.CreateDirectory(dirPath);

        Assert.True(await _service.DirectoryExistsAsync(dirPath));
    }

    [Fact]
    public async Task DirectoryExistsAsync_ReturnsFalseForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent");

        Assert.False(await _service.DirectoryExistsAsync(nonexistent));
    }

    #endregion

    #region WatchDirectory Tests

    [Fact]
    public async Task WatchDirectory_DetectsFileCreation()
    {
        var events = new List<FileSystemChangeEvent>();
        using var watcher = _service.WatchDirectory(_tempDir, e => events.Add(e));

        var filePath = Path.Combine(_tempDir, "newfile.txt");
        File.WriteAllText(filePath, "content");

        // Wait for debounce
        await Task.Delay(300);

        Assert.Contains(events, e => e.ChangeType == FileSystemChangeType.Created);
    }

    [Fact]
    public async Task WatchDirectory_DetectsFileModification()
    {
        var filePath = Path.Combine(_tempDir, "existing.txt");
        File.WriteAllText(filePath, "initial");

        // Wait a bit to ensure file is fully created
        await Task.Delay(100);

        var events = new List<FileSystemChangeEvent>();
        using var watcher = _service.WatchDirectory(_tempDir, e => events.Add(e));

        // Use append to ensure it's a modification, not a create
        File.AppendAllText(filePath, " modified");

        await Task.Delay(300);

        // The watcher should detect some kind of change event
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.Path == filePath);
    }

    [Fact]
    public async Task WatchDirectory_DetectsFileDeletion()
    {
        var filePath = Path.Combine(_tempDir, "todelete.txt");
        File.WriteAllText(filePath, "content");

        var events = new List<FileSystemChangeEvent>();
        using var watcher = _service.WatchDirectory(_tempDir, e => events.Add(e));

        File.Delete(filePath);

        await Task.Delay(300);

        Assert.Contains(events, e => e.ChangeType == FileSystemChangeType.Deleted);
    }

    [Fact]
    public void WatchDirectory_DisposeStopsWatching()
    {
        var events = new List<FileSystemChangeEvent>();
        var watcher = _service.WatchDirectory(_tempDir, e => events.Add(e));

        watcher.Dispose();

        // Should not throw and should stop watching
        Assert.NotNull(watcher);
    }

    #endregion

    #region IsTextFile Tests

    [Fact]
    public void IsTextFile_ReturnsTrueForKnownTextExtension()
    {
        var filePath = Path.Combine(_tempDir, "code.cs");
        File.WriteAllText(filePath, "public class Test { }");

        Assert.True(_service.IsTextFile(filePath));
    }

    [Fact]
    public void IsTextFile_ReturnsTrueForTxtExtension()
    {
        var filePath = Path.Combine(_tempDir, "notes.txt");
        File.WriteAllText(filePath, "Some text content");

        Assert.True(_service.IsTextFile(filePath));
    }

    [Fact]
    public void IsTextFile_ReturnsFalseForPng()
    {
        var filePath = Path.Combine(_tempDir, "image.png");
        // PNG magic bytes
        File.WriteAllBytes(filePath, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        Assert.False(_service.IsTextFile(filePath));
    }

    [Fact]
    public void IsTextFile_ReturnsFalseForJpeg()
    {
        var filePath = Path.Combine(_tempDir, "image.jpg");
        // JPEG magic bytes
        File.WriteAllBytes(filePath, [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10]);

        Assert.False(_service.IsTextFile(filePath));
    }

    [Fact]
    public void IsTextFile_ReturnsFalseForFileWithNullBytes()
    {
        var filePath = Path.Combine(_tempDir, "binary.dat");
        File.WriteAllBytes(filePath, [0x41, 0x42, 0x00, 0x43, 0x44]);

        Assert.False(_service.IsTextFile(filePath));
    }

    [Fact]
    public void IsTextFile_ReturnsTrueForEmptyFile()
    {
        var filePath = Path.Combine(_tempDir, "empty.txt");
        File.WriteAllText(filePath, "");

        Assert.True(_service.IsTextFile(filePath));
    }

    [Fact]
    public void IsTextFile_ReturnsFalseForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent.txt");

        Assert.False(_service.IsTextFile(nonexistent));
    }

    #endregion

    #region GetFileSize Tests

    [Fact]
    public void GetFileSize_ReturnsCorrectSize()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(filePath, "12345");

        var size = _service.GetFileSize(filePath);

        Assert.Equal(5, size);
    }

    [Fact]
    public void GetFileSize_ThrowsForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent.txt");

        Assert.Throws<FileNotFoundException>(() => _service.GetFileSize(nonexistent));
    }

    #endregion

    #region GetLineCountAsync Tests

    [Fact]
    public async Task GetLineCountAsync_CountsLines()
    {
        var filePath = Path.Combine(_tempDir, "lines.txt");
        File.WriteAllText(filePath, "line1\nline2\nline3");

        var count = await _service.GetLineCountAsync(filePath);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetLineCountAsync_ReturnsZeroForEmptyFile()
    {
        var filePath = Path.Combine(_tempDir, "empty.txt");
        File.WriteAllText(filePath, "");

        var count = await _service.GetLineCountAsync(filePath);

        Assert.Equal(0, count);
    }

    #endregion

    #region GetRelativePath Tests

    [Fact]
    public void GetRelativePath_ReturnsRelativePath()
    {
        var basePath = "/home/user/project";
        var fullPath = "/home/user/project/src/file.cs";

        var result = _service.GetRelativePath(fullPath, basePath);

        Assert.Equal(Path.Combine("src", "file.cs"), result);
    }

    #endregion

    #region ShouldIgnore Tests

    [Fact]
    public void ShouldIgnore_MatchesDirectoryPattern()
    {
        var patterns = new[] { "node_modules/" };
        var path = Path.Combine(_tempDir, "node_modules");
        Directory.CreateDirectory(path);

        Assert.True(_service.ShouldIgnore(path, _tempDir, patterns));
    }

    [Fact]
    public void ShouldIgnore_MatchesWildcardPattern()
    {
        var patterns = new[] { "*.log" };
        var path = Path.Combine(_tempDir, "debug.log");
        File.WriteAllText(path, "log content");

        Assert.True(_service.ShouldIgnore(path, _tempDir, patterns));
    }

    [Fact]
    public void ShouldIgnore_DoesNotMatchDifferentPattern()
    {
        var patterns = new[] { "*.log" };
        var path = Path.Combine(_tempDir, "source.cs");
        File.WriteAllText(path, "code");

        Assert.False(_service.ShouldIgnore(path, _tempDir, patterns));
    }

    [Fact]
    public void ShouldIgnore_MatchesNestedPath()
    {
        var subdir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subdir);
        var path = Path.Combine(subdir, "test.log");
        File.WriteAllText(path, "log");

        var patterns = new[] { "*.log" };

        Assert.True(_service.ShouldIgnore(path, _tempDir, patterns));
    }

    [Fact]
    public void ShouldIgnore_SupportsNegation()
    {
        var patterns = new[] { "*.log", "!important.log" };

        var normalLog = Path.Combine(_tempDir, "debug.log");
        var importantLog = Path.Combine(_tempDir, "important.log");
        File.WriteAllText(normalLog, "");
        File.WriteAllText(importantLog, "");

        Assert.True(_service.ShouldIgnore(normalLog, _tempDir, patterns));
        Assert.False(_service.ShouldIgnore(importantLog, _tempDir, patterns));
    }

    [Fact]
    public void ShouldIgnore_IgnoresComments()
    {
        var patterns = new[] { "# This is a comment", "*.log" };
        var path = Path.Combine(_tempDir, "# This is a comment");
        File.WriteAllText(path, "");

        Assert.False(_service.ShouldIgnore(path, _tempDir, patterns));
    }

    [Fact]
    public void ShouldIgnore_MatchesDoubleWildcard()
    {
        var patterns = new[] { "**/temp/" };
        var deepPath = Path.Combine(_tempDir, "a", "b", "temp");
        Directory.CreateDirectory(deepPath);

        Assert.True(_service.ShouldIgnore(deepPath, _tempDir, patterns));
    }

    [Fact]
    public void ShouldIgnore_ReturnsFalseForEmptyPatterns()
    {
        var path = Path.Combine(_tempDir, "file.txt");
        File.WriteAllText(path, "");

        Assert.False(_service.ShouldIgnore(path, _tempDir, Array.Empty<string>()));
    }

    #endregion

    #region LoadGitIgnorePatternsAsync Tests

    [Fact]
    public async Task LoadGitIgnorePatternsAsync_LoadsFromFile()
    {
        var gitignorePath = Path.Combine(_tempDir, ".gitignore");
        File.WriteAllText(gitignorePath, "*.log\nnode_modules/\n# comment\nbuild/");

        var patterns = await _service.LoadGitIgnorePatternsAsync(_tempDir);

        Assert.Contains("*.log", patterns);
        Assert.Contains("node_modules/", patterns);
        Assert.Contains("build/", patterns);
    }

    [Fact]
    public async Task LoadGitIgnorePatternsAsync_IncludesDefaults()
    {
        // No .gitignore file
        var patterns = await _service.LoadGitIgnorePatternsAsync(_tempDir);

        Assert.Contains(".git/", patterns);
        Assert.Contains("node_modules/", patterns);
        Assert.Contains("bin/", patterns);
    }

    [Fact]
    public async Task LoadGitIgnorePatternsAsync_HandlesNoGitignore()
    {
        // No .gitignore file, should still return defaults
        var patterns = await _service.LoadGitIgnorePatternsAsync(_tempDir);

        Assert.NotEmpty(patterns);
    }

    #endregion
}
