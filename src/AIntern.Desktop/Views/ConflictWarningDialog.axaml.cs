using Avalonia.Controls;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CONFLICT WARNING DIALOG (v0.4.3g)                                        │
// │ Displays conflict information and allows user to choose a resolution.  │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Conflict Warning dialog window.
/// Displays conflict information and allows user to choose a resolution.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3g.</para>
/// </remarks>
public partial class ConflictWarningDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the ConflictWarningDialog.
    /// </summary>
    public ConflictWarningDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the Conflict Warning dialog and returns the user's resolution choice.
    /// </summary>
    /// <param name="parent">The parent window.</param>
    /// <param name="conflict">The conflict information.</param>
    /// <param name="filePath">The path to the conflicting file.</param>
    /// <returns>The user's chosen resolution.</returns>
    public static async Task<ConflictResolution> ShowAsync(
        Window parent,
        ConflictInfo conflict,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(conflict);
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path required", nameof(filePath));

        var dialog = new ConflictWarningDialog();
        
        var viewModel = new ConflictWarningDialogViewModel(
            conflict,
            filePath,
            closeAction: result =>
            {
                dialog.Close(result);
            });

        dialog.DataContext = viewModel;

        var result = await dialog.ShowDialog<ConflictResolution?>(parent);
        return result ?? ConflictResolution.Cancel;
    }

    /// <summary>
    /// Shows the Conflict Warning dialog for a specific conflict reason.
    /// </summary>
    /// <param name="parent">The parent window.</param>
    /// <param name="filePath">The path to the conflicting file.</param>
    /// <param name="reason">The conflict reason.</param>
    /// <param name="lastModified">When the file was last modified.</param>
    /// <param name="snapshotTime">When the snapshot was taken.</param>
    /// <returns>The user's chosen resolution.</returns>
    public static Task<ConflictResolution> ShowAsync(
        Window parent,
        string filePath,
        ConflictReason reason,
        DateTime? lastModified = null,
        DateTime? snapshotTime = null)
    {
        var conflict = new ConflictInfo
        {
            HasConflict = true,
            Reason = reason,
            LastModified = lastModified,
            SnapshotTime = snapshotTime
        };

        return ShowAsync(parent, conflict, filePath);
    }
}
