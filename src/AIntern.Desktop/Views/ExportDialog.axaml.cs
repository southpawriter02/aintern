using Avalonia.Controls;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

public partial class ExportDialog : Window
{
    public ExportDialog()
    {
        InitializeComponent();
    }

    public ExportDialog(ExportViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is ExportViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is ExportViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }
    }
}
