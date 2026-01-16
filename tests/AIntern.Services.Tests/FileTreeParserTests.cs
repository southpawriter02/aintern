using Xunit;
using AIntern.Core.Models;
using AIntern.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIntern.Services.Tests;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PARSER TESTS (v0.4.4b)                                         │
// │ Unit tests for FileTreeParser service.                                   │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class FileTreeParserTests
{
    private readonly FileTreeParser _parser;
    private readonly Mock<ILogger<FileTreeParser>> _loggerMock;

    public FileTreeParserTests()
    {
        _loggerMock = new Mock<ILogger<FileTreeParser>>();
        _parser = new FileTreeParser(
            FileTreeParserOptions.Default,
            _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ContainsFileTree Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ContainsFileTree_ReturnsFalse_WhenContentIsEmpty()
    {
        Assert.False(_parser.ContainsFileTree(""));
        Assert.False(_parser.ContainsFileTree(null!));
    }

    [Fact]
    public void ContainsFileTree_ReturnsTrue_WhenTreeBlockPresent()
    {
        var content = @"Here's the project structure:
```
src/
├── Models/
│   └── User.cs
```";
        Assert.True(_parser.ContainsFileTree(content));
    }

    [Fact]
    public void ContainsFileTree_ReturnsFalse_WhenNoIndicator()
    {
        // Without indicator (RequireStructureIndicator is true by default)
        var content = @"```
src/
├── Models/
```";
        Assert.False(_parser.ContainsFileTree(content));
    }

    [Fact]
    public void ContainsFileTree_ReturnsTrue_WithDifferentIndicators()
    {
        var indicators = new[]
        {
            "file structure",
            "folder structure",
            "directory structure",
            "create these files"
        };

        foreach (var indicator in indicators)
        {
            var content = $@"Here's the {indicator}:
```
src/
├── test.cs
```";
            Assert.True(_parser.ContainsFileTree(content));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ParseAsciiTree Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseAsciiTree_ReturnsEmpty_WhenContentIsEmpty()
    {
        var result = _parser.ParseAsciiTree("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAsciiTree_ParsesStandardTree()
    {
        var tree = @"src/
├── Models/
│   └── User.cs
└── Services/
    └── UserService.cs";

        var paths = _parser.ParseAsciiTree(tree);

        Assert.Equal(2, paths.Count);
        // Parser extracts relative paths under root
        Assert.Contains(paths, p => p.EndsWith("User.cs"));
        Assert.Contains(paths, p => p.EndsWith("UserService.cs"));
    }

    [Fact]
    public void ParseAsciiTree_ParsesDeepNesting()
    {
        var tree = @"src/
├── Controllers/
│   ├── v1/
│   │   └── UsersController.cs
│   └── v2/
│       └── UsersController.cs";

        var paths = _parser.ParseAsciiTree(tree);

        Assert.Equal(2, paths.Count);
        // Parser extracts nested paths
        Assert.Contains(paths, p => p.Contains("v1") && p.EndsWith("UsersController.cs"));
        Assert.Contains(paths, p => p.Contains("v2") && p.EndsWith("UsersController.cs"));
    }

    [Fact]
    public void ParseAsciiTree_ExcludesDirectories()
    {
        var tree = @"src/
├── Models/
├── Services/
└── file.cs";

        var paths = _parser.ParseAsciiTree(tree);

        // Only file.cs should be included, not directories
        Assert.Single(paths);
        Assert.Contains("file.cs", paths[0]);
    }

    [Fact]
    public void ParseAsciiTree_HandlesTrailingSlashes()
    {
        var tree = @"project/
├── docs/
├── src/
│   └── main.py
└── README.md";

        var paths = _parser.ParseAsciiTree(tree);

        Assert.Equal(2, paths.Count);
        Assert.Contains(paths, p => p.EndsWith("main.py"));
        Assert.Contains(paths, p => p.EndsWith("README.md"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ParseAsciiTreeStructured Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseAsciiTreeStructured_ReturnsNull_WhenEmpty()
    {
        Assert.Null(_parser.ParseAsciiTreeStructured(""));
    }

    [Fact]
    public void ParseAsciiTreeStructured_BuildsCorrectHierarchy()
    {
        var tree = @"src/
├── Models/
│   └── User.cs
└── Services/";

        var root = _parser.ParseAsciiTreeStructured(tree);

        Assert.NotNull(root);
        Assert.True(root.IsDirectory);
        // Root is a virtual node, children are parsed
        Assert.True(root.Children.Count >= 1);
    }

    [Fact]
    public void ParseAsciiTreeStructured_SetsParentCorrectly()
    {
        var tree = @"Models/
└── User.cs";

        var root = _parser.ParseAsciiTreeStructured(tree);

        Assert.NotNull(root);
        // Root node is virtual, first child is Models
        Assert.True(root.Children.Count >= 1);
        var firstChild = root.Children[0];
        Assert.Equal(root, firstChild.Parent);
    }

    [Fact]
    public void ParseAsciiTreeStructured_GetAllFilePaths_Works()
    {
        var tree = @"src/
├── a.cs
└── b.cs";

        var root = _parser.ParseAsciiTreeStructured(tree);
        var filePaths = root!.GetAllFilePaths().ToList();

        // Should find at least the file paths
        Assert.True(filePaths.Count >= 2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FindCommonRoot Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FindCommonRoot_ReturnsEmpty_WhenNoPaths()
    {
        var result = _parser.FindCommonRoot(Array.Empty<string>());
        Assert.Empty(result);
    }

    [Fact]
    public void FindCommonRoot_ReturnsSingleFileDirectory()
    {
        var result = _parser.FindCommonRoot(new[] { "src/Models/User.cs" });
        Assert.Equal("src/Models", result);
    }

    [Fact]
    public void FindCommonRoot_FindsCommonPrefix()
    {
        var paths = new[]
        {
            "src/Models/User.cs",
            "src/Models/Product.cs",
            "src/Services/UserService.cs"
        };

        var result = _parser.FindCommonRoot(paths);
        Assert.Equal("src", result);
    }

    [Fact]
    public void FindCommonRoot_ReturnsEmpty_WhenNoCommonPrefix()
    {
        var paths = new[]
        {
            "src/User.cs",
            "tests/UserTests.cs"
        };

        var result = _parser.FindCommonRoot(paths);
        Assert.Empty(result);
    }

    [Fact]
    public void FindCommonRoot_HandlesDeepNesting()
    {
        var paths = new[]
        {
            "project/src/app/models/User.cs",
            "project/src/app/models/Product.cs",
            "project/src/app/services/UserService.cs"
        };

        var result = _parser.FindCommonRoot(paths);
        Assert.Equal("project/src/app", result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsValidPath Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("src/User.cs", true)]
    [InlineData("src/Models/User.cs", true)]
    [InlineData("file.txt", true)]
    [InlineData("my-project/file.cs", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("../etc/passwd", false)]
    [InlineData("/absolute/path.cs", false)]
    [InlineData("path with spaces.cs", false)]
    public void IsValidPath_ValidatesCorrectly(string? path, bool expected)
    {
        Assert.Equal(expected, _parser.IsValidPath(path!));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ExtractDescription Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractDescription_ReturnsNull_WhenEmpty()
    {
        Assert.Null(_parser.ExtractDescription(""));
    }

    [Fact]
    public void ExtractDescription_FindsDescriptionNearIndicator()
    {
        var content = @"Here's the project structure for your authentication system:
```
src/
├── auth.cs
```";

        var description = _parser.ExtractDescription(content);

        Assert.NotNull(description);
        Assert.Contains("project structure", description);
    }

    [Fact]
    public void ExtractDescription_CleansMarkdownFormatting()
    {
        var content = @"## Here's the file structure
```
src/
```";

        var description = _parser.ExtractDescription(content);

        Assert.NotNull(description);
        // Should not start with # or have markdown symbols
        Assert.DoesNotContain("##", description);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TryParseProposal Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TryParseProposal_ReturnsFalse_WhenContentEmpty()
    {
        var result = _parser.TryParseProposal(
            "",
            Guid.NewGuid(),
            Array.Empty<CodeBlock>(),
            out var proposal,
            out var reason);

        Assert.False(result);
        Assert.Null(proposal);
        Assert.Equal("Content is empty", reason);
    }

    [Fact]
    public void TryParseProposal_ReturnsFalse_WhenNoCodeBlocks()
    {
        var result = _parser.TryParseProposal(
            "some content",
            Guid.NewGuid(),
            Array.Empty<CodeBlock>(),
            out var proposal,
            out var reason);

        Assert.False(result);
        Assert.Contains("No code blocks", reason);
    }

    [Fact]
    public void TryParseProposal_ReturnsFalse_WhenTooFewBlocksWithPaths()
    {
        var blocks = new[]
        {
            new CodeBlock
            {
                Content = "test",
                BlockType = CodeBlockType.CompleteFile,
                TargetFilePath = "src/file.cs"
            }
        };

        var result = _parser.TryParseProposal(
            "some content",
            Guid.NewGuid(),
            blocks,
            out var proposal,
            out var reason);

        Assert.False(result);
        Assert.Contains("minimum", reason);
    }

    [Fact]
    public void TryParseProposal_ReturnsTrue_WithValidInput()
    {
        var blocks = new[]
        {
            new CodeBlock
            {
                Content = "public class User { }",
                Language = "csharp",
                BlockType = CodeBlockType.CompleteFile,
                TargetFilePath = "src/Models/User.cs"
            },
            new CodeBlock
            {
                Content = "public class Product { }",
                Language = "csharp",
                BlockType = CodeBlockType.CompleteFile,
                TargetFilePath = "src/Models/Product.cs"
            }
        };

        var messageId = Guid.NewGuid();
        var result = _parser.TryParseProposal(
            "Here's the project structure:\n```\nsrc/\n```",
            messageId,
            blocks,
            out var proposal,
            out var reason);

        Assert.True(result);
        Assert.NotNull(proposal);
        Assert.Equal(messageId, proposal.MessageId);
        Assert.Equal(2, proposal.Operations.Count);
        Assert.Equal("src/Models", proposal.RootPath);
    }

    [Fact]
    public void ParseProposal_SkipsInvalidPaths()
    {
        var blocks = new[]
        {
            new CodeBlock
            {
                Content = "test",
                BlockType = CodeBlockType.CompleteFile,
                TargetFilePath = "src/valid.cs"
            },
            new CodeBlock
            {
                Content = "test",
                BlockType = CodeBlockType.CompleteFile,
                TargetFilePath = "../invalid.cs"
            },
            new CodeBlock
            {
                Content = "test2",
                BlockType = CodeBlockType.CompleteFile,
                TargetFilePath = "src/valid2.cs"
            }
        };

        var proposal = _parser.ParseProposal(
            "Project structure here",
            Guid.NewGuid(),
            blocks);

        // Should have 2 operations (skipped invalid path)
        Assert.NotNull(proposal);
        Assert.Equal(2, proposal.Operations.Count);
    }
}
