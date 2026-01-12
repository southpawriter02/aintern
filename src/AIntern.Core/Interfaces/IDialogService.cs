namespace AIntern.Core.Interfaces;

/// <summary>
/// Service for displaying dialogs to the user.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an error dialog to the user.
    /// </summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows an information dialog to the user.
    /// </summary>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with multiple options.
    /// </summary>
    /// <returns>The selected option text, or null if cancelled.</returns>
    Task<string?> ShowConfirmDialogAsync(string title, string message, IEnumerable<string> options);

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveDialogAsync(
        string title,
        string suggestedName,
        IReadOnlyList<(string Name, string[] Extensions)> filters);

    /// <summary>
    /// Shows an open file dialog.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowOpenFileDialogAsync(
        string title,
        IReadOnlyList<(string Name, string[] Extensions)> filters,
        bool allowMultiple = false);

    /// <summary>
    /// Shows a folder picker dialog.
    /// </summary>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    Task<string?> ShowFolderPickerAsync(string title);

    /// <summary>
    /// Shows a go-to-line dialog.
    /// </summary>
    /// <returns>The selected line number, or null if cancelled.</returns>
    Task<int?> ShowGoToLineDialogAsync(int maxLine, int currentLine);
}

