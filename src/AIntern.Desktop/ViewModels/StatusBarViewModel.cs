namespace AIntern.Desktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

/// <summary>
/// ViewModel for the application status bar.
/// </summary>
/// <remarks>
/// <para>Displays workspace, file, cursor, encoding, and model information.</para>
/// <para>Added in v0.3.5d.</para>
/// <para>v0.4.5i: Added pending changes indicator and IStatusBarService integration.</para>
/// </remarks>
public partial class StatusBarViewModel : ViewModelBase
{
    private readonly ILogger<StatusBarViewModel>? _logger;
    private readonly IStatusBarService? _statusBarService;
    private readonly IDispatcher? _dispatcher;

    #region Workspace Segment

    /// <summary>
    /// Current workspace name.
    /// </summary>
    [ObservableProperty]
    private string? _workspaceName;

    /// <summary>
    /// Whether a workspace is open.
    /// </summary>
    [ObservableProperty]
    private bool _hasWorkspace;

    /// <summary>
    /// Tooltip for workspace segment.
    /// </summary>
    public string WorkspaceTooltip => HasWorkspace
        ? $"Workspace: {WorkspaceName}\nClick to open folder"
        : "No workspace open\nClick to open a folder";

    #endregion

    #region File Segment

    /// <summary>
    /// Active file name.
    /// </summary>
    [ObservableProperty]
    private string? _activeFileName;

    /// <summary>
    /// Full path of active file (for tooltip).
    /// </summary>
    [ObservableProperty]
    private string? _activeFilePath;

    /// <summary>
    /// Whether a file is active.
    /// </summary>
    [ObservableProperty]
    private bool _hasActiveFile;

    /// <summary>
    /// Tooltip for file segment.
    /// </summary>
    public string FileTooltip => HasActiveFile
        ? $"Active file: {ActiveFilePath ?? ActiveFileName}\nClick to reveal in explorer"
        : "No file open";

    #endregion

    #region Language Segment

    /// <summary>
    /// Language identifier.
    /// </summary>
    [ObservableProperty]
    private string? _language;

    /// <summary>
    /// Human-readable language name.
    /// </summary>
    [ObservableProperty]
    private string? _languageDisplayName;

    /// <summary>
    /// Tooltip for language segment.
    /// </summary>
    public string LanguageTooltip => $"Language: {LanguageDisplayName ?? "Plain Text"}\nClick to change";

    #endregion

    #region Cursor Segment

    /// <summary>
    /// Current line number.
    /// </summary>
    [ObservableProperty]
    private int _line = 1;

    /// <summary>
    /// Current column number.
    /// </summary>
    [ObservableProperty]
    private int _column = 1;

    /// <summary>
    /// Number of selected characters.
    /// </summary>
    [ObservableProperty]
    private int _selectionLength;

    /// <summary>
    /// Formatted cursor display text.
    /// </summary>
    public string CursorDisplay => SelectionLength > 0
        ? $"Ln {Line}, Col {Column} ({SelectionLength} selected)"
        : $"Ln {Line}, Col {Column}";

    /// <summary>
    /// Tooltip for cursor segment.
    /// </summary>
    public string CursorTooltip => "Click to go to line";

    #endregion

    #region Encoding Segment

    /// <summary>
    /// File encoding.
    /// </summary>
    [ObservableProperty]
    private string _encoding = "UTF-8";

    /// <summary>
    /// Tooltip for encoding segment.
    /// </summary>
    public string EncodingTooltip => $"Encoding: {Encoding}\nClick to change";

    #endregion

    #region Line Ending Segment

    /// <summary>
    /// Line ending type.
    /// </summary>
    [ObservableProperty]
    private string _lineEnding = "LF";

    /// <summary>
    /// Tooltip for line ending segment.
    /// </summary>
    public string LineEndingTooltip => $"Line ending: {LineEnding switch
    {
        "LF" => "Unix (LF)",
        "CRLF" => "Windows (CRLF)",
        "CR" => "Classic Mac (CR)",
        _ => LineEnding
    }}\nClick to change";

    #endregion

    #region Unsaved Indicator

    /// <summary>
    /// Whether there are unsaved changes.
    /// </summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Count of unsaved files.
    /// </summary>
    [ObservableProperty]
    private int _unsavedFilesCount;

    #endregion

    #region Pending Changes (v0.4.5i)

    /// <summary>
    /// Count of pending undoable changes.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PendingChangesDisplay))]
    [NotifyPropertyChangedFor(nameof(HasPendingChanges))]
    [NotifyPropertyChangedFor(nameof(PendingChangesTooltip))]
    private int _pendingChangesCount;

    /// <summary>
    /// Whether there are pending changes.
    /// </summary>
    public bool HasPendingChanges => PendingChangesCount > 0;

    /// <summary>
    /// Formatted pending changes display.
    /// </summary>
    public string PendingChangesDisplay => PendingChangesCount switch
    {
        0 => "",
        1 => "1 change",
        _ => $"{PendingChangesCount} changes"
    };

    /// <summary>
    /// Tooltip for pending changes indicator.
    /// </summary>
    public string PendingChangesTooltip => HasPendingChanges
        ? "Click to view change history"
        : "No pending changes";

    /// <summary>
    /// Model temperature for display.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemperatureDisplay))]
    [NotifyPropertyChangedFor(nameof(TemperatureTooltip))]
    private double _temperature = 0.7;

    /// <summary>
    /// Formatted temperature display.
    /// </summary>
    public string TemperatureDisplay => $"T: {Temperature:F1}";

    /// <summary>
    /// Temperature tooltip.
    /// </summary>
    public string TemperatureTooltip => $"Temperature: {Temperature:F2}\nClick to adjust";

    #endregion

    #region File Watcher Status

    /// <summary>
    /// Whether file watcher is active.
    /// </summary>
    [ObservableProperty]
    private bool _isFileWatcherActive;

    /// <summary>
    /// Tooltip for sync indicator.
    /// </summary>
    public string SyncTooltip => IsFileWatcherActive
        ? "File watcher active"
        : "File watcher inactive";

    #endregion

    #region Model Status

    /// <summary>
    /// Current model name.
    /// </summary>
    [ObservableProperty]
    private string? _modelName;

    /// <summary>
    /// Whether a model is loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isModelLoaded;

    /// <summary>
    /// Whether a model is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isModelLoading;

    /// <summary>
    /// Tooltip for model segment.
    /// </summary>
    public string ModelTooltip => IsModelLoaded
        ? $"Model: {ModelName}"
        : IsModelLoading
            ? "Loading model..."
            : "No model loaded";

    #endregion

    #region Events

    /// <summary>
    /// Raised when open workspace is requested.
    /// </summary>
    public event EventHandler? OpenWorkspaceRequested;

    /// <summary>
    /// Raised when reveal file is requested.
    /// </summary>
    public event EventHandler? RevealFileRequested;

    /// <summary>
    /// Raised when change language is requested.
    /// </summary>
    public event EventHandler? ChangeLanguageRequested;

    /// <summary>
    /// Raised when go to line is requested.
    /// </summary>
    public event EventHandler? GoToLineRequested;

    /// <summary>
    /// Raised when change encoding is requested.
    /// </summary>
    public event EventHandler? ChangeEncodingRequested;

    /// <summary>
    /// Raised when change line ending is requested.
    /// </summary>
    public event EventHandler? ChangeLineEndingRequested;

    /// <summary>
    /// Raised when change history panel should be shown (v0.4.5i).
    /// </summary>
    public event EventHandler? ShowChangeHistoryRequested;

    /// <summary>
    /// Raised when temperature slider should be shown (v0.4.5i).
    /// </summary>
    public event EventHandler? ShowTemperatureSliderRequested;

    #endregion

    /// <summary>
    /// Creates a new StatusBarViewModel.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="statusBarService">Optional status bar service (v0.4.5i).</param>
    /// <param name="dispatcher">Optional dispatcher for UI thread (v0.4.5i).</param>
    public StatusBarViewModel(
        ILogger<StatusBarViewModel>? logger = null,
        IStatusBarService? statusBarService = null,
        IDispatcher? dispatcher = null)
    {
        _logger = logger;
        _statusBarService = statusBarService;
        _dispatcher = dispatcher;

        // v0.4.5i: Subscribe to status bar service events
        if (_statusBarService != null)
        {
            _statusBarService.ItemChanged += OnStatusBarItemChanged;
            _statusBarService.CommandRequested += OnStatusBarCommandRequested;
            InitializeFromStatusBarService();
        }

        _logger?.LogDebug("[INIT] StatusBarViewModel created");
    }

    #region Commands

    /// <summary>
    /// Opens workspace picker.
    /// </summary>
    [RelayCommand]
    private void OpenWorkspace()
    {
        _logger.LogDebug("[CMD] OpenWorkspace");
        OpenWorkspaceRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reveals active file in explorer.
    /// </summary>
    [RelayCommand]
    private void RevealActiveFile()
    {
        if (HasActiveFile)
        {
            _logger.LogDebug("[CMD] RevealActiveFile: {Path}", ActiveFilePath);
            RevealFileRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Opens language picker.
    /// </summary>
    [RelayCommand]
    private void ChangeLanguage()
    {
        _logger.LogDebug("[CMD] ChangeLanguage");
        ChangeLanguageRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens go to line dialog.
    /// </summary>
    [RelayCommand]
    private void GoToLine()
    {
        _logger.LogDebug("[CMD] GoToLine");
        GoToLineRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens encoding picker.
    /// </summary>
    [RelayCommand]
    private void ChangeEncoding()
    {
        _logger.LogDebug("[CMD] ChangeEncoding");
        ChangeEncodingRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens line ending picker.
    /// </summary>
    [RelayCommand]
    private void ChangeLineEnding()
    {
        _logger?.LogDebug("[CMD] ChangeLineEnding");
        ChangeLineEndingRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Updates status bar from editor tab.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <param name="filePath">Full file path.</param>
    /// <param name="language">Language identifier.</param>
    /// <param name="line">Caret line.</param>
    /// <param name="column">Caret column.</param>
    /// <param name="selectionLength">Selection length.</param>
    /// <param name="encoding">File encoding.</param>
    /// <param name="lineEnding">Line ending type.</param>
    public void UpdateFromEditor(
        string? fileName,
        string? filePath,
        string? language,
        int line,
        int column,
        int selectionLength,
        string encoding,
        string lineEnding)
    {
        if (fileName == null)
        {
            HasActiveFile = false;
            ActiveFileName = null;
            ActiveFilePath = null;
            Language = null;
            LanguageDisplayName = null;
            Line = 1;
            Column = 1;
            SelectionLength = 0;
            Encoding = "UTF-8";
            LineEnding = "LF";
            return;
        }

        HasActiveFile = true;
        ActiveFileName = fileName;
        ActiveFilePath = filePath;
        Language = language;
        LanguageDisplayName = GetLanguageDisplayName(language);
        Line = line;
        Column = column;
        SelectionLength = selectionLength;
        Encoding = encoding;
        LineEnding = lineEnding;

        OnPropertyChanged(nameof(CursorDisplay));
        OnPropertyChanged(nameof(FileTooltip));
        OnPropertyChanged(nameof(LanguageTooltip));
        OnPropertyChanged(nameof(EncodingTooltip));
        OnPropertyChanged(nameof(LineEndingTooltip));
    }

    /// <summary>
    /// Updates workspace segment.
    /// </summary>
    /// <param name="workspaceName">Workspace display name.</param>
    public void UpdateWorkspace(string? workspaceName)
    {
        HasWorkspace = workspaceName != null;
        WorkspaceName = workspaceName;
        OnPropertyChanged(nameof(WorkspaceTooltip));
    }

    /// <summary>
    /// Updates unsaved indicator.
    /// </summary>
    /// <param name="unsavedCount">Number of unsaved files.</param>
    public void UpdateUnsavedStatus(int unsavedCount)
    {
        UnsavedFilesCount = unsavedCount;
        HasUnsavedChanges = unsavedCount > 0;
    }

    /// <summary>
    /// Updates model status.
    /// </summary>
    /// <param name="modelName">Model name.</param>
    /// <param name="isLoaded">Whether loaded.</param>
    /// <param name="isLoading">Whether loading.</param>
    public void UpdateModelStatus(string? modelName, bool isLoaded, bool isLoading)
    {
        ModelName = modelName;
        IsModelLoaded = isLoaded;
        IsModelLoading = isLoading;
        OnPropertyChanged(nameof(ModelTooltip));
    }

    /// <summary>
    /// Updates file watcher status.
    /// </summary>
    /// <param name="isActive">Whether watcher is active.</param>
    public void UpdateFileWatcher(bool isActive)
    {
        IsFileWatcherActive = isActive;
        OnPropertyChanged(nameof(SyncTooltip));
    }

    #endregion

    #region Pending Changes Methods (v0.4.5i)

    /// <summary>
    /// Updates pending changes count from undo manager.
    /// </summary>
    public void UpdatePendingChanges(int count)
    {
        PendingChangesCount = count;
    }

    /// <summary>
    /// Updates temperature display.
    /// </summary>
    public void UpdateTemperature(double temperature)
    {
        Temperature = temperature;
    }

    /// <summary>
    /// Initializes from status bar service.
    /// </summary>
    private void InitializeFromStatusBarService()
    {
        if (_statusBarService == null) return;

        var pendingItem = _statusBarService.Items.FirstOrDefault(i => i.Id == "pending-changes");
        if (pendingItem != null)
        {
            PendingChangesCount = pendingItem.BadgeCount;
        }

        var tempItem = _statusBarService.Items.FirstOrDefault(i => i.Id == "temperature");
        if (tempItem != null && double.TryParse(tempItem.Text?.Replace("T: ", ""), out var temp))
        {
            Temperature = temp;
        }
    }

    private void OnStatusBarItemChanged(object? sender, StatusBarItemChangedEventArgs e)
    {
        _dispatcher?.InvokeAsync(() =>
        {
            switch (e.ItemId)
            {
                case "pending-changes":
                    PendingChangesCount = e.NewItem.BadgeCount;
                    break;
                case "temperature":
                    if (double.TryParse(e.NewItem.Text?.Replace("T: ", ""), out var temp))
                    {
                        Temperature = temp;
                    }
                    break;
            }
        });
    }

    private void OnStatusBarCommandRequested(object? sender, string commandId)
    {
        _dispatcher?.InvokeAsync(() =>
        {
            switch (commandId)
            {
                case "ShowChangeHistory":
                    ShowChangeHistoryRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case "ShowTemperatureSlider":
                    ShowTemperatureSliderRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        });
    }

    /// <summary>
    /// Shows the change history panel.
    /// </summary>
    [RelayCommand]
    private void ShowChangeHistory()
    {
        _logger?.LogDebug("[CMD] ShowChangeHistory");
        ShowChangeHistoryRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shows the temperature slider.
    /// </summary>
    [RelayCommand]
    private void ShowTemperatureSlider()
    {
        _logger?.LogDebug("[CMD] ShowTemperatureSlider");
        ShowTemperatureSliderRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Converts language ID to display name.
    /// </summary>
    private static string? GetLanguageDisplayName(string? language)
    {
        return language?.ToLowerInvariant() switch
        {
            "csharp" => "C#",
            "javascript" => "JavaScript",
            "typescript" => "TypeScript",
            "javascriptreact" => "JavaScript React",
            "typescriptreact" => "TypeScript React",
            "python" => "Python",
            "json" => "JSON",
            "xml" => "XML",
            "xaml" => "XAML",
            "html" => "HTML",
            "css" => "CSS",
            "markdown" => "Markdown",
            "yaml" => "YAML",
            "shellscript" => "Shell Script",
            "powershell" => "PowerShell",
            "sql" => "SQL",
            "rust" => "Rust",
            "go" => "Go",
            "java" => "Java",
            "cpp" => "C++",
            "c" => "C",
            null => null,
            _ => language
        };
    }

    #endregion
}
