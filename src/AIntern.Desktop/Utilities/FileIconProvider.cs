namespace AIntern.Desktop.Utilities;

/// <summary>
/// Provides icon keys for file extensions (v0.3.2c stub).
/// </summary>
public static class FileIconProvider
{
    /// <summary>
    /// Gets the icon key for a given file extension.
    /// </summary>
    public static string GetIconKeyForExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return "file";

        // Normalize extension
        var ext = extension.TrimStart('.').ToLowerInvariant();

        return ext switch
        {
            // Programming languages
            "cs" => "file-csharp",
            "fs" or "fsx" => "file-fsharp",
            "vb" => "file-vb",
            "ts" => "file-typescript",
            "tsx" => "file-typescript-react",
            "js" => "file-javascript",
            "jsx" => "file-javascript-react",
            "py" => "file-python",
            "rb" => "file-ruby",
            "go" => "file-go",
            "rs" => "file-rust",
            "java" => "file-java",
            "kt" or "kts" => "file-kotlin",
            "swift" => "file-swift",
            "cpp" or "cc" or "cxx" => "file-cpp",
            "c" => "file-c",
            "h" or "hpp" => "file-header",
            "php" => "file-php",
            
            // Web
            "html" or "htm" => "file-html",
            "css" => "file-css",
            "scss" or "sass" => "file-sass",
            "less" => "file-less",
            "vue" => "file-vue",
            "svelte" => "file-svelte",
            
            // Data formats
            "json" => "file-json",
            "xml" => "file-xml",
            "yaml" or "yml" => "file-yaml",
            "toml" => "file-toml",
            "csv" => "file-csv",
            "sql" => "file-sql",
            
            // Documents
            "md" or "markdown" => "file-markdown",
            "txt" => "file-text",
            "pdf" => "file-pdf",
            "doc" or "docx" => "file-word",
            "xls" or "xlsx" => "file-excel",
            "ppt" or "pptx" => "file-powerpoint",
            
            // Images
            "png" or "jpg" or "jpeg" or "gif" or "bmp" or "ico" => "file-image",
            "svg" => "file-svg",
            
            // Config
            "gitignore" or "gitattributes" => "file-git",
            "env" => "file-env",
            "editorconfig" => "file-editorconfig",
            "dockerignore" => "file-docker",
            "sln" => "file-solution",
            "csproj" or "fsproj" or "vbproj" => "file-project",
            
            // Shell
            "sh" or "bash" or "zsh" => "file-shell",
            "ps1" => "file-powershell",
            "bat" or "cmd" => "file-batch",
            
            // Default
            _ => "file"
        };
    }
}
