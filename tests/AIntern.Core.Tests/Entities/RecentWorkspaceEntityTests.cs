using Xunit;
using AIntern.Core.Entities;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Entities;

/// <summary>
/// Unit tests for RecentWorkspaceEntity (v0.3.1b).
/// </summary>
public class RecentWorkspaceEntityTests
{
    [Fact]
    public void ToWorkspace_MapsAllProperties()
    {
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            RootPath = "/test/path",
            LastAccessedAt = DateTime.UtcNow,
            OpenFilesJson = "[\"file1.cs\",\"file2.cs\"]",
            ActiveFilePath = "file1.cs",
            ExpandedFoldersJson = "[\"src\",\"tests\"]",
            IsPinned = true
        };

        var workspace = entity.ToWorkspace();

        Assert.Equal(entity.Id, workspace.Id);
        Assert.Equal(entity.Name, workspace.Name);
        Assert.Equal(entity.RootPath, workspace.RootPath);
        Assert.Equal(entity.LastAccessedAt, workspace.LastAccessedAt);
        Assert.Equal(entity.ActiveFilePath, workspace.ActiveFilePath);
        Assert.Equal(entity.IsPinned, workspace.IsPinned);
        Assert.Equal(2, workspace.OpenFiles.Count);
        Assert.Contains("file1.cs", workspace.OpenFiles);
        Assert.Equal(2, workspace.ExpandedFolders.Count);
        Assert.Contains("src", workspace.ExpandedFolders);
    }

    [Fact]
    public void ToWorkspace_HandlesNullJson()
    {
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            RootPath = "/test/path",
            LastAccessedAt = DateTime.UtcNow,
            OpenFilesJson = null,
            ExpandedFoldersJson = null
        };

        var workspace = entity.ToWorkspace();

        Assert.Empty(workspace.OpenFiles);
        Assert.Empty(workspace.ExpandedFolders);
    }

    [Fact]
    public void ToWorkspace_HandlesEmptyJson()
    {
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            RootPath = "/test/path",
            LastAccessedAt = DateTime.UtcNow,
            OpenFilesJson = "",
            ExpandedFoldersJson = ""
        };

        var workspace = entity.ToWorkspace();

        Assert.Empty(workspace.OpenFiles);
        Assert.Empty(workspace.ExpandedFolders);
    }

    [Fact]
    public void ToWorkspace_HandlesInvalidJson()
    {
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            RootPath = "/test/path",
            LastAccessedAt = DateTime.UtcNow,
            OpenFilesJson = "not valid json",
            ExpandedFoldersJson = "{invalid}"
        };

        var workspace = entity.ToWorkspace();

        // Should handle gracefully and return empty lists
        Assert.Empty(workspace.OpenFiles);
        Assert.Empty(workspace.ExpandedFolders);
    }

    [Fact]
    public void FromWorkspace_MapsAllProperties()
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Test Workspace",
            RootPath = "/test/path",
            LastAccessedAt = DateTime.UtcNow,
            OpenFiles = new List<string> { "file1.cs", "file2.cs" },
            ActiveFilePath = "file1.cs",
            ExpandedFolders = new List<string> { "src", "tests" },
            IsPinned = true
        };

        var entity = RecentWorkspaceEntity.FromWorkspace(workspace);

        Assert.Equal(workspace.Id, entity.Id);
        Assert.Equal(workspace.Name, entity.Name);
        Assert.Equal(workspace.RootPath, entity.RootPath);
        Assert.Equal(workspace.LastAccessedAt, entity.LastAccessedAt);
        Assert.Equal(workspace.ActiveFilePath, entity.ActiveFilePath);
        Assert.Equal(workspace.IsPinned, entity.IsPinned);
        Assert.NotNull(entity.OpenFilesJson);
        Assert.Contains("file1.cs", entity.OpenFilesJson);
        Assert.NotNull(entity.ExpandedFoldersJson);
        Assert.Contains("src", entity.ExpandedFoldersJson);
    }

    [Fact]
    public void FromWorkspace_HandlesEmptyLists()
    {
        var workspace = new Workspace
        {
            RootPath = "/test/path",
            OpenFiles = new List<string>(),
            ExpandedFolders = new List<string>()
        };

        var entity = RecentWorkspaceEntity.FromWorkspace(workspace);

        Assert.Null(entity.OpenFilesJson);
        Assert.Null(entity.ExpandedFoldersJson);
    }

    [Fact]
    public void FromWorkspace_HandlesEmptyName()
    {
        var workspace = new Workspace
        {
            RootPath = "/test/path",
            Name = ""
        };

        var entity = RecentWorkspaceEntity.FromWorkspace(workspace);

        Assert.Null(entity.Name);
    }

    [Fact]
    public void FromWorkspace_HandlesWhitespaceName()
    {
        var workspace = new Workspace
        {
            RootPath = "/test/path",
            Name = "   "
        };

        var entity = RecentWorkspaceEntity.FromWorkspace(workspace);

        Assert.Null(entity.Name);
    }

    [Fact]
    public void UpdateFrom_UpdatesAllMutableProperties()
    {
        var entity = new RecentWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            RootPath = "/test/path",
            Name = "Old Name",
            LastAccessedAt = DateTime.UtcNow.AddDays(-1),
            IsPinned = false
        };

        var workspace = new Workspace
        {
            RootPath = "/test/path",
            Name = "New Name",
            LastAccessedAt = DateTime.UtcNow,
            OpenFiles = new List<string> { "new.cs" },
            ActiveFilePath = "new.cs",
            ExpandedFolders = new List<string> { "newFolder" },
            IsPinned = true
        };

        entity.UpdateFrom(workspace);

        Assert.Equal("New Name", entity.Name);
        Assert.Equal(workspace.LastAccessedAt, entity.LastAccessedAt);
        Assert.Equal(workspace.ActiveFilePath, entity.ActiveFilePath);
        Assert.Equal(workspace.IsPinned, entity.IsPinned);
        Assert.Contains("new.cs", entity.OpenFilesJson!);
        Assert.Contains("newFolder", entity.ExpandedFoldersJson!);
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var originalWorkspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "My Project",
            RootPath = "/home/user/project",
            LastAccessedAt = DateTime.UtcNow,
            OpenFiles = new List<string> { "Program.cs", "Startup.cs" },
            ActiveFilePath = "Program.cs",
            ExpandedFolders = new List<string> { "Controllers", "Models", "Views" },
            IsPinned = true
        };

        var entity = RecentWorkspaceEntity.FromWorkspace(originalWorkspace);
        var roundTrippedWorkspace = entity.ToWorkspace();

        Assert.Equal(originalWorkspace.Id, roundTrippedWorkspace.Id);
        Assert.Equal(originalWorkspace.Name, roundTrippedWorkspace.Name);
        Assert.Equal(originalWorkspace.RootPath, roundTrippedWorkspace.RootPath);
        Assert.Equal(originalWorkspace.LastAccessedAt, roundTrippedWorkspace.LastAccessedAt);
        Assert.Equal(originalWorkspace.ActiveFilePath, roundTrippedWorkspace.ActiveFilePath);
        Assert.Equal(originalWorkspace.IsPinned, roundTrippedWorkspace.IsPinned);
        Assert.Equal(originalWorkspace.OpenFiles, roundTrippedWorkspace.OpenFiles);
        Assert.Equal(originalWorkspace.ExpandedFolders, roundTrippedWorkspace.ExpandedFolders);
    }
}
