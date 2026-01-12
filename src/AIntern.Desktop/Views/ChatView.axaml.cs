using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using AIntern.Desktop.Services;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Initialize preview popup with syntax highlighting service (v0.3.4e)
        var syntaxService = App.Services.GetRequiredService<SyntaxHighlightingService>();
        PreviewPopup.Initialize(syntaxService);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (DataContext is ChatViewModel viewModel)
            {
                viewModel.HandleEnterKey();
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Closes the preview when the backdrop is clicked. (v0.3.4e)
    /// </summary>
    private void OnPreviewBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ChatViewModel viewModel)
        {
            viewModel.HidePreviewCommand.Execute(null);
        }
    }
}
