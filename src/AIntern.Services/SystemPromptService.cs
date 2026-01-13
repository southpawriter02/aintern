using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AIntern.Core.Entities;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data.Repositories;

namespace AIntern.Services;

/// <summary>
/// Service for managing system prompts with CRUD operations, template support, and persistence.
/// </summary>
/// <remarks>
/// <para>
/// This service is the single source of truth for system prompt selection at runtime.
/// It coordinates between the repository (CRUD), settings service (persistence),
/// and consumers (ViewModels, conversation service).
/// </para>
/// <para>
/// <b>Initialization:</b> Call <see cref="InitializeAsync"/> during app startup
/// to load the last-selected prompt from settings.json.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// </para>
/// <list type="bullet">
///   <item><description>All async operations use <see cref="SemaphoreSlim"/> for thread safety</description></item>
///   <item><description>Events fire with immutable data for safety</description></item>
///   <item><description><see cref="CurrentPrompt"/> returns a snapshot</description></item>
/// </list>
/// <para>
/// <b>Logging:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>[ENTER]:</b> Method entry with parameters</description></item>
///   <item><description><b>[INFO]:</b> Significant state changes</description></item>
///   <item><description><b>[SKIP]:</b> No-op conditions</description></item>
///   <item><description><b>[EXIT]:</b> Method completion with duration</description></item>
///   <item><description><b>[EVENT]:</b> Event firing</description></item>
/// </list>
/// <para>Added in v0.2.4b.</para>
/// </remarks>
public sealed class SystemPromptService : ISystemPromptService, IAsyncDisposable
{
    #region Fields

    private readonly ISystemPromptRepository _repository;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SystemPromptService> _logger;

    /// <summary>
    /// Currently selected system prompt for new conversations.
    /// </summary>
    private SystemPrompt? _currentPrompt;

    /// <summary>
    /// Semaphore for thread-safe async operations.
    /// </summary>
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Flag indicating whether the service has been initialized.
    /// </summary>
    private bool _isInitialized;

    #endregion

    #region Properties

    /// <inheritdoc />
    public SystemPrompt? CurrentPrompt => _currentPrompt;

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<PromptListChangedEventArgs>? PromptListChanged;

    /// <inheritdoc />
    public event EventHandler<CurrentPromptChangedEventArgs>? CurrentPromptChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemPromptService"/> class.
    /// </summary>
    /// <param name="repository">Repository for prompt CRUD operations.</param>
    /// <param name="settingsService">Service for persisting current prompt ID.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <remarks>
    /// <para>
    /// After construction, call <see cref="InitializeAsync"/> to load the last-selected
    /// prompt from settings.json.
    /// </para>
    /// </remarks>
    public SystemPromptService(
        ISystemPromptRepository repository,
        ISettingsService settingsService,
        ILogger<SystemPromptService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[INIT] SystemPromptService created");
    }

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPrompt>> GetAllPromptsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetAllPromptsAsync");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entities = await _repository.GetAllActiveAsync(cancellationToken);
            var prompts = entities.Select(SystemPrompt.FromEntity).ToList();

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] GetAllPromptsAsync - Count: {Count}, Duration: {Ms}ms",
                prompts.Count, stopwatch.ElapsedMilliseconds);

            return prompts;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPrompt>> GetUserPromptsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetUserPromptsAsync");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entities = await _repository.GetUserPromptsAsync(cancellationToken);
            var prompts = entities.Select(SystemPrompt.FromEntity).ToList();

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] GetUserPromptsAsync - Count: {Count}, Duration: {Ms}ms",
                prompts.Count, stopwatch.ElapsedMilliseconds);

            return prompts;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPrompt>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetTemplatesAsync");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entities = await _repository.GetBuiltInPromptsAsync(cancellationToken);
            var prompts = entities.Select(SystemPrompt.FromEntity).ToList();

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] GetTemplatesAsync - Count: {Count}, Duration: {Ms}ms",
                prompts.Count, stopwatch.ElapsedMilliseconds);

            return prompts;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SystemPrompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetByIdAsync - Id: {Id}", id);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);

            stopwatch.Stop();
            if (entity == null)
            {
                _logger.LogDebug("[EXIT] GetByIdAsync - Not found, Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
                return null;
            }

            var prompt = SystemPrompt.FromEntity(entity);
            _logger.LogDebug(
                "[EXIT] GetByIdAsync - Found '{Name}', Duration: {Ms}ms",
                prompt.Name, stopwatch.ElapsedMilliseconds);

            return prompt;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SystemPrompt?> GetDefaultPromptAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetDefaultPromptAsync");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entity = await _repository.GetDefaultAsync(cancellationToken);

            stopwatch.Stop();
            if (entity == null)
            {
                _logger.LogDebug("[EXIT] GetDefaultPromptAsync - No default set, Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
                return null;
            }

            var prompt = SystemPrompt.FromEntity(entity);
            _logger.LogDebug(
                "[EXIT] GetDefaultPromptAsync - Default is '{Name}', Duration: {Ms}ms",
                prompt.Name, stopwatch.ElapsedMilliseconds);

            return prompt;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemPrompt>> SearchPromptsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SearchPromptsAsync - SearchTerm: '{Term}'", searchTerm);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            stopwatch.Stop();
            _logger.LogDebug("[EXIT] SearchPromptsAsync - Empty search term, returning empty list, Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
            return [];
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entities = await _repository.SearchAsync(searchTerm, cancellationToken);
            var prompts = entities.Select(SystemPrompt.FromEntity).ToList();

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] SearchPromptsAsync - Found {Count} matches, Duration: {Ms}ms",
                prompts.Count, stopwatch.ElapsedMilliseconds);

            return prompts;
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region Mutation Operations

    /// <inheritdoc />
    public async Task<SystemPrompt> CreatePromptAsync(
        string name,
        string content,
        string? description = null,
        string? category = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] CreatePromptAsync - Name: '{Name}', ContentLength: {Length}, Category: '{Category}'",
            name, content?.Length ?? 0, category ?? "General");

        // Validate required parameters
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Check for name uniqueness
            if (await _repository.NameExistsAsync(name, cancellationToken: cancellationToken))
            {
                _logger.LogWarning("[EXIT] CreatePromptAsync - Name already exists: '{Name}'", name);
                throw new InvalidOperationException($"A prompt named '{name}' already exists.");
            }

            // Create domain model and validate
            var prompt = new SystemPrompt
            {
                Id = Guid.NewGuid(),
                Name = name,
                Content = content,
                Description = description,
                Category = category ?? "General",
                Tags = tags?.ToList() ?? [],
                IsBuiltIn = false,
                IsDefault = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UsageCount = 0
            };

            var validationResult = prompt.Validate();
            if (!validationResult.IsValid)
            {
                var errors = validationResult.GetAllErrors();
                _logger.LogWarning("[EXIT] CreatePromptAsync - Validation failed: {Errors}", errors);
                throw new ArgumentException($"Prompt validation failed: {errors}");
            }

            _logger.LogDebug("[INFO] CreatePromptAsync - Creating entity for '{Name}'", name);

            // Convert to entity and save
            var entity = prompt.ToEntity();
            var createdEntity = await _repository.CreateAsync(entity, cancellationToken);
            var createdPrompt = SystemPrompt.FromEntity(createdEntity);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] CreatePromptAsync - Created '{Name}' (ID: {Id}), Duration: {Ms}ms",
                createdPrompt.Name, createdPrompt.Id, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPromptListChanged(PromptListChangeType.PromptCreated, createdPrompt.Id, createdPrompt.Name);

            return createdPrompt;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SystemPrompt> CreateFromTemplateAsync(
        Guid templateId,
        string? newName = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] CreateFromTemplateAsync - TemplateId: {TemplateId}, NewName: '{NewName}'",
            templateId, newName ?? "(auto-generate)");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Load the template
            var templateEntity = await _repository.GetByIdAsync(templateId, cancellationToken);
            if (templateEntity == null)
            {
                _logger.LogWarning("[EXIT] CreateFromTemplateAsync - Template not found: {TemplateId}", templateId);
                throw new InvalidOperationException($"Template not found: {templateId}");
            }

            var template = SystemPrompt.FromEntity(templateEntity);
            _logger.LogDebug("[INFO] CreateFromTemplateAsync - Found template '{Name}'", template.Name);

            // Generate unique name
            var baseName = newName ?? template.Name;
            var uniqueName = await GenerateUniqueNameAsync(baseName, cancellationToken);

            _logger.LogDebug("[INFO] CreateFromTemplateAsync - Generated unique name: '{Name}'", uniqueName);

            // Create duplicate with new name
            var duplicate = template.Duplicate(uniqueName);

            // Convert to entity and save
            var entity = duplicate.ToEntity();
            var createdEntity = await _repository.CreateAsync(entity, cancellationToken);
            var createdPrompt = SystemPrompt.FromEntity(createdEntity);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] CreateFromTemplateAsync - Created '{Name}' from template '{Template}' (ID: {Id}), Duration: {Ms}ms",
                createdPrompt.Name, template.Name, createdPrompt.Id, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPromptListChanged(PromptListChangeType.PromptCreated, createdPrompt.Id, createdPrompt.Name);

            return createdPrompt;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SystemPrompt> UpdatePromptAsync(
        Guid id,
        string? name = null,
        string? content = null,
        string? description = null,
        string? category = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] UpdatePromptAsync - Id: {Id}, Name: '{Name}', HasContent: {HasContent}",
            id, name ?? "(unchanged)", content != null);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Load existing prompt
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[EXIT] UpdatePromptAsync - Prompt not found: {Id}", id);
                throw new InvalidOperationException($"Prompt not found: {id}");
            }

            _logger.LogDebug("[INFO] UpdatePromptAsync - Found existing prompt '{Name}'", entity.Name);

            // Check if name is being changed and would conflict
            if (name != null && !string.Equals(name, entity.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (await _repository.NameExistsAsync(name, excludeId: id, cancellationToken: cancellationToken))
                {
                    _logger.LogWarning("[EXIT] UpdatePromptAsync - Name already exists: '{Name}'", name);
                    throw new InvalidOperationException($"A prompt named '{name}' already exists.");
                }
            }

            // Track if anything changed
            var hasChanges = false;

            // Apply updates (only non-null parameters)
            if (name != null && name != entity.Name)
            {
                _logger.LogDebug("[INFO] UpdatePromptAsync - Updating name: '{Old}' -> '{New}'", entity.Name, name);
                entity.Name = name;
                hasChanges = true;
            }

            if (content != null && content != entity.Content)
            {
                _logger.LogDebug("[INFO] UpdatePromptAsync - Updating content (length: {Old} -> {New})", entity.Content.Length, content.Length);
                entity.Content = content;
                hasChanges = true;
            }

            if (description != null && description != entity.Description)
            {
                _logger.LogDebug("[INFO] UpdatePromptAsync - Updating description");
                entity.Description = description;
                hasChanges = true;
            }

            if (category != null && category != entity.Category)
            {
                _logger.LogDebug("[INFO] UpdatePromptAsync - Updating category: '{Old}' -> '{New}'", entity.Category, category);
                entity.Category = category;
                hasChanges = true;
            }

            if (tags != null)
            {
                var tagsJson = tags.Any() ? JsonSerializer.Serialize(tags.ToList()) : null;
                if (tagsJson != entity.TagsJson)
                {
                    _logger.LogDebug("[INFO] UpdatePromptAsync - Updating tags");
                    entity.TagsJson = tagsJson;
                    hasChanges = true;
                }
            }

            if (!hasChanges)
            {
                stopwatch.Stop();
                _logger.LogDebug("[SKIP] UpdatePromptAsync - No changes detected, Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
                return SystemPrompt.FromEntity(entity);
            }

            // Validate before saving
            var prompt = SystemPrompt.FromEntity(entity);
            var validationResult = prompt.Validate();
            if (!validationResult.IsValid)
            {
                var errors = validationResult.GetAllErrors();
                _logger.LogWarning("[EXIT] UpdatePromptAsync - Validation failed: {Errors}", errors);
                throw new ArgumentException($"Prompt validation failed: {errors}");
            }

            // Save changes
            await _repository.UpdateAsync(entity, cancellationToken);

            // Refresh current prompt if it was updated
            if (_currentPrompt?.Id == id)
            {
                _currentPrompt = SystemPrompt.FromEntity(entity);
                _logger.LogDebug("[INFO] UpdatePromptAsync - Refreshed CurrentPrompt reference");
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] UpdatePromptAsync - Updated '{Name}' (ID: {Id}), Duration: {Ms}ms",
                entity.Name, id, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPromptListChanged(PromptListChangeType.PromptUpdated, id, entity.Name);

            return SystemPrompt.FromEntity(entity);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeletePromptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] DeletePromptAsync - Id: {Id}", id);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Load existing prompt
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[EXIT] DeletePromptAsync - Prompt not found: {Id}", id);
                throw new InvalidOperationException($"Prompt not found: {id}");
            }

            // Prevent deleting built-in prompts
            if (entity.IsBuiltIn)
            {
                _logger.LogWarning("[EXIT] DeletePromptAsync - Cannot delete built-in prompt: '{Name}'", entity.Name);
                throw new InvalidOperationException($"Cannot delete built-in prompt: {entity.Name}");
            }

            var deletedPromptName = entity.Name;
            var wasCurrentPrompt = _currentPrompt?.Id == id;

            _logger.LogDebug("[INFO] DeletePromptAsync - Soft-deleting prompt '{Name}'", deletedPromptName);

            // Soft delete
            await _repository.DeleteAsync(id, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] DeletePromptAsync - Deleted '{Name}' (ID: {Id}), Duration: {Ms}ms",
                deletedPromptName, id, stopwatch.ElapsedMilliseconds);

            // Fire list changed event
            OnPromptListChanged(PromptListChangeType.PromptDeleted, id, deletedPromptName);

            // If the deleted prompt was current, reset to default
            if (wasCurrentPrompt)
            {
                _logger.LogDebug("[INFO] DeletePromptAsync - Deleted prompt was current, resetting to default");
                await ResetCurrentToDefaultInternalAsync(cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SystemPrompt> DuplicatePromptAsync(
        Guid id,
        string? newName = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] DuplicatePromptAsync - Id: {Id}, NewName: '{NewName}'",
            id, newName ?? "(auto-generate)");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Load the source prompt
            var sourceEntity = await _repository.GetByIdAsync(id, cancellationToken);
            if (sourceEntity == null)
            {
                _logger.LogWarning("[EXIT] DuplicatePromptAsync - Source prompt not found: {Id}", id);
                throw new InvalidOperationException($"Prompt not found: {id}");
            }

            var source = SystemPrompt.FromEntity(sourceEntity);
            _logger.LogDebug("[INFO] DuplicatePromptAsync - Found source prompt '{Name}'", source.Name);

            // Generate unique name
            var baseName = newName ?? $"{source.Name} (Copy)";
            var uniqueName = await GenerateUniqueNameAsync(baseName, cancellationToken);

            _logger.LogDebug("[INFO] DuplicatePromptAsync - Generated unique name: '{Name}'", uniqueName);

            // Create duplicate
            var duplicate = source.Duplicate(uniqueName);

            // Convert to entity and save
            var entity = duplicate.ToEntity();
            var createdEntity = await _repository.CreateAsync(entity, cancellationToken);
            var createdPrompt = SystemPrompt.FromEntity(createdEntity);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] DuplicatePromptAsync - Created '{Name}' from '{Source}' (ID: {Id}), Duration: {Ms}ms",
                createdPrompt.Name, source.Name, createdPrompt.Id, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPromptListChanged(PromptListChangeType.PromptCreated, createdPrompt.Id, createdPrompt.Name);

            return createdPrompt;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SetAsDefaultAsync - Id: {Id}", id);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Verify the prompt exists
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[EXIT] SetAsDefaultAsync - Prompt not found: {Id}", id);
                throw new InvalidOperationException($"Prompt not found: {id}");
            }

            // Check if already default
            if (entity.IsDefault)
            {
                stopwatch.Stop();
                _logger.LogDebug("[SKIP] SetAsDefaultAsync - '{Name}' is already default, Duration: {Ms}ms", entity.Name, stopwatch.ElapsedMilliseconds);
                return;
            }

            _logger.LogDebug("[INFO] SetAsDefaultAsync - Setting '{Name}' as default", entity.Name);

            // Set as default (repository handles clearing previous default)
            await _repository.SetAsDefaultAsync(id, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] SetAsDefaultAsync - Set '{Name}' as default, Duration: {Ms}ms",
                entity.Name, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPromptListChanged(PromptListChangeType.DefaultChanged, id, entity.Name);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetCurrentPromptAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SetCurrentPromptAsync - Id: {Id}", id?.ToString() ?? "(null)");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Handle null case - reset to default
            if (!id.HasValue)
            {
                _logger.LogDebug("[INFO] SetCurrentPromptAsync - Null ID, resetting to default");
                await ResetCurrentToDefaultInternalAsync(cancellationToken);
                stopwatch.Stop();
                _logger.LogDebug("[EXIT] SetCurrentPromptAsync - Reset to default, Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
                return;
            }

            // Check if already current
            if (_currentPrompt?.Id == id.Value)
            {
                stopwatch.Stop();
                _logger.LogDebug("[SKIP] SetCurrentPromptAsync - Already current prompt, Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
                return;
            }

            // Load the prompt
            var entity = await _repository.GetByIdAsync(id.Value, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[EXIT] SetCurrentPromptAsync - Prompt not found: {Id}", id.Value);
                throw new InvalidOperationException($"Prompt not found: {id.Value}");
            }

            if (!entity.IsActive)
            {
                _logger.LogWarning("[EXIT] SetCurrentPromptAsync - Prompt is inactive: {Id}", id.Value);
                throw new InvalidOperationException($"Cannot select inactive prompt: {entity.Name}");
            }

            var previousPrompt = _currentPrompt;
            var newPrompt = SystemPrompt.FromEntity(entity);

            _logger.LogDebug(
                "[INFO] SetCurrentPromptAsync - Changing current prompt: '{Old}' -> '{New}'",
                previousPrompt?.Name ?? "(none)", newPrompt.Name);

            // Update current prompt
            _currentPrompt = newPrompt;

            // Increment usage count
            await _repository.IncrementUsageCountAsync(id.Value, cancellationToken);
            _logger.LogDebug("[INFO] SetCurrentPromptAsync - Incremented usage count for '{Name}'", newPrompt.Name);

            // Persist to settings
            var appSettings = _settingsService.CurrentSettings;
            appSettings.CurrentSystemPromptId = id.Value;
            await _settingsService.SaveSettingsAsync(appSettings);
            _logger.LogDebug("[INFO] SetCurrentPromptAsync - Persisted selection to settings.json");

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] SetCurrentPromptAsync - Set current to '{Name}', Duration: {Ms}ms",
                newPrompt.Name, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnCurrentPromptChanged(newPrompt, previousPrompt);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region Utility Operations

    /// <inheritdoc />
    public string FormatPromptForContext(SystemPrompt? prompt)
    {
        _logger.LogDebug("[ENTER] FormatPromptForContext - Prompt: '{Name}'", prompt?.Name ?? "(null)");

        if (prompt == null)
        {
            _logger.LogDebug("[EXIT] FormatPromptForContext - Null prompt, returning empty string");
            return string.Empty;
        }

        // Currently returns content as-is.
        // This method provides a hook for future model-specific formatting.
        _logger.LogDebug("[EXIT] FormatPromptForContext - Returning content ({Length} chars)", prompt.Content.Length);
        return prompt.Content;
    }

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] InitializeAsync");

        // Prevent double initialization
        if (_isInitialized)
        {
            _logger.LogDebug("[SKIP] InitializeAsync - Already initialized");
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_isInitialized)
            {
                _logger.LogDebug("[SKIP] InitializeAsync - Already initialized (after lock)");
                return;
            }

            // Load settings to get the current prompt ID
            var appSettings = await _settingsService.LoadSettingsAsync();
            var currentPromptId = appSettings.CurrentSystemPromptId;

            _logger.LogDebug("[INFO] InitializeAsync - CurrentSystemPromptId from settings: {Id}", currentPromptId?.ToString() ?? "(null)");

            if (currentPromptId.HasValue)
            {
                // Try to load the saved prompt
                var entity = await _repository.GetByIdAsync(currentPromptId.Value, cancellationToken);
                if (entity != null && entity.IsActive)
                {
                    _currentPrompt = SystemPrompt.FromEntity(entity);

                    _logger.LogDebug("[INFO] InitializeAsync - Loaded saved prompt '{Name}'", _currentPrompt.Name);

                    _isInitialized = true;

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "[EXIT] InitializeAsync - Loaded prompt '{Name}', Duration: {Ms}ms",
                        _currentPrompt.Name, stopwatch.ElapsedMilliseconds);

                    return;
                }

                _logger.LogDebug("[INFO] InitializeAsync - Saved prompt not found or inactive, falling back to default");
            }

            // Fall back to default prompt
            var defaultEntity = await _repository.GetDefaultAsync(cancellationToken);
            if (defaultEntity != null)
            {
                _currentPrompt = SystemPrompt.FromEntity(defaultEntity);

                _logger.LogDebug("[INFO] InitializeAsync - Loaded default prompt '{Name}'", _currentPrompt.Name);

                // Update settings with the default prompt ID
                appSettings.CurrentSystemPromptId = _currentPrompt.Id;
                await _settingsService.SaveSettingsAsync(appSettings);
            }
            else
            {
                // Fall back to first available prompt
                var allPrompts = await _repository.GetAllActiveAsync(cancellationToken);
                if (allPrompts.Count > 0)
                {
                    _currentPrompt = SystemPrompt.FromEntity(allPrompts[0]);

                    _logger.LogDebug("[INFO] InitializeAsync - No default, using first available: '{Name}'", _currentPrompt.Name);

                    appSettings.CurrentSystemPromptId = _currentPrompt.Id;
                    await _settingsService.SaveSettingsAsync(appSettings);
                }
                else
                {
                    _logger.LogWarning("[INFO] InitializeAsync - No prompts available");
                    _currentPrompt = null;
                }
            }

            _isInitialized = true;

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] InitializeAsync - Initialized with '{PromptName}', Duration: {Ms}ms",
                _currentPrompt?.Name ?? "(none)", stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a unique name by appending a number suffix if needed.
    /// </summary>
    /// <param name="baseName">The base name to make unique.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A unique name (the base name or with " (N)" appended).</returns>
    /// <remarks>
    /// <para>
    /// If baseName already exists, tries "baseName (1)", "baseName (2)", etc.
    /// until a unique name is found.
    /// </para>
    /// <para>
    /// This method assumes the caller already holds <see cref="_lock"/>.
    /// </para>
    /// </remarks>
    private async Task<string> GenerateUniqueNameAsync(string baseName, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[ENTER] GenerateUniqueNameAsync - BaseName: '{Name}'", baseName);

        // Check if the base name is available
        if (!await _repository.NameExistsAsync(baseName, cancellationToken: cancellationToken))
        {
            _logger.LogDebug("[EXIT] GenerateUniqueNameAsync - Base name is available");
            return baseName;
        }

        // Try numbered suffixes
        for (var i = 1; i <= 100; i++)
        {
            var candidateName = $"{baseName} ({i})";
            if (!await _repository.NameExistsAsync(candidateName, cancellationToken: cancellationToken))
            {
                _logger.LogDebug("[EXIT] GenerateUniqueNameAsync - Found unique name: '{Name}'", candidateName);
                return candidateName;
            }
        }

        // Extremely unlikely edge case - use timestamp
        var fallbackName = $"{baseName} ({DateTime.UtcNow:yyyyMMddHHmmss})";
        _logger.LogWarning("[EXIT] GenerateUniqueNameAsync - Using timestamp fallback: '{Name}'", fallbackName);
        return fallbackName;
    }

    /// <summary>
    /// Resets the current prompt to the default. Called internally when current prompt is deleted.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <remarks>
    /// <para>
    /// This method assumes the caller already holds <see cref="_lock"/>.
    /// </para>
    /// </remarks>
    private async Task ResetCurrentToDefaultInternalAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("[ENTER] ResetCurrentToDefaultInternalAsync");

        var previousPrompt = _currentPrompt;

        // Try to get default prompt
        var defaultEntity = await _repository.GetDefaultAsync(cancellationToken);
        if (defaultEntity != null)
        {
            _currentPrompt = SystemPrompt.FromEntity(defaultEntity);
            _logger.LogDebug("[INFO] ResetCurrentToDefaultInternalAsync - Reset to default: '{Name}'", _currentPrompt.Name);
        }
        else
        {
            // Fall back to first available
            var allPrompts = await _repository.GetAllActiveAsync(cancellationToken);
            if (allPrompts.Count > 0)
            {
                _currentPrompt = SystemPrompt.FromEntity(allPrompts[0]);
                _logger.LogDebug("[INFO] ResetCurrentToDefaultInternalAsync - No default, using first: '{Name}'", _currentPrompt.Name);
            }
            else
            {
                _currentPrompt = null;
                _logger.LogWarning("[INFO] ResetCurrentToDefaultInternalAsync - No prompts available");
            }
        }

        // Update settings
        var appSettings = _settingsService.CurrentSettings;
        appSettings.CurrentSystemPromptId = _currentPrompt?.Id;
        await _settingsService.SaveSettingsAsync(appSettings);

        _logger.LogDebug("[EXIT] ResetCurrentToDefaultInternalAsync - Current is now: '{Name}'", _currentPrompt?.Name ?? "(none)");

        // Fire event if the prompt changed
        if (previousPrompt?.Id != _currentPrompt?.Id)
        {
            OnCurrentPromptChanged(_currentPrompt, previousPrompt);
        }
    }

    /// <summary>
    /// Fires the PromptListChanged event.
    /// </summary>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <param name="promptId">The ID of the affected prompt.</param>
    /// <param name="promptName">The name of the affected prompt.</param>
    private void OnPromptListChanged(PromptListChangeType changeType, Guid? promptId, string? promptName)
    {
        _logger.LogDebug(
            "[EVENT] PromptListChanged - Type: {Type}, Id: {Id}, Name: '{Name}'",
            changeType, promptId?.ToString() ?? "(null)", promptName ?? "(null)");

        PromptListChanged?.Invoke(this, new PromptListChangedEventArgs
        {
            ChangeType = changeType,
            AffectedPromptId = promptId,
            AffectedPromptName = promptName
        });
    }

    /// <summary>
    /// Fires the CurrentPromptChanged event.
    /// </summary>
    /// <param name="newPrompt">The newly selected prompt.</param>
    /// <param name="previousPrompt">The previously selected prompt.</param>
    private void OnCurrentPromptChanged(SystemPrompt? newPrompt, SystemPrompt? previousPrompt)
    {
        _logger.LogDebug(
            "[EVENT] CurrentPromptChanged - New: '{New}', Previous: '{Prev}'",
            newPrompt?.Name ?? "(null)", previousPrompt?.Name ?? "(null)");

        CurrentPromptChanged?.Invoke(this, new CurrentPromptChangedEventArgs
        {
            NewPrompt = newPrompt,
            PreviousPrompt = previousPrompt
        });
    }

    #endregion

    #region IAsyncDisposable

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("[DISPOSE] SystemPromptService - Releasing resources");

        _lock.Dispose();

        await ValueTask.CompletedTask;
    }

    #endregion
}
