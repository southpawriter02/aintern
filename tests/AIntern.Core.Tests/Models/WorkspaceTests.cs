using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for Workspace model (v0.3.1a).
/// </summary>
public class WorkspaceTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var workspace = new Workspace { RootPath = "/test/path" };

        Assert.NotEqual(Guid.Empty, workspace.Id);
        Assert.Equal(string.Empty, workspace.Name);
        Assert.Equal("/test/path", workspace.RootPath);
        Assert.True(workspace.OpenedAt <= DateTime.UtcNow);
        Assert.True(workspace.LastAccessedAt <= DateTime.UtcNow);
        Assert.Empty(workspace.OpenFiles);
        Assert.Null(workspace.ActiveFilePath);
        Assert.Empty(workspace.ExpandedFolders);
        Assert.False(workspace.IsPinned);
        Assert.Empty(workspace.GitIgnorePatterns);
    }

    [Fact]
    public void DisplayName_ReturnsCustomName_WhenSet()
    {
        var workspace = new Workspace
        {
            RootPath = "/test/myproject",
            Name = "My Custom Name"
        };

        Assert.Equal("My Custom Name", workspace.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsFolderName_WhenNameIsEmpty()
    {
        var workspace = new Workspace { RootPath = "/test/myproject" };

        Assert.Equal("myproject", workspace.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsFolderName_WhenNameIsWhitespace()
    {
        var workspace = new Workspace
        {
            RootPath = "/test/myproject",
            Name = "   "
        };

        Assert.Equal("myproject", workspace.DisplayName);
    }

    [Fact]
    public void GetAbsolutePath_CombinesPathsCorrectly()
    {
        var workspace = new Workspace { RootPath = "/home/user/project" };

        var result = workspace.GetAbsolutePath("src/file.cs");

        Assert.EndsWith("src/file.cs", result.Replace("\\", "/"));
    }

    [Fact]
    public void GetRelativePath_ExtractsRelativePathCorrectly()
    {
        var workspace = new Workspace { RootPath = "/home/user/project" };

        var result = workspace.GetRelativePath("/home/user/project/src/file.cs");

        Assert.Equal("src/file.cs", result.Replace("\\", "/"));
    }

    [Fact]
    public void ContainsPath_ReturnsTrueForPathsWithinWorkspace()
    {
        var workspace = new Workspace { RootPath = "/home/user/project" };

        Assert.True(workspace.ContainsPath("/home/user/project/src/file.cs"));
        Assert.True(workspace.ContainsPath("/home/user/project/"));
        Assert.True(workspace.ContainsPath("/home/user/project"));
    }

    [Fact]
    public void ContainsPath_ReturnsFalseForPathsOutsideWorkspace()
    {
        var workspace = new Workspace { RootPath = "/home/user/project" };

        Assert.False(workspace.ContainsPath("/home/user/other/file.cs"));
        Assert.False(workspace.ContainsPath("/different/path"));
    }

    [Fact]
    public void Touch_UpdatesLastAccessedAt()
    {
        var workspace = new Workspace { RootPath = "/test" };
        var originalTime = workspace.LastAccessedAt;

        // Small delay to ensure time difference
        Thread.Sleep(10);
        workspace.Touch();

        Assert.True(workspace.LastAccessedAt >= originalTime);
    }

    [Fact]
    public void OpenFiles_CanBeModified()
    {
        var workspace = new Workspace { RootPath = "/test" };

        workspace.OpenFiles = new List<string> { "file1.cs", "file2.cs" };

        Assert.Equal(2, workspace.OpenFiles.Count);
        Assert.Contains("file1.cs", workspace.OpenFiles);
    }

    [Fact]
    public void ExpandedFolders_CanBeModified()
    {
        var workspace = new Workspace { RootPath = "/test" };

        workspace.ExpandedFolders = new List<string> { "src", "tests" };

        Assert.Equal(2, workspace.ExpandedFolders.Count);
        Assert.Contains("src", workspace.ExpandedFolders);
    }
}
