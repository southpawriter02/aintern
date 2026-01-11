using AIntern.Core.Entities;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Repository interface for system prompt data access operations.
/// </summary>
public interface ISystemPromptRepository
{
    /// <summary>
    /// Gets a system prompt by ID.
    /// </summary>
    Task<SystemPromptEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all system prompts.
    /// </summary>
    Task<IReadOnlyList<SystemPromptEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the default system prompt, if one is set.
    /// </summary>
    Task<SystemPromptEntity?> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new system prompt.
    /// </summary>
    Task<SystemPromptEntity> CreateAsync(SystemPromptEntity prompt, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing system prompt.
    /// </summary>
    Task UpdateAsync(SystemPromptEntity prompt, CancellationToken ct = default);

    /// <summary>
    /// Deletes a system prompt.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets a system prompt as the default (and unsets any previous default).
    /// </summary>
    Task SetDefaultAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Increments the usage count for a system prompt.
    /// </summary>
    Task IncrementUsageCountAsync(Guid id, CancellationToken ct = default);
}
