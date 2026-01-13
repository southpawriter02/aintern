using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository implementation for managing system prompts.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides a clean abstraction over Entity Framework Core operations
/// for system prompts, with comprehensive logging support.
/// </para>
/// <para>
/// Key implementation details:
/// </para>
/// <list type="bullet">
///   <item><description>Supports soft delete via IsActive flag</description></item>
///   <item><description>Protects built-in prompts from hard deletion</description></item>
///   <item><description>Provides atomic default switching</description></item>
///   <item><description>Category-based organization</description></item>
/// </list>
/// </remarks>
public sealed class SystemPromptRepository : ISystemPromptRepository
{
    #region Fields

    /// <summary>
    /// The database context for Entity Framework operations.
    /// </summary>
    private readonly AInternDbContext _context;

    /// <summary>
    /// Logger instance for diagnostic output.
    /// </summary>
    private readonly ILogger<SystemPromptRepository> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="SystemPromptRepository"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// If no logger is provided, a <see cref="NullLogger{T}"/> is used.
    /// </remarks>
    public SystemPromptRepository(AInternDbContext context, ILogger<SystemPromptRepository>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? NullLogger<SystemPromptRepository>.Instance;
        _logger.LogDebug("SystemPromptRepository instance created");
    }

    #endregion

    #region Read Operations

    /// <inheritdoc />
    public async Task<SystemPromptEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting system prompt by ID: {PromptId}", id);

        var prompt = await _context.SystemPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (prompt == null)
        {
            _logger.LogDebug("System prompt not found: {PromptId}", id);
        }

        return prompt;
    }

    /// <inheritdoc />
    public async Task<SystemPromptEntity?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting default system prompt");

        var prompt = await _context.SystemPrompts
            .AsNoTracking()
            .Where(p => p.IsActive && p.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        if (prompt == null)
        {
            _logger.LogDebug("No default system prompt found");
        }
        else
        {
            _logger.LogDebug("Default system prompt found: {PromptId} ({Name})", prompt.Id, prompt.Name);
        }

        return prompt;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPromptEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all active system prompts");

        var prompts = await _context.SystemPrompts
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} active system prompts", prompts.Count);

        return prompts;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPromptEntity>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting system prompts by category: '{Category}'", category);

        var prompts = await _context.SystemPrompts
            .AsNoTracking()
            .Where(p => p.IsActive && p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} system prompts in category '{Category}'", prompts.Count, category);

        return prompts;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting distinct categories");

        var categories = await _context.SystemPrompts
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} distinct categories", categories.Count);

        return categories;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPromptEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching system prompts with term: '{SearchTerm}'", searchTerm);

        var prompts = await _context.SystemPrompts
            .AsNoTracking()
            .Where(p => p.IsActive &&
                (p.Name.Contains(searchTerm) ||
                 (p.Description != null && p.Description.Contains(searchTerm))))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Search returned {Count} system prompts", prompts.Count);

        return prompts;
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if system prompt name exists: '{Name}'", name);

        var query = _context.SystemPrompts.Where(p => p.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);

        _logger.LogDebug("System prompt name '{Name}' exists: {Exists}", name, exists);

        return exists;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task<SystemPromptEntity> CreateAsync(SystemPromptEntity prompt, CancellationToken cancellationToken = default)
    {
        if (prompt.Id == Guid.Empty)
        {
            prompt.Id = Guid.NewGuid();
            _logger.LogDebug("Generated new ID for system prompt: {PromptId}", prompt.Id);
        }

        _logger.LogDebug("Creating system prompt: {PromptId} with name '{Name}'", prompt.Id, prompt.Name);

        _context.SystemPrompts.Add(prompt);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created system prompt: {PromptId}", prompt.Id);

        return prompt;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SystemPromptEntity prompt, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating system prompt: {PromptId}", prompt.Id);

        _context.SystemPrompts.Update(prompt);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated system prompt: {PromptId}", prompt.Id);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Soft-deleting system prompt: {PromptId}", id);

        var affectedRows = await _context.SystemPrompts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsActive, false)
                .SetProperty(p => p.IsDefault, false)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug("Soft-deleted system prompt {PromptId}, affected rows: {AffectedRows}", id, affectedRows);
    }

    /// <inheritdoc />
    public async Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Hard-deleting system prompt: {PromptId}", id);

        var prompt = await _context.SystemPrompts.FindAsync([id], cancellationToken);

        if (prompt == null)
        {
            _logger.LogDebug("System prompt not found for hard delete: {PromptId}", id);
            return;
        }

        if (prompt.IsBuiltIn)
        {
            _logger.LogWarning(
                "Cannot hard-delete built-in system prompt {PromptId}: {Name}",
                id,
                prompt.Name);
            return;
        }

        _context.SystemPrompts.Remove(prompt);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Hard-deleted system prompt: {PromptId}", id);
    }

    #endregion

    #region Actions

    /// <inheritdoc />
    public async Task SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting system prompt {PromptId} as default", id);

        // Clear existing default
        await _context.SystemPrompts
            .Where(p => p.IsDefault)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsDefault, false)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        // Set new default
        var affectedRows = await _context.SystemPrompts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsDefault, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug("Set system prompt {PromptId} as default, affected rows: {AffectedRows}", id, affectedRows);
    }

    /// <inheritdoc />
    public async Task IncrementUsageCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Incrementing usage count for system prompt: {PromptId}", id);

        var affectedRows = await _context.SystemPrompts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.UsageCount, p => p.UsageCount + 1)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug("Incremented usage count for system prompt {PromptId}, affected rows: {AffectedRows}", id, affectedRows);
    }

    /// <inheritdoc />
    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Restoring system prompt: {PromptId}", id);

        var affectedRows = await _context.SystemPrompts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsActive, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug("Restored system prompt {PromptId}, affected rows: {AffectedRows}", id, affectedRows);
    }

    #endregion
}
