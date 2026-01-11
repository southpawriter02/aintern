using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Interfaces;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel representing a single inference preset in the preset list.
/// </summary>
public sealed partial class InferencePresetViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isDefault;

    [ObservableProperty]
    private bool _isBuiltIn;

    [ObservableProperty]
    private float _temperature;

    [ObservableProperty]
    private float _topP;

    [ObservableProperty]
    private int _maxTokens;

    [ObservableProperty]
    private int _contextSize;

    public InferencePresetViewModel()
    {
    }

    public InferencePresetViewModel(InferencePreset preset)
    {
        Id = preset.Id;
        Name = preset.Name;
        IsDefault = preset.IsDefault;
        IsBuiltIn = preset.IsBuiltIn;
        Temperature = preset.Settings.Temperature;
        TopP = preset.Settings.TopP;
        MaxTokens = preset.Settings.MaxTokens;
        ContextSize = preset.Settings.ContextSize;
    }

    /// <summary>
    /// Gets a display description of the preset settings.
    /// </summary>
    public string Description => $"Temp: {Temperature:F1}, Top-P: {TopP:F2}, Max: {MaxTokens}, Ctx: {ContextSize}";
}
