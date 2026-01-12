using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AIntern.Desktop.Views.Dialogs;

/// <summary>
/// Icon types for MessageDialog.
/// </summary>
public enum MessageDialogIcon
{
    None,
    Information,
    Warning,
    Error,
    Question
}

public partial class MessageDialog : Window
{
    private MessageDialogIcon _dialogIconType;

    public MessageDialog()
    {
        InitializeComponent();
    }

    public string Message
    {
        get => MessageText.Text ?? string.Empty;
        set => MessageText.Text = value;
    }

    public MessageDialogIcon DialogIconType
    {
        get => _dialogIconType;
        set
        {
            _dialogIconType = value;
            UpdateIcon();
        }
    }

    public IEnumerable<string> Buttons
    {
        set => CreateButtons(value);
    }

    private void UpdateIcon()
    {
        var iconData = _dialogIconType switch
        {
            MessageDialogIcon.Information => Application.Current?.Resources.TryGetResource("InfoIcon", null, out var i) == true ? i : null,
            MessageDialogIcon.Warning => Application.Current?.Resources.TryGetResource("WarningIcon", null, out var w) == true ? w : null,
            MessageDialogIcon.Error => Application.Current?.Resources.TryGetResource("ErrorIcon", null, out var e) == true ? e : null,
            MessageDialogIcon.Question => Application.Current?.Resources.TryGetResource("QuestionIcon", null, out var q) == true ? q : null,
            _ => null
        };

        if (iconData is Geometry geometry)
        {
            DialogIcon.Data = geometry;
            DialogIcon.IsVisible = true;
        }
        else
        {
            DialogIcon.IsVisible = false;
        }
    }

    private void CreateButtons(IEnumerable<string> buttonLabels)
    {
        var buttons = buttonLabels.Select((label, index) =>
        {
            var button = new Button
            {
                Content = label,
                MinWidth = 80
            };

            if (index == 0) // First button is primary
            {
                button.Classes.Add("primary");
            }

            button.Click += (s, e) => Close(label);
            return button;
        });

        // Right-align with primary on right
        ButtonsPanel.ItemsSource = buttons.Reverse();
    }
}
