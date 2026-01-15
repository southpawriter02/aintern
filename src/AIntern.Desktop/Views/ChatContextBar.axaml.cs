namespace AIntern.Desktop.Views;

using Avalonia.Controls;
using Avalonia.Input;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Code-behind for ChatContextBar control.
/// Handles pointer events for context pills.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4d.</para>
/// </remarks>
public partial class ChatContextBar : UserControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="ChatContextBar"/>.
    /// </summary>
    public ChatContextBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles pointer press events on context pills.
    /// Left click shows preview, middle click removes context.
    /// </summary>
    private void OnContextPillPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border)
        {
            return;
        }

        if (border.DataContext is not FileContextViewModel context)
        {
            return;
        }

        if (DataContext is not ChatContextBarViewModel viewModel)
        {
            return;
        }

        var point = e.GetCurrentPoint(border);

        // Left click: show preview
        if (point.Properties.IsLeftButtonPressed)
        {
            viewModel.ShowPreviewCommand?.Execute(context);
        }
        // Middle click: remove
        else if (point.Properties.IsMiddleButtonPressed)
        {
            viewModel.RemoveContextCommand?.Execute(context);
        }
    }

    /// <summary>
    /// Handles pointer enter events on context pills.
    /// Sets IsHovered to true for visual feedback.
    /// </summary>
    private void OnContextPillEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.DataContext is FileContextViewModel context)
        {
            context.IsHovered = true;
        }
    }

    /// <summary>
    /// Handles pointer leave events on context pills.
    /// Clears IsHovered for visual feedback.
    /// </summary>
    private void OnContextPillExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.DataContext is FileContextViewModel context)
        {
            context.IsHovered = false;
        }
    }
}
