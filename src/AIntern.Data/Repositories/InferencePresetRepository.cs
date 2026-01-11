using Microsoft.EntityFrameworkCore;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository implementation for inference preset data access operations.
/// </summary>
public sealed class InferencePresetRepository : IInferencePresetRepository
{
    private readonly IDbContextFactory<AInternDbContext> _contextFactory;

    public InferencePresetRepository(IDbContextFactory<AInternDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<InferencePresetEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.InferencePresets
            .AsNoTracking()
            .FirstOrDefaultAsync(ip => ip.Id == id, ct);
    }

    public async Task<IReadOnlyList<InferencePresetEntity>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.InferencePresets
            .AsNoTracking()
            .OrderByDescending(ip => ip.IsDefault)
            .ThenByDescending(ip => ip.IsBuiltIn)
            .ThenBy(ip => ip.Name)
            .ToListAsync(ct);
    }

    public async Task<InferencePresetEntity?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.InferencePresets
            .AsNoTracking()
            .FirstOrDefaultAsync(ip => ip.IsDefault, ct);
    }

    public async Task<IReadOnlyList<InferencePresetEntity>> GetBuiltInAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.InferencePresets
            .AsNoTracking()
            .Where(ip => ip.IsBuiltIn)
            .OrderBy(ip => ip.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InferencePresetEntity>> GetUserPresetsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.InferencePresets
            .AsNoTracking()
            .Where(ip => !ip.IsBuiltIn)
            .OrderBy(ip => ip.Name)
            .ToListAsync(ct);
    }

    public async Task<InferencePresetEntity> CreateAsync(InferencePresetEntity preset, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.InferencePresets.Add(preset);
        await context.SaveChangesAsync(ct);
        return preset;
    }

    public async Task UpdateAsync(InferencePresetEntity preset, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.InferencePresets.Update(preset);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var preset = await context.InferencePresets.FindAsync(new object[] { id }, ct);

        // Only allow deletion of user presets
        if (preset is not null && !preset.IsBuiltIn)
        {
            context.InferencePresets.Remove(preset);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Clear existing default
        await context.InferencePresets
            .Where(ip => ip.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(ip => ip.IsDefault, false), ct);

        // Set new default
        await context.InferencePresets
            .Where(ip => ip.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(ip => ip.IsDefault, true), ct);
    }
}
