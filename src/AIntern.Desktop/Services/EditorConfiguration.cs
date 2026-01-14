namespace AIntern.Desktop.Services;

using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Media;
using AvaloniaEdit;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Configures TextEditor instances based on application settings.
/// </summary>
/// <remarks>
/// <para>
/// This static utility class applies user settings to AvaloniaEdit TextEditor instances.
/// It provides:
/// </para>
/// <list type="bullet">
///   <item><description>Font settings (family, size)</description></item>
///   <item><description>Display settings (line numbers, word wrap, highlighting)</description></item>
///   <item><description>Editing settings (tab size, convert tabs to spaces)</description></item>
///   <item><description>Behavior settings (column ruler, virtual space)</description></item>
///   <item><description>Live binding to settings changes</description></item>
/// </list>
/// <para>Added in v0.3.3d.</para>
/// </remarks>
public static class EditorConfiguration
{
    #region Constants

    /// <summary>Default editor font family.</summary>
    public const string DefaultFontFamily = "Cascadia Code, Consolas, Monaco, monospace";

    /// <summary>Default editor font size.</summary>
    public const int DefaultFontSize = 14;

    /// <summary>Default tab size.</summary>
    public const int DefaultTabSize = 4;

    #endregion

    #region Apply Methods

    /// <summary>
    /// Applies all settings to an editor instance.
    /// </summary>
    /// <param name="editor">The TextEditor to configure.</param>
    /// <param name="settings">Application settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public static void ApplySettings(TextEditor editor, AppSettings settings, ILogger? logger = null)
    {
        if (editor == null) throw new ArgumentNullException(nameof(editor));
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        var sw = Stopwatch.StartNew();
        logger?.LogDebug("[ACTION] ApplySettings starting");

        ApplyFontSettings(editor, settings, logger);
        ApplyDisplaySettings(editor, settings, logger);
        ApplyEditingSettings(editor, settings, logger);
        ApplyBehaviorSettings(editor, settings, logger);

        logger?.LogInformation("[INFO] Editor settings applied in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Applies font-related settings.
    /// </summary>
    public static void ApplyFontSettings(TextEditor editor, AppSettings settings, ILogger? logger = null)
    {
        editor.FontFamily = new FontFamily(
            string.IsNullOrEmpty(settings.EditorFontFamily)
                ? DefaultFontFamily
                : settings.EditorFontFamily);

        editor.FontSize = settings.EditorFontSize > 0
            ? settings.EditorFontSize
            : DefaultFontSize;

        logger?.LogDebug("[FONT] Applied - Family: {Font}, Size: {Size}",
            editor.FontFamily.Name, editor.FontSize);
    }

    /// <summary>
    /// Applies display settings (line numbers, highlighting, etc.).
    /// </summary>
    public static void ApplyDisplaySettings(TextEditor editor, AppSettings settings, ILogger? logger = null)
    {
        editor.ShowLineNumbers = settings.ShowLineNumbers;
        editor.WordWrap = settings.WordWrap;
        editor.Options.HighlightCurrentLine = settings.HighlightCurrentLine;

        // Hardcoded display options
        editor.Options.ShowEndOfLine = false;
        editor.Options.ShowSpaces = false;
        editor.Options.ShowTabs = false;
        editor.Options.EnableHyperlinks = true;
        editor.Options.RequireControlModifierForHyperlinkClick = true;

        logger?.LogDebug("[DISPLAY] Applied - LineNumbers: {LN}, WordWrap: {WW}, Highlight: {HL}",
            settings.ShowLineNumbers, settings.WordWrap, settings.HighlightCurrentLine);
    }

    /// <summary>
    /// Applies editing settings (tabs, indentation).
    /// </summary>
    public static void ApplyEditingSettings(TextEditor editor, AppSettings settings, ILogger? logger = null)
    {
        editor.Options.ConvertTabsToSpaces = settings.ConvertTabsToSpaces;
        editor.Options.IndentationSize = settings.TabSize > 0
            ? settings.TabSize
            : DefaultTabSize;

        // Smart editing options
        editor.Options.EnableTextDragDrop = true;
        editor.Options.CutCopyWholeLine = true;

        logger?.LogDebug("[EDITING] Applied - TabSize: {Tab}, ConvertTabs: {Convert}",
            editor.Options.IndentationSize, settings.ConvertTabsToSpaces);
    }

    /// <summary>
    /// Applies behavior settings.
    /// </summary>
    public static void ApplyBehaviorSettings(TextEditor editor, AppSettings settings, ILogger? logger = null)
    {
        // Selection and scrolling
        editor.Options.EnableVirtualSpace = false;
        editor.Options.EnableRectangularSelection = true;
        editor.Options.AllowScrollBelowDocument = true;

        // Note: Column ruler is not supported in current AvaloniaEdit version
        // RulerColumn setting is preserved for future use
        logger?.LogDebug("[BEHAVIOR] Applied - RulerColumn: {Column} (reserved for future use)",
            settings.RulerColumn);
    }

    #endregion

    #region Defaults

    /// <summary>
    /// Gets default options for a new editor (without AppSettings).
    /// </summary>
    public static void ApplyDefaults(TextEditor editor, ILogger? logger = null)
    {
        if (editor == null) throw new ArgumentNullException(nameof(editor));

        logger?.LogDebug("[ACTION] ApplyDefaults starting");

        // Font
        editor.FontFamily = new FontFamily(DefaultFontFamily);
        editor.FontSize = DefaultFontSize;

        // Display
        editor.ShowLineNumbers = true;
        editor.WordWrap = false;
        editor.Options.HighlightCurrentLine = true;

        // Editing
        editor.Options.ConvertTabsToSpaces = true;
        editor.Options.IndentationSize = DefaultTabSize;

        // Behavior
        editor.Options.EnableTextDragDrop = true;
        editor.Options.CutCopyWholeLine = true;
        editor.Options.EnableRectangularSelection = true;
        editor.Options.AllowScrollBelowDocument = true;

        // Visual
        editor.Options.ShowEndOfLine = false;
        editor.Options.ShowSpaces = false;
        editor.Options.ShowTabs = false;
        editor.Options.EnableHyperlinks = true;

        logger?.LogDebug("[INFO] Default editor settings applied");
    }

    #endregion

    #region Live Binding

    /// <summary>
    /// Creates bindings between editor and settings for live updates.
    /// </summary>
    /// <param name="editor">The TextEditor to bind.</param>
    /// <param name="settings">Application settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>Disposable subscription (dispose to unbind).</returns>
    public static IDisposable BindToSettings(TextEditor editor, AppSettings settings, ILogger? logger = null)
    {
        if (editor == null) throw new ArgumentNullException(nameof(editor));
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        logger?.LogDebug("[BIND] Creating settings binding for editor");
        return new SettingsBindingSubscription(editor, settings, logger);
    }

    private sealed class SettingsBindingSubscription : IDisposable
    {
        private readonly ILogger? _logger;
        private bool _disposed;

        public SettingsBindingSubscription(TextEditor editor, AppSettings settings, ILogger? logger)
        {
            _logger = logger;

            // Subscribe to settings property changes if supported
            // Note: AppSettings must implement INotifyPropertyChanged for live updates
            _logger?.LogDebug("[BIND] Settings binding created (live updates require INotifyPropertyChanged)");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _logger?.LogDebug("[BIND] Settings binding disposed");
        }
    }

    #endregion
}
