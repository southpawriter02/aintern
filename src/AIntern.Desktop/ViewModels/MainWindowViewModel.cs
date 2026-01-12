using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Desktop.Views;

namespace AIntern.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ILlmService _llmService;
    private readonly ISettingsService _settingsService;
    private readonly IConversationService _conversationService;
    private readonly ISearchService _searchService;
    private readonly IExportService _exportService;
    private readonly IWorkspaceService _workspaceService;
    private bool _disposed;

    public ChatViewModel ChatViewModel { get; }
    public ModelSelectorViewModel ModelSelectorViewModel { get; }
    public ConversationListViewModel ConversationListViewModel { get; }
    public InferenceSettingsViewModel InferenceSettingsViewModel { get; }
    public FileExplorerViewModel FileExplorer { get; }

    [ObservableProperty]
    private string _statusMessage = "No model loaded";

    [ObservableProperty]
    private string _tokenInfo = string.Empty;

    [ObservableProperty]
    private bool _isModelLoaded;

    [ObservableProperty]
    private bool _hasOpenWorkspace;

    public MainWindowViewModel(
        ChatViewModel chatViewModel,
        ModelSelectorViewModel modelSelectorViewModel,
        ConversationListViewModel conversationListViewModel,
        InferenceSettingsViewModel inferenceSettingsViewModel,
        FileExplorerViewModel fileExplorer,
        ILlmService llmService,
        ISettingsService settingsService,
        IConversationService conversationService,
        ISearchService searchService,
        IExportService exportService,
        IWorkspaceService workspaceService)
    {
        ChatViewModel = chatViewModel;
        ModelSelectorViewModel = modelSelectorViewModel;
        ConversationListViewModel = conversationListViewModel;
        InferenceSettingsViewModel = inferenceSettingsViewModel;
        FileExplorer = fileExplorer;
        _llmService = llmService;
        _settingsService = settingsService;
        _conversationService = conversationService;
        _searchService = searchService;
        _exportService = exportService;
        _workspaceService = workspaceService;

        // Subscribe to service events
        _llmService.ModelStateChanged += OnModelStateChanged;
        _llmService.InferenceProgress += OnInferenceProgress;

        // Subscribe to file explorer events
        FileExplorer.FileOpenRequested += OnFileOpenRequested;
        FileExplorer.FileAttachRequested += OnFileAttachRequested;

        // Subscribe to workspace changes
        _workspaceService.WorkspaceChanged += OnWorkspaceChanged;

        // Initialize HasOpenWorkspace
        HasOpenWorkspace = _workspaceService.CurrentWorkspace != null;

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
        IsModelLoaded = e.IsLoaded;
        StatusMessage = e.IsLoaded
            ? $"Model: {e.ModelName}"
            : "No model loaded";
    }

    private void OnInferenceProgress(object? sender, InferenceProgressEventArgs e)
    {
        TokenInfo = $"Tokens: {e.TokensGenerated} ({e.TokensPerSecond:F1} tok/s)";
    }

    private void OnFileOpenRequested(object? sender, FileOpenRequestedEventArgs e)
    {
        // Forward to editor panel (v0.3.3)
        // EditorPanel?.OpenFileCommand.Execute(e.FilePath);
        System.Diagnostics.Debug.WriteLine($"Open file requested: {e.FilePath}");
    }

    private void OnFileAttachRequested(object? sender, FileAttachRequestedEventArgs e)
    {
        // Forward to chat view model (v0.3.4)
        // ChatViewModel?.AttachFileCommand.Execute(e.FilePath);
        System.Diagnostics.Debug.WriteLine($"Attach file requested: {e.FilePath}");
    }

    private void OnWorkspaceChanged(object? sender, WorkspaceChangedEventArgs e)
    {
        HasOpenWorkspace = e.CurrentWorkspace != null;
    }

    [RelayCommand]
    private async Task OpenSearchAsync()
    {
        var viewModel = new SearchViewModel(_searchService);
        viewModel.NavigateToConversation += async (s, conversationId) =>
        {
            await ConversationListViewModel.SelectConversationByIdAsync(conversationId);
        };

        var dialog = new SearchDialog(viewModel);

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            await dialog.ShowDialog(desktop.MainWindow);
        }
    }

    private bool HasActiveConversation => ConversationListViewModel.SelectedConversation is not null;

    [RelayCommand(CanExecute = nameof(HasActiveConversation))]
    private async Task OpenExportAsync()
    {
        var selectedConversation = ConversationListViewModel.SelectedConversation;
        if (selectedConversation is null) return;

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is not null)
        {
            var viewModel = new ExportViewModel(
                _exportService,
                desktop.MainWindow.StorageProvider,
                selectedConversation.Id);

            var dialog = new ExportDialog(viewModel);
            await dialog.ShowDialog(desktop.MainWindow);
        }
    }

    [RelayCommand]
    private async Task NewConversationAsync()
    {
        await ConversationListViewModel.CreateNewConversationCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void OpenSystemPrompt()
    {
        ChatViewModel.OpenSystemPromptEditorCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(HasActiveConversation))]
    private async Task SaveConversationAsync()
    {
        if (ConversationListViewModel.SelectedConversation is null) return;

        await _conversationService.SaveCurrentConversationAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _llmService.ModelStateChanged -= OnModelStateChanged;
        _llmService.InferenceProgress -= OnInferenceProgress;
        FileExplorer.FileOpenRequested -= OnFileOpenRequested;
        FileExplorer.FileAttachRequested -= OnFileAttachRequested;
        _workspaceService.WorkspaceChanged -= OnWorkspaceChanged;

        ConversationListViewModel.Dispose();
        InferenceSettingsViewModel.Dispose();
        FileExplorer.Dispose();

        if (_conversationService is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}

