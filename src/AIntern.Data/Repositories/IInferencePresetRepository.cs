using AIntern.Core.Entities;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository interface for managing inference presets.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a complete abstraction over Entity Framework Core operations
/// for the <see cref="InferencePresetEntity"/> type.
/// </para>
/// <para>
/// Key features include:
/// </para>
/// <list type="bullet">
///   <item><description>CRUD operations for inference presets</description></item>
///   <item><description>Default preset management</description></item>
///   <item><description>Built-in vs user-created preset filtering</description></item>
///   <item><description>Preset duplication for customization</description></item>
///   <item><description>Built-in preset protection</description></item>
/// </list>
/// </remarks>
/// <example>
/// Basic usage with dependency injection:
/// <code>
/// public class InferenceService
/// {
///     private readonly IInferencePresetRepository _repository;
///
///     public InferenceService(IInferencePresetRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;InferencePresetEntity?&gt; GetDefaultPresetAsync()
///     {
///         return await _repository.GetDefaultAsync();
///     }
/// }
/// </code>
/// </example>
public interface IInferencePresetRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves an inference preset by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the inference preset.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The inference preset entity if found; otherwise, <c>null</c>.
    /// </returns>
    Task<InferencePresetEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the default inference preset.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The inference preset marked as default if one exists; otherwise, <c>null</c>.
    /// </returns>
    Task<InferencePresetEntity?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all inference presets.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all inference presets ordered by built-in status (descending)
    /// then by name.
    /// </returns>
    /// <remarks>
    /// Built-in presets appear before user-created presets.
    /// </remarks>
    Task<IReadOnlyList<InferencePresetEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all built-in inference presets.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of built-in inference presets ordered by name.
    /// </returns>
    Task<IReadOnlyList<InferencePresetEntity>> GetBuiltInAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all user-created inference presets.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of user-created inference presets ordered by
    /// <see cref="InferencePresetEntity.UpdatedAt"/> descending.
    /// </returns>
    Task<IReadOnlyList<InferencePresetEntity>> GetUserCreatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an inference preset with the specified name already exists.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="excludeId">An optional ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if a preset with the name exists; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates a new inference preset.
    /// </summary>
    /// <param name="preset">The inference preset entity to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created inference preset with any generated values populated.</returns>
    /// <remarks>
    /// <para>
    /// If <see cref="InferencePresetEntity.Id"/> is <see cref="Guid.Empty"/>,
    /// a new GUID will be generated.
    /// </para>
    /// <para>
    /// <see cref="InferencePresetEntity.CreatedAt"/> and <see cref="InferencePresetEntity.UpdatedAt"/>
    /// are automatically set by the DbContext.
    /// </para>
    /// </remarks>
    Task<InferencePresetEntity> CreateAsync(InferencePresetEntity preset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing inference preset.
    /// </summary>
    /// <param name="preset">The inference preset entity with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <see cref="InferencePresetEntity.UpdatedAt"/> is automatically updated by the DbContext.
    /// </remarks>
    Task UpdateAsync(InferencePresetEntity preset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an inference preset.
    /// </summary>
    /// <param name="id">The unique identifier of the inference preset to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Built-in presets (<see cref="InferencePresetEntity.IsBuiltIn"/> = <c>true</c>) are
    /// protected and cannot be deleted. A warning will be logged and the operation
    /// will return without deleting.
    /// </para>
    /// <para>
    /// If the deleted preset was the default, the first available preset will be
    /// set as the new default.
    /// </para>
    /// </remarks>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an inference preset as the default, clearing any existing default.
    /// </summary>
    /// <param name="id">The unique identifier of the inference preset to set as default.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This operation atomically clears the existing default (if any) and sets the new default.
    /// </remarks>
    Task SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates an existing inference preset with a new name.
    /// </summary>
    /// <param name="id">The unique identifier of the preset to duplicate.</param>
    /// <param name="newName">The name for the duplicated preset.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The newly created duplicate preset, or <c>null</c> if the source preset was not found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// All parameter values are copied to the new preset. The duplicated preset will have:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>A new unique ID</description></item>
    ///   <item><description>The specified new name</description></item>
    ///   <item><description><see cref="InferencePresetEntity.IsBuiltIn"/> set to <c>false</c></description></item>
    ///   <item><description><see cref="InferencePresetEntity.IsDefault"/> set to <c>false</c></description></item>
    ///   <item><description>New CreatedAt and UpdatedAt timestamps</description></item>
    /// </list>
    /// </remarks>
    Task<InferencePresetEntity?> DuplicateAsync(Guid id, string newName, CancellationToken cancellationToken = default);

    #endregion
}
