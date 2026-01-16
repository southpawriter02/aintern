namespace AIntern.Core.Models;

/// <summary>
/// Classification of a code block's purpose (v0.4.1a).
/// </summary>
public enum CodeBlockType
{
    /// <summary>
    /// A complete file to be created or replaced entirely.
    /// </summary>
    CompleteFile,

    /// <summary>
    /// A partial snippet to be inserted or to replace a section of a file.
    /// </summary>
    Snippet,

    /// <summary>
    /// Example/illustration code (not meant to be applied).
    /// </summary>
    Example,

    /// <summary>
    /// Shell/terminal command (not a code file).
    /// </summary>
    Command,

    /// <summary>
    /// Output/log content (readonly, not applicable).
    /// </summary>
    Output,

    /// <summary>
    /// Configuration or data file (JSON, YAML, XML, TOML, etc.).
    /// </summary>
    Config
}

/// <summary>
/// Status of a code block in the apply workflow (v0.4.1a).
/// </summary>
public enum CodeBlockStatus
{
    /// <summary>
    /// Not yet processed by user.
    /// </summary>
    Pending,

    /// <summary>
    /// Successfully applied to target file.
    /// </summary>
    Applied,

    /// <summary>
    /// User rejected this code block.
    /// </summary>
    Rejected,

    /// <summary>
    /// Skipped (e.g., not applicable or user chose to skip).
    /// </summary>
    Skipped,

    /// <summary>
    /// Conflict detected with current file state.
    /// </summary>
    Conflict,

    /// <summary>
    /// Error occurred during apply.
    /// </summary>
    Error
}

/// <summary>
/// Represents a range of text by character positions (v0.4.1a).
/// </summary>
public readonly record struct TextRange(int Start, int End)
{
    /// <summary>
    /// Length of this range in characters.
    /// </summary>
    public int Length => End - Start;

    /// <summary>
    /// Whether this is an empty range.
    /// </summary>
    public bool IsEmpty => Start == End;

    /// <summary>
    /// Whether this is a valid range (non-negative, end >= start).
    /// </summary>
    public bool IsValid => Start >= 0 && End >= Start;

    /// <summary>
    /// An empty range at position 0.
    /// </summary>
    public static TextRange Empty => new(0, 0);

    /// <summary>
    /// Creates a range from a start position and length.
    /// </summary>
    public static TextRange FromLength(int start, int length) => new(start, start + length);

    /// <summary>
    /// Whether this range contains the specified position.
    /// </summary>
    public bool Contains(int position) => position >= Start && position < End;

    /// <summary>
    /// Whether this range overlaps with another range.
    /// </summary>
    public bool Overlaps(TextRange other) =>
        Start < other.End && other.Start < End;
}

/// <summary>
/// Represents a range of lines in a file (v0.4.1a).
/// </summary>
public readonly record struct LineRange(int StartLine, int EndLine)
{
    /// <summary>
    /// Number of lines in this range (inclusive).
    /// </summary>
    public int LineCount => EndLine - StartLine + 1;

    /// <summary>
    /// Whether this is a valid range (start <= end, both positive).
    /// </summary>
    public bool IsValid => StartLine > 0 && EndLine >= StartLine;

    /// <summary>
    /// Whether this is an empty/invalid range.
    /// </summary>
    public bool IsEmpty => StartLine == 0 && EndLine == 0;

    /// <summary>
    /// An empty line range.
    /// </summary>
    public static LineRange Empty => new(0, 0);

    /// <summary>
    /// Creates a range for a single line.
    /// </summary>
    public static LineRange SingleLine(int line) => new(line, line);

    /// <summary>
    /// Whether this range contains the specified line.
    /// </summary>
    public bool Contains(int line) => line >= StartLine && line <= EndLine;

    /// <summary>
    /// Whether this range overlaps with another range.
    /// </summary>
    public bool Overlaps(LineRange other) =>
        StartLine <= other.EndLine && other.StartLine <= EndLine;

    /// <summary>
    /// Returns a string representation of this range.
    /// </summary>
    public override string ToString() =>
        StartLine == EndLine ? $"Line {StartLine}" : $"Lines {StartLine}-{EndLine}";
}
