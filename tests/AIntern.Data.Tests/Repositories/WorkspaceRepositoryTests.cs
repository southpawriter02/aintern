using Xunit;
using AIntern.Core.Models;
using AIntern.Data.Repositories;

namespace AIntern.Data.Tests.Repositories;

/// <summary>
/// Integration tests for WorkspaceRepository (v0.3.1f).
/// </summary>
public class WorkspaceRepositoryTests : IDisposable
{
    private readonly TestDbContextFactoryWrapper _contextFactory;
    private readonly WorkspaceRepository _repository;

    public WorkspaceRepositoryTests()
    {
        _contextFactory = new TestDbContextFactoryWrapper();
        _repository = new WorkspaceRepository(_contextFactory);
    }

    public void Dispose()
    {
        _contextFactory.Dispose();
    }

    #region GetRecentAsync Tests

    [Fact]
    public async Task GetRecentAsync_OrdersByPinnedThenLastAccessed()
    {
        await _repository.AddOrUpdateAsync(new Workspace
        {
            RootPath = "/old",
            LastAccessedAt = DateTime.UtcNow.AddDays(-2)
        });
        await _repository.AddOrUpdateAsync(new Workspace
        {
            RootPath = "/new",
            LastAccessedAt = DateTime.UtcNow
        });
        await _repository.AddOrUpdateAsync(new Workspace
        {
            RootPath = "/pinned",
            LastAccessedAt = DateTime.UtcNow.AddDays(-3),
            IsPinned = true
        });

        var result = await _repository.GetRecentAsync(10);

        Assert.Equal(3, result.Count);
        Assert.Equal("/pinned", result[0].RootPath);
        Assert.Equal("/new", result[1].RootPath);
        Assert.Equal("/old", result[2].RootPath);
    }

    [Fact]
    public async Task GetRecentAsync_RespectsCountLimit()
    {
        for (int i = 0; i < 5; i++)
            await _repository.AddOrUpdateAsync(new Workspace { RootPath = $"/project{i}" });

        var result = await _repository.GetRecentAsync(3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsEmptyWhenNoWorkspaces()
    {
        var result = await _repository.GetRecentAsync(10);

        Assert.Empty(result);
    }

    #endregion

    #region GetByPathAsync Tests

    [Fact]
    public async Task GetByPathAsync_ReturnsWorkspace_WhenExists()
    {
        await _repository.AddOrUpdateAsync(new Workspace
        {
            RootPath = "/project/test",
            Name = "Test Project"
        });

        var result = await _repository.GetByPathAsync("/project/test");

        Assert.NotNull(result);
        Assert.Equal("Test Project", result.Name);
    }

    [Fact]
    public async Task GetByPathAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByPathAsync("/nonexistent");

        Assert.Null(result);
    }

    #endregion

    #region AddOrUpdateAsync Tests

    [Fact]
    public async Task AddOrUpdateAsync_AddsNewWorkspace()
    {
        var workspace = new Workspace
        {
            RootPath = "/new/project",
            Name = "New Project"
        };

        await _repository.AddOrUpdateAsync(workspace);

        var result = await _repository.GetByPathAsync("/new/project");
        Assert.NotNull(result);
        Assert.Equal("New Project", result.Name);
    }

    [Fact]
    public async Task AddOrUpdateAsync_UpdatesExistingByPath()
    {
        await _repository.AddOrUpdateAsync(new Workspace
        {
            RootPath = "/project",
            Name = "Original"
        });

        await _repository.AddOrUpdateAsync(new Workspace
        {
            RootPath = "/project",
            Name = "Updated"
        });

        var result = await _repository.GetByPathAsync("/project");
        Assert.Equal("Updated", result?.Name);

        // Should only have one entry
        var all = await _repository.GetRecentAsync(100);
        Assert.Single(all);
    }

    [Fact]
    public async Task AddOrUpdateAsync_PreservesExistingId()
    {
        var original = new Workspace { RootPath = "/project" };
        await _repository.AddOrUpdateAsync(original);
        var firstResult = await _repository.GetByPathAsync("/project");

        await _repository.AddOrUpdateAsync(new Workspace { RootPath = "/project", Name = "Updated" });
        var secondResult = await _repository.GetByPathAsync("/project");

        Assert.Equal(firstResult!.Id, secondResult!.Id);
    }

    [Fact]
    public async Task AddOrUpdateAsync_EnforcesMaxLimit()
    {
        // Add 25 workspaces
        for (int i = 0; i < 25; i++)
        {
            await _repository.AddOrUpdateAsync(new Workspace
            {
                RootPath = $"/project{i}",
                LastAccessedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        var all = await _repository.GetRecentAsync(100);
        Assert.Equal(20, all.Count); // Max limit
    }

    [Fact]
    public async Task AddOrUpdateAsync_PreservesPinnedWhenEnforcing()
    {
        // Add pinned workspace first
        await _repository.AddOrUpdateAsync(new Workspace
        {
            RootPath = "/pinned",
            IsPinned = true,
            LastAccessedAt = DateTime.UtcNow.AddDays(-100) // Very old
        });

        // Add 25 more non-pinned
        for (int i = 0; i < 25; i++)
        {
            await _repository.AddOrUpdateAsync(new Workspace
            {
                RootPath = $"/project{i}",
                LastAccessedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        var pinned = await _repository.GetByPathAsync("/pinned");
        Assert.NotNull(pinned); // Pinned preserved even though old
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_RemovesWorkspace()
    {
        var workspace = new Workspace { RootPath = "/to-delete" };
        await _repository.AddOrUpdateAsync(workspace);
        var added = await _repository.GetByPathAsync("/to-delete");

        await _repository.RemoveAsync(added!.Id);

        var result = await _repository.GetByPathAsync("/to-delete");
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_DoesNothingWhenNotExists()
    {
        // Should not throw
        await _repository.RemoveAsync(Guid.NewGuid());
    }

    #endregion

    #region ClearAllAsync Tests

    [Fact]
    public async Task ClearAllAsync_RemovesAllWorkspaces()
    {
        await _repository.AddOrUpdateAsync(new Workspace { RootPath = "/project1" });
        await _repository.AddOrUpdateAsync(new Workspace { RootPath = "/project2" });
        await _repository.AddOrUpdateAsync(new Workspace { RootPath = "/project3" });

        await _repository.ClearAllAsync();

        var all = await _repository.GetRecentAsync(100);
        Assert.Empty(all);
    }

    #endregion

    #region SetPinnedAsync Tests

    [Fact]
    public async Task SetPinnedAsync_PinsWorkspace()
    {
        var workspace = new Workspace { RootPath = "/project", IsPinned = false };
        await _repository.AddOrUpdateAsync(workspace);
        var added = await _repository.GetByPathAsync("/project");

        await _repository.SetPinnedAsync(added!.Id, true);

        var result = await _repository.GetByPathAsync("/project");
        Assert.True(result?.IsPinned);
    }

    [Fact]
    public async Task SetPinnedAsync_UnpinsWorkspace()
    {
        var workspace = new Workspace { RootPath = "/project", IsPinned = true };
        await _repository.AddOrUpdateAsync(workspace);
        var added = await _repository.GetByPathAsync("/project");

        await _repository.SetPinnedAsync(added!.Id, false);

        var result = await _repository.GetByPathAsync("/project");
        Assert.False(result?.IsPinned);
    }

    #endregion

    #region RenameAsync Tests

    [Fact]
    public async Task RenameAsync_SetsCustomName()
    {
        var workspace = new Workspace { RootPath = "/project" };
        await _repository.AddOrUpdateAsync(workspace);
        var added = await _repository.GetByPathAsync("/project");

        await _repository.RenameAsync(added!.Id, "Custom Name");

        var result = await _repository.GetByPathAsync("/project");
        Assert.Equal("Custom Name", result?.Name);
    }

    [Fact]
    public async Task RenameAsync_ClearsNameWhenEmpty()
    {
        var workspace = new Workspace { RootPath = "/project", Name = "Has Name" };
        await _repository.AddOrUpdateAsync(workspace);
        var added = await _repository.GetByPathAsync("/project");

        await _repository.RenameAsync(added!.Id, "");

        var result = await _repository.GetByPathAsync("/project");
        Assert.True(string.IsNullOrEmpty(result?.Name));
    }

    #endregion
}
