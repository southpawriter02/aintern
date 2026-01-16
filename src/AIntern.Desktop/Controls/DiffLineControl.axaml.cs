using Avalonia.Controls;

namespace AIntern.Desktop.Controls;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF LINE CONTROL CODE-BEHIND (v0.4.2f)                                  │
// │ Control for rendering a single diff line with gutter and content.        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Control for rendering a single diff line with gutter and content.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2f.</para>
/// <para>
/// This control is purely declarative - all logic is handled via data binding
/// to the DiffLineViewModel. The control supports:
/// - Line number display in the gutter
/// - Conditional background colors based on change type
/// - Inline segment rendering for character-level changes
/// - Placeholder lines for side-by-side alignment
/// </para>
/// </remarks>
public partial class DiffLineControl : UserControl
{
    public DiffLineControl()
    {
        InitializeComponent();
    }
}
