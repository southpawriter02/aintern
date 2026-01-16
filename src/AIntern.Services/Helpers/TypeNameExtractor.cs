namespace AIntern.Services.Helpers;

using System.Text.RegularExpressions;

/// <summary>
/// Helper class for extracting type/class names from code content (v0.4.1e).
/// </summary>
/// <remarks>
/// <para>
/// Uses compiled regex patterns for efficient type name extraction across
/// multiple programming languages. Patterns are ordered by priority:
/// public types first, then classes before interfaces.
/// </para>
/// </remarks>
internal static partial class TypeNameExtractor
{
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ C# PATTERNS                                                              │
    // └─────────────────────────────────────────────────────────────────────────┘

    [GeneratedRegex(
        @"(?:public|internal|private|protected)?\s*(?:sealed|abstract|static|partial)?\s*(?:class|record|struct)\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex CSharpClassPattern();

    [GeneratedRegex(
        @"(?:public|internal)?\s*interface\s+(I?\w+)",
        RegexOptions.Compiled)]
    private static partial Regex CSharpInterfacePattern();

    [GeneratedRegex(
        @"(?:public|internal)?\s*enum\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex CSharpEnumPattern();

    [GeneratedRegex(
        @"namespace\s+([\w.]+)",
        RegexOptions.Compiled)]
    private static partial Regex CSharpNamespacePattern();

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ TYPESCRIPT/JAVASCRIPT PATTERNS                                           │
    // └─────────────────────────────────────────────────────────────────────────┘

    [GeneratedRegex(
        @"(?:export\s+)?(?:default\s+)?(?:abstract\s+)?class\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex TsJsClassPattern();

    [GeneratedRegex(
        @"(?:export\s+)?interface\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex TsJsInterfacePattern();

    [GeneratedRegex(
        @"(?:export\s+)?(?:async\s+)?function\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex TsJsFunctionPattern();

    [GeneratedRegex(
        @"(?:export\s+)?const\s+(\w+)\s*=",
        RegexOptions.Compiled)]
    private static partial Regex TsJsConstPattern();

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ PYTHON PATTERNS                                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    [GeneratedRegex(
        @"class\s+(\w+)\s*[:\(]",
        RegexOptions.Compiled)]
    private static partial Regex PythonClassPattern();

    [GeneratedRegex(
        @"def\s+(\w+)\s*\(",
        RegexOptions.Compiled)]
    private static partial Regex PythonFunctionPattern();

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ JAVA PATTERNS                                                            │
    // └─────────────────────────────────────────────────────────────────────────┘

    [GeneratedRegex(
        @"(?:public|private|protected)?\s*(?:final|abstract)?\s*class\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex JavaClassPattern();

    [GeneratedRegex(
        @"(?:public)?\s*interface\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex JavaInterfacePattern();

    [GeneratedRegex(
        @"package\s+([\w.]+)",
        RegexOptions.Compiled)]
    private static partial Regex JavaPackagePattern();

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ GO PATTERNS                                                              │
    // └─────────────────────────────────────────────────────────────────────────┘

    [GeneratedRegex(
        @"type\s+(\w+)\s+struct",
        RegexOptions.Compiled)]
    private static partial Regex GoStructPattern();

    [GeneratedRegex(
        @"type\s+(\w+)\s+interface",
        RegexOptions.Compiled)]
    private static partial Regex GoInterfacePattern();

    [GeneratedRegex(
        @"func\s+(\w+)\s*\(",
        RegexOptions.Compiled)]
    private static partial Regex GoFunctionPattern();

    [GeneratedRegex(
        @"package\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex GoPackagePattern();

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ RUST PATTERNS                                                            │
    // └─────────────────────────────────────────────────────────────────────────┘

    [GeneratedRegex(
        @"(?:pub\s+)?struct\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex RustStructPattern();

    [GeneratedRegex(
        @"(?:pub\s+)?enum\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex RustEnumPattern();

    [GeneratedRegex(
        @"(?:pub\s+)?trait\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex RustTraitPattern();

    [GeneratedRegex(
        @"mod\s+(\w+)",
        RegexOptions.Compiled)]
    private static partial Regex RustModPattern();

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ PUBLIC METHODS                                                           │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Extract the primary (first public) type name from code.
    /// </summary>
    /// <param name="content">The code content to analyze.</param>
    /// <param name="language">The programming language.</param>
    /// <returns>The extracted type name, or null if none found.</returns>
    public static string? ExtractPrimaryTypeName(string content, string? language)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrEmpty(language))
            return null;

        var patterns = GetPatterns(language);
        foreach (var pattern in patterns)
        {
            var match = pattern.Match(content);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract all type names declared in the content.
    /// </summary>
    /// <param name="content">The code content to analyze.</param>
    /// <param name="language">The programming language.</param>
    /// <returns>List of all declared type names.</returns>
    public static IReadOnlyList<string> ExtractAllTypeNames(string content, string? language)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrEmpty(language))
            return Array.Empty<string>();

        var results = new HashSet<string>(StringComparer.Ordinal);
        var patterns = GetPatterns(language);

        foreach (var pattern in patterns)
        {
            foreach (Match match in pattern.Matches(content))
            {
                if (match.Success && match.Groups.Count > 1)
                {
                    results.Add(match.Groups[1].Value);
                }
            }
        }

        return results.ToList();
    }

    /// <summary>
    /// Extract namespace/package from code.
    /// </summary>
    /// <param name="content">The code content to analyze.</param>
    /// <param name="language">The programming language.</param>
    /// <returns>The namespace/package name, or null if none found.</returns>
    public static string? ExtractNamespace(string content, string? language)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrEmpty(language))
            return null;

        var pattern = language.ToLowerInvariant() switch
        {
            "csharp" or "cs" => CSharpNamespacePattern(),
            "java" => JavaPackagePattern(),
            "go" or "golang" => GoPackagePattern(),
            _ => null
        };

        if (pattern == null)
            return null;

        var match = pattern.Match(content);
        return match.Success && match.Groups.Count > 1
            ? match.Groups[1].Value
            : null;
    }

    /// <summary>
    /// Build a suggested file path from type name and namespace.
    /// </summary>
    /// <param name="typeName">The extracted type name.</param>
    /// <param name="namespaceOrPackage">The namespace or package (optional).</param>
    /// <param name="extension">The file extension including dot.</param>
    /// <returns>Suggested relative file path.</returns>
    public static string BuildSuggestedPath(
        string typeName,
        string? namespaceOrPackage,
        string extension)
    {
        if (string.IsNullOrEmpty(namespaceOrPackage))
        {
            return $"{typeName}{extension}";
        }

        // Convert namespace to path (e.g., "AIntern.Core.Models" -> "AIntern/Core/Models")
        var pathPrefix = namespaceOrPackage.Replace('.', '/');
        return $"{pathPrefix}/{typeName}{extension}";
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ PRIVATE METHODS                                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Get the extraction patterns for a language, ordered by priority.
    /// </summary>
    private static Regex[] GetPatterns(string? language)
    {
        return language?.ToLowerInvariant() switch
        {
            "csharp" or "cs" =>
            [
                CSharpClassPattern(),
                CSharpInterfacePattern(),
                CSharpEnumPattern()
            ],

            "typescript" or "ts" or "javascript" or "js" =>
            [
                TsJsClassPattern(),
                TsJsInterfacePattern(),
                TsJsFunctionPattern(),
                TsJsConstPattern()
            ],

            "python" or "py" =>
            [
                PythonClassPattern(),
                PythonFunctionPattern()
            ],

            "java" =>
            [
                JavaClassPattern(),
                JavaInterfacePattern()
            ],

            "go" or "golang" =>
            [
                GoStructPattern(),
                GoInterfacePattern(),
                GoFunctionPattern()
            ],

            "rust" or "rs" =>
            [
                RustStructPattern(),
                RustEnumPattern(),
                RustTraitPattern()
            ],

            _ => Array.Empty<Regex>()
        };
    }
}
