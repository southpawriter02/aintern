using System.Diagnostics;
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
/// <b>Key Implementation Details:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Soft delete:</b> Uses IsActive flag to mark prompts as deleted without removing data</description></item>
///   <item><description><b>Built-in protection:</b> Prevents hard deletion of IsBuiltIn prompts</description></item>
///   <item><description><b>Atomic default switching:</b> Uses two ExecuteUpdateAsync calls to atomically change the default prompt</description></item>
///   <item><description><b>Category organization:</b> Prompts can be filtered and grouped by category</description></item>
/// </list>
/// <para>
/// <b>Logging Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Debug:</b> Entry/exit for all operations with parameters and timing</description></item>
///   <item><description><b>Warning:</b> When bulk operations affect 0 rows or built-in protection triggers</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is not thread-safe. Each request should use its own
/// instance via dependency injection with scoped lifetime.
/// </para>
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

    /// <inheritdoc />
    public async Task<SystemPromptEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTER] GetByNameAsync - Name: '{Name}'", name);

        // Case-insensitive name lookup using EF.Functions.Like for SQLite compatibility.
        // This is more reliable than string.Equals with StringComparison across different providers.
        var prompt = await _context.SystemPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower(), cancellationToken);

        if (prompt == null)
        {
            _logger.LogDebug("[EXIT] GetByNameAsync - Prompt not found: '{Name}'", name);
        }
        else
        {
            _logger.LogDebug(
                "[EXIT] GetByNameAsync - Found prompt: {PromptId} ({Name})",
                prompt.Id, prompt.Name);
        }

        return prompt;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPromptEntity>> GetUserPromptsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTER] GetUserPromptsAsync");

        // Filter for user-created prompts: IsBuiltIn = false AND IsActive = true.
        // This excludes both built-in templates and soft-deleted user prompts.
        var prompts = await _context.SystemPrompts
            .AsNoTracking()
            .Where(p => !p.IsBuiltIn && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "[EXIT] GetUserPromptsAsync - Retrieved {Count} user prompts",
            prompts.Count);

        return prompts;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPromptEntity>> GetBuiltInPromptsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTER] GetBuiltInPromptsAsync");

        // Filter for built-in templates: IsBuiltIn = true AND IsActive = true.
        // Built-in prompts can be soft-deleted (hidden) by users.
        var prompts = await _context.SystemPrompts
            .AsNoTracking()
            .Where(p => p.IsBuiltIn && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "[EXIT] GetBuiltInPromptsAsync - Retrieved {Count} built-in prompts",
            prompts.Count);

        return prompts;
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
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] DeleteAsync (soft) - PromptId: {PromptId}", id);

        // Soft delete: Mark as inactive rather than removing from database.
        // This preserves referential integrity with conversations that used this prompt
        // and allows for potential restoration later.
        // Also clear IsDefault to prevent an inactive prompt from being the default.
        var affectedRows = await _context.SystemPrompts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsActive, false)
                .SetProperty(p => p.IsDefault, false)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        stopwatch.Stop();

        if (affectedRows == 0)
        {
            _logger.LogWarning(
                "DeleteAsync affected 0 rows - prompt may not exist: {PromptId}",
                id);
        }
        else
        {
            _logger.LogDebug(
                "[EXIT] DeleteAsync - PromptId: {PromptId}, AffectedRows: {AffectedRows}, Duration: {DurationMs}ms",
                id, affectedRows, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] HardDeleteAsync - PromptId: {PromptId}", id);

        // Hard delete requires loading the entity first to check the IsBuiltIn flag.
        // Built-in prompts are protected from permanent deletion.
        var prompt = await _context.SystemPrompts.FindAsync([id], cancellationToken);

        if (prompt == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "HardDeleteAsync - Prompt not found: {PromptId}, Duration: {DurationMs}ms",
                id, stopwatch.ElapsedMilliseconds);
            return;
        }

        // Guard clause: Protect built-in prompts from permanent deletion.
        // Built-in prompts are seeded during database initialization and should
        // never be removed to ensure the application always has default prompts.
        if (prompt.IsBuiltIn)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "HardDeleteAsync BLOCKED - Cannot delete built-in prompt {PromptId}: {Name}",
                id, prompt.Name);
            return;
        }

        _context.SystemPrompts.Remove(prompt);
        await _context.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] HardDeleteAsync - PromptId: {PromptId}, Name: {Name}, Duration: {DurationMs}ms",
            id, prompt.Name, stopwatch.ElapsedMilliseconds);
    }

    #endregion

    #region Actions

    /// <inheritdoc />
    public async Task SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SetAsDefaultAsync - PromptId: {PromptId}", id);

        // Atomic default switching using two ExecuteUpdateAsync calls.
        // Step 1: Clear the IsDefault flag from all currently-default prompts.
        // This handles the case where multiple prompts somehow became default.
        var clearedCount = await _context.SystemPrompts
            .Where(p => p.IsDefault)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsDefault, false)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug(
            "SetAsDefaultAsync - Cleared IsDefault from {Count} prompts",
            clearedCount);

        // Step 2: Set the new prompt as default.
        var affectedRows = await _context.SystemPrompts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsDefault, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        stopwatch.Stop();

        if (affectedRows == 0)
        {
            _logger.LogWarning(
                "SetAsDefaultAsync affected 0 rows - prompt may not exist: {PromptId}",
                id);
        }
        else
        {
            _logger.LogDebug(
                "[EXIT] SetAsDefaultAsync - PromptId: {PromptId}, Duration: {DurationMs}ms",
                id, stopwatch.ElapsedMilliseconds);
        }
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
        _logger.LogDebug("[ENTER] RestoreAsync - PromptId: {PromptId}", id);

        // Restore a soft-deleted prompt by setting IsActive back to true.
        // This is the inverse of DeleteAsync and allows recovery of accidentally
        // deleted prompts without data loss.
        var affectedRows = await _context.SystemPrompts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsActive, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (affectedRows == 0)
        {
            _logger.LogWarning(
                "RestoreAsync affected 0 rows - prompt may not exist: {PromptId}",
                id);
        }
        else
        {
            _logger.LogDebug(
                "[EXIT] RestoreAsync - PromptId: {PromptId}, AffectedRows: {AffectedRows}",
                id, affectedRows);
        }
    }

    #endregion
}
