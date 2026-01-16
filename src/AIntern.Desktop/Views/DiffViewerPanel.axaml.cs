using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF VIEWER PANEL CODE-BEHIND (v0.4.2e, v0.4.2g)                         │
// │ Implements synchronized scrolling, hunk navigation, and keyboard input.  │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Side-by-side diff viewer panel with synchronized scrolling between panels.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2e, enhanced in v0.4.2g.</para>
/// <para>
/// This code-behind handles:
/// - Bidirectional synchronized scrolling between Original and Proposed panels
/// - Hunk navigation via ViewModel events
/// - Keyboard shortcuts for navigation and actions (v0.4.2g)
/// - Proper cleanup when DataContext changes
/// </para>
/// </remarks>
public partial class DiffViewerPanel : UserControl
{
    private readonly ILogger<DiffViewerPanel>? _logger;

    /// <summary>
    /// Guard flag to prevent infinite scroll synchronization loops.
    /// </summary>
    private bool _isSyncingScroll;

    /// <summary>
    /// Reference to currently subscribed ViewModel for event cleanup.
    /// </summary>
    private DiffViewerViewModel? _subscribedViewModel;

    /// <summary>
    /// Initializes the DiffViewerPanel.
    /// </summary>
    public DiffViewerPanel()
    {
        InitializeComponent();

        // v0.4.2g: Ensure the control can receive keyboard focus
        Focusable = true;

        _logger?.LogDebug("DiffViewerPanel initialized with Focusable=true");
    }

    /// <summary>
    /// Called when the template is applied. Wires up scroll synchronization.
    /// </summary>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Wire up synchronized scrolling event handlers
        if (OriginalScrollViewer != null)
        {
            OriginalScrollViewer.ScrollChanged += OnOriginalScrollChanged;
            _logger?.LogTrace("Subscribed to OriginalScrollViewer.ScrollChanged");
        }

        if (ProposedScrollViewer != null)
        {
            ProposedScrollViewer.ScrollChanged += OnProposedScrollChanged;
            _logger?.LogTrace("Subscribed to ProposedScrollViewer.ScrollChanged");
        }
    }

    /// <summary>
    /// Subscribe to ViewModel events when DataContext changes.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Unsubscribe from previous ViewModel
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.HunkNavigationRequested -= OnHunkNavigationRequested;
            _logger?.LogTrace("Unsubscribed from previous ViewModel");
            _subscribedViewModel = null;
        }

        // Subscribe to new ViewModel
        if (DataContext is DiffViewerViewModel vm)
        {
            vm.HunkNavigationRequested += OnHunkNavigationRequested;
            _subscribedViewModel = vm;
            _logger?.LogTrace("Subscribed to DiffViewerViewModel.HunkNavigationRequested");

            // v0.4.2g: Request focus when ViewModel is set
            Focus();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Keyboard Handler (v0.4.2g)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles keyboard input for navigation and action shortcuts.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.2g.</para>
    /// <para>
    /// Supported shortcuts:
    /// - Ctrl+↑: Previous hunk
    /// - Ctrl+↓: Next hunk
    /// - Ctrl+Enter: Apply changes
    /// - Ctrl+Home: First hunk
    /// - Ctrl+End: Last hunk
    /// - Ctrl+I: Toggle inline changes
    /// - Ctrl+W: Toggle word wrap
    /// - Escape: Close/reject diff
    /// </para>
    /// </remarks>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not DiffViewerViewModel vm)
        {
            return;
        }

        var modifiers = e.KeyModifiers;
        var key = e.Key;

        _logger?.LogTrace("KeyDown: {Key} with modifiers {Modifiers}", key, modifiers);

        // Handle Ctrl+Key combinations
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            switch (key)
            {
                case Key.Up:
                    // Navigate to previous hunk
                    if (vm.PreviousHunkCommand.CanExecute(null))
                    {
                        vm.PreviousHunkCommand.Execute(null);
                        _logger?.LogDebug("Keyboard: Ctrl+Up → Previous hunk");
                    }
                    e.Handled = true;
                    break;

                case Key.Down:
                    // Navigate to next hunk
                    if (vm.NextHunkCommand.CanExecute(null))
                    {
                        vm.NextHunkCommand.Execute(null);
                        _logger?.LogDebug("Keyboard: Ctrl+Down → Next hunk");
                    }
                    e.Handled = true;
                    break;

                case Key.Enter:
                    // Apply the proposed changes
                    if (vm.RequestApplyCommand.CanExecute(null))
                    {
                        vm.RequestApplyCommand.Execute(null);
                        _logger?.LogDebug("Keyboard: Ctrl+Enter → Apply changes");
                    }
                    e.Handled = true;
                    break;

                case Key.I:
                    // Toggle inline change highlighting
                    vm.ShowInlineChanges = !vm.ShowInlineChanges;
                    _logger?.LogDebug("Keyboard: Ctrl+I → Toggle inline changes: {Value}",
                        vm.ShowInlineChanges);
                    e.Handled = true;
                    break;

                case Key.W:
                    // Toggle word wrap
                    vm.WordWrap = !vm.WordWrap;
                    _logger?.LogDebug("Keyboard: Ctrl+W → Toggle word wrap: {Value}",
                        vm.WordWrap);
                    e.Handled = true;
                    break;

                case Key.Home:
                    // Navigate to first hunk
                    if (vm.Hunks.Count > 0 && vm.GoToHunkCommand.CanExecute(0))
                    {
                        vm.GoToHunkCommand.Execute(0);
                        _logger?.LogDebug("Keyboard: Ctrl+Home → First hunk");
                    }
                    e.Handled = true;
                    break;

                case Key.End:
                    // Navigate to last hunk
                    if (vm.Hunks.Count > 0)
                    {
                        var lastIndex = vm.Hunks.Count - 1;
                        if (vm.GoToHunkCommand.CanExecute(lastIndex))
                        {
                            vm.GoToHunkCommand.Execute(lastIndex);
                            _logger?.LogDebug("Keyboard: Ctrl+End → Last hunk");
                        }
                    }
                    e.Handled = true;
                    break;
            }
        }
        // Handle non-modified keys
        else if (modifiers == KeyModifiers.None)
        {
            switch (key)
            {
                case Key.Escape:
                    // Close/reject the diff viewer
                    if (vm.RequestRejectCommand.CanExecute(null))
                    {
                        vm.RequestRejectCommand.Execute(null);
                        _logger?.LogDebug("Keyboard: Escape → Reject/close");
                    }
                    e.Handled = true;
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Synchronized Scrolling
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles scroll changes on the Original panel.
    /// Synchronizes the Proposed panel if sync is enabled.
    /// </summary>
    private void OnOriginalScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Guard against infinite loop
        if (_isSyncingScroll) return;

        // Check if synchronized scrolling is enabled
        if (DataContext is DiffViewerViewModel { SynchronizedScroll: true } vm)
        {
            _isSyncingScroll = true;
            try
            {
                // Copy scroll offset from Original to Proposed
                if (ProposedScrollViewer != null && OriginalScrollViewer != null)
                {
                    ProposedScrollViewer.Offset = OriginalScrollViewer.Offset;
                    _logger?.LogTrace("Synced scroll Original → Proposed: {Offset}",
                        OriginalScrollViewer.Offset);
                }
            }
            finally
            {
                _isSyncingScroll = false;
            }
        }
    }

    /// <summary>
    /// Handles scroll changes on the Proposed panel.
    /// Synchronizes the Original panel if sync is enabled.
    /// </summary>
    private void OnProposedScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Guard against infinite loop
        if (_isSyncingScroll) return;

        // Check if synchronized scrolling is enabled
        if (DataContext is DiffViewerViewModel { SynchronizedScroll: true } vm)
        {
            _isSyncingScroll = true;
            try
            {
                // Copy scroll offset from Proposed to Original
                if (OriginalScrollViewer != null && ProposedScrollViewer != null)
                {
                    OriginalScrollViewer.Offset = ProposedScrollViewer.Offset;
                    _logger?.LogTrace("Synced scroll Proposed → Original: {Offset}",
                        ProposedScrollViewer.Offset);
                }
            }
            finally
            {
                _isSyncingScroll = false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Hunk Navigation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles hunk navigation events from the ViewModel.
    /// Scrolls to bring the specified hunk into view.
    /// </summary>
    private void OnHunkNavigationRequested(object? sender, int hunkIndex)
    {
        _logger?.LogDebug("Hunk navigation requested: index={Index}", hunkIndex);
        ScrollToHunk(hunkIndex);
    }

    /// <summary>
    /// Scrolls to bring the specified hunk into view.
    /// </summary>
    /// <param name="hunkIndex">0-based index of the hunk to scroll to.</param>
    private void ScrollToHunk(int hunkIndex)
    {
        // Get the ItemsControl containing the hunks
        if (OriginalScrollViewer?.Content is ItemsControl itemsControl)
        {
            // Find the container for the specified hunk index
            var container = itemsControl.ContainerFromIndex(hunkIndex);

            if (container is Control hunkControl)
            {
                // Scroll the control into view
                hunkControl.BringIntoView();
                _logger?.LogTrace("Scrolled hunk {Index} into view", hunkIndex);
            }
            else
            {
                _logger?.LogWarning("Could not find container for hunk index {Index}", hunkIndex);
            }
        }
    }
}

