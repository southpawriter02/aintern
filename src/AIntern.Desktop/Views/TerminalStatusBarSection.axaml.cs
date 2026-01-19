// ============================================================================
// File: TerminalStatusBarSection.axaml.cs
// Path: src/AIntern.Desktop/Views/TerminalStatusBarSection.axaml.cs
// Description: Code-behind for the Terminal Status Bar Section UserControl.
// Created: 2026-01-19
// AI Intern v0.5.5h - Status Bar Integration
// ============================================================================

namespace AIntern.Desktop.Views;

using Avalonia.Controls;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalStatusBarSection Code-Behind (v0.5.5h)                              │
// │ Minimal code-behind for the terminal status bar UserControl.                │
// │                                                                             │
// │ All functionality is implemented in TerminalStatusBarViewModel.             │
// │ This code-behind only handles XAML initialization.                          │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Code-behind for the Terminal Status Bar Section UserControl.
/// </summary>
/// <remarks>
/// <para>
/// This UserControl displays terminal session information in the main window
/// status bar, including:
/// <list type="bullet">
///   <item><description>Terminal icon with toggle functionality</description></item>
///   <item><description>Active shell name (bash, zsh, powershell, etc.)</description></item>
///   <item><description>Current working directory with ~ abbreviation</description></item>
///   <item><description>Terminal count badge when multiple terminals are open</description></item>
/// </list>
/// </para>
/// <para>
/// The control's visibility is bound to the terminal panel visibility via
/// <see cref="ViewModels.TerminalStatusBarViewModel.IsVisible"/>.
/// </para>
/// <para>Added in v0.5.5h.</para>
/// </remarks>
public partial class TerminalStatusBarSection : UserControl
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStatusBarSection"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The DataContext is expected to be set to a
    /// <see cref="ViewModels.TerminalStatusBarViewModel"/> instance by the
    /// parent container (MainWindow).
    /// </para>
    /// <para>Added in v0.5.5h.</para>
    /// </remarks>
    public TerminalStatusBarSection()
    {
        InitializeComponent();
    }
}
