using AIntern.Core.Entities;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository interface for managing system prompts.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a complete abstraction over Entity Framework Core operations
/// for the <see cref="SystemPromptEntity"/> type.
/// </para>
/// <para>
/// Key features include:
/// </para>
/// <list type="bullet">
///   <item><description>CRUD operations with soft delete support</description></item>
///   <item><description>Default prompt management</description></item>
///   <item><description>Category-based organization</description></item>
///   <item><description>Usage tracking</description></item>
///   <item><description>Built-in prompt protection</description></item>
/// </list>
/// </remarks>
/// <example>
/// Basic usage with dependency injection:
/// <code>
/// public class PromptService
/// {
///     private readonly ISystemPromptRepository _repository;
///
///     public PromptService(ISystemPromptRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;SystemPromptEntity?&gt; GetDefaultPromptAsync()
///     {
///         return await _repository.GetDefaultAsync();
///     }
/// }
/// </code>
/// </example>
public interface ISystemPromptRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves a system prompt by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the system prompt.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The system prompt entity if found; otherwise, <c>null</c>.
    /// </returns>
    Task<SystemPromptEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the default system prompt.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The system prompt marked as default if one exists; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Only active prompts (<see cref="SystemPromptEntity.IsActive"/> = <c>true</c>)
    /// are considered when searching for the default.
    /// </remarks>
    Task<SystemPromptEntity?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active system prompts.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of active system prompts ordered by category then by name.
    /// </returns>
    /// <remarks>
    /// Only prompts where <see cref="SystemPromptEntity.IsActive"/> is <c>true</c> are returned.
    /// </remarks>
    Task<IReadOnlyList<SystemPromptEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves system prompts by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of active system prompts in the specified category.
    /// </returns>
    /// <remarks>
    /// Only active prompts are returned, ordered by name.
    /// </remarks>
    Task<IReadOnlyList<SystemPromptEntity>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all distinct categories from active system prompts.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of distinct category names ordered alphabetically.
    /// </returns>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches system prompts by name or description.
    /// </summary>
    /// <param name="searchTerm">The search term to match against name and description.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of matching active system prompts.
    /// </returns>
    /// <remarks>
    /// The search is case-insensitive and matches any substring of the name or description.
    /// </remarks>
    Task<IReadOnlyList<SystemPromptEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a system prompt with the specified name already exists.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="excludeId">An optional ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if a prompt with the name exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This check includes inactive (soft-deleted) prompts.
    /// </remarks>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a system prompt by its unique name.
    /// </summary>
    /// <param name="name">The exact name of the system prompt.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The system prompt entity if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>The search is case-insensitive.</para>
    /// <para>Added in v0.2.4a.</para>
    /// </remarks>
    Task<SystemPromptEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active user-created (non-built-in) system prompts.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of active user-created prompts ordered by name.
    /// </returns>
    /// <remarks>
    /// <para>Filters where <see cref="SystemPromptEntity.IsBuiltIn"/> = <c>false</c>
    /// and <see cref="SystemPromptEntity.IsActive"/> = <c>true</c>.</para>
    /// <para>Added in v0.2.4a.</para>
    /// </remarks>
    Task<IReadOnlyList<SystemPromptEntity>> GetUserPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active built-in system prompts (templates).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of active built-in prompts ordered by name.
    /// </returns>
    /// <remarks>
    /// <para>Filters where <see cref="SystemPromptEntity.IsBuiltIn"/> = <c>true</c>
    /// and <see cref="SystemPromptEntity.IsActive"/> = <c>true</c>.</para>
    /// <para>Added in v0.2.4a.</para>
    /// </remarks>
    Task<IReadOnlyList<SystemPromptEntity>> GetBuiltInPromptsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates a new system prompt.
    /// </summary>
    /// <param name="prompt">The system prompt entity to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created system prompt with any generated values populated.</returns>
    /// <remarks>
    /// <para>
    /// If <see cref="SystemPromptEntity.Id"/> is <see cref="Guid.Empty"/>,
    /// a new GUID will be generated.
    /// </para>
    /// <para>
    /// <see cref="SystemPromptEntity.CreatedAt"/> and <see cref="SystemPromptEntity.UpdatedAt"/>
    /// are automatically set by the DbContext.
    /// </para>
    /// </remarks>
    Task<SystemPromptEntity> CreateAsync(SystemPromptEntity prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing system prompt.
    /// </summary>
    /// <param name="prompt">The system prompt entity with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <see cref="SystemPromptEntity.UpdatedAt"/> is automatically updated by the DbContext.
    /// </remarks>
    Task UpdateAsync(SystemPromptEntity prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a system prompt by setting <see cref="SystemPromptEntity.IsActive"/> to <c>false</c>.
    /// </summary>
    /// <param name="id">The unique identifier of the system prompt to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Soft-deleted prompts are excluded from queries that filter by active status.
    /// Use <see cref="RestoreAsync"/> to undelete a soft-deleted prompt.
    /// </para>
    /// <para>
    /// Built-in prompts can be soft-deleted but will log a warning.
    /// </para>
    /// </remarks>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a system prompt from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the system prompt to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This operation cannot be undone. Use <see cref="DeleteAsync"/> for soft delete.
    /// </para>
    /// <para>
    /// Built-in prompts (<see cref="SystemPromptEntity.IsBuiltIn"/> = <c>true</c>) are
    /// protected and cannot be hard-deleted. A warning will be logged and the operation
    /// will return without deleting.
    /// </para>
    /// </remarks>
    Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Actions

    /// <summary>
    /// Sets a system prompt as the default, clearing any existing default.
    /// </summary>
    /// <param name="id">The unique identifier of the system prompt to set as default.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This operation atomically clears the existing default (if any) and sets the new default.
    /// </remarks>
    Task SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the usage count for a system prompt.
    /// </summary>
    /// <param name="id">The unique identifier of the system prompt.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This is typically called when a new conversation uses the prompt.
    /// </remarks>
    Task IncrementUsageCountAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted system prompt by setting <see cref="SystemPromptEntity.IsActive"/> to <c>true</c>.
    /// </summary>
    /// <param name="id">The unique identifier of the system prompt to restore.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion
}
