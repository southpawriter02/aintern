using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE OPERATION TESTS (v0.4.4a)                                           │
// │ Unit tests for FileOperation model.                                      │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class FileOperationTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Constructor / Default Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var op = new FileOperation();

        Assert.NotEqual(Guid.Empty, op.Id);
        Assert.Equal(string.Empty, op.Path);
        Assert.True(op.IsSelected);
        Assert.Equal(FileOperationStatus.Pending, op.Status);
        Assert.Equal(0, op.Order);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Path Component Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("src/Models/User.cs", "User.cs")]
    [InlineData("test.txt", "test.txt")]
    [InlineData("deep/nested/path/file.ts", "file.ts")]
    public void FileName_ExtractsCorrectly(string path, string expected)
    {
        var op = new FileOperation { Path = path };
        Assert.Equal(expected, op.FileName);
    }

    [Theory]
    [InlineData("src/Models/User.cs", "src/Models")]
    [InlineData("test.txt", "")]
    [InlineData("deep/nested/path/file.ts", "deep/nested/path")]
    public void Directory_ExtractsCorrectly(string path, string expected)
    {
        var op = new FileOperation { Path = path };
        Assert.Equal(expected, op.Directory ?? string.Empty);
    }

    [Theory]
    [InlineData("src/Models/User.cs", ".cs")]
    [InlineData("README.md", ".md")]
    [InlineData("Makefile", "")]
    public void Extension_ExtractsCorrectly(string path, string expected)
    {
        var op = new FileOperation { Path = path };
        Assert.Equal(expected, op.Extension);
    }

    [Theory]
    [InlineData("src/Models/User.cs", 2)]
    [InlineData("test.txt", 0)]
    [InlineData("a/b/c/d/e.txt", 4)]
    public void PathDepth_CalculatesCorrectly(string path, int expected)
    {
        var op = new FileOperation { Path = path };
        Assert.Equal(expected, op.PathDepth);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Content Analysis Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ContentSizeBytes_CalculatesCorrectly()
    {
        var op = new FileOperation { Content = "Hello, World!" };
        Assert.Equal(13, op.ContentSizeBytes);
    }

    [Fact]
    public void ContentSizeBytes_ReturnsZeroWhenNull()
    {
        var op = new FileOperation { Content = null };
        Assert.Equal(0, op.ContentSizeBytes);
    }

    [Theory]
    [InlineData("Line1\nLine2\nLine3", 3)]
    [InlineData("Single line", 1)]
    [InlineData("", 1)]
    public void LineCount_CalculatesCorrectly(string content, int expected)
    {
        var op = new FileOperation { Content = content };
        Assert.Equal(expected, op.LineCount);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("content", false)]
    public void IsContentEmpty_ReturnsCorrectValue(string? content, bool expected)
    {
        var op = new FileOperation { Content = content };
        Assert.Equal(expected, op.IsContentEmpty);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // State Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(FileOperationStatus.Pending, FileOperationType.Create, true)]
    [InlineData(FileOperationStatus.Applied, FileOperationType.Create, false)]
    [InlineData(FileOperationStatus.Pending, FileOperationType.Unknown, false)]
    public void CanApply_ReturnsCorrectValue(FileOperationStatus status, FileOperationType type, bool expected)
    {
        var op = new FileOperation { Status = status, Type = type };
        Assert.Equal(expected, op.CanApply);
    }

    [Theory]
    [InlineData(FileOperationStatus.Applied, true)]
    [InlineData(FileOperationStatus.Failed, true)]
    [InlineData(FileOperationStatus.Skipped, true)]
    [InlineData(FileOperationStatus.Pending, false)]
    [InlineData(FileOperationStatus.InProgress, false)]
    public void IsCompleted_ReturnsCorrectValue(FileOperationStatus status, bool expected)
    {
        var op = new FileOperation { Status = status };
        Assert.Equal(expected, op.IsCompleted);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Display Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(FileOperationType.Create, "Create")]
    [InlineData(FileOperationType.Modify, "Modify")]
    [InlineData(FileOperationType.Delete, "Delete")]
    [InlineData(FileOperationType.Rename, "Rename")]
    [InlineData(FileOperationType.CreateDirectory, "Create Directory")]
    public void OperationDisplayText_ReturnsCorrectValue(FileOperationType type, string expected)
    {
        var op = new FileOperation { Type = type };
        Assert.Equal(expected, op.OperationDisplayText);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Method Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FromCodeBlock_CreatesCorrectOperation()
    {
        var block = new CodeBlock
        {
            Id = Guid.NewGuid(),
            Content = "public class Test { }",
            Language = "csharp",
            DisplayLanguage = "C#",
            TargetFilePath = "src/Test.cs"
        };

        var op = FileOperation.FromCodeBlock(block, 5);

        Assert.Equal("src/Test.cs", op.Path);
        Assert.Equal(FileOperationType.Create, op.Type);
        Assert.Equal(block.Content, op.Content);
        Assert.Equal(block.Id, op.CodeBlockId);
        Assert.Equal("csharp", op.Language);
        Assert.Equal("C#", op.DisplayLanguage);
        Assert.Equal(5, op.Order);
    }

    [Fact]
    public void CreateDirectory_CreatesCorrectOperation()
    {
        var op = FileOperation.CreateDirectory("src/Models", 1);

        Assert.Equal("src/Models", op.Path);
        Assert.Equal(FileOperationType.CreateDirectory, op.Type);
        Assert.Equal(1, op.Order);
    }

    [Fact]
    public void Delete_CreatesCorrectOperation()
    {
        var op = FileOperation.Delete("old/file.txt", 2);

        Assert.Equal("old/file.txt", op.Path);
        Assert.Equal(FileOperationType.Delete, op.Type);
        Assert.Equal(2, op.Order);
    }

    [Fact]
    public void Rename_CreatesCorrectOperation()
    {
        var op = FileOperation.Rename("old.txt", "new.txt", 3);

        Assert.Equal("old.txt", op.Path);
        Assert.Equal("new.txt", op.NewPath);
        Assert.Equal(FileOperationType.Rename, op.Type);
        Assert.Equal(3, op.Order);
    }
}
