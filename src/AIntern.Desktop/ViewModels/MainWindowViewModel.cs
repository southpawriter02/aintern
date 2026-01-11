using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;

namespace AIntern.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ILlmService _llmService;
    private readonly ISettingsService _settingsService;
    private readonly IConversationService _conversationService;
    private bool _disposed;

    public ChatViewModel ChatViewModel { get; }
    public ModelSelectorViewModel ModelSelectorViewModel { get; }
    public ConversationListViewModel ConversationListViewModel { get; }
    public InferenceSettingsViewModel InferenceSettingsViewModel { get; }

    [ObservableProperty]
    private string _statusMessage = "No model loaded";

    [ObservableProperty]
    private string _tokenInfo = string.Empty;

    public MainWindowViewModel(
        ChatViewModel chatViewModel,
        ModelSelectorViewModel modelSelectorViewModel,
        ConversationListViewModel conversationListViewModel,
        InferenceSettingsViewModel inferenceSettingsViewModel,
        ILlmService llmService,
        ISettingsService settingsService,
        IConversationService conversationService)
    {
        ChatViewModel = chatViewModel;
        ModelSelectorViewModel = modelSelectorViewModel;
        ConversationListViewModel = conversationListViewModel;
        InferenceSettingsViewModel = inferenceSettingsViewModel;
        _llmService = llmService;
        _settingsService = settingsService;
        _conversationService = conversationService;

        // Subscribe to service events
        _llmService.ModelStateChanged += OnModelStateChanged;
        _llmService.InferenceProgress += OnInferenceProgress;

        // Load settings and conversations on startup
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _settingsService.LoadSettingsAsync();
        await ConversationListViewModel.LoadConversationsCommand.ExecuteAsync(null);
    }

    private void OnModelStateChanged(object? sender, ModelStateChangedEventArgs e)
    {
        StatusMessage = e.IsLoaded
            ? $"Model: {e.ModelName}"
            : "No model loaded";
    }

    private void OnInferenceProgress(object? sender, InferenceProgressEventArgs e)
    {
        TokenInfo = $"Tokens: {e.TokensGenerated} ({e.TokensPerSecond:F1} tok/s)";
    }

    public void Dispose()
    {
        if (_disposed) return;

        _llmService.ModelStateChanged -= OnModelStateChanged;
        _llmService.InferenceProgress -= OnInferenceProgress;

        ConversationListViewModel.Dispose();
        InferenceSettingsViewModel.Dispose();

        if (_conversationService is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}
