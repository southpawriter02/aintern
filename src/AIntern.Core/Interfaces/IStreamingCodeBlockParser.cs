namespace AIntern.Core.Interfaces;

using AIntern.Core.Events;
using AIntern.Core.Models;

/// <summary>
/// Parser that extracts code blocks incrementally during LLM token streaming (v0.4.1f).
/// </summary>
/// <remarks>
/// <para>
/// The parser operates as a state machine, processing tokens character-by-character
/// and raising events as code blocks are detected, accumulate content, and complete.
/// </para>
/// <para>
/// Typical usage:
/// <code>
/// var parser = parserFactory.Create();
/// parser.Reset(messageId);
/// parser.BlockStarted += OnBlockStarted;
/// parser.ContentAdded += OnContentAdded;
/// parser.BlockCompleted += OnBlockCompleted;
///
/// await foreach (var token in llm.GenerateStreamingAsync(...))
/// {
///     parser.FeedToken(token);
/// }
/// parser.Complete();
///
/// var blocks = parser.GetCompletedBlocks();
/// </code>
/// </para>
/// </remarks>
public interface IStreamingCodeBlockParser : IDisposable
{
    #region State Properties

    /// <summary>
    /// Current state of the parser state machine.
    /// </summary>
    StreamingParserState State { get; }

    /// <summary>
    /// Total number of characters processed.
    /// </summary>
    int TotalCharactersProcessed { get; }

    /// <summary>
    /// Number of code blocks detected so far (completed + current).
    /// </summary>
    int BlockCount { get; }

    /// <summary>
    /// Whether there is currently a block being parsed.
    /// </summary>
    bool IsInsideCodeBlock { get; }

    /// <summary>
    /// ID of the message currently being parsed.
    /// </summary>
    Guid CurrentMessageId { get; }

    #endregion

    #region Core Methods

    /// <summary>
    /// Feed a token from the LLM stream to the parser.
    /// </summary>
    /// <param name="token">The token to process (may be single char or multiple chars).</param>
    void FeedToken(string token);

    /// <summary>
    /// Feed multiple tokens at once.
    /// </summary>
    /// <param name="tokens">Collection of tokens to process.</param>
    void FeedTokens(IEnumerable<string> tokens);

    /// <summary>
    /// Get all completed code blocks extracted so far.
    /// </summary>
    /// <returns>Read-only list of completed code blocks.</returns>
    IReadOnlyList<CodeBlock> GetCompletedBlocks();

    /// <summary>
    /// Get the current partial code block being accumulated.
    /// Returns null if not currently inside a code block.
    /// </summary>
    /// <returns>The partial block or null.</returns>
    PartialCodeBlock? GetCurrentBlock();

    /// <summary>
    /// Get a snapshot of the current block's content.
    /// More efficient than GetCurrentBlock().Content.ToString() for frequent polling.
    /// </summary>
    /// <returns>Current content or empty string if no active block.</returns>
    string GetCurrentBlockContent();

    /// <summary>
    /// Signal that streaming is complete.
    /// Finalizes any open code block (handles missing closing fence).
    /// </summary>
    void Complete();

    /// <summary>
    /// Reset the parser state for a new message.
    /// </summary>
    /// <param name="messageId">ID of the new message.</param>
    void Reset(Guid messageId);

    #endregion

    #region Events

    /// <summary>
    /// Raised when a new code block starts (opening fence detected and parsed).
    /// </summary>
    event EventHandler<CodeBlockStartedEventArgs>? BlockStarted;

    /// <summary>
    /// Raised when content is added to the current code block.
    /// </summary>
    event EventHandler<CodeBlockContentEventArgs>? ContentAdded;

    /// <summary>
    /// Raised when a code block is completed (closing fence detected or EOF).
    /// </summary>
    event EventHandler<CodeBlockCompletedEventArgs>? BlockCompleted;

    /// <summary>
    /// Raised when a parsing error occurs.
    /// </summary>
    event EventHandler<StreamingParseErrorEventArgs>? ParseError;

    #endregion
}
