namespace AIntern.Services.Tests;

using AIntern.Core.Models;
using AIntern.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CodeBlockParserService"/> (v0.4.1b).
/// </summary>
public class CodeBlockParserServiceTests
{
    private readonly CodeBlockParserService _service;
    private readonly Mock<ILogger<CodeBlockParserService>> _mockLogger = new();

    public CodeBlockParserServiceTests()
    {
        _service = new CodeBlockParserService(_mockLogger.Object);
    }

    #region ParseMessage Tests

    [Fact]
    public void ParseMessage_NullContent_ReturnsEmpty()
    {
        var result = _service.ParseMessage(null!, Guid.NewGuid());
        Assert.Empty(result);
    }

    [Fact]
    public void ParseMessage_NoCodeBlocks_ReturnsEmpty()
    {
        var content = "Just some text without code blocks.";
        var result = _service.ParseMessage(content, Guid.NewGuid());
        Assert.Empty(result);
    }

    [Fact]
    public void ParseMessage_StandardFence_ExtractsCorrectly()
    {
        var content = """
            Here's the code:

            ```csharp
            public class Foo { }
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal("csharp", result[0].Language);
        Assert.Equal("C#", result[0].DisplayLanguage);
        Assert.Contains("public class Foo", result[0].Content);
    }

    [Fact]
    public void ParseMessage_FenceWithPath_ExtractsPath()
    {
        var content = """
            ```csharp:src/Models/User.cs
            public class User { }
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal("src/Models/User.cs", result[0].TargetFilePath);
        // BlockType will be Snippet since no namespace/using present
    }

    [Fact]
    public void ParseMessage_PathInComment_ExtractsPath()
    {
        var content = """
            ```csharp
            // File: src/Interfaces/IUser.cs
            public interface IUser { }
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal("src/Interfaces/IUser.cs", result[0].TargetFilePath);
        Assert.DoesNotContain("// File:", result[0].Content);
    }

    [Fact]
    public void ParseMessage_MultipleBlocks_ExtractsAll()
    {
        var content = """
            First block:

            ```csharp
            class A { }
            ```

            Second block:

            ```python
            def hello():
                pass
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].SequenceNumber);
        Assert.Equal(1, result[1].SequenceNumber);
        Assert.Equal("csharp", result[0].Language);
        Assert.Equal("python", result[1].Language);
    }

    [Fact]
    public void ParseMessage_EmptyCodeBlock_Skipped()
    {
        var content = """
            ```csharp
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());
        Assert.Empty(result);
    }

    [Fact]
    public void ParseMessage_NoLanguageSpec_DetectsFromPath()
    {
        var content = """
            ```
            // File: test.py
            def foo(): pass
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal("python", result[0].Language);
    }

    [Fact]
    public void ParseMessage_BashCommand_ClassifiesAsCommand()
    {
        var content = """
            ```bash
            npm install
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal(CodeBlockType.Command, result[0].BlockType);
    }

    [Fact]
    public void ParseMessage_JsonConfig_ClassifiesAsConfig()
    {
        var content = """
            ```json
            { "name": "test" }
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal(CodeBlockType.Config, result[0].BlockType);
    }

    #endregion

    #region ContainsCodeBlocks Tests

    [Fact]
    public void ContainsCodeBlocks_WithBlocks_ReturnsTrue()
    {
        var content = "```csharp\nclass A {}\n```";
        Assert.True(_service.ContainsCodeBlocks(content));
    }

    [Fact]
    public void ContainsCodeBlocks_NoBlocks_ReturnsFalse()
    {
        var content = "Just text";
        Assert.False(_service.ContainsCodeBlocks(content));
    }

    #endregion

    #region CountCodeBlocks Tests

    [Fact]
    public void CountCodeBlocks_ReturnsCorrectCount()
    {
        var content = """
            ```a
            code1
            ```
            text
            ```b
            code2
            ```
            more
            ```c
            code3
            ```
            """;

        Assert.Equal(3, _service.CountCodeBlocks(content));
    }

    #endregion

    #region CreateProposal Tests

    [Fact]
    public void CreateProposal_CreatesProposalWithBlocks()
    {
        var messageId = Guid.NewGuid();
        var content = """
            ```csharp:src/Test.cs
            public class Test { }
            ```
            """;

        var proposal = _service.CreateProposal(content, messageId);

        Assert.NotEqual(Guid.Empty, proposal.Id);
        Assert.Equal(messageId, proposal.MessageId);
        Assert.Single(proposal.CodeBlocks);
        Assert.Equal(ProposalStatus.Pending, proposal.Status);
    }

    #endregion

    #region SourceRange Tests

    [Fact]
    public void ParseMessage_SetsSourceRange()
    {
        var content = "Some prefix```csharp\ncode\n```suffix";
        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.True(result[0].SourceRange.Start >= 0);
        Assert.True(result[0].SourceRange.End > result[0].SourceRange.Start);
    }

    #endregion

    #region Confidence Score Tests

    [Fact]
    public void ParseMessage_ExplicitLangAndPath_HighConfidence()
    {
        var content = """
            ```csharp:src/Test.cs
            public class Test { }
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal(1.0f, result[0].ConfidenceScore);
    }

    [Fact]
    public void ParseMessage_NoPath_ReducedConfidence()
    {
        var content = """
            ```csharp
            public class Test { }
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal(0.7f, result[0].ConfidenceScore);
    }

    #endregion

    #region Path Normalization Tests

    [Fact]
    public void ParseMessage_BackslashPath_Normalized()
    {
        var content = """
            ```csharp:src\Models\User.cs
            class User { }
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal("src/Models/User.cs", result[0].TargetFilePath);
    }

    [Fact]
    public void ParseMessage_HashCommentPath_Extracts()
    {
        var content = """
            ```python
            # File: src/main.py
            def main(): pass
            ```
            """;

        var result = _service.ParseMessage(content, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal("src/main.py", result[0].TargetFilePath);
    }

    #endregion
}
