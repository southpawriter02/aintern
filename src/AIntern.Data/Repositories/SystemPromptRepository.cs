using Microsoft.EntityFrameworkCore;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository implementation for system prompt data access operations.
/// </summary>
public sealed class SystemPromptRepository : ISystemPromptRepository
{
    private readonly IDbContextFactory<AInternDbContext> _contextFactory;

    public SystemPromptRepository(IDbContextFactory<AInternDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<SystemPromptEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.Id == id, ct);
    }

    public async Task<IReadOnlyList<SystemPromptEntity>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .OrderByDescending(sp => sp.IsDefault)
            .ThenBy(sp => sp.Name)
            .ToListAsync(ct);
    }

    public async Task<SystemPromptEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.SystemPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.IsDefault, ct);
    }

    public async Task<SystemPromptEntity> CreateAsync(SystemPromptEntity prompt, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.SystemPrompts.Add(prompt);
        await context.SaveChangesAsync(ct);
        return prompt;
    }

    public async Task UpdateAsync(SystemPromptEntity prompt, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        prompt.UpdatedAt = DateTime.UtcNow;
        context.SystemPrompts.Update(prompt);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var prompt = await context.SystemPrompts.FindAsync(new object[] { id }, ct);
        if (prompt is not null)
        {
            context.SystemPrompts.Remove(prompt);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Clear existing default
        await context.SystemPrompts
            .Where(sp => sp.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(sp => sp.IsDefault, false), ct);

        // Set new default
        await context.SystemPrompts
            .Where(sp => sp.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(sp => sp.IsDefault, true), ct);
    }

    public async Task IncrementUsageCountAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await context.SystemPrompts
            .Where(sp => sp.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(sp => sp.UsageCount, sp => sp.UsageCount + 1), ct);
    }
}
