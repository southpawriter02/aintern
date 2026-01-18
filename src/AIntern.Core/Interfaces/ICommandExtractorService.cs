// -----------------------------------------------------------------------
// <copyright file="ICommandExtractorService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Interfaces;

using AIntern.Core.Models.Terminal;

/// <summary>
/// Service for extracting executable commands from AI-generated markdown content.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4b.</para>
/// <para>
/// This service parses fenced code blocks and inline code to identify executable
/// shell commands. It detects shell types, flags dangerous operations, extracts
/// contextual descriptions, and assigns confidence scores.
/// </para>
/// <para>
/// <b>Extraction Pipeline:</b>
/// <list type="number">
/// <item><description>Parse fenced code blocks with shell language tags</description></item>
/// <item><description>Apply heuristics to unlabeled code blocks</description></item>
/// <item><description>Extract inline commands following indicator phrases</description></item>
/// <item><description>Check each command for dangerous patterns</description></item>
/// <item><description>Extract descriptions from preceding context</description></item>
/// <item><description>Assign confidence scores based on extraction method</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ICommandExtractorService
{
    /// <summary>
    /// Extract all command blocks from message content.
    /// </summary>
    /// <param name="content">The message content to parse (markdown format).</param>
    /// <param name="messageId">ID of the source message for tracking.</param>
    /// <returns>
    /// Extraction result containing commands ordered by appearance and any warnings.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a multi-pass extraction:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Pass 1: Fenced code blocks (```language...```)</description></item>
    /// <item><description>Pass 2: Inline code after indicator phrases (`command`)</description></item>
    /// </list>
    /// <para>
    /// Commands are de-duplicated - if the same command appears in both a fenced block
    /// and inline code, only the fenced block version is returned.
    /// </para>
    /// </remarks>
    CommandExtractionResult ExtractCommands(string content, Guid messageId);

    /// <summary>
    /// Determine if a code block represents a shell command.
    /// </summary>
    /// <param name="language">
    /// Language identifier from code fence (e.g., "bash", "powershell").
    /// May be null for unlabeled code blocks.
    /// </param>
    /// <param name="content">Content of the code block.</param>
    /// <returns>True if this appears to be an executable shell command.</returns>
    /// <remarks>
    /// <para>
    /// Detection logic:
    /// </para>
    /// <list type="bullet">
    /// <item><description>If language is a known shell (bash, powershell, etc.) → True</description></item>
    /// <item><description>If language is unknown/null → Apply heuristics</description></item>
    /// <item><description>If language is non-shell (python, csharp, etc.) → False</description></item>
    /// </list>
    /// </remarks>
    bool IsShellCommand(string? language, string content);

    /// <summary>
    /// Get the appropriate ShellType for a language identifier.
    /// </summary>
    /// <param name="language">Language identifier (bash, powershell, etc.).</param>
    /// <returns>
    /// ShellType if language maps to a specific shell, null for generic identifiers
    /// (console, terminal, cli) or unknown languages.
    /// </returns>
    /// <remarks>
    /// <para>Mapping examples:</para>
    /// <list type="bullet">
    /// <item><description>"bash", "sh" → ShellType.Bash</description></item>
    /// <item><description>"powershell", "pwsh" → ShellType.PowerShell</description></item>
    /// <item><description>"cmd", "batch" → ShellType.Cmd</description></item>
    /// <item><description>"console", "terminal" → null (any shell)</description></item>
    /// </list>
    /// </remarks>
    ShellType? GetShellTypeForLanguage(string? language);

    /// <summary>
    /// Check if a command is potentially dangerous.
    /// </summary>
    /// <param name="command">The command text to check.</param>
    /// <returns>
    /// Tuple with IsDangerous flag and warning message describing the risk.
    /// </returns>
    /// <remarks>
    /// <para>Detected patterns include:</para>
    /// <list type="bullet">
    /// <item><description>rm -rf / (recursive deletion)</description></item>
    /// <item><description>sudo rm (elevated deletion)</description></item>
    /// <item><description>dd of= (disk overwrite)</description></item>
    /// <item><description>DROP DATABASE (data deletion)</description></item>
    /// <item><description>curl | sh (piped scripts)</description></item>
    /// <item><description>Fork bombs</description></item>
    /// </list>
    /// </remarks>
    (bool IsDangerous, string? Warning) CheckCommandSafety(string command);

    /// <summary>
    /// Extract a description for a command from surrounding context.
    /// </summary>
    /// <param name="content">Full message content.</param>
    /// <param name="commandPosition">Character position of the command in content.</param>
    /// <returns>
    /// Description text extracted from context, or null if none found.
    /// </returns>
    /// <remarks>
    /// <para>Description sources (in priority order):</para>
    /// <list type="bullet">
    /// <item><description>Markdown header (# Step 1: Install dependencies)</description></item>
    /// <item><description>Command indicator phrase (Run the following command)</description></item>
    /// <item><description>Colon-ending line (Install the package:)</description></item>
    /// <item><description>Nearby sentence-like text</description></item>
    /// </list>
    /// </remarks>
    string? ExtractDescription(string content, int commandPosition);
}
