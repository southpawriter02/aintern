namespace AIntern.Core.Utilities;

/// <summary>
/// Detects programming language based on file extension or name.
/// Language identifiers follow VSCode/TextMate conventions.
/// </summary>
public static class LanguageDetector
{
    /// <summary>
    /// Maps file extensions to language identifiers.
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

    /// <summary>
    /// Maps special file names (without extension) to language identifiers.
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
        [".editorconfig"] = "ini",
    };

    /// <summary>
    /// Detects the programming language based on file extension.
    /// </summary>
    /// <param name="extension">File extension including dot (e.g., ".cs").</param>
    /// <returns>Language identifier or null if not recognized.</returns>
    public static string? DetectByExtension(string? extension)
    {
        if (string.IsNullOrEmpty(extension))
            return null;

        return ExtensionMap.TryGetValue(extension, out var language)
            ? language
            : null;
    }

    /// <summary>
    /// Detects the programming language based on file name.
    /// Checks special file names first, then falls back to extension.
    /// </summary>
    /// <param name="fileName">File name (e.g., "Program.cs" or "Dockerfile").</param>
    /// <returns>Language identifier or null if not recognized.</returns>
    public static string? DetectByFileName(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        // Check special file names first
        if (FileNameMap.TryGetValue(fileName, out var language))
            return language;

        // Fall back to extension
        var extension = Path.GetExtension(fileName);
        return DetectByExtension(extension);
    }

    /// <summary>
    /// Detects the programming language from a full file path.
    /// </summary>
    /// <param name="filePath">Absolute or relative file path.</param>
    /// <returns>Language identifier or null if not recognized.</returns>
    public static string? DetectByPath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var fileName = Path.GetFileName(filePath);
        return DetectByFileName(fileName);
    }

    /// <summary>
    /// Gets all supported file extensions.
    /// </summary>
    public static IReadOnlyCollection<string> GetAllSupportedExtensions()
        => ExtensionMap.Keys;

    /// <summary>
    /// Gets all special file names that have language mappings.
    /// </summary>
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
    public static IReadOnlyList<string> GetExtensionsForLanguage(string language)
        => ExtensionMap
            .Where(kvp => kvp.Value.Equals(language, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .ToList();

    /// <summary>
    /// Checks if a file extension is supported.
    /// </summary>
    public static bool IsExtensionSupported(string? extension)
        => !string.IsNullOrEmpty(extension) && ExtensionMap.ContainsKey(extension);

    /// <summary>
    /// Checks if a file is likely a text/code file (not binary).
    /// </summary>
    public static bool IsLikelyTextFile(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        // Check if we recognize the file
        var language = DetectByFileName(fileName);
        if (language is not null)
            return true;

        // Common text extensions not in our language map
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext is ".txt" or ".log" or ".csv" or ".tsv" or ".diff" or ".patch";
    }

    /// <summary>
    /// Gets a human-readable display name for a language identifier.
    /// </summary>
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
            _ => char.ToUpper(language[0]) + language[1..] // Capitalize first letter
        };
    }

    /// <summary>
    /// Gets the icon name/key for a language (for use with icon fonts/resources).
    /// </summary>
    public static string GetIconName(string? language)
    {
        return language switch
        {
            "csharp" or "fsharp" or "vb" => "dotnet",
            "javascript" or "javascriptreact" => "javascript",
            "typescript" or "typescriptreact" => "typescript",
            "python" or "pip-requirements" => "python",
            "ruby" => "ruby",
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
            "yaml" or "toml" or "ini" => "config",
            "markdown" or "mdx" or "restructuredtext" => "markdown",
            "dockerfile" => "docker",
            "shellscript" or "bash" or "zsh" => "terminal",
            "powershell" => "powershell",
            "sql" => "database",
            "git" or "ignore" => "git",
            _ => "file"
        };
    }
}
