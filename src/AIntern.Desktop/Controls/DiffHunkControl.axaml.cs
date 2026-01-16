using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Controls;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF HUNK CONTROL CODE-BEHIND (v0.4.2f)                                  │
// │ Control for rendering a diff hunk with header and lines.                 │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Control for rendering a diff hunk with header and lines.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2f.</para>
/// <para>
/// This control receives a DiffHunkViewModel and displays either the
/// OriginalLines or ProposedLines collection based on the Side property.
/// The Side property is set by the parent DiffViewerPanel to indicate
/// which side of the diff this control is rendering.
/// </para>
/// </remarks>
public partial class DiffHunkControl : UserControl
{
    private readonly ILogger<DiffHunkControl>? _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // Styled Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Identifies the <see cref="Side"/> styled property.
    /// </summary>
    public static readonly StyledProperty<DiffSide> SideProperty =
        AvaloniaProperty.Register<DiffHunkControl, DiffSide>(nameof(Side));

    /// <summary>
    /// Identifies the <see cref="ShowInlineChanges"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowInlineChangesProperty =
        AvaloniaProperty.Register<DiffHunkControl, bool>(nameof(ShowInlineChanges), true);

    /// <summary>
    /// Gets or sets which side of the diff this control displays.
    /// </summary>
    /// <remarks>
    /// When set to Original, displays the OriginalLines collection.
    /// When set to Proposed, displays the ProposedLines collection.
    /// </remarks>
    public DiffSide Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show character-level inline change highlighting.
    /// </summary>
    public bool ShowInlineChanges
    {
        get => GetValue(ShowInlineChangesProperty);
        set => SetValue(ShowInlineChangesProperty, value);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    public DiffHunkControl()
    {
        InitializeComponent();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Lifecycle Overrides
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handle property changes to update the lines source.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SideProperty)
        {
            _logger?.LogTrace("Side changed to {Side}, updating lines source", Side);
            UpdateLinesSource();
        }
    }

    /// <summary>
    /// Handle data context changes to update the lines source.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _logger?.LogTrace("DataContext changed, updating lines source");
        UpdateLinesSource();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Update the ItemsSource of LinesContainer based on the Side property.
    /// </summary>
    private void UpdateLinesSource()
    {
        if (DataContext is DiffHunkViewModel vm && LinesContainer != null)
        {
            LinesContainer.ItemsSource = Side == DiffSide.Original
                ? vm.OriginalLines
                : vm.ProposedLines;

            _logger?.LogTrace("Set ItemsSource to {Side}Lines ({Count} items)",
                Side, Side == DiffSide.Original ? vm.OriginalLines.Count : vm.ProposedLines.Count);
        }
    }
}
