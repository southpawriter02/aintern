using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the inference settings panel.
/// Manages temperature, top-p, max tokens, context size, and presets.
/// </summary>
public sealed partial class InferenceSettingsViewModel : ObservableObject, IDisposable
{
    private readonly IInferenceSettingsService _settingsService;
    private readonly System.Timers.Timer _debounceTimer;
    private bool _isUpdatingFromPreset;
    private bool _disposed;

    private const int DebounceMs = 300;

    // Slider value ranges
    public const float MinTemperature = 0.0f;
    public const float MaxTemperature = 2.0f;
    public const float MinTopP = 0.0f;
    public const float MaxTopP = 1.0f;
    public const int MinMaxTokens = 64;
    public const int MaxMaxTokens = 8192;
    public const int MinContextSize = 512;
    public const int MaxContextSize = 32768;

    [ObservableProperty]
    private float _temperature;

    [ObservableProperty]
    private float _topP;

    [ObservableProperty]
    private int _maxTokens;

    [ObservableProperty]
    private int _contextSize;

    [ObservableProperty]
    private ObservableCollection<InferencePresetViewModel> _presets = new();

    [ObservableProperty]
    private InferencePresetViewModel? _selectedPreset;

    [ObservableProperty]
    private bool _isPanelExpanded = true;

    [ObservableProperty]
    private bool _isLoading;

    public InferenceSettingsViewModel(IInferenceSettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Initialize from current settings
        var settings = _settingsService.CurrentSettings;
        _temperature = settings.Temperature;
        _topP = settings.TopP;
        _maxTokens = settings.MaxTokens;
        _contextSize = settings.ContextSize;

        // Setup debounce timer
        _debounceTimer = new System.Timers.Timer(DebounceMs);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += (_, _) => ApplySettingsToService();

        // Subscribe to settings changes
        _settingsService.SettingsChanged += OnSettingsChanged;

        // Load presets
        _ = LoadPresetsAsync();
    }

    partial void OnTemperatureChanged(float value)
    {
        if (!_isUpdatingFromPreset)
        {
            ScheduleSettingsUpdate();
        }
    }

    partial void OnTopPChanged(float value)
    {
        if (!_isUpdatingFromPreset)
        {
            ScheduleSettingsUpdate();
        }
    }

    partial void OnMaxTokensChanged(int value)
    {
        if (!_isUpdatingFromPreset)
        {
            ScheduleSettingsUpdate();
        }
    }

    partial void OnContextSizeChanged(int value)
    {
        if (!_isUpdatingFromPreset)
        {
            ScheduleSettingsUpdate();
        }
    }

    partial void OnSelectedPresetChanged(InferencePresetViewModel? value)
    {
        if (value is not null)
        {
            _ = ApplyPresetAsync(value.Id);
        }
    }

    [RelayCommand]
    private async Task LoadPresetsAsync()
    {
        IsLoading = true;
        try
        {
            var presets = await _settingsService.GetPresetsAsync();
            Presets.Clear();
            foreach (var preset in presets)
            {
                Presets.Add(new InferencePresetViewModel(preset));
            }

            // Select the default preset if one exists
            SelectedPreset = Presets.FirstOrDefault(p => p.IsDefault);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyPresetAsync(Guid presetId)
    {
        _isUpdatingFromPreset = true;
        try
        {
            await _settingsService.ApplyPresetAsync(presetId);

            var settings = _settingsService.CurrentSettings;
            Temperature = settings.Temperature;
            TopP = settings.TopP;
            MaxTokens = settings.MaxTokens;
            ContextSize = settings.ContextSize;
        }
        finally
        {
            _isUpdatingFromPreset = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsPresetAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var settings = new InferenceSettings
        {
            Temperature = Temperature,
            TopP = TopP,
            MaxTokens = MaxTokens,
            ContextSize = ContextSize
        };

        var preset = await _settingsService.SavePresetAsync(name, settings);
        Presets.Add(new InferencePresetViewModel(preset));
    }

    [RelayCommand]
    private async Task DeletePresetAsync(InferencePresetViewModel? preset)
    {
        if (preset is null || preset.IsBuiltIn)
        {
            return;
        }

        await _settingsService.DeletePresetAsync(preset.Id);
        Presets.Remove(preset);

        if (SelectedPreset?.Id == preset.Id)
        {
            SelectedPreset = Presets.FirstOrDefault(p => p.IsDefault) ?? Presets.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _settingsService.ResetToDefaults();

        var settings = _settingsService.CurrentSettings;
        _isUpdatingFromPreset = true;
        Temperature = settings.Temperature;
        TopP = settings.TopP;
        MaxTokens = settings.MaxTokens;
        ContextSize = settings.ContextSize;
        _isUpdatingFromPreset = false;

        // Select the "Balanced" preset if it exists
        SelectedPreset = Presets.FirstOrDefault(p => p.Name == "Balanced") ?? Presets.FirstOrDefault(p => p.IsDefault);
    }

    [RelayCommand]
    private void TogglePanel()
    {
        IsPanelExpanded = !IsPanelExpanded;
    }

    private void ScheduleSettingsUpdate()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void ApplySettingsToService()
    {
        var settings = new InferenceSettings
        {
            Temperature = Temperature,
            TopP = TopP,
            MaxTokens = MaxTokens,
            ContextSize = ContextSize
        };

        _settingsService.UpdateSettings(settings);
    }

    private void OnSettingsChanged(object? sender, InferenceSettingsChangedEventArgs e)
    {
        // External settings change - update UI
        if (!_isUpdatingFromPreset)
        {
            _isUpdatingFromPreset = true;
            Temperature = e.NewSettings.Temperature;
            TopP = e.NewSettings.TopP;
            MaxTokens = e.NewSettings.MaxTokens;
            ContextSize = e.NewSettings.ContextSize;
            _isUpdatingFromPreset = false;
        }
    }

    /// <summary>
    /// Gets a description of what the current temperature value means.
    /// </summary>
    public string GetTemperatureDescription()
    {
        return Temperature switch
        {
            < 0.3f => "Very precise, deterministic",
            < 0.6f => "Focused, mostly predictable",
            < 0.9f => "Balanced creativity",
            < 1.2f => "Creative, varied responses",
            _ => "Highly creative, unpredictable"
        };
    }

    /// <summary>
    /// Gets a description of what the current top-p value means.
    /// </summary>
    public string GetTopPDescription()
    {
        return TopP switch
        {
            < 0.5f => "Very narrow vocabulary",
            < 0.8f => "Focused vocabulary",
            < 0.95f => "Balanced vocabulary",
            _ => "Wide vocabulary range"
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _settingsService.SettingsChanged -= OnSettingsChanged;
        _debounceTimer.Stop();
        _debounceTimer.Dispose();
        _disposed = true;
    }
}
