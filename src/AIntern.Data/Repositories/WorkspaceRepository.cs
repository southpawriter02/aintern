using Microsoft.EntityFrameworkCore;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Data.Repositories;

/// <summary>
/// EF Core implementation of workspace repository (v0.3.1f).
/// </summary>
public sealed class WorkspaceRepository : IWorkspaceRepository
{
    private const int MaxRecentWorkspaces = 20;

    private readonly IDbContextFactory<AInternDbContext> _contextFactory;

    public WorkspaceRepository(IDbContextFactory<AInternDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<IReadOnlyList<Workspace>> GetRecentAsync(
        int count = 10,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var entities = await context.RecentWorkspaces
            .AsNoTracking()
            .OrderByDescending(w => w.IsPinned)
            .ThenByDescending(w => w.LastAccessedAt)
            .Take(count)
            .ToListAsync(ct);

        return entities.Select(e => e.ToWorkspace()).ToList();
    }

    public async Task<Workspace?> GetByPathAsync(
        string rootPath,
        CancellationToken ct = default)
    {
        // Normalize path for comparison
        rootPath = Path.GetFullPath(rootPath);

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var entity = await context.RecentWorkspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.RootPath == rootPath, ct);

        return entity?.ToWorkspace();
    }

    public async Task AddOrUpdateAsync(
        Workspace workspace,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var normalizedPath = Path.GetFullPath(workspace.RootPath);

        var existing = await context.RecentWorkspaces
            .FirstOrDefaultAsync(w => w.RootPath == normalizedPath, ct);

        if (existing != null)
        {
            // Update existing entity
            existing.UpdateFrom(workspace);
        }
        else
        {
            // Add new entity
            var entity = RecentWorkspaceEntity.FromWorkspace(workspace);
            entity.RootPath = normalizedPath;
            context.RecentWorkspaces.Add(entity);
        }

        await context.SaveChangesAsync(ct);

        // Enforce max recent workspaces limit
        await EnforceMaxRecentAsync(context, ct);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var entity = await context.RecentWorkspaces.FindAsync([id], ct);
        if (entity != null)
        {
            context.RecentWorkspaces.Remove(entity);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task ClearAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await context.RecentWorkspaces.ExecuteDeleteAsync(ct);
    }

    public async Task SetPinnedAsync(
        Guid id,
        bool isPinned,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        await context.RecentWorkspaces
            .Where(w => w.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(w => w.IsPinned, isPinned),
                ct);
    }

    public async Task RenameAsync(
        Guid id,
        string newName,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var name = string.IsNullOrWhiteSpace(newName) ? null : newName;

        await context.RecentWorkspaces
            .Where(w => w.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(w => w.Name, name),
                ct);
    }

    /// <summary>
    /// Enforces maximum 20 recent workspaces, preserving pinned entries.
    /// </summary>
    private static async Task EnforceMaxRecentAsync(AInternDbContext context, CancellationToken ct)
    {
        var count = await context.RecentWorkspaces.CountAsync(ct);

        if (count <= MaxRecentWorkspaces)
            return;

        // Get IDs of oldest non-pinned workspaces to remove
        var toRemoveCount = count - MaxRecentWorkspaces;

        var toRemove = await context.RecentWorkspaces
            .Where(w => !w.IsPinned)
            .OrderBy(w => w.LastAccessedAt)
            .Take(toRemoveCount)
            .Select(w => w.Id)
            .ToListAsync(ct);

        if (toRemove.Count > 0)
        {
            await context.RecentWorkspaces
                .Where(w => toRemove.Contains(w.Id))
                .ExecuteDeleteAsync(ct);
        }
    }
}
