using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for FileSystemItem model (v0.3.1a).
/// </summary>
public class FileSystemItemTests
{
    [Fact]
    public void IsDirectory_TrueForDirectoryType()
    {
        var item = new FileSystemItem
        {
            Path = "/test/folder",
            Name = "folder",
            Type = FileSystemItemType.Directory
        };

        Assert.True(item.IsDirectory);
        Assert.False(item.IsFile);
    }

    [Fact]
    public void IsFile_TrueForFileType()
    {
        var item = new FileSystemItem
        {
            Path = "/test/file.cs",
            Name = "file.cs",
            Type = FileSystemItemType.File
        };

        Assert.True(item.IsFile);
        Assert.False(item.IsDirectory);
    }

    [Fact]
    public void Extension_ReturnsExtensionForFiles()
    {
        var item = new FileSystemItem
        {
            Path = "/test/file.cs",
            Name = "file.cs",
            Type = FileSystemItemType.File
        };

        Assert.Equal(".cs", item.Extension);
    }

    [Fact]
    public void Extension_ReturnsEmptyForDirectories()
    {
        var item = new FileSystemItem
        {
            Path = "/test/folder",
            Name = "folder",
            Type = FileSystemItemType.Directory
        };

        Assert.Equal(string.Empty, item.Extension);
    }

    [Fact]
    public void Language_ReturnsDetectedLanguageForFiles()
    {
        var item = new FileSystemItem
        {
            Path = "/test/file.cs",
            Name = "file.cs",
            Type = FileSystemItemType.File
        };

        Assert.Equal("csharp", item.Language);
    }

    [Fact]
    public void Language_ReturnsNullForDirectories()
    {
        var item = new FileSystemItem
        {
            Path = "/test/folder",
            Name = "folder",
            Type = FileSystemItemType.Directory
        };

        Assert.Null(item.Language);
    }

    [Fact]
    public void Language_ReturnsNullForUnknownExtensions()
    {
        var item = new FileSystemItem
        {
            Path = "/test/file.unknown",
            Name = "file.unknown",
            Type = FileSystemItemType.File
        };

        Assert.Null(item.Language);
    }

    [Fact]
    public void FormattedSize_ReturnsEmptyForDirectories()
    {
        var item = new FileSystemItem
        {
            Path = "/test/folder",
            Name = "folder",
            Type = FileSystemItemType.Directory,
            Size = null
        };

        Assert.Equal(string.Empty, item.FormattedSize);
    }

    [Fact]
    public void FormattedSize_FormatsBytes()
    {
        var item = new FileSystemItem
        {
            Path = "/test/file.txt",
            Name = "file.txt",
            Type = FileSystemItemType.File,
            Size = 500
        };

        Assert.Equal("500 B", item.FormattedSize);
    }

    [Fact]
    public void FormattedSize_FormatsKilobytes()
    {
        var item = new FileSystemItem
        {
            Path = "/test/file.txt",
            Name = "file.txt",
            Type = FileSystemItemType.File,
            Size = 1536  // 1.5 KB
        };

        Assert.Equal("1.5 KB", item.FormattedSize);
    }

    [Fact]
    public void FormattedSize_FormatsMegabytes()
    {
        var item = new FileSystemItem
        {
            Path = "/test/file.txt",
            Name = "file.txt",
            Type = FileSystemItemType.File,
            Size = 2 * 1024 * 1024  // 2 MB
        };

        Assert.Equal("2.0 MB", item.FormattedSize);
    }

    [Fact]
    public void ParentPath_ReturnsParentDirectory()
    {
        var item = new FileSystemItem
        {
            Path = "/home/user/project/file.cs",
            Name = "file.cs",
            Type = FileSystemItemType.File
        };

        Assert.EndsWith("project", item.ParentPath?.Replace("\\", "/") ?? "");
    }

    [Fact]
    public void FromFileInfo_CreatesCorrectItem()
    {
        // Create a temp file for testing
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test content");
            var fileInfo = new FileInfo(tempFile);

            var item = FileSystemItem.FromFileInfo(fileInfo);

            Assert.Equal(fileInfo.FullName, item.Path);
            Assert.Equal(fileInfo.Name, item.Name);
            Assert.Equal(FileSystemItemType.File, item.Type);
            Assert.Equal(fileInfo.Length, item.Size);
            Assert.False(item.IsHidden);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void FromDirectoryInfo_CreatesCorrectItem()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            var dirInfo = new DirectoryInfo(tempDir);

            var item = FileSystemItem.FromDirectoryInfo(dirInfo, hasChildren: false);

            Assert.Equal(dirInfo.FullName, item.Path);
            Assert.Equal(dirInfo.Name, item.Name);
            Assert.Equal(FileSystemItemType.Directory, item.Type);
            Assert.Null(item.Size);
            Assert.False(item.HasChildren);
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void FromDirectoryInfo_SetsHasChildren()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(tempDir);
            var dirInfo = new DirectoryInfo(tempDir);

            var item = FileSystemItem.FromDirectoryInfo(dirInfo, hasChildren: true);

            Assert.True(item.HasChildren);
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void Children_CanBeSet()
    {
        var parent = new FileSystemItem
        {
            Path = "/test",
            Name = "test",
            Type = FileSystemItemType.Directory
        };

        var child = new FileSystemItem
        {
            Path = "/test/child",
            Name = "child",
            Type = FileSystemItemType.File
        };

        parent.Children = new List<FileSystemItem> { child };

        Assert.Single(parent.Children);
        Assert.Equal("child", parent.Children.First().Name);
    }

    [Fact]
    public void IsExpanded_CanBeToggled()
    {
        var item = new FileSystemItem
        {
            Path = "/test",
            Name = "test",
            Type = FileSystemItemType.Directory
        };

        Assert.False(item.IsExpanded);

        item.IsExpanded = true;
        Assert.True(item.IsExpanded);
    }
}
