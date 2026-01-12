namespace AIntern.Core.Models;

/// <summary>
/// Templates for context formatting in prompts.
/// </summary>
public static class ContextPromptTemplates
{
    /// <summary>
    /// Template for single file context.
    /// Placeholders: {FileName}, {Language}, {LineRange}, {Content}
    /// </summary>
    public const string SingleFileTemplate = """
        ### File: `{FileName}` ({Language})
        {LineRange}
        ```{Language}
        {Content}
        ```
        """;

    /// <summary>
    /// Template for context header in prompts.
    /// Placeholder: {FormattedContexts}
    /// </summary>
    public const string ContextHeaderTemplate = """
        I'm providing you with the following code context to help you understand my question:

        {FormattedContexts}

        Please consider this context when responding. If you need to reference specific code, use the file names and line numbers provided.

        ---

        """;

    /// <summary>
    /// Template for when context has been truncated.
    /// Placeholders: {TotalLines}, {TotalTokens}
    /// </summary>
    public const string TruncationNotice = """

        *Note: This file has been truncated to fit within context limits. The full file is {TotalLines} lines ({TotalTokens} tokens estimated).*
        """;

    /// <summary>
    /// Template for selection context.
    /// Placeholders: {FileName}, {StartLine}, {EndLine}, {Language}, {Content}
    /// </summary>
    public const string SelectionTemplate = """
        ### Selected Code from `{FileName}` (lines {StartLine}-{EndLine})
        ```{Language}
        {Content}
        ```
        """;

    /// <summary>
    /// Separator between multiple contexts.
    /// </summary>
    public const string ContextSeparator = "\n---\n";

    /// <summary>
    /// Comment indicating truncation in preview.
    /// Placeholder: {RemainingLines}
    /// </summary>
    public const string PreviewTruncationComment = "// ... ({RemainingLines} more lines)";
}
