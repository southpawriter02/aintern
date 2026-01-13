using System.Diagnostics;
using Microsoft.Extensions.Logging;
using AIntern.Core;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data.Repositories;

namespace AIntern.Services;

/// <summary>
/// Service for managing inference settings with preset support and persistence.
/// </summary>
/// <remarks>
/// <para>
/// This service is the single source of truth for inference parameters at runtime.
/// It coordinates between the repository (presets), settings service (persistence),
/// and consumers (LlmService, ViewModels).
/// </para>
/// <para>
/// <b>Initialization:</b> Call <see cref="InitializeAsync"/> during app startup
/// to load the last-used preset from settings.json.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Parameter updates are synchronous and lock-free</description></item>
///   <item><description>Async preset operations use <see cref="SemaphoreSlim"/></description></item>
///   <item><description>Events fire with cloned data for safety</description></item>
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
/// </remarks>
public sealed class InferenceSettingsService : IInferenceSettingsService, IAsyncDisposable
{
    #region Fields

    private readonly IInferencePresetRepository _presetRepository;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<InferenceSettingsService> _logger;

    /// <summary>
    /// Current live settings - used by inference operations.
    /// </summary>
    private InferenceSettings _currentSettings = new();

    /// <summary>
    /// Currently active preset (null = custom settings).
    /// </summary>
    private InferencePreset? _activePreset;

    /// <summary>
    /// Snapshot of preset settings for change detection.
    /// </summary>
    private InferenceSettings? _presetSnapshot;

    /// <summary>
    /// Semaphore for thread-safe async operations.
    /// </summary>
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Flag indicating whether the service has been initialized.
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    /// Epsilon for float comparisons to avoid floating-point precision issues.
    /// </summary>
    private const float FloatEpsilon = 0.001f;

    #endregion

    #region Properties

    /// <inheritdoc />
    public InferenceSettings CurrentSettings => _currentSettings;

    /// <inheritdoc />
    public InferencePreset? ActivePreset => _activePreset;

    /// <inheritdoc />
    public bool HasUnsavedChanges
    {
        get
        {
            // No changes possible if no preset is active or no snapshot exists
            if (_activePreset == null || _presetSnapshot == null)
            {
                return false;
            }

            return !SettingsEqual(_currentSettings, _presetSnapshot);
        }
    }

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<InferenceSettingsChangedEventArgs>? SettingsChanged;

    /// <inheritdoc />
    public event EventHandler<PresetChangedEventArgs>? PresetChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceSettingsService"/> class.
    /// </summary>
    /// <param name="presetRepository">Repository for preset CRUD operations.</param>
    /// <param name="settingsService">Service for persisting active preset ID.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <remarks>
    /// <para>
    /// After construction, call <see cref="InitializeAsync"/> to load the last-used
    /// preset from settings.json.
    /// </para>
    /// </remarks>
    public InferenceSettingsService(
        IInferencePresetRepository presetRepository,
        ISettingsService settingsService,
        ILogger<InferenceSettingsService> logger)
    {
        _presetRepository = presetRepository ?? throw new ArgumentNullException(nameof(presetRepository));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[INIT] InferenceSettingsService created");
    }

    #endregion

    #region Parameter Updates

    /// <inheritdoc />
    public void UpdateTemperature(float value)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdateTemperature - Value: {Value}", value);

        // Clamp to valid range
        var clamped = Math.Clamp(value, ParameterConstants.Temperature.Min, ParameterConstants.Temperature.Max);

        if (Math.Abs(clamped - value) > FloatEpsilon)
        {
            _logger.LogDebug("[INFO] Temperature clamped: {Original} -> {Clamped}", value, clamped);
        }

        // Check if value actually changed
        if (Math.Abs(_currentSettings.Temperature - clamped) < FloatEpsilon)
        {
            stopwatch.Stop();
            _logger.LogDebug("[SKIP] Temperature unchanged: {Value}, Duration: {Ms}ms", clamped, stopwatch.ElapsedMilliseconds);
            return;
        }

        var oldValue = _currentSettings.Temperature;
        _currentSettings.Temperature = clamped;

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] UpdateTemperature - OldValue: {Old}, NewValue: {New}, Duration: {Ms}ms",
            oldValue, clamped, stopwatch.ElapsedMilliseconds);

        OnSettingsChanged(InferenceSettingsChangeType.ParameterChanged, nameof(InferenceSettings.Temperature));
    }

    /// <inheritdoc />
    public void UpdateTopP(float value)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdateTopP - Value: {Value}", value);

        // Clamp to valid range
        var clamped = Math.Clamp(value, ParameterConstants.TopP.Min, ParameterConstants.TopP.Max);

        if (Math.Abs(clamped - value) > FloatEpsilon)
        {
            _logger.LogDebug("[INFO] TopP clamped: {Original} -> {Clamped}", value, clamped);
        }

        // Check if value actually changed
        if (Math.Abs(_currentSettings.TopP - clamped) < FloatEpsilon)
        {
            stopwatch.Stop();
            _logger.LogDebug("[SKIP] TopP unchanged: {Value}, Duration: {Ms}ms", clamped, stopwatch.ElapsedMilliseconds);
            return;
        }

        var oldValue = _currentSettings.TopP;
        _currentSettings.TopP = clamped;

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] UpdateTopP - OldValue: {Old}, NewValue: {New}, Duration: {Ms}ms",
            oldValue, clamped, stopwatch.ElapsedMilliseconds);

        OnSettingsChanged(InferenceSettingsChangeType.ParameterChanged, nameof(InferenceSettings.TopP));
    }

    /// <inheritdoc />
    public void UpdateTopK(int value)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdateTopK - Value: {Value}", value);

        // Clamp to valid range
        var clamped = Math.Clamp(value, ParameterConstants.TopK.Min, ParameterConstants.TopK.Max);

        if (clamped != value)
        {
            _logger.LogDebug("[INFO] TopK clamped: {Original} -> {Clamped}", value, clamped);
        }

        // Check if value actually changed
        if (_currentSettings.TopK == clamped)
        {
            stopwatch.Stop();
            _logger.LogDebug("[SKIP] TopK unchanged: {Value}, Duration: {Ms}ms", clamped, stopwatch.ElapsedMilliseconds);
            return;
        }

        var oldValue = _currentSettings.TopK;
        _currentSettings.TopK = clamped;

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] UpdateTopK - OldValue: {Old}, NewValue: {New}, Duration: {Ms}ms",
            oldValue, clamped, stopwatch.ElapsedMilliseconds);

        OnSettingsChanged(InferenceSettingsChangeType.ParameterChanged, nameof(InferenceSettings.TopK));
    }

    /// <inheritdoc />
    public void UpdateRepetitionPenalty(float value)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdateRepetitionPenalty - Value: {Value}", value);

        // Clamp to valid range
        var clamped = Math.Clamp(value, ParameterConstants.RepetitionPenalty.Min, ParameterConstants.RepetitionPenalty.Max);

        if (Math.Abs(clamped - value) > FloatEpsilon)
        {
            _logger.LogDebug("[INFO] RepetitionPenalty clamped: {Original} -> {Clamped}", value, clamped);
        }

        // Check if value actually changed
        if (Math.Abs(_currentSettings.RepetitionPenalty - clamped) < FloatEpsilon)
        {
            stopwatch.Stop();
            _logger.LogDebug("[SKIP] RepetitionPenalty unchanged: {Value}, Duration: {Ms}ms", clamped, stopwatch.ElapsedMilliseconds);
            return;
        }

        var oldValue = _currentSettings.RepetitionPenalty;
        _currentSettings.RepetitionPenalty = clamped;

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] UpdateRepetitionPenalty - OldValue: {Old}, NewValue: {New}, Duration: {Ms}ms",
            oldValue, clamped, stopwatch.ElapsedMilliseconds);

        OnSettingsChanged(InferenceSettingsChangeType.ParameterChanged, nameof(InferenceSettings.RepetitionPenalty));
    }

    /// <inheritdoc />
    public void UpdateMaxTokens(int value)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdateMaxTokens - Value: {Value}", value);

        // Clamp to valid range
        var clamped = Math.Clamp(value, ParameterConstants.MaxTokens.Min, ParameterConstants.MaxTokens.Max);

        if (clamped != value)
        {
            _logger.LogDebug("[INFO] MaxTokens clamped: {Original} -> {Clamped}", value, clamped);
        }

        // Check if value actually changed
        if (_currentSettings.MaxTokens == clamped)
        {
            stopwatch.Stop();
            _logger.LogDebug("[SKIP] MaxTokens unchanged: {Value}, Duration: {Ms}ms", clamped, stopwatch.ElapsedMilliseconds);
            return;
        }

        var oldValue = _currentSettings.MaxTokens;
        _currentSettings.MaxTokens = clamped;

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] UpdateMaxTokens - OldValue: {Old}, NewValue: {New}, Duration: {Ms}ms",
            oldValue, clamped, stopwatch.ElapsedMilliseconds);

        OnSettingsChanged(InferenceSettingsChangeType.ParameterChanged, nameof(InferenceSettings.MaxTokens));
    }

    /// <inheritdoc />
    public void UpdateContextSize(uint value)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdateContextSize - Value: {Value}", value);

        // Clamp to valid range
        var clamped = Math.Clamp(value, ParameterConstants.ContextSize.Min, ParameterConstants.ContextSize.Max);

        if (clamped != value)
        {
            _logger.LogDebug("[INFO] ContextSize clamped: {Original} -> {Clamped}", value, clamped);
        }

        // Check if value actually changed
        if (_currentSettings.ContextSize == clamped)
        {
            stopwatch.Stop();
            _logger.LogDebug("[SKIP] ContextSize unchanged: {Value}, Duration: {Ms}ms", clamped, stopwatch.ElapsedMilliseconds);
            return;
        }

        var oldValue = _currentSettings.ContextSize;
        _currentSettings.ContextSize = clamped;

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] UpdateContextSize - OldValue: {Old}, NewValue: {New}, Duration: {Ms}ms",
            oldValue, clamped, stopwatch.ElapsedMilliseconds);

        OnSettingsChanged(InferenceSettingsChangeType.ParameterChanged, nameof(InferenceSettings.ContextSize));
    }

    /// <inheritdoc />
    public void UpdateSeed(int value)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdateSeed - Value: {Value}", value);

        // Clamp to valid range (minimum is -1 for random)
        var clamped = Math.Max(value, ParameterConstants.Seed.Min);

        if (clamped != value)
        {
            _logger.LogDebug("[INFO] Seed clamped: {Original} -> {Clamped}", value, clamped);
        }

        // Check if value actually changed
        if (_currentSettings.Seed == clamped)
        {
            stopwatch.Stop();
            _logger.LogDebug("[SKIP] Seed unchanged: {Value}, Duration: {Ms}ms", clamped, stopwatch.ElapsedMilliseconds);
            return;
        }

        var oldValue = _currentSettings.Seed;
        _currentSettings.Seed = clamped;

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] UpdateSeed - OldValue: {Old}, NewValue: {New}, Duration: {Ms}ms",
            oldValue, clamped, stopwatch.ElapsedMilliseconds);

        OnSettingsChanged(InferenceSettingsChangeType.ParameterChanged, nameof(InferenceSettings.Seed));
    }

    /// <inheritdoc />
    public void UpdateAll(InferenceSettings settings)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdateAll");

        ArgumentNullException.ThrowIfNull(settings);

        // Clone and clamp all values
        var clamped = new InferenceSettings
        {
            Temperature = Math.Clamp(settings.Temperature, ParameterConstants.Temperature.Min, ParameterConstants.Temperature.Max),
            TopP = Math.Clamp(settings.TopP, ParameterConstants.TopP.Min, ParameterConstants.TopP.Max),
            TopK = Math.Clamp(settings.TopK, ParameterConstants.TopK.Min, ParameterConstants.TopK.Max),
            RepetitionPenalty = Math.Clamp(settings.RepetitionPenalty, ParameterConstants.RepetitionPenalty.Min, ParameterConstants.RepetitionPenalty.Max),
            MaxTokens = Math.Clamp(settings.MaxTokens, ParameterConstants.MaxTokens.Min, ParameterConstants.MaxTokens.Max),
            ContextSize = Math.Clamp(settings.ContextSize, ParameterConstants.ContextSize.Min, ParameterConstants.ContextSize.Max),
            Seed = Math.Max(settings.Seed, ParameterConstants.Seed.Min)
        };

        // Check if anything actually changed
        if (SettingsEqual(_currentSettings, clamped))
        {
            stopwatch.Stop();
            _logger.LogDebug("[SKIP] UpdateAll - No changes detected, Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);
            return;
        }

        _logger.LogDebug(
            "[INFO] UpdateAll - Applying new settings: Temp={Temp}, TopP={TopP}, TopK={TopK}, RepPen={RepPen}, MaxTok={MaxTok}, CtxSize={CtxSize}, Seed={Seed}",
            clamped.Temperature, clamped.TopP, clamped.TopK, clamped.RepetitionPenalty,
            clamped.MaxTokens, clamped.ContextSize, clamped.Seed);

        _currentSettings = clamped;

        stopwatch.Stop();
        _logger.LogDebug("[EXIT] UpdateAll - Duration: {Ms}ms", stopwatch.ElapsedMilliseconds);

        OnSettingsChanged(InferenceSettingsChangeType.AllChanged);
    }

    #endregion

    #region Preset Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferencePreset>> GetPresetsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetPresetsAsync");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entities = await _presetRepository.GetAllAsync(cancellationToken);
            var presets = entities.Select(InferencePreset.FromEntity).ToList();

            stopwatch.Stop();
            _logger.LogDebug("[EXIT] GetPresetsAsync - Count: {Count}, Duration: {Ms}ms", presets.Count, stopwatch.ElapsedMilliseconds);

            return presets;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ApplyPresetAsync(Guid presetId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] ApplyPresetAsync - PresetId: {PresetId}", presetId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Load preset from repository
            var entity = await _presetRepository.GetByIdAsync(presetId, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[EXIT] ApplyPresetAsync - Preset not found: {PresetId}", presetId);
                throw new InvalidOperationException($"Preset not found: {presetId}");
            }

            var preset = InferencePreset.FromEntity(entity);
            var previousPreset = _activePreset;

            _logger.LogDebug(
                "[INFO] ApplyPresetAsync - Loading preset '{PresetName}' with Temp={Temp}, TopP={TopP}, TopK={TopK}",
                preset.Name, preset.Options.Temperature, preset.Options.TopP, preset.Options.TopK);

            // Clone settings from preset
            _currentSettings = preset.Options.Clone();
            _activePreset = preset;
            _presetSnapshot = preset.Options.Clone();

            // Persist active preset ID to settings.json
            var appSettings = _settingsService.CurrentSettings;
            appSettings.ActivePresetId = presetId;
            await _settingsService.SaveSettingsAsync(appSettings);

            // Increment usage count for analytics
            await _presetRepository.IncrementUsageAsync(presetId, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] ApplyPresetAsync - Applied '{PresetName}', Duration: {Ms}ms",
                preset.Name, stopwatch.ElapsedMilliseconds);

            // Fire events
            OnSettingsChanged(InferenceSettingsChangeType.PresetApplied);
            OnPresetChanged(preset, previousPreset, PresetChangeType.Applied);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<InferencePreset> SaveAsPresetAsync(
        string name,
        string? description = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SaveAsPresetAsync - Name: {Name}, Category: {Category}", name, category ?? "(none)");

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Check for name uniqueness
            if (await _presetRepository.NameExistsAsync(name, cancellationToken: cancellationToken))
            {
                _logger.LogWarning("[EXIT] SaveAsPresetAsync - Name already exists: {Name}", name);
                throw new InvalidOperationException($"A preset named '{name}' already exists.");
            }

            // Create new preset from current settings
            var preset = InferencePreset.FromOptions(name, _currentSettings);
            preset.Description = description;
            preset.Category = category;

            _logger.LogDebug(
                "[INFO] SaveAsPresetAsync - Creating preset with Temp={Temp}, TopP={TopP}, MaxTokens={MaxTok}",
                preset.Options.Temperature, preset.Options.TopP, preset.Options.MaxTokens);

            // Save to repository
            var entity = preset.ToEntity();
            var createdEntity = await _presetRepository.CreateAsync(entity, cancellationToken);
            var createdPreset = InferencePreset.FromEntity(createdEntity);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] SaveAsPresetAsync - Created preset '{Name}' (ID: {Id}), Duration: {Ms}ms",
                createdPreset.Name, createdPreset.Id, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPresetChanged(createdPreset, null, PresetChangeType.Created);

            return createdPreset;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdatePresetAsync(Guid presetId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] UpdatePresetAsync - PresetId: {PresetId}", presetId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Load existing preset
            var entity = await _presetRepository.GetByIdAsync(presetId, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[EXIT] UpdatePresetAsync - Preset not found: {PresetId}", presetId);
                throw new InvalidOperationException($"Preset not found: {presetId}");
            }

            // Prevent updating built-in presets
            if (entity.IsBuiltIn)
            {
                _logger.LogWarning("[EXIT] UpdatePresetAsync - Cannot update built-in preset: {Name}", entity.Name);
                throw new InvalidOperationException($"Cannot update built-in preset: {entity.Name}");
            }

            var previousPreset = InferencePreset.FromEntity(entity);

            _logger.LogDebug(
                "[INFO] UpdatePresetAsync - Updating '{Name}' with Temp={Temp}, TopP={TopP}, MaxTokens={MaxTok}",
                entity.Name, _currentSettings.Temperature, _currentSettings.TopP, _currentSettings.MaxTokens);

            // Update entity with current settings
            entity.Temperature = _currentSettings.Temperature;
            entity.TopP = _currentSettings.TopP;
            entity.TopK = _currentSettings.TopK;
            entity.RepeatPenalty = _currentSettings.RepetitionPenalty;
            entity.MaxTokens = _currentSettings.MaxTokens;
            entity.ContextSize = (int)_currentSettings.ContextSize;
            entity.Seed = _currentSettings.Seed;

            await _presetRepository.UpdateAsync(entity, cancellationToken);

            var updatedPreset = InferencePreset.FromEntity(entity);

            // Update snapshot if this is the active preset
            if (_activePreset?.Id == presetId)
            {
                _presetSnapshot = _currentSettings.Clone();
                _activePreset = updatedPreset;
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] UpdatePresetAsync - Updated preset '{Name}', Duration: {Ms}ms",
                entity.Name, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPresetChanged(updatedPreset, previousPreset, PresetChangeType.Updated);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeletePresetAsync(Guid presetId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] DeletePresetAsync - PresetId: {PresetId}", presetId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Load existing preset
            var entity = await _presetRepository.GetByIdAsync(presetId, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[EXIT] DeletePresetAsync - Preset not found: {PresetId}", presetId);
                throw new InvalidOperationException($"Preset not found: {presetId}");
            }

            // Prevent deleting built-in presets
            if (entity.IsBuiltIn)
            {
                _logger.LogWarning("[EXIT] DeletePresetAsync - Cannot delete built-in preset: {Name}", entity.Name);
                throw new InvalidOperationException($"Cannot delete built-in preset: {entity.Name}");
            }

            var deletedPreset = InferencePreset.FromEntity(entity);

            _logger.LogDebug("[INFO] DeletePresetAsync - Deleting preset '{Name}'", entity.Name);

            await _presetRepository.DeleteAsync(presetId, cancellationToken);

            // Clear active preset if it was the deleted one
            if (_activePreset?.Id == presetId)
            {
                _logger.LogDebug("[INFO] DeletePresetAsync - Clearing active preset reference");
                _activePreset = null;
                _presetSnapshot = null;
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] DeletePresetAsync - Deleted preset '{Name}', Duration: {Ms}ms",
                deletedPreset.Name, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPresetChanged(null, deletedPreset, PresetChangeType.Deleted);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] ResetToDefaultsAsync");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Get the default preset
            var defaultEntity = await _presetRepository.GetDefaultAsync(cancellationToken);
            if (defaultEntity == null)
            {
                // Fall back to Balanced preset ID
                _logger.LogDebug("[INFO] ResetToDefaultsAsync - No default preset found, using Balanced");
                defaultEntity = await _presetRepository.GetByIdAsync(InferencePreset.BalancedPresetId, cancellationToken);
            }

            if (defaultEntity == null)
            {
                _logger.LogWarning("[EXIT] ResetToDefaultsAsync - No presets available");
                throw new InvalidOperationException("No presets available for reset.");
            }

            var preset = InferencePreset.FromEntity(defaultEntity);
            var previousPreset = _activePreset;

            _logger.LogDebug("[INFO] ResetToDefaultsAsync - Resetting to '{PresetName}'", preset.Name);

            // Clone settings from preset
            _currentSettings = preset.Options.Clone();
            _activePreset = preset;
            _presetSnapshot = preset.Options.Clone();

            // Persist active preset ID
            var appSettings = _settingsService.CurrentSettings;
            appSettings.ActivePresetId = preset.Id;
            await _settingsService.SaveSettingsAsync(appSettings);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] ResetToDefaultsAsync - Reset to '{PresetName}', Duration: {Ms}ms",
                preset.Name, stopwatch.ElapsedMilliseconds);

            // Fire events with ResetToDefaults type
            OnSettingsChanged(InferenceSettingsChangeType.ResetToDefaults);
            OnPresetChanged(preset, previousPreset, PresetChangeType.Applied);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetDefaultPresetAsync(Guid presetId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SetDefaultPresetAsync - PresetId: {PresetId}", presetId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Verify the preset exists
            var entity = await _presetRepository.GetByIdAsync(presetId, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[EXIT] SetDefaultPresetAsync - Preset not found: {PresetId}", presetId);
                throw new InvalidOperationException($"Preset not found: {presetId}");
            }

            // Get previous default for event
            var previousDefaultEntity = await _presetRepository.GetDefaultAsync(cancellationToken);
            var previousDefault = previousDefaultEntity != null ? InferencePreset.FromEntity(previousDefaultEntity) : null;

            _logger.LogDebug("[INFO] SetDefaultPresetAsync - Setting '{Name}' as default", entity.Name);

            await _presetRepository.SetAsDefaultAsync(presetId, cancellationToken);

            var newDefault = InferencePreset.FromEntity(entity);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] SetDefaultPresetAsync - Set '{Name}' as default, Duration: {Ms}ms",
                entity.Name, stopwatch.ElapsedMilliseconds);

            // Fire event
            OnPresetChanged(newDefault, previousDefault, PresetChangeType.DefaultChanged);
        }
        finally
        {
            _lock.Release();
        }
    }

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

            // Load settings to get the active preset ID
            var appSettings = await _settingsService.LoadSettingsAsync();
            var activePresetId = appSettings.ActivePresetId;

            _logger.LogDebug("[INFO] InitializeAsync - ActivePresetId from settings: {PresetId}", activePresetId?.ToString() ?? "(null)");

            if (activePresetId.HasValue)
            {
                // Try to load the saved preset
                var entity = await _presetRepository.GetByIdAsync(activePresetId.Value, cancellationToken);
                if (entity != null)
                {
                    var preset = InferencePreset.FromEntity(entity);

                    _logger.LogDebug("[INFO] InitializeAsync - Loading saved preset '{Name}'", preset.Name);

                    _currentSettings = preset.Options.Clone();
                    _activePreset = preset;
                    _presetSnapshot = preset.Options.Clone();

                    _isInitialized = true;

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "[EXIT] InitializeAsync - Loaded preset '{Name}', Duration: {Ms}ms",
                        preset.Name, stopwatch.ElapsedMilliseconds);

                    return;
                }

                _logger.LogDebug("[INFO] InitializeAsync - Saved preset not found, falling back to default");
            }

            // Fall back to default preset
            var defaultEntity = await _presetRepository.GetDefaultAsync(cancellationToken);
            if (defaultEntity == null)
            {
                defaultEntity = await _presetRepository.GetByIdAsync(InferencePreset.BalancedPresetId, cancellationToken);
            }

            if (defaultEntity != null)
            {
                var defaultPreset = InferencePreset.FromEntity(defaultEntity);

                _logger.LogDebug("[INFO] InitializeAsync - Loading default preset '{Name}'", defaultPreset.Name);

                _currentSettings = defaultPreset.Options.Clone();
                _activePreset = defaultPreset;
                _presetSnapshot = defaultPreset.Options.Clone();

                // Update settings with the default preset ID
                appSettings.ActivePresetId = defaultPreset.Id;
                await _settingsService.SaveSettingsAsync(appSettings);
            }
            else
            {
                _logger.LogWarning("[INFO] InitializeAsync - No presets available, using defaults");
                // Keep default InferenceSettings values
            }

            _isInitialized = true;

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] InitializeAsync - Initialized with '{PresetName}', Duration: {Ms}ms",
                _activePreset?.Name ?? "(defaults)", stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Compares two InferenceSettings instances for equality using epsilon for floats.
    /// </summary>
    /// <param name="a">First settings instance.</param>
    /// <param name="b">Second settings instance.</param>
    /// <returns>True if all properties are equal within epsilon tolerance.</returns>
    /// <remarks>
    /// <para>
    /// Float comparisons use <see cref="FloatEpsilon"/> (0.001) to avoid
    /// false negatives due to floating-point precision issues.
    /// </para>
    /// </remarks>
    private static bool SettingsEqual(InferenceSettings a, InferenceSettings b)
    {
        return Math.Abs(a.Temperature - b.Temperature) < FloatEpsilon &&
               Math.Abs(a.TopP - b.TopP) < FloatEpsilon &&
               a.TopK == b.TopK &&
               Math.Abs(a.RepetitionPenalty - b.RepetitionPenalty) < FloatEpsilon &&
               a.MaxTokens == b.MaxTokens &&
               a.ContextSize == b.ContextSize &&
               a.Seed == b.Seed;
    }

    /// <summary>
    /// Fires the SettingsChanged event with a clone of current settings.
    /// </summary>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <param name="parameter">The name of the parameter that changed (for single-parameter changes).</param>
    /// <remarks>
    /// <para>
    /// The event args include a clone of the current settings to ensure
    /// thread safety - subscribers receive an independent copy.
    /// </para>
    /// </remarks>
    private void OnSettingsChanged(InferenceSettingsChangeType changeType, string? parameter = null)
    {
        _logger.LogDebug("[EVENT] SettingsChanged - Type: {Type}, Parameter: {Param}", changeType, parameter ?? "(all)");

        SettingsChanged?.Invoke(this, new InferenceSettingsChangedEventArgs
        {
            NewSettings = _currentSettings.Clone(),
            ChangeType = changeType,
            ChangedParameter = parameter
        });
    }

    /// <summary>
    /// Fires the PresetChanged event.
    /// </summary>
    /// <param name="newPreset">The new preset (for create/apply/update operations).</param>
    /// <param name="previousPreset">The previous preset (for apply/delete operations).</param>
    /// <param name="changeType">The type of preset operation that occurred.</param>
    private void OnPresetChanged(InferencePreset? newPreset, InferencePreset? previousPreset, PresetChangeType changeType)
    {
        _logger.LogDebug(
            "[EVENT] PresetChanged - Type: {Type}, New: {New}, Previous: {Prev}",
            changeType, newPreset?.Name ?? "(null)", previousPreset?.Name ?? "(null)");

        PresetChanged?.Invoke(this, new PresetChangedEventArgs
        {
            NewPreset = newPreset,
            PreviousPreset = previousPreset,
            ChangeType = changeType
        });
    }

    #endregion

    #region IAsyncDisposable

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("[DISPOSE] InferenceSettingsService - Releasing resources");

        _lock.Dispose();

        await ValueTask.CompletedTask;
    }

    #endregion
}
