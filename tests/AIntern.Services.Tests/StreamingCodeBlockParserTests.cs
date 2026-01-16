namespace AIntern.Services.Tests;

using Moq;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for StreamingCodeBlockParser (v0.4.1f).
/// </summary>
public class StreamingCodeBlockParserTests
{
    private readonly Mock<ILanguageDetectionService> _mockLanguageService;
    private readonly Mock<IBlockClassificationService> _mockClassificationService;
    private readonly StreamingCodeBlockParser _parser;

    public StreamingCodeBlockParserTests()
    {
        _mockLanguageService = new Mock<ILanguageDetectionService>();
        _mockClassificationService = new Mock<IBlockClassificationService>();

        SetupDefaultMocks();

        _parser = new StreamingCodeBlockParser(
            _mockLanguageService.Object,
            _mockClassificationService.Object);
    }

    private void SetupDefaultMocks()
    {
        _mockLanguageService
            .Setup(s => s.DetectLanguage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns<string, string, string?>((lang, _, _) => (lang.ToLower(), lang));

        _mockClassificationService
            .Setup(s => s.ClassifyBlock(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(CodeBlockType.CompleteFile);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ BASIC DETECTION                                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FeedToken_DetectsBlockStart_RaisesBlockStartedEvent()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockStartedEventArgs? eventArgs = null;
        _parser.BlockStarted += (s, e) => eventArgs = e;

        _parser.FeedToken("Here's code:\n```csharp\n");

        Assert.NotNull(eventArgs);
        Assert.Equal("csharp", eventArgs.Language);
        Assert.Equal(StreamingParserState.CodeContent, _parser.State);
    }

    [Fact]
    public void FeedToken_DetectsBlockEnd_RaisesBlockCompletedEvent()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockCompletedEventArgs? eventArgs = null;
        _parser.BlockCompleted += (s, e) => eventArgs = e;

        _parser.FeedToken("```csharp\npublic class Test { }\n```\n");

        Assert.NotNull(eventArgs);
        Assert.Equal("public class Test { }", eventArgs.Block.Content);
        Assert.False(eventArgs.WasTruncated);
    }

    [Fact]
    public void FeedToken_NoCodeBlock_StateRemainsText()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("Hello, this is just regular text with no code.");

        Assert.Equal(StreamingParserState.Text, _parser.State);
        Assert.Empty(_parser.GetCompletedBlocks());
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CONTENT ACCUMULATION                                                     │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FeedToken_AccumulatesContent_CharacterByCharacter()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("```csharp\n");

        // Feed character by character
        foreach (var ch in "public class Test { }")
        {
            _parser.FeedToken(ch.ToString());
        }

        var current = _parser.GetCurrentBlock();
        Assert.NotNull(current);
        Assert.Equal("public class Test { }", current.Content.ToString());
    }

    [Fact]
    public void FeedToken_RaisesContentAddedEvents()
    {
        _parser.Reset(Guid.NewGuid());

        var contentEvents = new List<string>();
        _parser.ContentAdded += (s, e) => contentEvents.Add(e.Content);

        _parser.FeedToken("```csharp\n");
        _parser.FeedToken("abc");

        Assert.Equal(3, contentEvents.Count);
        Assert.Equal("a", contentEvents[0]);
        Assert.Equal("b", contentEvents[1]);
        Assert.Equal("c", contentEvents[2]);
    }

    [Fact]
    public void GetCurrentBlockContent_ReturnsAccumulatedContent()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("```csharp\npublic class");

        var content = _parser.GetCurrentBlockContent();
        Assert.Equal("public class", content);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ FENCE LINE PARSING                                                       │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FeedToken_ParsesLanguageOnly()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockStartedEventArgs? eventArgs = null;
        _parser.BlockStarted += (s, e) => eventArgs = e;

        _parser.FeedToken("```typescript\ncode\n```\n");

        Assert.NotNull(eventArgs);
        Assert.Equal("typescript", eventArgs.Language);
        Assert.Null(eventArgs.TargetFilePath);
    }

    [Fact]
    public void FeedToken_ParsesLanguageAndPath()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockStartedEventArgs? eventArgs = null;
        _parser.BlockStarted += (s, e) => eventArgs = e;

        _parser.FeedToken("```csharp:src/Models/User.cs\ncode\n```\n");

        Assert.NotNull(eventArgs);
        Assert.Equal("csharp", eventArgs.Language);
        Assert.Equal("src/Models/User.cs", eventArgs.TargetFilePath);
    }

    [Fact]
    public void FeedToken_ParsesQuotedPath()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockStartedEventArgs? eventArgs = null;
        _parser.BlockStarted += (s, e) => eventArgs = e;

        _parser.FeedToken("```csharp:\"src/My Models/User.cs\"\ncode\n```\n");

        Assert.NotNull(eventArgs);
        Assert.Equal("src/My Models/User.cs", eventArgs.TargetFilePath);
    }

    [Fact]
    public void FeedToken_HandlesNoLanguage()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockStartedEventArgs? eventArgs = null;
        _parser.BlockStarted += (s, e) => eventArgs = e;

        _parser.FeedToken("```\nsome code\n```\n");

        Assert.NotNull(eventArgs);
        Assert.Null(eventArgs.Language);
    }

    [Fact]
    public void FeedToken_ParsesSingleQuotedPath()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockStartedEventArgs? eventArgs = null;
        _parser.BlockStarted += (s, e) => eventArgs = e;

        _parser.FeedToken("```python:'src/utils/helper.py'\ncode\n```\n");

        Assert.NotNull(eventArgs);
        Assert.Equal("src/utils/helper.py", eventArgs.TargetFilePath);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ MULTIPLE BLOCKS                                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FeedToken_HandlesMultipleBlocks()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("First:\n```js\ncode1\n```\n\nSecond:\n```py\ncode2\n```\n");

        var blocks = _parser.GetCompletedBlocks();

        Assert.Equal(2, blocks.Count);
        Assert.Equal(0, blocks[0].SequenceNumber);
        Assert.Equal(1, blocks[1].SequenceNumber);
        Assert.Equal("code1", blocks[0].Content);
        Assert.Equal("code2", blocks[1].Content);
    }

    [Fact]
    public void FeedToken_SequenceNumbersIncrease()
    {
        _parser.Reset(Guid.NewGuid());

        var sequences = new List<int>();
        _parser.BlockCompleted += (s, e) => sequences.Add(e.SequenceNumber);

        _parser.FeedToken("```a\n1\n```\n```b\n2\n```\n```c\n3\n```\n");

        Assert.Equal(new[] { 0, 1, 2 }, sequences);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EXTENDED FENCES                                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FeedToken_Handles4BacktickFence()
    {
        _parser.Reset(Guid.NewGuid());

        // 4-backtick fence should work and require 4 backticks to close
        _parser.FeedToken("````csharp\n```not closing```\n````\n");

        var blocks = _parser.GetCompletedBlocks();
        Assert.Single(blocks);
        Assert.Contains("```not closing```", blocks[0].Content);
    }

    [Fact]
    public void FeedToken_HandlesTildeFence()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("~~~python\nprint('hello')\n~~~\n");

        var blocks = _parser.GetCompletedBlocks();
        Assert.Single(blocks);
        Assert.Equal("python", blocks[0].Language);
        Assert.Equal("print('hello')", blocks[0].Content);
    }

    [Fact]
    public void FeedToken_RequiresMatchingFenceType()
    {
        _parser.Reset(Guid.NewGuid());

        // Start with backticks, ~~~ should not close it
        _parser.FeedToken("```csharp\ncode\n~~~\nmore code\n```\n");

        var blocks = _parser.GetCompletedBlocks();
        Assert.Single(blocks);
        Assert.Contains("~~~", blocks[0].Content);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EDGE CASES                                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FeedToken_IgnoresFenceNotAtLineStart()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("```csharp\nvar x = \"```\";\n```\n");

        var blocks = _parser.GetCompletedBlocks();
        Assert.Single(blocks);
        // The ``` inside string should not close the block
        Assert.Equal("var x = \"```\";", blocks[0].Content);
    }

    [Fact]
    public void Complete_FinalizesUnclosedBlock()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockCompletedEventArgs? eventArgs = null;
        _parser.BlockCompleted += (s, e) => eventArgs = e;

        _parser.FeedToken("```csharp\nunclosed code");
        _parser.Complete();

        Assert.NotNull(eventArgs);
        Assert.True(eventArgs.WasTruncated);
        Assert.Equal("unclosed code", eventArgs.Block.Content);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("```csharp\nsome code\n```\n");

        var newMessageId = Guid.NewGuid();
        _parser.Reset(newMessageId);

        Assert.Equal(StreamingParserState.Text, _parser.State);
        Assert.Equal(0, _parser.TotalCharactersProcessed);
        Assert.Empty(_parser.GetCompletedBlocks());
        Assert.Null(_parser.GetCurrentBlock());
        Assert.Equal(newMessageId, _parser.CurrentMessageId);
    }

    [Fact]
    public void FeedToken_HandlesEmptyToken()
    {
        _parser.Reset(Guid.NewGuid());

        // Should not throw
        _parser.FeedToken("");
        _parser.FeedToken(null!);

        Assert.Equal(StreamingParserState.Text, _parser.State);
    }

    [Fact]
    public void FeedToken_HandlesBacktickInCode()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("```js\nconst x = `template`;\n```\n");

        var blocks = _parser.GetCompletedBlocks();
        Assert.Single(blocks);
        Assert.Equal("const x = `template`;", blocks[0].Content);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STATE PROPERTIES                                                         │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void TotalCharactersProcessed_TracksCorrectly()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("Hello");
        Assert.Equal(5, _parser.TotalCharactersProcessed);

        _parser.FeedToken(" World");
        Assert.Equal(11, _parser.TotalCharactersProcessed);
    }

    [Fact]
    public void BlockCount_IncludesCurrentBlock()
    {
        _parser.Reset(Guid.NewGuid());

        Assert.Equal(0, _parser.BlockCount);

        _parser.FeedToken("```js\n");
        Assert.Equal(1, _parser.BlockCount);

        _parser.FeedToken("code\n```\n");
        Assert.Equal(1, _parser.BlockCount);

        _parser.FeedToken("```py\n");
        Assert.Equal(2, _parser.BlockCount);
    }

    [Fact]
    public void IsInsideCodeBlock_ReflectsState()
    {
        _parser.Reset(Guid.NewGuid());

        Assert.False(_parser.IsInsideCodeBlock);

        _parser.FeedToken("```js\n");
        Assert.True(_parser.IsInsideCodeBlock);

        _parser.FeedToken("code\n```\n");
        Assert.False(_parser.IsInsideCodeBlock);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CLASSIFICATION INTEGRATION                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void BlockCompleted_IncludesClassifiedType()
    {
        _mockClassificationService
            .Setup(s => s.ClassifyBlock(It.IsAny<string>(), "bash", It.IsAny<string>()))
            .Returns(CodeBlockType.Command);

        _parser.Reset(Guid.NewGuid());

        CodeBlockCompletedEventArgs? eventArgs = null;
        _parser.BlockCompleted += (s, e) => eventArgs = e;

        _parser.FeedToken("```bash\nnpm install\n```\n");

        Assert.NotNull(eventArgs);
        Assert.Equal(CodeBlockType.Command, eventArgs.Block.BlockType);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ BATCH PROCESSING                                                         │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FeedTokens_ProcessesMultipleTokens()
    {
        _parser.Reset(Guid.NewGuid());

        var tokens = new[] { "```", "csharp", "\n", "code", "\n```\n" };
        _parser.FeedTokens(tokens);

        var blocks = _parser.GetCompletedBlocks();
        Assert.Single(blocks);
        Assert.Equal("code", blocks[0].Content);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ SOURCE RANGE                                                             │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void CompletedBlock_HasCorrectSourceRange()
    {
        _parser.Reset(Guid.NewGuid());

        var prefix = "Some text before\n";
        _parser.FeedToken(prefix);
        _parser.FeedToken("```js\ncode\n```\n");

        var blocks = _parser.GetCompletedBlocks();
        Assert.Single(blocks);

        var range = blocks[0].SourceRange;
        Assert.Equal(prefix.Length, range.Start);
        Assert.True(range.End > range.Start);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ DURATION TRACKING                                                        │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void BlockCompleted_IncludesDuration()
    {
        _parser.Reset(Guid.NewGuid());

        CodeBlockCompletedEventArgs? eventArgs = null;
        _parser.BlockCompleted += (s, e) => eventArgs = e;

        _parser.FeedToken("```csharp\ncode\n```\n");

        Assert.NotNull(eventArgs);
        Assert.True(eventArgs.Duration >= TimeSpan.Zero);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EMPTY CODE BLOCK                                                         │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FeedToken_HandlesEmptyCodeBlock()
    {
        _parser.Reset(Guid.NewGuid());

        _parser.FeedToken("```csharp\n```\n");

        var blocks = _parser.GetCompletedBlocks();
        Assert.Single(blocks);
        Assert.Equal("", blocks[0].Content);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ DISPOSE                                                                  │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void Dispose_ClearsState()
    {
        _parser.Reset(Guid.NewGuid());
        _parser.FeedToken("```js\ncode\n```\n");

        _parser.Dispose();

        Assert.Empty(_parser.GetCompletedBlocks());
        Assert.Null(_parser.GetCurrentBlock());
    }
}
