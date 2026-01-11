using AIntern.Core.Entities;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Repository interface for inference preset data access operations.
/// </summary>
public interface IInferencePresetRepository
{
    /// <summary>
    /// Gets an inference preset by ID.
    /// </summary>
    Task<InferencePresetEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all inference presets.
    /// </summary>
    Task<IReadOnlyList<InferencePresetEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the default inference preset, if one is set.
    /// </summary>
    Task<InferencePresetEntity?> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all built-in presets.
    /// </summary>
    Task<IReadOnlyList<InferencePresetEntity>> GetBuiltInAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all user-created presets.
    /// </summary>
    Task<IReadOnlyList<InferencePresetEntity>> GetUserPresetsAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new inference preset.
    /// </summary>
    Task<InferencePresetEntity> CreateAsync(InferencePresetEntity preset, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing inference preset.
    /// </summary>
    Task UpdateAsync(InferencePresetEntity preset, CancellationToken ct = default);

    /// <summary>
    /// Deletes an inference preset (only user presets can be deleted).
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets an inference preset as the default (and unsets any previous default).
    /// </summary>
    Task SetDefaultAsync(Guid id, CancellationToken ct = default);
}
