namespace AIntern.Desktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for the ChatContextBar control.
/// Manages attached file contexts and token limit tracking.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4d.</para>
/// </remarks>
public partial class ChatContextBarViewModel : ViewModelBase
{
    #region Constants

    /// <summary>
    /// Default maximum context tokens (8K).
    /// </summary>
    private const int DefaultMaxTokens = 8000;

    /// <summary>
    /// Threshold percentage for near-limit warning (80%).
    /// </summary>
    private const double WarningThreshold = 0.8;

    #endregion

    #region Fields

    private readonly ILogger<ChatContextBarViewModel>? _logger;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Collection of attached file contexts.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FileContextViewModel> _attachedContexts = [];

    /// <summary>
    /// Maximum allowed context tokens.
    /// </summary>
    [ObservableProperty]
    private int _maxContextTokens = DefaultMaxTokens;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Total token count across all attached contexts.
    /// </summary>
    public int TotalContextTokens => AttachedContexts.Sum(c => c.EstimatedTokens);

    /// <summary>
    /// Whether any contexts are attached.
    /// </summary>
    public bool HasAttachedContexts => AttachedContexts.Count > 0;

    /// <summary>
    /// Whether multiple contexts are attached (for Clear All visibility).
    /// </summary>
    public bool HasMultipleContexts => AttachedContexts.Count > 1;

    /// <summary>
    /// Whether the current token count is near the limit (80-100%).
    /// </summary>
    public bool IsNearTokenLimit
    {
        get
        {
            if (MaxContextTokens <= 0) return false;
            var ratio = (double)TotalContextTokens / MaxContextTokens;
            return ratio >= WarningThreshold && ratio < 1.0;
        }
    }

    /// <summary>
    /// Whether the current token count exceeds the limit.
    /// </summary>
    public bool IsOverTokenLimit => TotalContextTokens > MaxContextTokens;

    /// <summary>
    /// Token usage as a percentage (0-100+).
    /// </summary>
    public double TokenUsagePercent => MaxContextTokens > 0
        ? (double)TotalContextTokens / MaxContextTokens * 100
        : 0;

    #endregion

    #region Commands

    /// <summary>
    /// Command to remove a single context.
    /// </summary>
    public ICommand? RemoveContextCommand { get; private set; }

    /// <summary>
    /// Command to clear all contexts.
    /// </summary>
    public ICommand? ClearAllContextsCommand { get; private set; }

    /// <summary>
    /// Command to show context preview (left click).
    /// </summary>
    public ICommand? ShowPreviewCommand { get; private set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="ChatContextBarViewModel"/>.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ChatContextBarViewModel(ILogger<ChatContextBarViewModel>? logger = null)
    {
        _logger = logger;

        // Initialize commands
        RemoveContextCommand = new RelayCommand<FileContextViewModel>(RemoveContext);
        ClearAllContextsCommand = new RelayCommand(ClearAllContexts);
        ShowPreviewCommand = new RelayCommand<FileContextViewModel>(ShowPreview);

        // Subscribe to collection changes to update computed properties
        AttachedContexts.CollectionChanged += (_, _) => NotifyTokenPropertiesChanged();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a context to the attached contexts collection.
    /// </summary>
    /// <param name="context">The context to add.</param>
    public void AddContext(FileContextViewModel context)
    {
        AttachedContexts.Add(context);
        _logger?.LogDebug("[CONTEXT_BAR] Added context: {FileName} (~{Tokens} tokens)",
            context.FileName, context.EstimatedTokens);
    }

    /// <summary>
    /// Removes a context from the attached contexts collection.
    /// </summary>
    /// <param name="context">The context to remove.</param>
    public void RemoveContext(FileContextViewModel? context)
    {
        if (context == null) return;

        AttachedContexts.Remove(context);
        _logger?.LogDebug("[CONTEXT_BAR] Removed context: {FileName}", context.FileName);
    }

    /// <summary>
    /// Clears all attached contexts.
    /// </summary>
    public void ClearAllContexts()
    {
        var count = AttachedContexts.Count;
        AttachedContexts.Clear();
        _logger?.LogDebug("[CONTEXT_BAR] Cleared {Count} contexts", count);
    }

    /// <summary>
    /// Shows preview for a context (placeholder for integration).
    /// </summary>
    /// <param name="context">The context to preview.</param>
    public void ShowPreview(FileContextViewModel? context)
    {
        if (context == null) return;

        // Toggle expanded state for now
        context.IsExpanded = !context.IsExpanded;
        _logger?.LogDebug("[CONTEXT_BAR] Toggled preview for: {FileName}", context.FileName);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Notifies all token-related properties that they may have changed.
    /// </summary>
    private void NotifyTokenPropertiesChanged()
    {
        OnPropertyChanged(nameof(TotalContextTokens));
        OnPropertyChanged(nameof(HasAttachedContexts));
        OnPropertyChanged(nameof(HasMultipleContexts));
        OnPropertyChanged(nameof(IsNearTokenLimit));
        OnPropertyChanged(nameof(IsOverTokenLimit));
        OnPropertyChanged(nameof(TokenUsagePercent));
    }

    #endregion
}
