using Avalonia.Controls;
using Avalonia.Input;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Controls;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE ITEM CONTROL (v0.4.4e)                                        │
// │ Code-behind for individual tree item control.                           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Code-behind for the FileTreeItemControl.
/// Handles double-click for preview and expansion toggle.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4e.</para>
/// </remarks>
public partial class FileTreeItemControl : UserControl
{
    private readonly ILogger<FileTreeItemControl>? _logger;

    /// <summary>
    /// Creates a new FileTreeItemControl.
    /// </summary>
    public FileTreeItemControl()
    {
        InitializeComponent();
        DoubleTapped += OnDoubleTapped;
    }

    /// <summary>
    /// Handles double-tap to toggle expansion or trigger preview.
    /// </summary>
    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not FileOperationItemViewModel item)
        {
            return;
        }

        _logger?.LogDebug("Double-tapped on {Path}", item.Path);

        if (item.IsDirectory)
        {
            // Toggle expansion for directories
            item.IsExpanded = !item.IsExpanded;
            _logger?.LogTrace("Toggled expansion for directory {Path}: {Expanded}", item.Path, item.IsExpanded);
        }
        // For files, preview is handled at the panel level
    }
}
