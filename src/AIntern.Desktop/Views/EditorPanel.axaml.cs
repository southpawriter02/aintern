namespace AIntern.Desktop.Views;

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Search;
using AIntern.Core.Models;
using AIntern.Desktop.Services;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

/// <summary>
/// Code-behind for the EditorPanel view, handling TextEditor integration
/// and keyboard shortcuts.
/// </summary>
/// <remarks>
/// <para>
/// This class bridges the AvaloniaEdit TextEditor control with the
/// EditorPanelViewModel, managing:
/// </para>
/// <list type="bullet">
///   <item><description>Tab switching and document binding</description></item>
///   <item><description>Syntax highlighting via SyntaxHighlightingService</description></item>
///   <item><description>Editor configuration via EditorConfiguration</description></item>
///   <item><description>Keyboard shortcuts (Ctrl+S, Ctrl+W, Ctrl+Tab, etc.)</description></item>
///   <item><description>Caret position updates for status bar</description></item>
///   <item><description>ViewModel event handling (undo, redo, find, replace, go-to-line)</description></item>
/// </list>
/// <para>Added in v0.3.3e.</para>
/// </remarks>
public partial class EditorPanel : UserControl
{
    #region Fields

    private EditorPanelViewModel? _viewModel;
    private SyntaxHighlightingService? _syntaxService;
    private IDisposable? _settingsBinding;
    private ILogger<EditorPanel>? _logger;
    private SearchPanel? _searchPanel;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the EditorPanel.
    /// </summary>
    public EditorPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the EditorPanel with required services.
    /// </summary>
    /// <param name="syntaxService">Syntax highlighting service.</param>
    /// <param name="settings">Application settings for editor configuration.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public void Initialize(
        SyntaxHighlightingService syntaxService,
        AppSettings settings,
        ILogger<EditorPanel>? logger = null)
    {
        _logger = logger;
        _syntaxService = syntaxService;

        _logger?.LogDebug("[INIT] EditorPanel initializing");

        // Apply editor configuration
        EditorConfiguration.ApplySettings(Editor, settings, _logger);

        // Create settings binding for live updates
        _settingsBinding?.Dispose();
        _settingsBinding = EditorConfiguration.BindToSettings(Editor, settings, _logger);

        // Initialize syntax highlighting (no language yet)
        _syntaxService.ApplyHighlighting(Editor, null);

        // Install search panel
        _searchPanel = SearchPanel.Install(Editor);

        _logger?.LogInformation("[INIT] EditorPanel initialized successfully");
    }

    #endregion

    #region DataContext Handling

    /// <summary>
    /// Handles DataContext changes, subscribing to new ViewModel events.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.UndoRequested -= OnUndoRequested;
            _viewModel.RedoRequested -= OnRedoRequested;
            _viewModel.FindRequested -= OnFindRequested;
            _viewModel.ReplaceRequested -= OnReplaceRequested;
            _viewModel.GoToLineRequested -= OnGoToLineRequested;
            _logger?.LogDebug("[BIND] Unsubscribed from old ViewModel");
        }

        _viewModel = DataContext as EditorPanelViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.UndoRequested += OnUndoRequested;
            _viewModel.RedoRequested += OnRedoRequested;
            _viewModel.FindRequested += OnFindRequested;
            _viewModel.ReplaceRequested += OnReplaceRequested;
            _viewModel.GoToLineRequested += OnGoToLineRequested;

            _logger?.LogDebug("[BIND] Subscribed to new ViewModel");
            UpdateEditorForActiveTab();
        }
    }

    /// <summary>
    /// Handles ViewModel property changes, particularly ActiveTab changes.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorPanelViewModel.ActiveTab))
        {
            _logger?.LogDebug("[TAB] ActiveTab changed, updating editor");
            UpdateEditorForActiveTab();
        }
    }

    #endregion

    #region Editor Updates

    /// <summary>
    /// Updates the TextEditor to display the active tab's content.
    /// </summary>
    private void UpdateEditorForActiveTab()
    {
        var activeTab = _viewModel?.ActiveTab;

        if (activeTab == null)
        {
            Editor.Document = null;
            _logger?.LogDebug("[EDITOR] No active tab, document cleared");
            return;
        }

        // Unsubscribe from previous caret events
        Editor.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
        Editor.TextArea.SelectionChanged -= OnSelectionChanged;

        // Bind document and settings
        Editor.Document = activeTab.Document;
        Editor.IsReadOnly = activeTab.IsReadOnly;

        // Apply syntax highlighting for language
        if (_syntaxService != null)
        {
            _syntaxService.SetLanguage(Editor, activeTab.Language);
        }

        // Subscribe to caret events
        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        Editor.TextArea.SelectionChanged += OnSelectionChanged;

        // Update initial caret position
        UpdateCaretPosition();

        _logger?.LogDebug("[EDITOR] Tab displayed: {FileName}, Language: {Language}",
            activeTab.FileName, activeTab.Language ?? "plain");
    }

    #endregion

    #region Caret Position Tracking

    /// <summary>
    /// Handles caret position changes.
    /// </summary>
    private void OnCaretPositionChanged(object? sender, EventArgs e) => UpdateCaretPosition();

    /// <summary>
    /// Handles selection changes.
    /// </summary>
    private void OnSelectionChanged(object? sender, EventArgs e) => UpdateCaretPosition();

    /// <summary>
    /// Updates the active tab's caret position from the editor.
    /// </summary>
    private void UpdateCaretPosition()
    {
        if (_viewModel?.ActiveTab == null) return;

        var caret = Editor.TextArea.Caret;
        var selection = Editor.TextArea.Selection;
        var selectionLength = selection.IsEmpty ? 0 : Math.Abs(selection.Length);

        _viewModel.ActiveTab.UpdateCaretPosition(caret.Line, caret.Column, selectionLength);
    }

    #endregion

    #region ViewModel Event Handlers

    /// <summary>
    /// Handles undo request from ViewModel.
    /// </summary>
    private void OnUndoRequested(object? sender, EventArgs e)
    {
        _logger?.LogDebug("[CMD] Undo executed");
        Editor.Undo();
    }

    /// <summary>
    /// Handles redo request from ViewModel.
    /// </summary>
    private void OnRedoRequested(object? sender, EventArgs e)
    {
        _logger?.LogDebug("[CMD] Redo executed");
        Editor.Redo();
    }

    /// <summary>
    /// Handles find request from ViewModel.
    /// </summary>
    private void OnFindRequested(object? sender, EventArgs e)
    {
        _logger?.LogDebug("[CMD] Find panel opened");
        _searchPanel ??= SearchPanel.Install(Editor);
        _searchPanel.Open();
    }

    /// <summary>
    /// Handles replace request from ViewModel.
    /// </summary>
    private void OnReplaceRequested(object? sender, EventArgs e)
    {
        _logger?.LogDebug("[CMD] Replace panel opened");
        _searchPanel ??= SearchPanel.Install(Editor);
        _searchPanel.Open();
        _searchPanel.IsReplaceMode = true;
    }

    /// <summary>
    /// Handles go-to-line request from ViewModel.
    /// </summary>
    private void OnGoToLineRequested(object? sender, int lineNumber)
    {
        if (_viewModel?.ActiveTab == null) return;

        _logger?.LogDebug("[CMD] GoToLine: {Line}", lineNumber);

        var offset = _viewModel.ActiveTab.GetOffsetForLine(lineNumber);
        Editor.TextArea.Caret.Offset = offset;
        Editor.TextArea.Caret.BringCaretToView();
        Editor.Focus();
    }

    #endregion

    #region Keyboard Shortcuts

    /// <summary>
    /// Handles keyboard shortcuts for editor operations.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.S:
                    _viewModel?.SaveCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("[KEY] Ctrl+S - Save");
                    break;

                case Key.W:
                    if (_viewModel?.ActiveTab != null)
                        _viewModel.CloseTabCommand.Execute(_viewModel.ActiveTab);
                    e.Handled = true;
                    _logger?.LogDebug("[KEY] Ctrl+W - Close Tab");
                    break;

                case Key.G:
                    _viewModel?.GoToLineCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("[KEY] Ctrl+G - Go To Line");
                    break;

                case Key.F:
                    _viewModel?.FindCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("[KEY] Ctrl+F - Find");
                    break;

                case Key.H:
                    _viewModel?.ReplaceCommand.Execute(null);
                    e.Handled = true;
                    _logger?.LogDebug("[KEY] Ctrl+H - Replace");
                    break;
            }
        }
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Tab)
        {
            _viewModel?.NextTabCommand.Execute(null);
            e.Handled = true;
            _logger?.LogDebug("[KEY] Ctrl+Tab - Next Tab");
        }
        else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.Tab)
        {
            _viewModel?.PreviousTabCommand.Execute(null);
            e.Handled = true;
            _logger?.LogDebug("[KEY] Ctrl+Shift+Tab - Previous Tab");
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Handles view unloading, disposing resources and unsubscribing events.
    /// </summary>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        _logger?.LogDebug("[DISPOSE] EditorPanel unloading");

        _settingsBinding?.Dispose();
        _syntaxService?.RemoveHighlighting(Editor);

        // Unsubscribe from caret events
        Editor.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
        Editor.TextArea.SelectionChanged -= OnSelectionChanged;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.UndoRequested -= OnUndoRequested;
            _viewModel.RedoRequested -= OnRedoRequested;
            _viewModel.FindRequested -= OnFindRequested;
            _viewModel.ReplaceRequested -= OnReplaceRequested;
            _viewModel.GoToLineRequested -= OnGoToLineRequested;
        }

        _logger?.LogInformation("[DISPOSE] EditorPanel cleanup complete");
    }

    #endregion
}
