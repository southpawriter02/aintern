using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AIntern.Desktop.Views.Dialogs;

public partial class GoToLineDialog : Window
{
    private int _maxLine = 1;
    private int _currentLine = 1;

    public GoToLineDialog()
    {
        InitializeComponent();
        LineNumberInput.KeyDown += OnInputKeyDown;
    }

    public int MaxLine
    {
        get => _maxLine;
        set
        {
            _maxLine = Math.Max(1, value);
            PromptText.Text = $"Go to line (1 - {_maxLine}):";
        }
    }

    public int CurrentLine
    {
        get => _currentLine;
        set
        {
            _currentLine = value;
            LineNumberInput.Text = value.ToString();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        LineNumberInput.Focus();
        LineNumberInput.SelectAll();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryGoToLine();
            e.Handled = true;
        }
    }

    private void OnGoClick(object? sender, RoutedEventArgs e)
    {
        TryGoToLine();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void TryGoToLine()
    {
        if (int.TryParse(LineNumberInput.Text, out var line))
        {
            if (line >= 1 && line <= _maxLine)
            {
                Close(line);
                return;
            }
        }

        // Invalid input - refocus
        LineNumberInput.Focus();
        LineNumberInput.SelectAll();
    }
}
