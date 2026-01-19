// ============================================================================
// File: TerminalStatusBarViewModel.cs
// Path: src/AIntern.Desktop/ViewModels/TerminalStatusBarViewModel.cs
// Description: ViewModel for the terminal status bar section that displays
//              active terminal information in the main window status bar.
// Created: 2026-01-19
// AI Intern v0.5.5h - Status Bar Integration
// ============================================================================

namespace AIntern.Desktop.ViewModels;

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalStatusBarViewModel (v0.5.5h)                                        │
// │ Displays terminal session information in the main window status bar.        │
// │                                                                             │
// │ Features:                                                                   │
// │   • Shows active shell type (bash, zsh, powershell, etc.)                  │
// │   • Displays current working directory with ~ abbreviation                 │
// │   • Shows terminal count badge when multiple terminals open                │
// │   • Visibility tied to terminal panel visibility                           │
// │   • Click to toggle terminal panel                                         │
// └─────────────────────────────────────────────────────────────────────────────┘

#region Type Documentation

/// <summary>
/// ViewModel for the terminal status bar section that displays active terminal
/// information in the main window status bar.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel integrates with <see cref="TerminalPanelViewModel"/> to display:
/// <list type="bullet">
///   <item><description>Active shell name (bash, zsh, powershell, etc.)</description></item>
///   <item><description>Current working directory with home directory abbreviated as ~</description></item>
///   <item><description>Terminal count badge when multiple terminals are open</description></item>
/// </list>
/// </para>
/// <para>
/// The status bar section visibility is tied to the terminal panel visibility,
/// so it appears only when the terminal panel is visible.
/// </para>
/// <para>
/// Path Abbreviation Examples:
/// <code>
/// /Users/ryan/Projects/app    → ~/Projects/app
/// /home/user/code             → ~/code
/// C:\Users\Admin\Documents    → ~\Documents (Windows)
/// /var/log                    → /var/log (no abbreviation)
/// </code>
/// </para>
/// <para>Added in v0.5.5h.</para>
/// </remarks>

#endregion

public partial class TerminalStatusBarViewModel : ViewModelBase, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Private Fields
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logger for diagnostic output.
    /// </summary>
    private readonly ILogger<TerminalStatusBarViewModel> _logger;

    /// <summary>
    /// Reference to the terminal panel ViewModel for state binding.
    /// </summary>
    private TerminalPanelViewModel? _terminalPanelViewModel;

    /// <summary>
    /// Reference to the currently subscribed active session for property change notifications.
    /// </summary>
    private TerminalSessionViewModel? _subscribedActiveSession;

    /// <summary>
    /// Cached home directory path for path abbreviation.
    /// </summary>
    private readonly string _homeDirectory;

    /// <summary>
    /// Flag indicating whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    // ═══════════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets or sets whether the status bar section is visible.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bound to <see cref="TerminalPanelViewModel.IsVisible"/>.
    /// The status bar section should only be visible when the terminal panel is visible.
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Gets or sets whether there is an active terminal session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, the shell name and directory are displayed.
    /// When false, only the terminal icon is shown.
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    [ObservableProperty]
    private bool _hasActiveTerminal;

    /// <summary>
    /// Gets or sets the name of the active shell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Possible values: "bash", "zsh", "fish", "powershell", "cmd", "nushell", "terminal".
    /// Falls back to "Terminal" when no active session.
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    [ObservableProperty]
    private string _activeShellName = "Terminal";

    /// <summary>
    /// Gets or sets the current working directory of the active terminal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Raw path without abbreviation. Use <see cref="CurrentDirectoryDisplay"/>
    /// for UI display with ~ abbreviation.
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    [ObservableProperty]
    private string _currentDirectory = string.Empty;

    /// <summary>
    /// Gets or sets the current working directory with home directory abbreviated as ~.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the display-friendly version of <see cref="CurrentDirectory"/>.
    /// Examples:
    /// <list type="bullet">
    ///   <item><description>/Users/ryan/code → ~/code</description></item>
    ///   <item><description>/var/log → /var/log</description></item>
    /// </list>
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    [ObservableProperty]
    private string _currentDirectoryDisplay = string.Empty;

    /// <summary>
    /// Gets or sets the number of open terminal sessions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used to show a count badge when multiple terminals are open.
    /// The badge is only visible when count > 1.
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    [ObservableProperty]
    private int _terminalCount;

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStatusBarViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// After construction, call <see cref="Initialize"/> with a
    /// <see cref="TerminalPanelViewModel"/> to begin tracking terminal state.
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    public TerminalStatusBarViewModel(ILogger<TerminalStatusBarViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Cache home directory for path abbreviation
        _homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        _logger.LogDebug(
            "[TerminalStatusBarViewModel] Instance created - HomeDirectory: {HomeDir}",
            _homeDirectory);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Initialization
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes the ViewModel by subscribing to the terminal panel's events.
    /// </summary>
    /// <param name="terminalPanelViewModel">
    /// The terminal panel ViewModel to track for state changes.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="terminalPanelViewModel"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method sets up event subscriptions for:
    /// <list type="bullet">
    ///   <item><description>PropertyChanged - for IsVisible, ActiveSession, HasActiveSession</description></item>
    ///   <item><description>Sessions.CollectionChanged - for terminal count updates</description></item>
    ///   <item><description>ActiveSession.PropertyChanged - for directory/state changes</description></item>
    /// </list>
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    public void Initialize(TerminalPanelViewModel terminalPanelViewModel)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[TerminalStatusBarViewModel] Initialize called");

        ArgumentNullException.ThrowIfNull(terminalPanelViewModel);

        // Store reference
        _terminalPanelViewModel = terminalPanelViewModel;

        // Subscribe to property changes on the terminal panel
        _terminalPanelViewModel.PropertyChanged += OnTerminalPanelPropertyChanged;

        // Subscribe to session collection changes
        _terminalPanelViewModel.Sessions.CollectionChanged += OnSessionsCollectionChanged;

        // Set initial values from current state
        UpdateFromTerminalPanel();

        // Subscribe to active session if one exists
        SubscribeToActiveSession(_terminalPanelViewModel.ActiveSession);

        _logger.LogInformation(
            "[TerminalStatusBarViewModel] Initialized - IsVisible: {IsVisible}, " +
            "HasActiveTerminal: {HasActive}, TerminalCount: {Count}",
            IsVisible, HasActiveTerminal, TerminalCount);

        _logger.LogDebug(
            "[TerminalStatusBarViewModel] Initialize completed - {ElapsedMs}ms",
            sw.ElapsedMilliseconds);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the command to toggle the terminal panel visibility.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Delegates to <see cref="TerminalPanelViewModel.TogglePanelCommand"/>.
    /// Bound to the terminal status bar button click.
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    [RelayCommand]
    private void ToggleTerminalPanel()
    {
        _logger.LogDebug("[TerminalStatusBarViewModel] ToggleTerminalPanel called");

        if (_terminalPanelViewModel?.TogglePanelCommand?.CanExecute(null) == true)
        {
            _terminalPanelViewModel.TogglePanelCommand.Execute(null);
            _logger.LogDebug("[TerminalStatusBarViewModel] Terminal panel toggled");
        }
        else
        {
            _logger.LogWarning("[TerminalStatusBarViewModel] Cannot toggle - ViewModel not initialized");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles property changes on the terminal panel ViewModel.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">Property changed event arguments.</param>
    private void OnTerminalPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _logger.LogDebug(
            "[TerminalStatusBarViewModel] TerminalPanel PropertyChanged: {PropertyName}",
            e.PropertyName);

        switch (e.PropertyName)
        {
            case nameof(TerminalPanelViewModel.IsVisible):
                IsVisible = _terminalPanelViewModel?.IsVisible ?? false;
                _logger.LogDebug(
                    "[TerminalStatusBarViewModel] IsVisible updated: {IsVisible}",
                    IsVisible);
                break;

            case nameof(TerminalPanelViewModel.ActiveSession):
                // Resubscribe to the new active session
                SubscribeToActiveSession(_terminalPanelViewModel?.ActiveSession);
                UpdateFromActiveSession();
                break;

            case nameof(TerminalPanelViewModel.HasActiveSession):
                HasActiveTerminal = _terminalPanelViewModel?.HasActiveSession ?? false;
                _logger.LogDebug(
                    "[TerminalStatusBarViewModel] HasActiveTerminal updated: {HasActive}",
                    HasActiveTerminal);
                break;
        }
    }

    /// <summary>
    /// Handles changes to the sessions collection.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">Collection changed event arguments.</param>
    private void OnSessionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var newCount = _terminalPanelViewModel?.Sessions.Count ?? 0;

        _logger.LogDebug(
            "[TerminalStatusBarViewModel] Sessions collection changed: " +
            "Action={Action}, NewCount={Count}",
            e.Action, newCount);

        TerminalCount = newCount;
    }

    /// <summary>
    /// Handles property changes on the active session ViewModel.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">Property changed event arguments.</param>
    private void OnActiveSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _logger.LogDebug(
            "[TerminalStatusBarViewModel] ActiveSession PropertyChanged: {PropertyName}",
            e.PropertyName);

        switch (e.PropertyName)
        {
            case nameof(TerminalSessionViewModel.WorkingDirectory):
                UpdateDirectoryDisplay();
                break;

            case nameof(TerminalSessionViewModel.State):
                // State changes might affect display, refresh
                UpdateFromActiveSession();
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates all properties from the terminal panel ViewModel.
    /// </summary>
    private void UpdateFromTerminalPanel()
    {
        if (_terminalPanelViewModel == null)
        {
            _logger.LogDebug("[TerminalStatusBarViewModel] UpdateFromTerminalPanel - No ViewModel");
            return;
        }

        IsVisible = _terminalPanelViewModel.IsVisible;
        HasActiveTerminal = _terminalPanelViewModel.HasActiveSession;
        TerminalCount = _terminalPanelViewModel.Sessions.Count;

        UpdateFromActiveSession();

        _logger.LogDebug(
            "[TerminalStatusBarViewModel] UpdateFromTerminalPanel - " +
            "IsVisible: {IsVisible}, HasActive: {HasActive}, Count: {Count}",
            IsVisible, HasActiveTerminal, TerminalCount);
    }

    /// <summary>
    /// Updates properties from the active terminal session.
    /// </summary>
    private void UpdateFromActiveSession()
    {
        var activeSession = _terminalPanelViewModel?.ActiveSession;

        if (activeSession == null)
        {
            ActiveShellName = "Terminal";
            CurrentDirectory = string.Empty;
            CurrentDirectoryDisplay = string.Empty;
            HasActiveTerminal = false;

            _logger.LogDebug(
                "[TerminalStatusBarViewModel] UpdateFromActiveSession - No active session");
            return;
        }

        // Capitalize the shell type for display (bash → Bash)
        var shellType = activeSession.ShellType;
        ActiveShellName = string.IsNullOrEmpty(shellType)
            ? "Terminal"
            : char.ToUpperInvariant(shellType[0]) + shellType[1..];

        UpdateDirectoryDisplay();
        HasActiveTerminal = true;

        _logger.LogDebug(
            "[TerminalStatusBarViewModel] UpdateFromActiveSession - " +
            "Shell: {Shell}, Directory: {Dir}",
            ActiveShellName, CurrentDirectoryDisplay);
    }

    /// <summary>
    /// Updates the directory display properties from the active session.
    /// </summary>
    private void UpdateDirectoryDisplay()
    {
        var activeSession = _terminalPanelViewModel?.ActiveSession;
        CurrentDirectory = activeSession?.WorkingDirectory ?? string.Empty;
        CurrentDirectoryDisplay = AbbreviatePath(CurrentDirectory);

        _logger.LogDebug(
            "[TerminalStatusBarViewModel] Directory updated: {Raw} → {Display}",
            CurrentDirectory, CurrentDirectoryDisplay);
    }

    /// <summary>
    /// Subscribes to property changes on the specified active session.
    /// </summary>
    /// <param name="session">The session to subscribe to, or null to unsubscribe only.</param>
    private void SubscribeToActiveSession(TerminalSessionViewModel? session)
    {
        // Unsubscribe from previous session if any
        if (_subscribedActiveSession != null)
        {
            _subscribedActiveSession.PropertyChanged -= OnActiveSessionPropertyChanged;
            _logger.LogDebug(
                "[TerminalStatusBarViewModel] Unsubscribed from previous active session: {Id}",
                _subscribedActiveSession.Id);
        }

        _subscribedActiveSession = session;

        // Subscribe to new session if any
        if (_subscribedActiveSession != null)
        {
            _subscribedActiveSession.PropertyChanged += OnActiveSessionPropertyChanged;
            _logger.LogDebug(
                "[TerminalStatusBarViewModel] Subscribed to active session: {Id}",
                _subscribedActiveSession.Id);
        }
    }

    /// <summary>
    /// Abbreviates a path by replacing the home directory with ~.
    /// </summary>
    /// <param name="path">The path to abbreviate.</param>
    /// <returns>
    /// The path with the home directory replaced by ~, or the original path
    /// if it doesn't start with the home directory.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><description>/Users/ryan/code → ~/code</description></item>
    ///   <item><description>/var/log → /var/log</description></item>
    ///   <item><description>C:\Users\Admin\Documents → ~\Documents</description></item>
    ///   <item><description>null or empty → empty string</description></item>
    /// </list>
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    public string AbbreviatePath(string? path)
    {
        // Handle null or empty
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        // Handle empty home directory (unlikely but defensive)
        if (string.IsNullOrEmpty(_homeDirectory))
        {
            return path;
        }

        // Check if path starts with home directory
        if (path.StartsWith(_homeDirectory, StringComparison.OrdinalIgnoreCase))
        {
            // Replace home directory with ~
            var relativePath = path[_homeDirectory.Length..];

            // Ensure the path separator is preserved
            if (relativePath.Length == 0)
            {
                return "~";
            }

            // Handle case where path is exactly home + separator
            if (relativePath.Length == 1 && (relativePath[0] == '/' || relativePath[0] == '\\'))
            {
                return "~";
            }

            return "~" + relativePath;
        }

        // Path doesn't start with home directory, return as-is
        return path;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IDisposable Implementation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Releases all resources used by this ViewModel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unsubscribes from all event handlers to prevent memory leaks.
    /// Safe to call multiple times.
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by this ViewModel.
    /// </summary>
    /// <param name="disposing">
    /// True if called from <see cref="Dispose"/>, false if called from finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            _logger.LogDebug("[TerminalStatusBarViewModel] Already disposed, skipping");
            return;
        }

        if (disposing)
        {
            _logger.LogDebug("[TerminalStatusBarViewModel] Disposing - unsubscribing from events");

            // Unsubscribe from active session
            SubscribeToActiveSession(null);

            // Unsubscribe from terminal panel
            if (_terminalPanelViewModel != null)
            {
                _terminalPanelViewModel.PropertyChanged -= OnTerminalPanelPropertyChanged;
                _terminalPanelViewModel.Sessions.CollectionChanged -= OnSessionsCollectionChanged;
                _terminalPanelViewModel = null;
            }

            _logger.LogInformation("[TerminalStatusBarViewModel] Disposed successfully");
        }

        _disposed = true;
    }
}
