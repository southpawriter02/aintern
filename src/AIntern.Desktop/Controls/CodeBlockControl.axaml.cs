using Avalonia.Controls;

namespace AIntern.Desktop.Controls;

/// <summary>
/// User control for rendering a single code block extracted from LLM output.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.1h.</para>
/// <para>
/// Displays language badge, file path, code content, and action buttons.
/// Supports streaming indicator during content reception and status badges
/// for applied/rejected/conflict states.
/// </para>
/// </remarks>
public partial class CodeBlockControl : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBlockControl"/> class.
    /// </summary>
    public CodeBlockControl()
    {
        InitializeComponent();
    }
}
