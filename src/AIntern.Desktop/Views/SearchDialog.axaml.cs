using Avalonia.Controls;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

public partial class SearchDialog : Window
{
    public SearchDialog()
    {
        InitializeComponent();
    }

    public SearchDialog(SearchViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        this.FindControl<TextBox>("SearchInput")?.Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is SearchViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
            viewModel.Dispose();
        }
    }
}
