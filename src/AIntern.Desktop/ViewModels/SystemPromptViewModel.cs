using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for a system prompt in lists.
/// </summary>
public partial class SystemPromptViewModel : ViewModelBase
{
    public Guid Id { get; init; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _category = "Custom";

    [ObservableProperty]
    private bool _isBuiltIn;

    [ObservableProperty]
    private bool _isDefault;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _usageCount;

    public int CharacterCount => Content?.Length ?? 0;

    public int EstimatedTokenCount => CharacterCount / 4;

    public string ContentPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content))
                return string.Empty;

            var clean = Content.Replace("\n", " ").Replace("\r", " ").Trim();
            return clean.Length > 100 ? clean[..100] + "..." : clean;
        }
    }

    public string TypeLabel => IsBuiltIn ? "Template" : "Custom";

    public string CategoryIcon => Category switch
    {
        "Code" => "CodeIcon",
        "Creative" => "PaletteIcon",
        "Technical" => "DocumentIcon",
        "General" => "ChatIcon",
        _ => "PromptIcon"
    };

    public SystemPromptViewModel()
    {
    }

    public SystemPromptViewModel(SystemPrompt prompt)
    {
        Id = prompt.Id;
        Name = prompt.Name;
        Content = prompt.Content;
        Description = prompt.Description;
        Category = prompt.Category;
        IsBuiltIn = prompt.IsBuiltIn;
        IsDefault = prompt.IsDefault;
        UsageCount = prompt.UsageCount;
    }

    partial void OnContentChanged(string value)
    {
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(EstimatedTokenCount));
        OnPropertyChanged(nameof(ContentPreview));
    }
}
