namespace AIntern.Services;

using System.Text;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

/// <summary>
/// State machine-based parser for extracting code blocks during LLM streaming (v0.4.1f).
/// </summary>
/// <remarks>
/// <para>
/// Processes tokens character-by-character to accurately detect fence boundaries
/// regardless of token chunking from the LLM. Uses a four-state machine:
/// Text → FenceOpening → CodeContent → FenceClosing → Text.
/// </para>
/// </remarks>
public sealed class StreamingCodeBlockParser : IStreamingCodeBlockParser
{
    private readonly ILanguageDetectionService _languageService;
    private readonly IBlockClassificationService _classificationService;
    private readonly ILogger<StreamingCodeBlockParser>? _logger;

    // State
    private Guid _messageId;
    private StreamingParserState _state = StreamingParserState.Text;
    private readonly StringBuilder _buffer = new();
    private readonly StringBuilder _fenceLineBuffer = new();
    private int _position;
    private int _blockSequence;

    // Current block being parsed
    private PartialCodeBlock? _currentBlock;
    private DateTime _currentBlockStartTime;

    // Completed blocks
    private readonly List<CodeBlock> _completedBlocks = new();

    // Fence detection
    private FenceType _currentFenceType;
    private int _currentFenceLength;
    private int _pendingFenceChars;
    private char _fenceChar;
    private bool _atLineStart = true;

    // Constants
    private const char BacktickChar = '`';
    private const char TildeChar = '~';
    private const int MinFenceLength = 3;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ PROPERTIES                                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    public StreamingParserState State => _state;
    public int TotalCharactersProcessed => _position;
    public int BlockCount => _completedBlocks.Count + (_currentBlock != null ? 1 : 0);
    public bool IsInsideCodeBlock => _currentBlock != null;
    public Guid CurrentMessageId => _messageId;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EVENTS                                                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    public event EventHandler<CodeBlockStartedEventArgs>? BlockStarted;
    public event EventHandler<CodeBlockContentEventArgs>? ContentAdded;
    public event EventHandler<CodeBlockCompletedEventArgs>? BlockCompleted;
    public event EventHandler<StreamingParseErrorEventArgs>? ParseError;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingCodeBlockParser"/> class.
    /// </summary>
    public StreamingCodeBlockParser(
        ILanguageDetectionService languageService,
        IBlockClassificationService classificationService,
        ILogger<StreamingCodeBlockParser>? logger = null)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
        _logger = logger;
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CORE METHODS                                                             │
    // └─────────────────────────────────────────────────────────────────────────┘

    public void FeedToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return;

        foreach (var ch in token)
        {
            ProcessCharacter(ch);
            _position++;
            _buffer.Append(ch);
        }
    }

    public void FeedTokens(IEnumerable<string> tokens)
    {
        foreach (var token in tokens)
        {
            FeedToken(token);
        }
    }

    public IReadOnlyList<CodeBlock> GetCompletedBlocks() => _completedBlocks.AsReadOnly();

    public PartialCodeBlock? GetCurrentBlock() => _currentBlock;

    public string GetCurrentBlockContent() => _currentBlock?.Content.ToString() ?? string.Empty;

    public void Complete()
    {
        // If we're inside a code block, finalize it (unclosed fence)
        if (_currentBlock != null && _state == StreamingParserState.CodeContent)
        {
            _logger?.LogWarning(
                "[WARN] Completing message with unclosed code block. Block {BlockId} at position {Position}",
                _currentBlock.Id, _currentBlock.StartPosition);

            CompleteCurrentBlock(truncated: true);
        }

        // Handle pending fence at EOF
        if (_state == StreamingParserState.FenceClosing && _currentBlock != null)
        {
            CompleteCurrentBlock(truncated: false);
        }

        _state = StreamingParserState.Text;
        _logger?.LogDebug("[INFO] Parser completed for message {MessageId}. Total blocks: {BlockCount}",
            _messageId, _completedBlocks.Count);
    }

    public void Reset(Guid messageId)
    {
        _messageId = messageId;
        _state = StreamingParserState.Text;
        _buffer.Clear();
        _fenceLineBuffer.Clear();
        _position = 0;
        _blockSequence = 0;
        _currentBlock = null;
        _completedBlocks.Clear();
        _pendingFenceChars = 0;
        _atLineStart = true;

        _logger?.LogDebug("[INFO] Parser reset for message {MessageId}", messageId);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STATE MACHINE                                                            │
    // └─────────────────────────────────────────────────────────────────────────┘

    private void ProcessCharacter(char ch)
    {
        switch (_state)
        {
            case StreamingParserState.Text:
                ProcessTextState(ch);
                break;

            case StreamingParserState.FenceOpening:
                ProcessFenceOpeningState(ch);
                break;

            case StreamingParserState.CodeContent:
                ProcessCodeContentState(ch);
                break;

            case StreamingParserState.FenceClosing:
                ProcessFenceClosingState(ch);
                break;
        }

        // Track line starts (for fence detection)
        _atLineStart = (ch == '\n');
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ TEXT STATE                                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    private void ProcessTextState(char ch)
    {
        // Check for potential fence start
        if (ch == BacktickChar || ch == TildeChar)
        {
            if (_pendingFenceChars == 0)
            {
                _fenceChar = ch;
                _pendingFenceChars = 1;
            }
            else if (ch == _fenceChar)
            {
                _pendingFenceChars++;
                // Continue accumulating for extended fences (````, etc.)
            }
            else
            {
                // Different fence char, reset
                ResetFenceDetection();
            }
        }
        else if (_pendingFenceChars >= MinFenceLength)
        {
            // We have a complete fence, transition to FenceOpening
            BeginFenceOpening(ch);
        }
        else
        {
            // Not a fence, reset
            ResetFenceDetection();
        }
    }

    private void BeginFenceOpening(char firstChar)
    {
        _currentFenceType = _fenceChar == BacktickChar ? FenceType.Backtick : FenceType.Tilde;
        _currentFenceLength = _pendingFenceChars;

        _currentBlock = new PartialCodeBlock
        {
            MessageId = _messageId,
            SequenceNumber = _blockSequence++,
            StartPosition = _position - _currentFenceLength,
            FenceType = _currentFenceType,
            FenceLength = _currentFenceLength
        };

        _currentBlockStartTime = DateTime.UtcNow;
        _fenceLineBuffer.Clear();
        _state = StreamingParserState.FenceOpening;

        // Process the character that triggered transition
        if (firstChar != '\n')
        {
            _fenceLineBuffer.Append(firstChar);
        }
        else
        {
            // Immediate newline after fence = no language specified
            CompleteFenceOpening();
        }

        _pendingFenceChars = 0;

        _logger?.LogDebug("[INFO] Fence opening detected at position {Position}, length {Length}",
            _currentBlock.StartPosition, _currentFenceLength);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ FENCE OPENING STATE                                                      │
    // └─────────────────────────────────────────────────────────────────────────┘

    private void ProcessFenceOpeningState(char ch)
    {
        if (ch == '\n')
        {
            CompleteFenceOpening();
        }
        else
        {
            _fenceLineBuffer.Append(ch);
        }
    }

    private void CompleteFenceOpening()
    {
        if (_currentBlock == null) return;

        // Parse the fence line for language and optional path
        var fenceLine = _fenceLineBuffer.ToString().Trim();
        _currentBlock.FenceLine = fenceLine;

        ParseFenceLine(fenceLine);

        _state = StreamingParserState.CodeContent;

        _logger?.LogDebug(
            "[INFO] Code block started: BlockId={BlockId}, Language={Language}, Path={Path}",
            _currentBlock.Id, _currentBlock.Language, _currentBlock.TargetFilePath);

        BlockStarted?.Invoke(this, new CodeBlockStartedEventArgs
        {
            Block = _currentBlock,
            MessageId = _messageId
        });
    }

    private void ParseFenceLine(string fenceLine)
    {
        if (_currentBlock == null || string.IsNullOrEmpty(fenceLine))
            return;

        string? language = null;
        string? path = null;

        // Check for colon separator (lang:path format)
        var colonIndex = fenceLine.IndexOf(':');
        if (colonIndex > 0)
        {
            language = fenceLine[..colonIndex].Trim();
            var pathPart = fenceLine[(colonIndex + 1)..].Trim();

            // Handle quoted paths
            if (pathPart.StartsWith('"') && pathPart.EndsWith('"') && pathPart.Length > 2)
            {
                path = pathPart[1..^1];
            }
            else if (pathPart.StartsWith('\'') && pathPart.EndsWith('\'') && pathPart.Length > 2)
            {
                path = pathPart[1..^1];
            }
            else
            {
                path = pathPart;
            }
        }
        else
        {
            // Just language, no path
            language = fenceLine;
        }

        // Normalize language using ILanguageDetectionService
        if (!string.IsNullOrEmpty(language))
        {
            var (normalizedLang, displayLang) = _languageService.DetectLanguage(language, "", path);
            _currentBlock.Language = normalizedLang;
            _currentBlock.DisplayLanguage = displayLang;
        }

        _currentBlock.TargetFilePath = path;
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CODE CONTENT STATE                                                       │
    // └─────────────────────────────────────────────────────────────────────────┘

    private void ProcessCodeContentState(char ch)
    {
        // Check for potential closing fence (must be at line start)
        if (_atLineStart && (ch == BacktickChar || ch == TildeChar) && ch == _fenceChar)
        {
            _pendingFenceChars = 1;
            _state = StreamingParserState.FenceClosing;
            return;
        }

        // Normal code content
        AppendToCurrentBlock(ch);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ FENCE CLOSING STATE                                                      │
    // └─────────────────────────────────────────────────────────────────────────┘

    private void ProcessFenceClosingState(char ch)
    {
        if (ch == _fenceChar)
        {
            _pendingFenceChars++;
        }
        else if (_pendingFenceChars >= _currentFenceLength)
        {
            // We have a matching closing fence
            if (ch == '\n' || ch == '\r')
            {
                CompleteCurrentBlock(truncated: false);
            }
            else
            {
                // Not a valid closing fence (has trailing chars)
                // Add the fence chars as content and continue
                AppendFenceCharsAsContent();
                _state = StreamingParserState.CodeContent;
                AppendToCurrentBlock(ch);
            }
            _pendingFenceChars = 0;
        }
        else
        {
            // Not enough fence chars, add as content
            AppendFenceCharsAsContent();
            _state = StreamingParserState.CodeContent;
            if (ch != '\n')
            {
                AppendToCurrentBlock(ch);
            }
            _pendingFenceChars = 0;
        }
    }

    private void AppendFenceCharsAsContent()
    {
        for (int i = 0; i < _pendingFenceChars; i++)
        {
            AppendToCurrentBlock(_fenceChar);
        }
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ HELPER METHODS                                                           │
    // └─────────────────────────────────────────────────────────────────────────┘

    private void AppendToCurrentBlock(char ch)
    {
        if (_currentBlock == null) return;

        _currentBlock.AppendContent(ch);

        ContentAdded?.Invoke(this, new CodeBlockContentEventArgs
        {
            Content = ch.ToString(),
            Block = _currentBlock
        });
    }

    private void CompleteCurrentBlock(bool truncated)
    {
        if (_currentBlock == null) return;

        var duration = DateTime.UtcNow - _currentBlockStartTime;
        var block = _currentBlock.ToCodeBlock(_position);

        // Classify the block using IBlockClassificationService
        var surroundingText = GetSurroundingText(block.SourceRange.Start);
        block = block.With(
            blockType: _classificationService.ClassifyBlock(
                block.Content, block.Language, surroundingText));

        _completedBlocks.Add(block);

        _logger?.LogDebug(
            "[INFO] Code block completed: BlockId={BlockId}, Type={Type}, Lines={Lines}, Duration={Duration}ms, Truncated={Truncated}",
            block.Id, block.BlockType, block.LineCount, duration.TotalMilliseconds, truncated);

        BlockCompleted?.Invoke(this, new CodeBlockCompletedEventArgs
        {
            Block = block,
            MessageId = _messageId,
            Duration = duration,
            WasTruncated = truncated
        });

        _currentBlock = null;
        _state = StreamingParserState.Text;
    }

    private void ResetFenceDetection()
    {
        _pendingFenceChars = 0;
    }

    private string GetSurroundingText(int position)
    {
        var bufferStr = _buffer.ToString();
        var contextWindow = 300;
        var start = Math.Max(0, position - contextWindow);
        var end = Math.Min(bufferStr.Length, position + 100);
        return start < end ? bufferStr[start..end] : string.Empty;
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ IDISPOSABLE                                                              │
    // └─────────────────────────────────────────────────────────────────────────┘

    public void Dispose()
    {
        _buffer.Clear();
        _fenceLineBuffer.Clear();
        _completedBlocks.Clear();
        _currentBlock = null;
    }
}
