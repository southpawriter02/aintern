namespace AIntern.Core.Utilities;

/// <summary>
/// Detects programming language from file extensions and names.
/// Language identifiers follow VSCode/TextMate conventions.
/// </summary>
/// <remarks>
/// <para>
/// Used for syntax highlighting hints, token estimation adjustments,
/// and file type detection in the workspace explorer.
/// </para>
/// <para>
/// This class provides:
/// </para>
/// <list type="bullet">
///   <item><description>Detection from extensions, file names, and paths</description></item>
///   <item><description>Human-readable display names</description></item>
///   <item><description>Icon identifiers for UI</description></item>
///   <item><description>Query methods for supported languages</description></item>
/// </list>
/// </remarks>
public static class LanguageDetector
{
    #region Extension Mappings

    /// <summary>
    /// Maps file extensions to their programming language identifiers.
    /// Keys are lowercase with leading dot (e.g., ".cs").
    /// </summary>
    private static readonly Dictionary<string, string> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET / C#
        [".cs"] = "csharp",
        [".csx"] = "csharp",
        [".vb"] = "vb",
        [".fs"] = "fsharp",
        [".fsx"] = "fsharp",
        [".csproj"] = "xml",
        [".fsproj"] = "xml",
        [".vbproj"] = "xml",
        [".sln"] = "text",
        [".props"] = "xml",
        [".targets"] = "xml",
        [".nuspec"] = "xml",

        // Avalonia / XAML
        [".axaml"] = "xml",
        [".xaml"] = "xml",
        [".paml"] = "xml",

        // JavaScript / TypeScript
        [".js"] = "javascript",
        [".mjs"] = "javascript",
        [".cjs"] = "javascript",
        [".jsx"] = "javascriptreact",
        [".ts"] = "typescript",
        [".mts"] = "typescript",
        [".cts"] = "typescript",
        [".tsx"] = "typescriptreact",

        // Python
        [".py"] = "python",
        [".pyi"] = "python",
        [".pyw"] = "python",
        [".pyx"] = "python",

        // Ruby
        [".rb"] = "ruby",
        [".rake"] = "ruby",
        [".gemspec"] = "ruby",
        [".erb"] = "erb",

        // Go
        [".go"] = "go",
        [".mod"] = "go",
        [".sum"] = "text",

        // Rust
        [".rs"] = "rust",

        // Java / Kotlin
        [".java"] = "java",
        [".kt"] = "kotlin",
        [".kts"] = "kotlin",
        [".gradle"] = "groovy",
        [".groovy"] = "groovy",

        // Swift / Objective-C
        [".swift"] = "swift",
        [".m"] = "objective-c",
        [".mm"] = "objective-cpp",

        // C / C++
        [".c"] = "c",
        [".h"] = "c",
        [".cpp"] = "cpp",
        [".cc"] = "cpp",
        [".cxx"] = "cpp",
        [".hpp"] = "cpp",
        [".hxx"] = "cpp",

        // Web
        [".html"] = "html",
        [".htm"] = "html",
        [".xhtml"] = "html",
        [".css"] = "css",
        [".scss"] = "scss",
        [".sass"] = "sass",
        [".less"] = "less",
        [".vue"] = "vue",
        [".svelte"] = "svelte",

        // Data / Config
        [".json"] = "json",
        [".jsonc"] = "jsonc",
        [".json5"] = "json5",
        [".xml"] = "xml",
        [".xsl"] = "xml",
        [".xslt"] = "xml",
        [".yaml"] = "yaml",
        [".yml"] = "yaml",
        [".toml"] = "toml",
        [".ini"] = "ini",
        [".cfg"] = "ini",
        [".conf"] = "ini",
        [".config"] = "xml",
        [".env"] = "properties",
        [".properties"] = "properties",

        // Shell / Scripts
        [".sh"] = "shellscript",
        [".bash"] = "shellscript",
        [".zsh"] = "shellscript",
        [".fish"] = "shellscript",
        [".ps1"] = "powershell",
        [".psm1"] = "powershell",
        [".psd1"] = "powershell",
        [".bat"] = "bat",
        [".cmd"] = "bat",

        // Markup / Documentation
        [".md"] = "markdown",
        [".mdx"] = "mdx",
        [".markdown"] = "markdown",
        [".rst"] = "restructuredtext",
        [".tex"] = "latex",
        [".ltx"] = "latex",
        [".bib"] = "bibtex",
        [".adoc"] = "asciidoc",

        // Database
        [".sql"] = "sql",
        [".pgsql"] = "sql",
        [".mysql"] = "sql",

        // Docker / Containers
        [".dockerfile"] = "dockerfile",
        [".containerfile"] = "dockerfile",

        // Ignore files
        [".gitignore"] = "ignore",
        [".dockerignore"] = "ignore",
        [".npmignore"] = "ignore",
        [".eslintignore"] = "ignore",
        [".gitattributes"] = "properties",
        [".editorconfig"] = "editorconfig",

        // Other languages
        [".r"] = "r",
        [".R"] = "r",
        [".lua"] = "lua",
        [".pl"] = "perl",
        [".pm"] = "perl",
        [".php"] = "php",
        [".scala"] = "scala",
        [".clj"] = "clojure",
        [".cljs"] = "clojure",
        [".erl"] = "erlang",
        [".ex"] = "elixir",
        [".exs"] = "elixir",
        [".hs"] = "haskell",
        [".ml"] = "ocaml",
        [".mli"] = "ocaml",
        [".dart"] = "dart",
        [".graphql"] = "graphql",
        [".gql"] = "graphql",
        [".proto"] = "protobuf",
        [".tf"] = "terraform",
        [".tfvars"] = "terraform",
    };

    #endregion

    #region Special File Name Mappings

    /// <summary>
    /// Maps special file names (exact match) to their languages.
    /// </summary>
    private static readonly Dictionary<string, string> FileNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Docker
        ["Dockerfile"] = "dockerfile",
        ["Containerfile"] = "dockerfile",

        // Build systems
        ["Makefile"] = "makefile",
        ["GNUmakefile"] = "makefile",
        ["makefile"] = "makefile",
        ["CMakeLists.txt"] = "cmake",
        ["Jenkinsfile"] = "groovy",

        // Ruby
        ["Rakefile"] = "ruby",
        ["Gemfile"] = "ruby",
        ["Podfile"] = "ruby",
        ["Vagrantfile"] = "ruby",

        // Config files (with extension in name)
        ["tsconfig.json"] = "jsonc",
        ["jsconfig.json"] = "jsonc",
        ["package.json"] = "json",
        ["package-lock.json"] = "json",
        ["composer.json"] = "json",
        [".prettierrc"] = "json",
        [".eslintrc"] = "json",
        [".babelrc"] = "json",

        // Cargo / Rust
        ["Cargo.toml"] = "toml",
        ["Cargo.lock"] = "toml",

        // Go
        ["go.mod"] = "go",
        ["go.sum"] = "text",

        // Python
        ["requirements.txt"] = "pip-requirements",
        ["setup.py"] = "python",
        ["setup.cfg"] = "ini",
        ["pyproject.toml"] = "toml",
        ["Pipfile"] = "toml",
        ["Pipfile.lock"] = "json",

        // Other
        ["Procfile"] = "text",
        [".gitattributes"] = "properties",
        [".editorconfig"] = "editorconfig",
    };

    #endregion

    #region Detection Methods

    /// <summary>
    /// Detects programming language from a file extension.
    /// </summary>
    /// <param name="extension">File extension including the dot (e.g., ".cs").</param>
    /// <returns>Language identifier, or null if unknown.</returns>
    public static string? DetectByExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        return ExtensionMap.TryGetValue(extension, out var language)
            ? language
            : null;
    }

    /// <summary>
    /// Detects programming language from a file name.
    /// Checks special file names first, then falls back to extension detection.
    /// </summary>
    /// <param name="fileName">File name (with or without path).</param>
    /// <returns>Language identifier, or null if unknown.</returns>
    public static string? DetectByFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        // Extract just the file name if a path was provided
        var name = Path.GetFileName(fileName);

        // Check special file names first
        if (FileNameMap.TryGetValue(name, out var language))
            return language;

        // Fall back to extension detection
        var extension = Path.GetExtension(name);
        return DetectByExtension(extension);
    }

    /// <summary>
    /// Detects programming language from a full file path.
    /// </summary>
    /// <param name="filePath">Absolute or relative file path.</param>
    /// <returns>Language identifier, or null if unknown.</returns>
    public static string? DetectByPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var fileName = Path.GetFileName(filePath);
        return DetectByFileName(fileName);
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets all file extensions supported by the detector.
    /// </summary>
    /// <returns>Collection of supported extensions (with leading dots).</returns>
    public static IReadOnlyCollection<string> GetAllSupportedExtensions()
        => ExtensionMap.Keys;

    /// <summary>
    /// Gets all special file names supported by the detector.
    /// </summary>
    /// <returns>Collection of special file names.</returns>
    public static IReadOnlyCollection<string> GetAllSpecialFileNames()
        => FileNameMap.Keys;

    /// <summary>
    /// Gets all unique language identifiers.
    /// </summary>
    public static IReadOnlyCollection<string> GetAllLanguages()
        => ExtensionMap.Values.Concat(FileNameMap.Values).Distinct().ToList();

    /// <summary>
    /// Gets extensions for a specific language.
    /// </summary>
    /// <param name="language">Language identifier to search for.</param>
    /// <returns>List of extensions that map to this language.</returns>
    public static IReadOnlyList<string> GetExtensionsForLanguage(string language)
        => ExtensionMap
            .Where(kvp => kvp.Value.Equals(language, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .ToList();

    /// <summary>
    /// Checks if a file extension is supported.
    /// </summary>
    /// <param name="extension">File extension to check.</param>
    /// <returns>True if the extension is recognized.</returns>
    public static bool IsExtensionSupported(string? extension)
        => !string.IsNullOrWhiteSpace(extension) && ExtensionMap.ContainsKey(extension);

    /// <summary>
    /// Checks if a file is likely a text/code file (not binary).
    /// </summary>
    /// <param name="fileName">File name to check.</param>
    /// <returns>True if likely a text file.</returns>
    public static bool IsLikelyTextFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Check if we recognize the file
        var language = DetectByFileName(fileName);
        if (language is not null)
            return true;

        // Common text extensions not in our language map
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext is ".txt" or ".log" or ".csv" or ".tsv" or ".diff" or ".patch";
    }

    #endregion

    #region Display Methods

    /// <summary>
    /// Gets a human-readable display name for a language identifier.
    /// </summary>
    /// <param name="language">Language identifier.</param>
    /// <returns>Human-readable name.</returns>
    public static string GetDisplayName(string? language)
    {
        if (string.IsNullOrEmpty(language))
            return "Plain Text";

        return language switch
        {
            "csharp" => "C#",
            "fsharp" => "F#",
            "vb" => "Visual Basic",
            "javascript" => "JavaScript",
            "javascriptreact" => "JavaScript (React)",
            "typescript" => "TypeScript",
            "typescriptreact" => "TypeScript (React)",
            "cpp" => "C++",
            "c" => "C",
            "objective-c" => "Objective-C",
            "objective-cpp" => "Objective-C++",
            "shellscript" => "Shell Script",
            "powershell" => "PowerShell",
            "bat" => "Batch File",
            "restructuredtext" => "reStructuredText",
            "jsonc" => "JSON with Comments",
            "json5" => "JSON5",
            "pip-requirements" => "Python Requirements",
            "xml" => "XML",
            "html" => "HTML",
            "css" => "CSS",
            "scss" => "SCSS",
            "sass" => "Sass",
            "less" => "Less",
            "yaml" => "YAML",
            "toml" => "TOML",
            "ini" => "INI",
            "sql" => "SQL",
            "dockerfile" => "Dockerfile",
            "makefile" => "Makefile",
            "cmake" => "CMake",
            "markdown" => "Markdown",
            "mdx" => "MDX",
            "latex" => "LaTeX",
            "bibtex" => "BibTeX",
            "graphql" => "GraphQL",
            "protobuf" => "Protocol Buffers",
            "terraform" => "Terraform",
            "ignore" => "Ignore File",
            "properties" => "Properties",
            "editorconfig" => "EditorConfig",
            "asciidoc" => "AsciiDoc",
            _ => char.ToUpper(language[0]) + language[1..] // Capitalize first letter
        };
    }

    /// <summary>
    /// Gets the icon name/key for a language (for use with icon fonts/resources).
    /// </summary>
    /// <param name="language">Language identifier.</param>
    /// <returns>Icon identifier for UI frameworks.</returns>
    public static string GetIconName(string? language)
    {
        return language switch
        {
            "csharp" or "fsharp" or "vb" => "dotnet",
            "javascript" or "javascriptreact" => "javascript",
            "typescript" or "typescriptreact" => "typescript",
            "python" or "pip-requirements" => "python",
            "ruby" or "erb" => "ruby",
            "go" => "go",
            "rust" => "rust",
            "java" or "kotlin" or "groovy" => "java",
            "swift" or "objective-c" or "objective-cpp" => "apple",
            "c" or "cpp" => "c",
            "html" or "xml" => "html",
            "css" or "scss" or "sass" or "less" => "css",
            "vue" => "vue",
            "svelte" => "svelte",
            "json" or "jsonc" or "json5" => "json",
            "yaml" or "toml" or "ini" or "editorconfig" or "properties" => "config",
            "markdown" or "mdx" or "restructuredtext" or "asciidoc" => "markdown",
            "dockerfile" => "docker",
            "shellscript" or "bat" => "terminal",
            "powershell" => "powershell",
            "sql" => "database",
            "ignore" or "git" => "git",
            "terraform" => "terraform",
            "graphql" => "graphql",
            _ => "file"
        };
    }

    #endregion
}
