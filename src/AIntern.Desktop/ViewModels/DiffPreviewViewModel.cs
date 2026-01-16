using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF PREVIEW VIEW MODEL (v0.4.4f)                                       │
// │ Individual file diff preview for the batch preview dialog.              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for an individual file diff preview within the batch preview dialog.
/// </summary>
/// <remarks>
/// <para>
/// Wraps a <see cref="DiffResult"/> and provides a <see cref="DiffViewerViewModel"/>
/// for displaying the diff content, along with computed properties for UI display.
/// </para>
/// <para>Added in v0.4.4f.</para>
/// </remarks>
public partial class DiffPreviewViewModel : ViewModelBase
{
    private readonly DiffResult _diff;
    private readonly ILogger<DiffPreviewViewModel>? _logger;

    #region Observable Properties

    /// <summary>
    /// The diff viewer ViewModel for displaying the diff content.
    /// </summary>
    [ObservableProperty]
    private DiffViewerViewModel? _diffViewModel;

    #endregion

    #region Properties

    /// <summary>
    /// The underlying diff result.
    /// </summary>
    public DiffResult DiffResult => _diff;

    /// <summary>
    /// The file name without path.
    /// </summary>
    public string FileName => Path.GetFileName(_diff.OriginalFilePath);

    /// <summary>
    /// The full file path.
    /// </summary>
    public string FilePath => _diff.OriginalFilePath;

    /// <summary>
    /// Whether this is a new file (original didn't exist).
    /// </summary>
    public bool IsNewFile => _diff.IsNewFile;

    /// <summary>
    /// Whether this is a modification of an existing file.
    /// </summary>
    public bool IsModification => !_diff.IsNewFile && !_diff.IsDeleteFile;

    /// <summary>
    /// Number of lines added.
    /// </summary>
    public int AddedLines => _diff.Stats.AddedLines;

    /// <summary>
    /// Number of lines removed.
    /// </summary>
    public int RemovedLines => _diff.Stats.RemovedLines;

    /// <summary>
    /// Net change in lines (added - removed).
    /// </summary>
    public int NetChange => AddedLines - RemovedLines;

    /// <summary>
    /// The detected programming language.
    /// </summary>
    public string? Language => DetectLanguage();

    /// <summary>
    /// Icon name based on file type.
    /// </summary>
    public string FileIcon => GetFileIcon();

    /// <summary>
    /// Truncated display name for tabs.
    /// </summary>
    public string DisplayName
    {
        get
        {
            var name = FileName;
            if (name.Length > 20)
            {
                return name[..17] + "...";
            }
            return name;
        }
    }

    /// <summary>
    /// Summary of changes (e.g., "+12 -3").
    /// </summary>
    public string ChangesSummary
    {
        get
        {
            if (IsNewFile)
                return $"+{AddedLines}";
            return $"+{AddedLines} -{RemovedLines}";
        }
    }

    /// <summary>
    /// Whether the diff has any actual changes.
    /// </summary>
    public bool HasChanges => _diff.HasChanges;

    /// <summary>
    /// File extension without the dot.
    /// </summary>
    public string Extension => Path.GetExtension(FilePath).TrimStart('.');

    /// <summary>
    /// Total line count of the proposed content.
    /// </summary>
    public int TotalLines => _diff.ProposedContent?.Split('\n').Length ?? 0;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new DiffPreviewViewModel.
    /// </summary>
    /// <param name="diff">The diff result to preview.</param>
    /// <param name="diffService">The diff service for the viewer.</param>
    /// <param name="inlineDiffService">The inline diff service for the viewer.</param>
    /// <param name="logger">Optional logger.</param>
    public DiffPreviewViewModel(
        DiffResult diff,
        IDiffService diffService,
        IInlineDiffService inlineDiffService,
        ILogger<DiffPreviewViewModel>? logger = null)
    {
        _diff = diff;
        _logger = logger;

        _logger?.LogDebug("Creating DiffPreviewViewModel for {FilePath}", diff.OriginalFilePath);

        // Create the diff viewer ViewModel with required services
        _diffViewModel = new DiffViewerViewModel(diffService, inlineDiffService);
        _diffViewModel.LoadDiff(diff);

        _logger?.LogTrace("DiffViewerViewModel loaded for {FileName}", FileName);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the appropriate icon name based on file extension.
    /// </summary>
    private string GetFileIcon()
    {
        return Path.GetExtension(FilePath).ToLowerInvariant() switch
        {
            ".cs" => "CSharpIcon",
            ".ts" or ".tsx" => "TypeScriptIcon",
            ".js" or ".jsx" or ".mjs" => "JavaScriptIcon",
            ".py" => "PythonIcon",
            ".json" => "JsonIcon",
            ".xml" or ".axaml" or ".xaml" => "XmlIcon",
            ".md" or ".markdown" => "MarkdownIcon",
            ".html" or ".htm" => "HtmlIcon",
            ".css" or ".scss" or ".sass" or ".less" => "CssIcon",
            ".yaml" or ".yml" => "YamlIcon",
            ".sql" => "FileIcon",
            ".sh" or ".bash" => "FileIcon",
            ".ps1" => "FileIcon",
            ".java" => "FileIcon",
            ".rb" => "FileIcon",
            ".go" => "FileIcon",
            ".rs" => "FileIcon",
            ".cpp" or ".cc" or ".cxx" or ".c" or ".h" or ".hpp" => "FileIcon",
            ".swift" => "FileIcon",
            ".kt" or ".kts" => "FileIcon",
            ".php" => "FileIcon",
            ".vue" => "FileIcon",
            ".svelte" => "FileIcon",
            ".razor" => "FileIcon",
            _ => "FileIcon"
        };
    }

    /// <summary>
    /// Detects the programming language based on file extension.
    /// </summary>
    private string? DetectLanguage()
    {
        return Path.GetExtension(FilePath).ToLowerInvariant() switch
        {
            ".cs" => "C#",
            ".ts" or ".tsx" => "TypeScript",
            ".js" or ".jsx" or ".mjs" => "JavaScript",
            ".py" => "Python",
            ".json" => "JSON",
            ".xml" or ".axaml" or ".xaml" => "XML",
            ".md" or ".markdown" => "Markdown",
            ".html" or ".htm" => "HTML",
            ".css" => "CSS",
            ".yaml" or ".yml" => "YAML",
            ".sql" => "SQL",
            ".sh" or ".bash" => "Shell",
            ".java" => "Java",
            ".rb" => "Ruby",
            ".go" => "Go",
            ".rs" => "Rust",
            ".cpp" or ".cc" or ".cxx" => "C++",
            ".c" or ".h" => "C",
            ".swift" => "Swift",
            ".kt" or ".kts" => "Kotlin",
            ".php" => "PHP",
            _ => null
        };
    }

    #endregion
}
