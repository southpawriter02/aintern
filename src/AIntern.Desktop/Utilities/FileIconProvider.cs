namespace AIntern.Desktop.Utilities;

/// <summary>
/// Provides icon keys for file extensions.
/// Maps file extensions to icon asset keys for the file explorer.
/// </summary>
/// <remarks>Added in v0.3.2a.</remarks>
public static class FileIconProvider
{
    /// <summary>
    /// Gets the icon key for a file extension.
    /// </summary>
    /// <param name="extension">File extension including dot (e.g., ".cs").</param>
    /// <returns>Icon key string for asset lookup.</returns>
    public static string GetIconKeyForExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return "file";

        // Normalize extension
        var ext = extension.ToLowerInvariant().TrimStart('.');

        return ext switch
        {
            // C# / .NET
            "cs" => "file-csharp",
            "csx" => "file-csharp",
            "csproj" => "file-csproj",
            "sln" => "file-sln",
            "props" => "file-xml",
            "targets" => "file-xml",
            "config" => "file-settings",
            "xaml" => "file-xaml",
            "axaml" => "file-xaml",

            // Web
            "js" => "file-javascript",
            "jsx" => "file-react",
            "ts" => "file-typescript",
            "tsx" => "file-react-ts",
            "html" or "htm" => "file-html",
            "css" => "file-css",
            "scss" or "sass" => "file-sass",
            "less" => "file-less",
            "vue" => "file-vue",
            "svelte" => "file-svelte",

            // Data
            "json" => "file-json",
            "xml" => "file-xml",
            "yaml" or "yml" => "file-yaml",
            "toml" => "file-toml",
            "csv" => "file-csv",
            "sql" => "file-sql",

            // Documentation
            "md" => "file-markdown",
            "txt" => "file-text",
            "pdf" => "file-pdf",
            "doc" or "docx" => "file-word",

            // Images
            "png" or "jpg" or "jpeg" or "gif" or "bmp" or "ico" or "svg" or "webp" => "file-image",

            // Python
            "py" => "file-python",
            "pyw" => "file-python",
            "pyx" => "file-python",
            "pyc" => "file-python-compiled",

            // Java / JVM
            "java" => "file-java",
            "kt" or "kts" => "file-kotlin",
            "jar" => "file-jar",

            // Go
            "go" => "file-go",
            "mod" => "file-go-mod",

            // Rust
            "rs" => "file-rust",

            // C/C++
            "c" => "file-c",
            "cpp" or "cc" or "cxx" => "file-cpp",
            "h" => "file-h",
            "hpp" or "hxx" => "file-hpp",

            // Shell
            "sh" or "bash" or "zsh" => "file-shell",
            "ps1" => "file-powershell",
            "bat" or "cmd" => "file-batch",

            // Config / DevOps
            "dockerfile" => "file-docker",
            "docker" => "file-docker",
            "gitignore" or "gitattributes" => "file-git",
            "env" => "file-env",
            "lock" => "file-lock",
            "editorconfig" => "file-editorconfig",

            // Default
            _ => "file"
        };
    }

    /// <summary>
    /// Gets the icon key for a special file by name.
    /// </summary>
    /// <param name="fileName">Full file name.</param>
    /// <returns>Icon key if special file, null otherwise.</returns>
    public static string? GetIconKeyForSpecialFile(string fileName)
    {
        var lowerName = fileName.ToLowerInvariant();

        return lowerName switch
        {
            "readme.md" or "readme.txt" or "readme" => "file-readme",
            "license" or "license.md" or "license.txt" => "file-license",
            "changelog.md" or "changelog.txt" or "changelog" => "file-changelog",
            "dockerfile" => "file-docker",
            "docker-compose.yml" or "docker-compose.yaml" => "file-docker-compose",
            "package.json" => "file-npm",
            "package-lock.json" => "file-npm-lock",
            "tsconfig.json" => "file-tsconfig",
            "webpack.config.js" => "file-webpack",
            "vite.config.js" or "vite.config.ts" => "file-vite",
            ".gitignore" => "file-git",
            ".gitattributes" => "file-git",
            ".editorconfig" => "file-editorconfig",
            ".eslintrc" or ".eslintrc.js" or ".eslintrc.json" => "file-eslint",
            ".prettierrc" or ".prettierrc.js" or ".prettierrc.json" => "file-prettier",
            "nuget.config" => "file-nuget",
            "global.json" => "file-dotnet",
            "appsettings.json" or "appsettings.development.json" => "file-settings",
            _ => null
        };
    }
}
