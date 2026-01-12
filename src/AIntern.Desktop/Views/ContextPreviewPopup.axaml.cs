using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using AIntern.Desktop.Services;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// Code-behind for ContextPreviewPopup - displays full context content with syntax highlighting.
/// </summary>
public partial class ContextPreviewPopup : UserControl
{
    private SyntaxHighlightingService? _syntaxService;

    #region Styled Properties

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<ContextPreviewPopup, ICommand?>(nameof(CloseCommand));

    public static readonly StyledProperty<ICommand?> OpenInEditorCommandProperty =
        AvaloniaProperty.Register<ContextPreviewPopup, ICommand?>(nameof(OpenInEditorCommand));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
        AvaloniaProperty.Register<ContextPreviewPopup, ICommand?>(nameof(RemoveCommand));

    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public ICommand? OpenInEditorCommand
    {
        get => GetValue(OpenInEditorCommandProperty);
        set => SetValue(OpenInEditorCommandProperty, value);
    }

    public ICommand? RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    #endregion

    public ICommand CopyContentCommand { get; }

    public ContextPreviewPopup()
    {
        InitializeComponent();

        CopyContentCommand = new RelayCommand(CopyContent);
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Initializes the preview popup with the syntax highlighting service.
    /// </summary>
    public void Initialize(SyntaxHighlightingService syntaxService)
    {
        _syntaxService = syntaxService;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not FileContextViewModel context) return;

        // Set content
        PreviewEditor.Text = context.Content;

        // Apply syntax highlighting
        if (_syntaxService != null && !string.IsNullOrEmpty(context.Language))
        {
            _syntaxService.ApplyHighlighting(PreviewEditor, context.Language);
        }

        // Scroll to start
        PreviewEditor.TextArea.Caret.Offset = 0;

        // If selection, scroll to the start line
        if (context.StartLine.HasValue && context.EndLine.HasValue)
        {
            var lineNumber = Math.Min(context.StartLine.Value, PreviewEditor.Document.LineCount);
            if (lineNumber > 0)
            {
                var line = PreviewEditor.Document.GetLineByNumber(lineNumber);
                PreviewEditor.TextArea.Caret.Offset = line.Offset;
                PreviewEditor.TextArea.Caret.BringCaretToView();
            }
        }

        // Focus the popup for keyboard handling
        Focus();
    }

    private async void CopyContent()
    {
        if (DataContext is not FileContextViewModel context) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(context.Content);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            CloseCommand?.Execute(null);
            e.Handled = true;
        }
    }
}
