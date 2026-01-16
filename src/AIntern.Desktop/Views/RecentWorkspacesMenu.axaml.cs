namespace AIntern.Desktop.Views;

using Avalonia.Controls;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Recent workspaces popup menu.
/// </summary>
/// <remarks>Added in v0.3.5e.</remarks>
public partial class RecentWorkspacesMenu : UserControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="RecentWorkspacesMenu"/>.
    /// </summary>
    public RecentWorkspacesMenu()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the ViewModel and loads data.
    /// </summary>
    public async Task InitializeAsync(RecentWorkspacesViewModel viewModel)
    {
        DataContext = viewModel;
        await viewModel.LoadAsync();
    }
}
