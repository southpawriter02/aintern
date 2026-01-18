// -----------------------------------------------------------------------
// <copyright file="CommandExtractorService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Services.Terminal;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

/// <summary>
/// Extracts executable commands from AI-generated markdown content.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4b.</para>
/// <para>
/// This service implements a multi-stage extraction pipeline:
/// </para>
/// <list type="number">
/// <item><description>Extract fenced code blocks using regex</description></item>
/// <item><description>Filter blocks by shell language or heuristics</description></item>
/// <item><description>Extract inline commands after indicator phrases</description></item>
/// <item><description>Check each command for dangerous patterns</description></item>
/// <item><description>Extract descriptions from surrounding context</description></item>
/// <item><description>Assign confidence scores based on extraction method</description></item>
/// </list>
/// <para>
/// Uses source-generated regex for all patterns to optimize performance.
/// </para>
/// </remarks>
public sealed partial class CommandExtractorService : ICommandExtractorService
{
    private readonly ILogger<CommandExtractorService> _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // SHELL LANGUAGE IDENTIFIERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Language identifiers that indicate shell/terminal commands.
    /// </summary>
    /// <remarks>
    /// These identifiers are recognized in code fence language tags.
    /// Case-insensitive matching is used.
    /// </remarks>
    private static readonly HashSet<string> ShellLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Unix shells
        "bash", "sh", "shell", "zsh", "fish", "ksh", "csh", "tcsh",

        // Windows shells
        "powershell", "pwsh", "ps1", "ps",
        "cmd", "batch", "bat", "dos",

        // Generic terminal identifiers
        "console", "terminal", "command", "cli"
    };

    // ═══════════════════════════════════════════════════════════════════════
    // COMMON EXECUTABLE COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Known command executables used for heuristic detection.
    /// </summary>
    /// <remarks>
    /// When a code block has no language tag, the first word is checked
    /// against this set to determine if it's likely a command.
    /// </remarks>
    private static readonly HashSet<string> CommonCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        // Package managers - JavaScript/Node
        "npm", "yarn", "pnpm", "npx", "bun",

        // Package managers - Python
        "pip", "pip3", "pipx", "poetry", "conda",

        // Package managers - Ruby
        "gem", "bundle", "bundler",

        // Package managers - Rust
        "cargo", "rustup",

        // Package managers - Go
        "go", "gofmt",

        // Package managers - .NET
        "dotnet", "nuget",

        // Package managers - PHP
        "composer", "php",

        // System package managers
        "brew", "apt", "apt-get", "yum", "dnf", "pacman", "apk", "zypper",
        "choco", "scoop", "winget",

        // Version control
        "git", "gh", "hub", "svn", "hg",

        // Containers & orchestration
        "docker", "docker-compose", "podman",
        "kubectl", "helm", "minikube", "kind",
        "terraform", "pulumi", "ansible",

        // Build tools
        "make", "cmake", "ninja", "meson",
        "gradle", "mvn", "maven", "ant",
        "msbuild", "xcodebuild",

        // File operations
        "cd", "ls", "dir", "mkdir", "rm", "cp", "mv", "cat", "less", "more",
        "touch", "chmod", "chown", "ln", "find", "grep", "sed", "awk",
        "tar", "zip", "unzip", "gzip", "gunzip",

        // Network utilities
        "curl", "wget", "ssh", "scp", "rsync", "ping", "nc", "netcat",
        "http", "https",

        // Miscellaneous utilities
        "echo", "printf", "export", "set", "env", "source",
        "sudo", "su", "doas",
        "systemctl", "service", "launchctl",
        "code", "vim", "nano", "emacs",
        "python", "python3", "node", "ruby", "perl"
    };

    // ═══════════════════════════════════════════════════════════════════════
    // DANGEROUS COMMAND PATTERNS (Source-Generated Regex)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dangerous command patterns with associated warning messages.
    /// </summary>
    /// <remarks>
    /// Each pattern is compiled using source-generated regex for optimal performance.
    /// Patterns are checked in order; first match returns.
    /// </remarks>
    private static readonly (Regex Pattern, string Warning)[] DangerousPatterns =
    {
        (RmRfPattern(), "This command recursively deletes files. Verify the path carefully."),
        (FormatPattern(), "This command formats a disk. This is destructive."),
        (DdPattern(), "dd can overwrite disk data. Verify source and destination."),
        (ChmodRecursivePattern(), "Recursive permission changes can affect many files."),
        (SudoRmPattern(), "Elevated deletion command. Be very careful."),
        (DropDatabasePattern(), "This will delete a database permanently."),
        (TruncatePattern(), "This truncates/empties files."),
        (MkfsPattern(), "This creates a filesystem, erasing existing data."),
        (KillAllPattern(), "This terminates multiple processes."),
        (RebootPattern(), "This will restart or shutdown the system."),
        (ForkBombPattern(), "This appears to be a fork bomb. Do NOT run."),
        (CurlPipeShPattern(), "Piping curl to shell can be dangerous. Review the script first.")
    };

    // --- Source-generated regex patterns for dangerous commands ---

    /// <summary>Matches recursive rm commands targeting sensitive paths.</summary>
    [GeneratedRegex(@"\brm\s+(-[rf]+\s+)*(/|~|\$HOME|\.\.)", RegexOptions.IgnoreCase)]
    private static partial Regex RmRfPattern();

    /// <summary>Matches Windows format command.</summary>
    [GeneratedRegex(@"\bformat\s+[a-z]:", RegexOptions.IgnoreCase)]
    private static partial Regex FormatPattern();

    /// <summary>Matches dd with output file specified.</summary>
    [GeneratedRegex(@"\bdd\s+.*\bof=", RegexOptions.IgnoreCase)]
    private static partial Regex DdPattern();

    /// <summary>Matches chmod with recursive flag.</summary>
    [GeneratedRegex(@"\bchmod\s+-[Rr]", RegexOptions.IgnoreCase)]
    private static partial Regex ChmodRecursivePattern();

    /// <summary>Matches sudo rm command.</summary>
    [GeneratedRegex(@"\bsudo\s+rm\b", RegexOptions.IgnoreCase)]
    private static partial Regex SudoRmPattern();

    /// <summary>Matches SQL DROP statements.</summary>
    [GeneratedRegex(@"\bDROP\s+(DATABASE|TABLE|SCHEMA)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DropDatabasePattern();

    /// <summary>Matches file truncation patterns.</summary>
    [GeneratedRegex(@">\s*/dev/null|>\s*\$\(|truncate\s+-s\s*0", RegexOptions.IgnoreCase)]
    private static partial Regex TruncatePattern();

    /// <summary>Matches mkfs filesystem creation.</summary>
    [GeneratedRegex(@"\bmkfs\.", RegexOptions.IgnoreCase)]
    private static partial Regex MkfsPattern();

    /// <summary>Matches killall/pkill commands.</summary>
    [GeneratedRegex(@"\bkillall\b|\bpkill\b", RegexOptions.IgnoreCase)]
    private static partial Regex KillAllPattern();

    /// <summary>Matches reboot/shutdown commands.</summary>
    [GeneratedRegex(@"\b(reboot|shutdown|halt|poweroff)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RebootPattern();

    /// <summary>Matches bash fork bomb pattern.</summary>
    [GeneratedRegex(@":\(\)\s*\{\s*:\|:&\s*\};:", RegexOptions.None)]
    private static partial Regex ForkBombPattern();

    /// <summary>Matches curl piped to shell.</summary>
    [GeneratedRegex(@"curl\s+.*\|\s*(ba)?sh", RegexOptions.IgnoreCase)]
    private static partial Regex CurlPipeShPattern();

    // ═══════════════════════════════════════════════════════════════════════
    // CONTENT PARSING PATTERNS (Source-Generated Regex)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Matches fenced code blocks with optional language tag.</summary>
    [GeneratedRegex(@"```(?<lang>\w+)?\r?\n(?<content>[\s\S]*?)```", RegexOptions.Compiled)]
    private static partial Regex FencedCodeBlockPattern();

    /// <summary>Matches inline code (backtick-wrapped).</summary>
    [GeneratedRegex(@"`(?<cmd>[^`\n]+)`", RegexOptions.Compiled)]
    private static partial Regex InlineCodePattern();

    // ═══════════════════════════════════════════════════════════════════════
    // COMMAND INDICATOR PHRASES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phrases that indicate the following content is a command.
    /// </summary>
    /// <remarks>
    /// When these phrases appear in text, subsequent inline code is likely a command.
    /// </remarks>
    private static readonly string[] CommandIndicators =
    {
        "run the following",
        "execute the command",
        "run this command",
        "use this command",
        "try running",
        "you can run",
        "enter the command",
        "type the following",
        "run the command",
        "execute this",
        "use the command",
        "in your terminal",
        "from the command line",
        "in the shell"
    };

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExtractorService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public CommandExtractorService(ILogger<CommandExtractorService> logger)
    {
        _logger = logger;
        _logger.LogDebug("CommandExtractorService initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MAIN EXTRACTION
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public CommandExtractionResult ExtractCommands(string content, Guid messageId)
    {
        _logger.LogDebug("Starting command extraction for message {MessageId}, content length: {Length}",
            messageId, content?.Length ?? 0);

        // Handle empty/null content
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogDebug("Content is empty, returning no commands");
            return CommandExtractionResult.Empty;
        }

        var commands = new List<CommandBlock>();
        var warnings = new List<string>();
        var sequenceNumber = 0;

        // ─────────────────────────────────────────────────────────────────
        // Step 1: Extract from fenced code blocks
        // ─────────────────────────────────────────────────────────────────
        _logger.LogTrace("Step 1: Extracting fenced code blocks");

        var fencedMatches = FencedCodeBlockPattern().Matches(content);
        _logger.LogDebug("Found {Count} fenced code blocks", fencedMatches.Count);

        foreach (Match match in fencedMatches)
        {
            var language = match.Groups["lang"].Value;
            var blockContent = match.Groups["content"].Value.Trim();

            _logger.LogTrace("Processing code block: language='{Language}', content length={Length}",
                language, blockContent.Length);

            // Check if this is a shell command
            if (IsShellCommand(language, blockContent))
            {
                var (isDangerous, warning) = CheckCommandSafety(blockContent);
                var description = ExtractDescription(content, match.Index);
                var confidenceScore = CalculateConfidenceScore(language, blockContent);
                var shellType = GetShellTypeForLanguage(language) ?? InferShellType(blockContent);

                var commandBlock = new CommandBlock
                {
                    Command = blockContent,
                    Language = string.IsNullOrEmpty(language) ? null : language,
                    DetectedShellType = shellType,
                    MessageId = messageId,
                    SequenceNumber = sequenceNumber++,
                    Description = description,
                    SourceRange = new TextRange(match.Index, match.Index + match.Length),
                    IsPotentiallyDangerous = isDangerous,
                    DangerWarning = warning,
                    ConfidenceScore = confidenceScore
                };

                commands.Add(commandBlock);

                _logger.LogDebug(
                    "Extracted command #{Seq}: shell={Shell}, confidence={Confidence:F2}, dangerous={Dangerous}",
                    sequenceNumber, shellType, confidenceScore, isDangerous);

                // Add warning to result if dangerous
                if (isDangerous && warning != null)
                {
                    warnings.Add($"Command {sequenceNumber}: {warning}");
                }
            }
            else
            {
                _logger.LogTrace("Skipping non-shell code block: language='{Language}'", language);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Step 2: Extract inline commands following indicator phrases
        // ─────────────────────────────────────────────────────────────────
        _logger.LogTrace("Step 2: Extracting inline commands after indicator phrases");
        ExtractInlineCommands(content, messageId, commands, warnings, ref sequenceNumber);

        // ─────────────────────────────────────────────────────────────────
        // Build and return result
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation(
            "Extraction complete for message {MessageId}: {CommandCount} commands, {WarningCount} warnings",
            messageId, commands.Count, warnings.Count);

        return new CommandExtractionResult
        {
            Commands = commands.AsReadOnly(),
            Warnings = warnings.AsReadOnly()
        };
    }

    /// <summary>
    /// Extract inline commands that follow indicator phrases.
    /// </summary>
    private void ExtractInlineCommands(
        string content,
        Guid messageId,
        List<CommandBlock> commands,
        List<string> warnings,
        ref int sequenceNumber)
    {
        var lines = content.Split('\n');
        var existingCommands = commands.Select(c => c.Command).ToHashSet();
        var extractedCount = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lowerLine = line.ToLowerInvariant();

            // Check if line contains a command indicator phrase
            if (!CommandIndicators.Any(ind => lowerLine.Contains(ind)))
            {
                continue;
            }

            _logger.LogTrace("Found indicator phrase on line {Line}: '{Text}'", i, line.Trim());

            // Scan next 1-3 lines for inline code
            for (int j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
            {
                var nextLine = lines[j].Trim();
                var inlineMatch = InlineCodePattern().Match(nextLine);

                if (!inlineMatch.Success)
                {
                    continue;
                }

                var cmd = inlineMatch.Groups["cmd"].Value;

                // Skip if already captured from fenced block
                if (existingCommands.Contains(cmd))
                {
                    _logger.LogTrace("Skipping duplicate inline command: '{Command}'", cmd);
                    continue;
                }

                // Skip if it doesn't look like a command
                if (!IsLikelyCommand(cmd))
                {
                    _logger.LogTrace("Skipping non-command inline code: '{Code}'", cmd);
                    continue;
                }

                var (isDangerous, warning) = CheckCommandSafety(cmd);
                var startIndex = content.IndexOf(nextLine, StringComparison.Ordinal);

                var commandBlock = new CommandBlock
                {
                    Command = cmd,
                    DetectedShellType = InferShellType(cmd),
                    MessageId = messageId,
                    SequenceNumber = sequenceNumber++,
                    Description = line.Trim(),
                    SourceRange = new TextRange(startIndex, startIndex + nextLine.Length),
                    IsPotentiallyDangerous = isDangerous,
                    DangerWarning = warning,
                    ConfidenceScore = 0.60f // Lower confidence for inline commands
                };

                commands.Add(commandBlock);
                existingCommands.Add(cmd);
                extractedCount++;

                _logger.LogDebug(
                    "Extracted inline command #{Seq}: '{Command}', dangerous={Dangerous}",
                    sequenceNumber, cmd, isDangerous);

                if (isDangerous && warning != null)
                {
                    warnings.Add($"Command {sequenceNumber}: {warning}");
                }

                break; // Only capture first inline code after indicator
            }
        }

        _logger.LogDebug("Extracted {Count} inline commands", extractedCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SHELL COMMAND DETECTION
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public bool IsShellCommand(string? language, string content)
    {
        // Explicit shell language tag → definitely a shell command
        if (!string.IsNullOrEmpty(language) && ShellLanguages.Contains(language))
        {
            _logger.LogTrace("Language '{Language}' is a known shell language", language);
            return true;
        }

        // If there's a language tag but it's not a shell → not a shell command
        if (!string.IsNullOrEmpty(language))
        {
            _logger.LogTrace("Language '{Language}' is not a shell language", language);
            return false;
        }

        // No language tag → use heuristics
        var isLikelyCommand = IsLikelyCommand(content);
        _logger.LogTrace("No language tag, heuristic result: {IsCommand}", isLikelyCommand);
        return isLikelyCommand;
    }

    /// <summary>
    /// Determine if content looks like a command using heuristics.
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <returns>True if the content appears to be a command.</returns>
    private bool IsLikelyCommand(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return false;
        }

        // Commands are typically short (1-10 lines)
        // Longer blocks are likely source code, not commands
        if (lines.Length > 10)
        {
            _logger.LogTrace("Content has {Count} lines, too many for typical command", lines.Length);
            return false;
        }

        var firstLine = lines[0].Trim();

        // ─────────────────────────────────────────────────────────────────
        // Rule 1: Check for shell prompt prefixes
        // ─────────────────────────────────────────────────────────────────
        if (firstLine.StartsWith("$ ") ||          // Unix prompt
            firstLine.StartsWith("> ") ||          // Generic prompt
            firstLine.StartsWith("PS>") ||         // PowerShell prompt
            firstLine.StartsWith("PS C:\\"))       // PowerShell with path
        {
            _logger.LogTrace("Detected shell prompt prefix");
            return true;
        }

        // Special handling for # - it's root prompt only if followed by space and command
        // (not a markdown heading which is # followed by space and then text starting with uppercase)
        if (firstLine.StartsWith("# ") && firstLine.Length > 2)
        {
            var afterHash = firstLine[2..].Trim();
            // Root prompt usually has lowercase command, markdown heading has capitalized text
            if (afterHash.Length > 0 && char.IsLower(afterHash[0]))
            {
                _logger.LogTrace("Detected root prompt (# followed by lowercase)");
                return true;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Rule 2: Check if first word is a known command
        // ─────────────────────────────────────────────────────────────────
        var firstWord = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .TrimStart('.', '/', '\\') ?? "";

        // Remove path if present (e.g., "/usr/bin/git" → "git")
        if (firstWord.Contains('/') || firstWord.Contains('\\'))
        {
            firstWord = Path.GetFileName(firstWord);
        }

        if (CommonCommands.Contains(firstWord))
        {
            _logger.LogTrace("First word '{Word}' is a known command", firstWord);
            return true;
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SHELL TYPE DETECTION
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public ShellType? GetShellTypeForLanguage(string? language)
    {
        if (string.IsNullOrEmpty(language))
        {
            return null;
        }

        ShellType? result = language.ToLowerInvariant() switch
        {
            // Unix shells
            "bash" or "sh" or "shell" => ShellType.Bash,
            "zsh" => ShellType.Zsh,
            "fish" => ShellType.Fish,
            "ksh" => ShellType.Ksh,
            "tcsh" or "csh" => ShellType.Tcsh,

            // Windows shells
            "powershell" or "pwsh" or "ps1" or "ps" => ShellType.PowerShell,
            "cmd" or "batch" or "bat" or "dos" => ShellType.Cmd,

            // Generic terminal identifiers and unknown - no specific shell type
            _ => null
        };

        _logger.LogTrace("Language '{Language}' mapped to ShellType: {ShellType}", language, result);
        return result;
    }

    /// <summary>
    /// Infer shell type from command content patterns.
    /// </summary>
    /// <param name="command">The command content to analyze.</param>
    /// <returns>Inferred ShellType or null if cannot be determined.</returns>
    private ShellType? InferShellType(string command)
    {
        // PowerShell patterns
        if (command.Contains("$env:") || command.Contains("Get-") || command.Contains("Set-"))
        {
            _logger.LogTrace("Inferred PowerShell from content patterns");
            return ShellType.PowerShell;
        }

        // CMD patterns: %VAR% variable syntax (must have at least two % signs)
        if (command.Contains('%') && command.IndexOf('%') != command.LastIndexOf('%'))
        {
            _logger.LogTrace("Inferred Cmd from %VAR% syntax");
            return ShellType.Cmd;
        }

        // Fish patterns: "set " without "="
        if (command.StartsWith("set ") && !command.Contains('='))
        {
            _logger.LogTrace("Inferred Fish from 'set' syntax");
            return ShellType.Fish;
        }

        // Default: no specific shell (use platform default)
        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SAFETY CHECKING
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public (bool IsDangerous, string? Warning) CheckCommandSafety(string command)
    {
        _logger.LogTrace("Checking command safety: '{Command}'", command);

        foreach (var (pattern, warning) in DangerousPatterns)
        {
            if (pattern.IsMatch(command))
            {
                _logger.LogWarning(
                    "Dangerous command pattern detected: {Warning} (Command: '{Command}')",
                    warning, command);
                return (true, warning);
            }
        }

        _logger.LogTrace("Command passed safety check");
        return (false, null);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DESCRIPTION EXTRACTION
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public string? ExtractDescription(string content, int commandPosition)
    {
        if (commandPosition <= 0)
        {
            return null;
        }

        var beforeContent = content[..commandPosition];
        var lines = beforeContent.Split('\n');

        _logger.LogTrace("Extracting description from {LineCount} lines before position {Position}",
            lines.Length, commandPosition);

        // Check last 1-4 non-empty lines before command
        for (int i = lines.Length - 1; i >= Math.Max(0, lines.Length - 4); i--)
        {
            var line = lines[i].Trim();

            // Skip empty lines
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            // Stop if we hit another code block
            if (line.StartsWith("```"))
            {
                _logger.LogTrace("Hit code block boundary, stopping description search");
                break;
            }

            // Priority 1: Use markdown header as description
            if (line.StartsWith('#'))
            {
                var description = line.TrimStart('#', ' ');
                _logger.LogTrace("Found header description: '{Description}'", description);
                return description;
            }

            // Priority 2: Check for command indicator phrase
            var lowerLine = line.ToLowerInvariant();
            if (CommandIndicators.Any(ind => lowerLine.Contains(ind)))
            {
                _logger.LogTrace("Found indicator phrase description: '{Description}'", line);
                return line;
            }

            // Priority 3: Use line ending with colon as description
            if (line.EndsWith(':'))
            {
                var description = line.TrimEnd(':');
                _logger.LogTrace("Found colon-ending description: '{Description}'", description);
                return description;
            }

            // Priority 4: Use sentence-like line as description
            // (starts with uppercase, at least 10 chars)
            if (line.Length > 10 && char.IsUpper(line[0]))
            {
                _logger.LogTrace("Found sentence description: '{Description}'", line);
                return line;
            }
        }

        _logger.LogTrace("No description found");
        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONFIDENCE SCORING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calculate confidence score based on extraction method.
    /// </summary>
    /// <param name="language">Language tag from code fence (may be null).</param>
    /// <param name="content">The command content.</param>
    /// <returns>Confidence score from 0.0 to 1.0.</returns>
    /// <remarks>
    /// <para>Scoring guidelines:</para>
    /// <list type="bullet">
    /// <item><description>0.95: Explicit shell language (bash, powershell)</description></item>
    /// <item><description>0.85: Generic terminal identifier (console, cli)</description></item>
    /// <item><description>0.70: No language but passes heuristics</description></item>
    /// <item><description>0.40: Default low confidence</description></item>
    /// </list>
    /// </remarks>
    private float CalculateConfidenceScore(string? language, string content)
    {
        // Explicit shell language tag = high confidence
        if (!string.IsNullOrEmpty(language))
        {
            if (ShellLanguages.Contains(language))
            {
                // Generic terms have slightly lower confidence
                var score = language.ToLowerInvariant() switch
                {
                    "console" or "terminal" or "command" or "cli" => 0.85f,
                    _ => 0.95f
                };

                _logger.LogTrace("Confidence for language '{Language}': {Score:F2}", language, score);
                return score;
            }
        }

        // No language but passes heuristics
        if (IsLikelyCommand(content))
        {
            _logger.LogTrace("Heuristic-based confidence: 0.70");
            return 0.70f;
        }

        // Default low confidence
        _logger.LogTrace("Default low confidence: 0.40");
        return 0.40f;
    }
}
