using Avalonia.Controls;
using Avalonia.Input;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Controls;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY PROGRESS OVERLAY CODE-BEHIND (v0.4.4g)                            │
// │ Minimal code-behind with Escape key handling for cancellation.          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Code-behind for the ApplyProgressOverlay control.
/// </summary>
/// <remarks>
/// <para>Provides Escape key handling for cancellation.</para>
/// <para>Added in v0.4.4g.</para>
/// </remarks>
public partial class ApplyProgressOverlay : UserControl
{
    /// <summary>
    /// Initialize the overlay control.
    /// </summary>
    public ApplyProgressOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle keyboard input - Escape triggers cancellation if allowed.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && DataContext is ApplyProgressViewModel viewModel)
        {
            if (viewModel.ShowCancelButton && viewModel.CancelCommand.CanExecute(null))
            {
                viewModel.CancelCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
