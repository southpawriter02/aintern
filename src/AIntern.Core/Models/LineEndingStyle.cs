namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ LINE ENDING STYLE (v0.4.3a)                                              │
// │ Enum for file line ending detection and handling.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Line ending styles for file handling.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3a.</para>
/// </remarks>
public enum LineEndingStyle
{
    /// <summary>
    /// Unix-style line endings (LF).
    /// </summary>
    LF,

    /// <summary>
    /// Windows-style line endings (CRLF).
    /// </summary>
    CRLF,

    /// <summary>
    /// Old Mac-style line endings (CR).
    /// </summary>
    CR,

    /// <summary>
    /// Mixed line endings detected.
    /// </summary>
    Mixed,

    /// <summary>
    /// Unknown or no line endings (single-line file).
    /// </summary>
    Unknown
}

/// <summary>
/// Extension methods for <see cref="LineEndingStyle"/>.
/// </summary>
public static class LineEndingStyleExtensions
{
    /// <summary>
    /// Gets the string representation of the line ending.
    /// </summary>
    public static string ToLineEnding(this LineEndingStyle style) => style switch
    {
        LineEndingStyle.LF => "\n",
        LineEndingStyle.CRLF => "\r\n",
        LineEndingStyle.CR => "\r",
        _ => Environment.NewLine
    };

    /// <summary>
    /// Detects the predominant line ending style in content.
    /// </summary>
    public static LineEndingStyle DetectLineEndings(string content)
    {
        if (string.IsNullOrEmpty(content))
            return LineEndingStyle.Unknown;

        var crlfCount = 0;
        var lfCount = 0;
        var crCount = 0;

        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '\r')
            {
                if (i + 1 < content.Length && content[i + 1] == '\n')
                {
                    crlfCount++;
                    i++; // Skip the LF
                }
                else
                {
                    crCount++;
                }
            }
            else if (content[i] == '\n')
            {
                lfCount++;
            }
        }

        var total = crlfCount + lfCount + crCount;
        if (total == 0)
            return LineEndingStyle.Unknown;

        // Check for mixed
        var typesPresent = (crlfCount > 0 ? 1 : 0) + (lfCount > 0 ? 1 : 0) + (crCount > 0 ? 1 : 0);
        if (typesPresent > 1)
            return LineEndingStyle.Mixed;

        if (crlfCount > 0) return LineEndingStyle.CRLF;
        if (lfCount > 0) return LineEndingStyle.LF;
        if (crCount > 0) return LineEndingStyle.CR;

        return LineEndingStyle.Unknown;
    }
}
