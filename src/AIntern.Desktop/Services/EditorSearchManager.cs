using AvaloniaEdit;
using AvaloniaEdit.Search;

namespace AIntern.Desktop.Services;

/// <summary>
/// Manages the search panel for TextEditor instances.
/// </summary>
public static class EditorSearchManager
{
    private static readonly Dictionary<TextEditor, SearchPanel> _searchPanels = new();

    /// <summary>
    /// Opens the search panel for find operations.
    /// </summary>
    /// <param name="editor">The TextEditor to search in.</param>
    /// <returns>The SearchPanel instance.</returns>
    public static SearchPanel OpenFind(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = GetOrCreatePanel(editor);
        panel.Open();

        // If there's a selection, use it as the search text
        if (!editor.TextArea.Selection.IsEmpty)
        {
            var selectedText = editor.TextArea.Selection.GetText();
            // Only use selection if it's a single line
            if (!string.IsNullOrEmpty(selectedText) && !selectedText.Contains('\n'))
            {
                panel.SearchPattern = selectedText;
            }
        }

        return panel;
    }

    /// <summary>
    /// Opens the search panel with replace visible.
    /// </summary>
    /// <param name="editor">The TextEditor to search in.</param>
    /// <returns>The SearchPanel instance in replace mode.</returns>
    public static SearchPanel OpenReplace(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = OpenFind(editor);
        panel.IsReplaceMode = true;
        return panel;
    }

    /// <summary>
    /// Finds the next occurrence.
    /// </summary>
    /// <param name="editor">The TextEditor to search in.</param>
    public static void FindNext(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = GetOrCreatePanel(editor);
        panel.FindNext();
    }

    /// <summary>
    /// Finds the previous occurrence.
    /// </summary>
    /// <param name="editor">The TextEditor to search in.</param>
    public static void FindPrevious(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = GetOrCreatePanel(editor);
        panel.FindPrevious();
    }

    /// <summary>
    /// Closes the search panel.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    public static void Close(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_searchPanels.TryGetValue(editor, out var panel))
        {
            panel.Close();
        }
    }

    /// <summary>
    /// Checks if search panel is open for the editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <returns>True if search panel is open.</returns>
    public static bool IsOpen(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_searchPanels.TryGetValue(editor, out var panel))
        {
            return panel.IsClosed == false;
        }
        return false;
    }

    /// <summary>
    /// Gets the current search pattern for the editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <returns>The current search pattern, or null.</returns>
    public static string? GetSearchPattern(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_searchPanels.TryGetValue(editor, out var panel))
        {
            return panel.SearchPattern;
        }
        return null;
    }

    /// <summary>
    /// Sets the search pattern for the editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <param name="pattern">The search pattern to set.</param>
    public static void SetSearchPattern(TextEditor editor, string pattern)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = GetOrCreatePanel(editor);
        panel.SearchPattern = pattern;
    }

    /// <summary>
    /// Removes the search panel from an editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    public static void Uninstall(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_searchPanels.TryGetValue(editor, out var panel))
        {
            panel.Uninstall();
            _searchPanels.Remove(editor);
        }
    }

    /// <summary>
    /// Checks if a panel exists for the editor.
    /// </summary>
    internal static bool HasPanel(TextEditor editor)
    {
        return _searchPanels.ContainsKey(editor);
    }

    /// <summary>
    /// Clears all panels (for testing).
    /// </summary>
    internal static void ClearAll()
    {
        foreach (var panel in _searchPanels.Values)
        {
            panel.Uninstall();
        }
        _searchPanels.Clear();
    }

    private static SearchPanel GetOrCreatePanel(TextEditor editor)
    {
        if (!_searchPanels.TryGetValue(editor, out var panel))
        {
            panel = SearchPanel.Install(editor);
            _searchPanels[editor] = panel;
        }
        return panel;
    }
}
