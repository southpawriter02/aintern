namespace AIntern.Desktop.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

/// <summary>
/// Converts a language identifier to an icon geometry for display in editor tabs.
/// </summary>
/// <remarks>
/// <para>
/// Maps language identifiers (e.g., "csharp", "javascript") to corresponding
/// StreamGeometry icon resources. Falls back to the default FileIcon for
/// unknown languages.
/// </para>
/// <para>Added in v0.3.3e.</para>
/// </remarks>
public class LanguageToIconConverter : IValueConverter
{
    /// <summary>Singleton instance for resource sharing.</summary>
    public static LanguageToIconConverter Instance { get; } = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var language = value as string;

        // Return icon key based on language
        // Icons should be defined in Icons.axaml
        return language?.ToLowerInvariant() switch
        {
            "csharp" => GetGeometry("CSharpIcon"),
            "javascript" or "javascriptreact" => GetGeometry("JavaScriptIcon"),
            "typescript" or "typescriptreact" => GetGeometry("TypeScriptIcon"),
            "python" => GetGeometry("PythonIcon"),
            "html" => GetGeometry("HtmlIcon"),
            "css" or "scss" or "less" => GetGeometry("CssIcon"),
            "json" or "jsonc" => GetGeometry("JsonIcon"),
            "markdown" => GetGeometry("MarkdownIcon"),
            "xml" => GetGeometry("XmlIcon"),
            "yaml" or "toml" => GetGeometry("ConfigIcon"),
            "rust" => GetGeometry("RustIcon"),
            "go" => GetGeometry("GoIcon"),
            "java" or "kotlin" => GetGeometry("JavaIcon"),
            "shellscript" or "bash" or "powershell" => GetGeometry("TerminalIcon"),
            "dockerfile" => GetGeometry("DockerIcon"),
            _ => GetGeometry("FileIcon") // Default file icon
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("LanguageToIconConverter only supports one-way conversion.");
    }

    /// <summary>
    /// Retrieves a geometry resource by key from application resources.
    /// </summary>
    /// <param name="resourceKey">The resource key to look up.</param>
    /// <returns>The StreamGeometry if found, null otherwise.</returns>
    private static Geometry? GetGeometry(string resourceKey)
    {
        if (Avalonia.Application.Current?.TryGetResource(resourceKey, null, out var resource) == true)
            return resource as Geometry;
        return null;
    }
}
