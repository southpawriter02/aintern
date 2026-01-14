namespace AIntern.Desktop.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using Microsoft.Extensions.Logging;
using TextMateSharp.Grammars;

/// <summary>
/// Provides syntax highlighting using TextMate grammars.
/// </summary>
/// <remarks>
/// <para>
/// This service integrates TextMate-based syntax highlighting for 46+ programming languages.
/// It provides:
/// </para>
/// <list type="bullet">
///   <item><description>Language-to-scope mapping for TextMate grammars</description></item>
///   <item><description>Theme switching across all registered editors</description></item>
///   <item><description>Installation lifecycle management</description></item>
/// </list>
/// <para>Added in v0.3.3c.</para>
/// </remarks>
public sealed class SyntaxHighlightingService : IDisposable
{
    #region Fields

    private readonly ILogger<SyntaxHighlightingService>? _logger;
    private readonly Dictionary<string, string> _languageToScope;
    private readonly Dictionary<TextEditor, (TextMate.Installation Installation, string? Scope)> _installations = new();
    private RegistryOptions _registryOptions;
    private ThemeName _currentTheme;
    private bool _disposed;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new SyntaxHighlightingService with the specified theme.
    /// </summary>
    /// <param name="useDarkTheme">Whether to use dark theme (default: true).</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public SyntaxHighlightingService(bool useDarkTheme = true, ILogger<SyntaxHighlightingService>? logger = null)
    {
        var sw = Stopwatch.StartNew();
        _logger = logger;

        _currentTheme = useDarkTheme ? ThemeName.DarkPlus : ThemeName.LightPlus;
        _registryOptions = new RegistryOptions(_currentTheme);
        _languageToScope = BuildLanguageScopeMap();

        _logger?.LogDebug("[INIT] SyntaxHighlightingService created in {ElapsedMs}ms - Theme: {Theme}, Languages: {Count}",
            sw.ElapsedMilliseconds, _currentTheme, _languageToScope.Count);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Available themes for syntax highlighting.
    /// </summary>
    public static IReadOnlyList<ThemeName> AvailableThemes { get; } = new[]
    {
        ThemeName.DarkPlus,
        ThemeName.LightPlus,
        ThemeName.Monokai,
        ThemeName.SolarizedDark,
        ThemeName.SolarizedLight,
        ThemeName.HighContrastDark,
        ThemeName.HighContrastLight,
    };

    /// <summary>
    /// Currently active theme.
    /// </summary>
    public ThemeName CurrentTheme => _currentTheme;

    /// <summary>
    /// Gets the TextMate registry options.
    /// </summary>
    public RegistryOptions RegistryOptions => _registryOptions;

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    public IReadOnlyList<string> SupportedLanguages => _languageToScope.Keys.ToList();

    /// <summary>
    /// Number of editors currently registered.
    /// </summary>
    public int RegisteredEditorCount => _installations.Count;

    #endregion

    #region Core Methods

    /// <summary>
    /// Applies syntax highlighting to an editor for the specified language.
    /// </summary>
    /// <param name="editor">The TextEditor to apply highlighting to.</param>
    /// <param name="language">Language identifier (e.g., "csharp", "javascript").</param>
    /// <returns>The TextMate installation for further configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when editor is null.</exception>
    public TextMate.Installation ApplyHighlighting(TextEditor editor, string? language)
    {
        if (editor == null) throw new ArgumentNullException(nameof(editor));

        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ACTION] ApplyHighlighting - Language: {Language}", language ?? "none");

        // Remove existing installation if any
        if (_installations.TryGetValue(editor, out var existing))
        {
            _logger?.LogDebug("[CLEANUP] Removing existing installation");
            existing.Installation.Dispose();
            _installations.Remove(editor);
        }

        // Create new installation
        var installation = editor.InstallTextMate(_registryOptions);
        string? appliedScope = null;

        // Set grammar if language is specified
        if (!string.IsNullOrEmpty(language) && _languageToScope.TryGetValue(language, out var scope))
        {
            try
            {
                installation.SetGrammar(scope);
                appliedScope = scope;
                _logger?.LogDebug("[GRAMMAR] Applied grammar scope: {Scope}", scope);
            }
            catch (Exception ex)
            {
                // Grammar not found, continue without highlighting
                _logger?.LogWarning(ex, "[WARN] Grammar not found for language: {Language}", language);
            }
        }
        else if (!string.IsNullOrEmpty(language))
        {
            _logger?.LogDebug("[INFO] Unknown language, no grammar applied: {Language}", language);
        }

        _installations[editor] = (installation, appliedScope);

        _logger?.LogInformation("[INFO] Highlighting applied in {ElapsedMs}ms - Language: {Language}",
            sw.ElapsedMilliseconds, language ?? "none");

        return installation;
    }

    /// <summary>
    /// Updates the grammar for an existing editor installation.
    /// </summary>
    /// <param name="editor">The TextEditor to update.</param>
    /// <param name="language">Language identifier (null to clear).</param>
    public void SetLanguage(TextEditor editor, string? language)
    {
        if (!_installations.TryGetValue(editor, out var entry))
        {
            _logger?.LogDebug("[INFO] SetLanguage called for unregistered editor");
            return;
        }

        _logger?.LogDebug("[ACTION] SetLanguage - Language: {Language}", language ?? "none");

        if (string.IsNullOrEmpty(language))
        {
            entry.Installation.SetGrammar(null);
            _installations[editor] = (entry.Installation, null);
            return;
        }

        if (_languageToScope.TryGetValue(language, out var scope))
        {
            try
            {
                entry.Installation.SetGrammar(scope);
                _installations[editor] = (entry.Installation, scope);
                _logger?.LogDebug("[GRAMMAR] Changed grammar to: {Scope}", scope);
            }
            catch (Exception ex)
            {
                // Grammar not found
                _logger?.LogWarning(ex, "[WARN] Failed to set grammar for: {Language}", language);
            }
        }
    }

    /// <summary>
    /// Changes the theme for all registered editors.
    /// </summary>
    /// <param name="theme">The new theme to apply.</param>
    public void ChangeTheme(ThemeName theme)
    {
        if (_currentTheme == theme)
        {
            _logger?.LogDebug("[SKIP] Theme already active: {Theme}", theme);
            return;
        }

        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("[ACTION] ChangeTheme - From: {From}, To: {To}", _currentTheme, theme);

        _currentTheme = theme;
        _registryOptions = new RegistryOptions(theme);

        // Re-apply to all editors
        var editorList = _installations.ToArray();
        foreach (var (editor, entry) in editorList)
        {
            var currentScope = entry.Scope;

            entry.Installation.Dispose();
            _installations.Remove(editor);

            var newInstallation = editor.InstallTextMate(_registryOptions);
            if (!string.IsNullOrEmpty(currentScope))
            {
                try
                {
                    newInstallation.SetGrammar(currentScope);
                }
                catch
                {
                    // Grammar may not be available
                }
            }

            _installations[editor] = (newInstallation, currentScope);
        }

        _logger?.LogInformation("[INFO] Theme changed to {Theme} in {ElapsedMs}ms - Updated {Count} editors",
            theme, sw.ElapsedMilliseconds, editorList.Length);
    }

    /// <summary>
    /// Removes syntax highlighting from an editor.
    /// </summary>
    /// <param name="editor">The TextEditor to remove highlighting from.</param>
    public void RemoveHighlighting(TextEditor editor)
    {
        if (_installations.TryGetValue(editor, out var entry))
        {
            _logger?.LogDebug("[ACTION] RemoveHighlighting");
            entry.Installation.Dispose();
            _installations.Remove(editor);
        }
    }

    /// <summary>
    /// Gets the scope name for a language identifier.
    /// </summary>
    /// <param name="language">Language identifier.</param>
    /// <returns>TextMate scope name, or null if not found.</returns>
    public string? GetScopeForLanguage(string language)
    {
        return _languageToScope.TryGetValue(language, out var scope) ? scope : null;
    }

    /// <summary>
    /// Checks if a language is supported.
    /// </summary>
    /// <param name="language">Language identifier to check.</param>
    /// <returns>True if the language is supported.</returns>
    public bool IsLanguageSupported(string language)
    {
        return _languageToScope.ContainsKey(language);
    }

    #endregion

    #region Language Scope Mapping

    private static Dictionary<string, string> BuildLanguageScopeMap()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // .NET Languages
            ["csharp"] = "source.cs",
            ["fsharp"] = "source.fsharp",
            ["vb"] = "source.asp.vb.net",

            // Web Languages
            ["javascript"] = "source.js",
            ["javascriptreact"] = "source.js.jsx",
            ["typescript"] = "source.ts",
            ["typescriptreact"] = "source.tsx",
            ["html"] = "text.html.basic",
            ["css"] = "source.css",
            ["scss"] = "source.css.scss",
            ["less"] = "source.css.less",
            ["vue"] = "source.vue",

            // Systems Languages
            ["c"] = "source.c",
            ["cpp"] = "source.cpp",
            ["rust"] = "source.rust",
            ["go"] = "source.go",
            ["swift"] = "source.swift",
            ["objective-c"] = "source.objc",

            // Scripting Languages
            ["python"] = "source.python",
            ["ruby"] = "source.ruby",
            ["php"] = "source.php",
            ["perl"] = "source.perl",
            ["lua"] = "source.lua",
            ["r"] = "source.r",

            // JVM Languages
            ["java"] = "source.java",
            ["kotlin"] = "source.kotlin",
            ["scala"] = "source.scala",
            ["groovy"] = "source.groovy",

            // Shell/Scripts
            ["shellscript"] = "source.shell",
            ["bash"] = "source.shell",
            ["powershell"] = "source.powershell",
            ["bat"] = "source.batchfile",

            // Data/Config
            ["json"] = "source.json",
            ["jsonc"] = "source.json.comments",
            ["xml"] = "text.xml",
            ["yaml"] = "source.yaml",
            ["toml"] = "source.toml",
            ["ini"] = "source.ini",
            ["properties"] = "source.ini",

            // Markup
            ["markdown"] = "text.html.markdown",
            ["latex"] = "text.tex.latex",
            ["restructuredtext"] = "text.restructuredtext",

            // Database
            ["sql"] = "source.sql",

            // Build/Config
            ["dockerfile"] = "source.dockerfile",
            ["makefile"] = "source.makefile",
            ["cmake"] = "source.cmake",

            // Other
            ["diff"] = "source.diff",
            ["gitignore"] = "source.ignore",
        };
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes all TextMate installations and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger?.LogDebug("[DISPOSE] SyntaxHighlightingService disposing {Count} installations", _installations.Count);

        foreach (var (_, entry) in _installations)
        {
            entry.Installation.Dispose();
        }
        _installations.Clear();
    }

    #endregion
}
