using AIntern.Core.Entities;
using AIntern.Core.Interfaces;

namespace AIntern.Services;

/// <summary>
/// Service for managing inference parameter settings with database persistence.
/// </summary>
public sealed class InferenceSettingsService : IInferenceSettingsService
{
    private readonly IInferencePresetRepository _presetRepository;
    private InferenceSettings _currentSettings = InferenceSettings.Default;

    public InferenceSettingsService(IInferencePresetRepository presetRepository)
    {
        _presetRepository = presetRepository ?? throw new ArgumentNullException(nameof(presetRepository));

        // Load default settings on construction
        _ = LoadDefaultSettingsAsync();
    }

    public InferenceSettings CurrentSettings => _currentSettings;

    public event EventHandler<InferenceSettingsChangedEventArgs>? SettingsChanged;

    public void UpdateSettings(InferenceSettings settings)
    {
        var previous = _currentSettings;
        _currentSettings = settings;
        SettingsChanged?.Invoke(this, new InferenceSettingsChangedEventArgs(settings, previous));
    }

    public async Task<IReadOnlyList<InferencePreset>> GetPresetsAsync(CancellationToken ct = default)
    {
        var entities = await _presetRepository.GetAllAsync(ct);
        return entities.Select(MapToPreset).ToList();
    }

    public async Task<InferencePreset> SavePresetAsync(string name, InferenceSettings settings, CancellationToken ct = default)
    {
        var entity = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Temperature = settings.Temperature,
            TopP = settings.TopP,
            MaxTokens = settings.MaxTokens,
            ContextSize = settings.ContextSize,
            IsDefault = false,
            IsBuiltIn = false
        };

        await _presetRepository.CreateAsync(entity, ct);
        return MapToPreset(entity);
    }

    public async Task DeletePresetAsync(Guid presetId, CancellationToken ct = default)
    {
        await _presetRepository.DeleteAsync(presetId, ct);
    }

    public async Task ApplyPresetAsync(Guid presetId, CancellationToken ct = default)
    {
        var entity = await _presetRepository.GetByIdAsync(presetId, ct);

        if (entity is null)
        {
            throw new InvalidOperationException($"Preset with ID {presetId} not found.");
        }

        var settings = new InferenceSettings
        {
            Temperature = entity.Temperature,
            TopP = entity.TopP,
            MaxTokens = entity.MaxTokens,
            ContextSize = entity.ContextSize
        };

        UpdateSettings(settings);
    }

    public async Task SetDefaultPresetAsync(Guid presetId, CancellationToken ct = default)
    {
        await _presetRepository.SetDefaultAsync(presetId, ct);
    }

    public void ResetToDefaults()
    {
        UpdateSettings(InferenceSettings.Default);
    }

    private async Task LoadDefaultSettingsAsync()
    {
        try
        {
            var defaultPreset = await _presetRepository.GetDefaultAsync();
            if (defaultPreset is not null)
            {
                _currentSettings = new InferenceSettings
                {
                    Temperature = defaultPreset.Temperature,
                    TopP = defaultPreset.TopP,
                    MaxTokens = defaultPreset.MaxTokens,
                    ContextSize = defaultPreset.ContextSize
                };
            }
        }
        catch
        {
            // If loading fails, keep default settings
        }
    }

    private static InferencePreset MapToPreset(InferencePresetEntity entity)
    {
        return new InferencePreset
        {
            Id = entity.Id,
            Name = entity.Name,
            Settings = new InferenceSettings
            {
                Temperature = entity.Temperature,
                TopP = entity.TopP,
                MaxTokens = entity.MaxTokens,
                ContextSize = entity.ContextSize
            },
            IsDefault = entity.IsDefault,
            IsBuiltIn = entity.IsBuiltIn
        };
    }
}
