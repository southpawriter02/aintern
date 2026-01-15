namespace AIntern.Desktop.Services;

using System;
using System.Collections.Generic;
using AvaloniaEdit;
using AvaloniaEdit.Search;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manages search panels for TextEditor instances, providing a centralized
/// API for find and replace operations.
/// </summary>
/// <remarks>
/// <para>
/// This static utility class integrates AvaloniaEdit's built-in <see cref="SearchPanel"/>
/// for find and replace functionality. It manages search panel lifecycle and provides:
/// </para>
/// <list type="bullet">
///   <item><description>Find mode with pattern matching</description></item>
///   <item><description>Replace mode with single/all replacement</description></item>
///   <item><description>Selection-based search text initialization</description></item>
///   <item><description>Find Next/Previous navigation</description></item>
///   <item><description>Match case, whole word, and regex options</description></item>
/// </list>
/// <para>Added in v0.3.3f.</para>
/// </remarks>
public static class EditorSearchManager
{
    #region Fields

    /// <summary>
    /// Maps TextEditor instances to their SearchPanel instances.
    /// </summary>
    private static readonly Dictionary<TextEditor, SearchPanel> _searchPanels = new();

    /// <summary>
    /// Optional logger for diagnostics.
    /// </summary>
    private static ILogger? _logger;

    #endregion

    #region Configuration

    /// <summary>
    /// Sets the logger for diagnostics.
    /// </summary>
    /// <param name="logger">Logger instance to use.</param>
    public static void SetLogger(ILogger? logger)
    {
        _logger = logger;
    }

    #endregion

    #region Open/Close Methods

    /// <summary>
    /// Opens the search panel for find operations.
    /// </summary>
    /// <param name="editor">The TextEditor to search in.</param>
    /// <returns>The SearchPanel instance.</returns>
    /// <remarks>
    /// <para>
    /// If text is currently selected and is a single line, it will be
    /// automatically used as the search pattern.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static SearchPanel OpenFind(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _logger?.LogDebug("[SEARCH] Opening find panel");

        var panel = GetOrCreatePanel(editor);
        panel.Open();

        // If there's a single-line selection, use it as the search text
        if (!editor.TextArea.Selection.IsEmpty)
        {
            var selectedText = editor.TextArea.Selection.GetText();
            if (!string.IsNullOrEmpty(selectedText) && !selectedText.Contains('\n'))
            {
                panel.SearchPattern = selectedText;
                _logger?.LogDebug("[SEARCH] Auto-filled search pattern from selection: '{Pattern}'",
                    selectedText.Length > 50 ? selectedText[..50] + "..." : selectedText);
            }
        }

        return panel;
    }

    /// <summary>
    /// Opens the search panel with replace visible.
    /// </summary>
    /// <param name="editor">The TextEditor to search in.</param>
    /// <returns>The SearchPanel instance in replace mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static SearchPanel OpenReplace(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _logger?.LogDebug("[SEARCH] Opening replace panel");

        var panel = OpenFind(editor);
        panel.IsReplaceMode = true;
        return panel;
    }

    /// <summary>
    /// Closes the search panel.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static void Close(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_searchPanels.TryGetValue(editor, out var panel))
        {
            panel.Close();
            _logger?.LogDebug("[SEARCH] Panel closed");
        }
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Finds the next occurrence.
    /// </summary>
    /// <param name="editor">The TextEditor to search in.</param>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static void FindNext(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = GetOrCreatePanel(editor);
        panel.FindNext();
        _logger?.LogDebug("[SEARCH] FindNext executed");
    }

    /// <summary>
    /// Finds the previous occurrence.
    /// </summary>
    /// <param name="editor">The TextEditor to search in.</param>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static void FindPrevious(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = GetOrCreatePanel(editor);
        panel.FindPrevious();
        _logger?.LogDebug("[SEARCH] FindPrevious executed");
    }

    #endregion

    #region State Methods

    /// <summary>
    /// Checks if search panel is installed for the editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <returns>True if search panel exists for this editor.</returns>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static bool IsInstalled(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        return _searchPanels.ContainsKey(editor);
    }

    /// <summary>
    /// Checks if search panel is open for the editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <returns>True if search panel is open.</returns>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static bool IsOpen(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_searchPanels.TryGetValue(editor, out var panel))
        {
            return !panel.IsClosed;
        }
        return false;
    }

    /// <summary>
    /// Gets the current search pattern for the editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <returns>The current search pattern, or null if no panel exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static void SetSearchPattern(TextEditor editor, string pattern)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = GetOrCreatePanel(editor);
        panel.SearchPattern = pattern ?? string.Empty;
        _logger?.LogDebug("[SEARCH] Pattern set: '{Pattern}'",
            pattern?.Length > 50 ? pattern[..50] + "..." : pattern ?? "(empty)");
    }

    /// <summary>
    /// Gets whether replace mode is active.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <returns>True if in replace mode, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static bool IsReplaceMode(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_searchPanels.TryGetValue(editor, out var panel))
        {
            return panel.IsReplaceMode;
        }
        return false;
    }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Removes the search panel from an editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <remarks>
    /// <para>
    /// Call this when the editor is being disposed to clean up resources.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if editor is null.</exception>
    public static void Uninstall(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_searchPanels.TryGetValue(editor, out var panel))
        {
            panel.Uninstall();
            _searchPanels.Remove(editor);
            _logger?.LogDebug("[SEARCH] Panel uninstalled and removed");
        }
    }

    /// <summary>
    /// Gets the count of currently installed search panels.
    /// </summary>
    /// <returns>Number of installed panels.</returns>
    /// <remarks>
    /// Primarily used for testing and diagnostics.
    /// </remarks>
    public static int GetInstalledPanelCount()
    {
        return _searchPanels.Count;
    }

    /// <summary>
    /// Clears all installed panels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method should only be used for cleanup during application shutdown
    /// or for testing purposes. It uninstalls all panels without disposing
    /// the associated editors.
    /// </para>
    /// </remarks>
    public static void ClearAll()
    {
        _logger?.LogDebug("[SEARCH] Clearing {Count} panels", _searchPanels.Count);

        foreach (var kvp in _searchPanels)
        {
            try
            {
                kvp.Value.Uninstall();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[SEARCH] Error uninstalling panel during ClearAll");
            }
        }
        _searchPanels.Clear();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets an existing SearchPanel or creates a new one for the editor.
    /// </summary>
    /// <param name="editor">The TextEditor.</param>
    /// <returns>The SearchPanel instance.</returns>
    private static SearchPanel GetOrCreatePanel(TextEditor editor)
    {
        if (!_searchPanels.TryGetValue(editor, out var panel))
        {
            panel = SearchPanel.Install(editor);
            _searchPanels[editor] = panel;
            _logger?.LogDebug("[SEARCH] SearchPanel installed for editor");
        }
        return panel;
    }

    #endregion
}
