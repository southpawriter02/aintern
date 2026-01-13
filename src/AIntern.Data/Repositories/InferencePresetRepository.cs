using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository implementation for managing inference presets.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides a clean abstraction over Entity Framework Core operations
/// for inference presets, with comprehensive logging support.
/// </para>
/// <para>
/// Key implementation details:
/// </para>
/// <list type="bullet">
///   <item><description>Protects built-in presets from deletion</description></item>
///   <item><description>Provides atomic default switching</description></item>
///   <item><description>Supports preset duplication for customization</description></item>
///   <item><description>Automatic default reassignment on deletion</description></item>
/// </list>
/// </remarks>
public sealed class InferencePresetRepository : IInferencePresetRepository
{
    #region Fields

    /// <summary>
    /// The database context for Entity Framework operations.
    /// </summary>
    private readonly AInternDbContext _context;

    /// <summary>
    /// Logger instance for diagnostic output.
    /// </summary>
    private readonly ILogger<InferencePresetRepository> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="InferencePresetRepository"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// If no logger is provided, a <see cref="NullLogger{T}"/> is used.
    /// </remarks>
    public InferencePresetRepository(AInternDbContext context, ILogger<InferencePresetRepository>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? NullLogger<InferencePresetRepository>.Instance;
        _logger.LogDebug("InferencePresetRepository instance created");
    }

    #endregion

    #region Read Operations

    /// <inheritdoc />
    public async Task<InferencePresetEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting inference preset by ID: {PresetId}", id);

        var preset = await _context.InferencePresets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (preset == null)
        {
            _logger.LogDebug("Inference preset not found: {PresetId}", id);
        }

        return preset;
    }

    /// <inheritdoc />
    public async Task<InferencePresetEntity?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting default inference preset");

        var preset = await _context.InferencePresets
            .AsNoTracking()
            .Where(p => p.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        if (preset == null)
        {
            _logger.LogDebug("No default inference preset found");
        }
        else
        {
            _logger.LogDebug("Default inference preset found: {PresetId} ({Name})", preset.Id, preset.Name);
        }

        return preset;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferencePresetEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all inference presets");

        var presets = await _context.InferencePresets
            .AsNoTracking()
            .OrderByDescending(p => p.IsBuiltIn)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} inference presets", presets.Count);

        return presets;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferencePresetEntity>> GetBuiltInAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting built-in inference presets");

        var presets = await _context.InferencePresets
            .AsNoTracking()
            .Where(p => p.IsBuiltIn)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} built-in inference presets", presets.Count);

        return presets;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferencePresetEntity>> GetUserCreatedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting user-created inference presets");

        var presets = await _context.InferencePresets
            .AsNoTracking()
            .Where(p => !p.IsBuiltIn)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} user-created inference presets", presets.Count);

        return presets;
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if inference preset name exists: '{Name}'", name);

        var query = _context.InferencePresets.Where(p => p.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);

        _logger.LogDebug("Inference preset name '{Name}' exists: {Exists}", name, exists);

        return exists;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task<InferencePresetEntity> CreateAsync(InferencePresetEntity preset, CancellationToken cancellationToken = default)
    {
        if (preset.Id == Guid.Empty)
        {
            preset.Id = Guid.NewGuid();
            _logger.LogDebug("Generated new ID for inference preset: {PresetId}", preset.Id);
        }

        _logger.LogDebug("Creating inference preset: {PresetId} with name '{Name}'", preset.Id, preset.Name);

        _context.InferencePresets.Add(preset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created inference preset: {PresetId}", preset.Id);

        return preset;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(InferencePresetEntity preset, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating inference preset: {PresetId}", preset.Id);

        _context.InferencePresets.Update(preset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated inference preset: {PresetId}", preset.Id);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting inference preset: {PresetId}", id);

        var preset = await _context.InferencePresets.FindAsync([id], cancellationToken);

        if (preset == null)
        {
            _logger.LogDebug("Inference preset not found for deletion: {PresetId}", id);
            return;
        }

        if (preset.IsBuiltIn)
        {
            _logger.LogWarning(
                "Cannot delete built-in inference preset {PresetId}: {Name}",
                id,
                preset.Name);
            return;
        }

        var wasDefault = preset.IsDefault;

        _context.InferencePresets.Remove(preset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Deleted inference preset: {PresetId}", id);

        // Reassign default if needed
        if (wasDefault)
        {
            _logger.LogDebug("Reassigning default after deleting default preset");

            var newDefault = await _context.InferencePresets
                .OrderByDescending(p => p.IsBuiltIn)
                .ThenBy(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (newDefault != null)
            {
                await _context.InferencePresets
                    .Where(p => p.Id == newDefault.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.IsDefault, true)
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                _logger.LogDebug("Reassigned default to preset {PresetId}: {Name}", newDefault.Id, newDefault.Name);
            }
        }
    }

    /// <inheritdoc />
    public async Task SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting inference preset {PresetId} as default", id);

        // Clear existing default
        await _context.InferencePresets
            .Where(p => p.IsDefault)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsDefault, false)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        // Set new default
        var affectedRows = await _context.InferencePresets
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsDefault, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug("Set inference preset {PresetId} as default, affected rows: {AffectedRows}", id, affectedRows);
    }

    /// <inheritdoc />
    public async Task<InferencePresetEntity?> DuplicateAsync(Guid id, string newName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Duplicating inference preset {PresetId} with new name '{NewName}'", id, newName);

        var source = await _context.InferencePresets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (source == null)
        {
            _logger.LogDebug("Source inference preset not found for duplication: {PresetId}", id);
            return null;
        }

        var duplicate = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = newName,
            Description = source.Description,
            Temperature = source.Temperature,
            TopP = source.TopP,
            TopK = source.TopK,
            RepeatPenalty = source.RepeatPenalty,
            MaxTokens = source.MaxTokens,
            ContextSize = source.ContextSize,
            IsDefault = false,
            IsBuiltIn = false
        };

        _context.InferencePresets.Add(duplicate);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Duplicated inference preset {SourceId} to {DuplicateId} with name '{Name}'",
            id,
            duplicate.Id,
            newName);

        return duplicate;
    }

    #endregion
}
