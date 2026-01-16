namespace AIntern.Core.Models;

/// <summary>
/// Represents the current state of the streaming code block parser (v0.4.1f).
/// </summary>
public enum StreamingParserState
{
    /// <summary>
    /// Parser is outside any code block, processing regular text.
    /// Watching for fence start sequence (``` or ~~~).
    /// </summary>
    Text,

    /// <summary>
    /// Parser detected fence start, now reading language/path info.
    /// Waiting for newline to complete the opening fence line.
    /// </summary>
    FenceOpening,

    /// <summary>
    /// Parser is inside code block content.
    /// Accumulating code and watching for closing fence.
    /// </summary>
    CodeContent,

    /// <summary>
    /// Parser detected potential closing fence.
    /// Confirming end of block on newline or EOF.
    /// </summary>
    FenceClosing
}

/// <summary>
/// Type of fence delimiter used for code blocks (v0.4.1f).
/// </summary>
public enum FenceType
{
    /// <summary>
    /// Backtick fence (```).
    /// </summary>
    Backtick,

    /// <summary>
    /// Tilde fence (~~~).
    /// </summary>
    Tilde
}
