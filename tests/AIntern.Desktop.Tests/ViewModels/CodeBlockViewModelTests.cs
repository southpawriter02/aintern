namespace AIntern.Desktop.Tests.ViewModels;

using CommunityToolkit.Mvvm.Messaging;
using Xunit;
using AIntern.Core.Models;
using AIntern.Desktop.Messages;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Unit tests for CodeBlockViewModel (v0.4.1g).
/// </summary>
[Collection("Sequential")]
public class CodeBlockViewModelTests : IDisposable
{
    public CodeBlockViewModelTests()
    {
        // Reset messenger between tests
        WeakReferenceMessenger.Default.Reset();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Reset();
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMPUTED PROPERTIES TESTS                                                │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData(CodeBlockType.CompleteFile, true, true)]
    [InlineData(CodeBlockType.Snippet, true, true)]
    [InlineData(CodeBlockType.Config, true, true)]
    [InlineData(CodeBlockType.Example, true, false)]
    [InlineData(CodeBlockType.Command, true, false)]
    [InlineData(CodeBlockType.CompleteFile, false, false)]
    public void IsApplicable_ReturnsCorrectValue(
        CodeBlockType type, bool hasPath, bool expected)
    {
        var vm = new CodeBlockViewModel
        {
            BlockType = type,
            TargetFilePath = hasPath ? "test.cs" : null,
            Status = CodeBlockStatus.Pending
        };

        Assert.Equal(expected, vm.IsApplicable);
    }

    [Fact]
    public void LineCount_CalculatesCorrectly()
    {
        var vm = new CodeBlockViewModel
        {
            Content = "line1\nline2\nline3"
        };

        Assert.Equal(3, vm.LineCount);
    }

    [Fact]
    public void LineCount_EmptyContent_ReturnsZero()
    {
        var vm = new CodeBlockViewModel { Content = "" };
        Assert.Equal(0, vm.LineCount);
    }

    [Fact]
    public void FileName_ExtractsFromPath()
    {
        var vm = new CodeBlockViewModel
        {
            TargetFilePath = "src/Models/User.cs"
        };

        Assert.Equal("User.cs", vm.FileName);
    }

    [Fact]
    public void FileName_NullWhenNoPath()
    {
        var vm = new CodeBlockViewModel { TargetFilePath = null };
        Assert.Null(vm.FileName);
    }

    [Theory]
    [InlineData(CodeBlockStatus.Applied, "Applied")]
    [InlineData(CodeBlockStatus.Rejected, "Rejected")]
    [InlineData(CodeBlockStatus.Pending, "")]
    [InlineData(CodeBlockStatus.Reviewing, "Reviewing")]
    [InlineData(CodeBlockStatus.Applying, "Applying...")]
    public void StatusText_ReturnsCorrectValue(
        CodeBlockStatus status, string expected)
    {
        var vm = new CodeBlockViewModel { Status = status };
        Assert.Equal(expected, vm.StatusText);
    }

    [Fact]
    public void ShowApplyButton_TrueWhenApplicableAndPending()
    {
        var vm = new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "test.cs",
            Status = CodeBlockStatus.Pending,
            IsStreaming = false
        };

        Assert.True(vm.ShowApplyButton);
    }

    [Fact]
    public void ShowApplyButton_FalseWhenStreaming()
    {
        var vm = new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "test.cs",
            Status = CodeBlockStatus.Pending,
            IsStreaming = true
        };

        Assert.False(vm.ShowApplyButton);
    }

    [Fact]
    public void ConfidencePercent_FormatsCorrectly()
    {
        var vm = new CodeBlockViewModel { ConfidenceScore = 0.85f };
        Assert.Equal("85%", vm.ConfidencePercent);
    }

    [Theory]
    [InlineData(0.5f, true)]
    [InlineData(0.7f, false)]
    [InlineData(0.9f, false)]
    public void IsLowConfidence_ReturnsCorrectValue(float score, bool expected)
    {
        var vm = new CodeBlockViewModel { ConfidenceScore = score };
        Assert.Equal(expected, vm.IsLowConfidence);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMMANDS TESTS                                                           │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void CopyToClipboard_SendsMessage()
    {
        CopyToClipboardRequestMessage? received = null;
        WeakReferenceMessenger.Default.Register<CopyToClipboardRequestMessage>(
            this, (r, m) => received = m);

        var vm = new CodeBlockViewModel { Content = "test content" };
        vm.CopyToClipboardCommand.Execute(null);

        Assert.NotNull(received);
        Assert.Equal("test content", received.Value);
    }

    [Fact]
    public void ShowDiff_SendsMessageAndSetsReviewing()
    {
        ShowDiffRequestMessage? received = null;
        WeakReferenceMessenger.Default.Register<ShowDiffRequestMessage>(
            this, (r, m) => received = m);

        var vm = new CodeBlockViewModel
        {
            TargetFilePath = "test.cs",
            Status = CodeBlockStatus.Pending
        };

        vm.ShowDiffCommand.Execute(null);

        // Status change is the critical behavior to test
        Assert.Equal(CodeBlockStatus.Reviewing, vm.Status);
        // Message may be null in parallel test execution due to timing, but should generally work
        Assert.NotNull(received);
        Assert.Same(vm, received.Value);
    }

    [Fact]
    public void Reject_ChangesStatusAndSendsMessage()
    {
        CodeBlockStatusChangedMessage? received = null;
        WeakReferenceMessenger.Default.Register<CodeBlockStatusChangedMessage>(
            this, (r, m) => received = m);

        var vm = new CodeBlockViewModel
        {
            Status = CodeBlockStatus.Pending
        };

        vm.RejectCommand.Execute(null);

        Assert.Equal(CodeBlockStatus.Rejected, vm.Status);
        Assert.NotNull(received);
        Assert.Equal(CodeBlockStatus.Pending, received.OldStatus);
        Assert.Equal(CodeBlockStatus.Rejected, received.NewStatus);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STREAMING TESTS                                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void AppendContent_AddsToExistingContent()
    {
        var vm = new CodeBlockViewModel { Content = "Hello" };

        vm.AppendContent(" World");

        Assert.Equal("Hello World", vm.Content);
    }

    [Fact]
    public void CompleteStreaming_UpdatesAllProperties()
    {
        var vm = new CodeBlockViewModel
        {
            IsStreaming = true,
            Content = "partial"
        };

        var finalBlock = new CodeBlock
        {
            Content = "complete content",
            Language = "csharp",
            DisplayLanguage = "C#",
            BlockType = CodeBlockType.CompleteFile,
            ConfidenceScore = 0.95f
        };

        vm.CompleteStreaming(finalBlock);

        Assert.False(vm.IsStreaming);
        Assert.Equal("complete content", vm.Content);
        Assert.Equal("csharp", vm.Language);
        Assert.Equal("C#", vm.DisplayLanguage);
        Assert.Equal(CodeBlockType.CompleteFile, vm.BlockType);
        Assert.Equal(0.95f, vm.ConfidenceScore);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STATUS UPDATES TESTS                                                     │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void MarkAsApplied_SetsStatusAndSendsMessage()
    {
        CodeBlockStatusChangedMessage? received = null;
        WeakReferenceMessenger.Default.Register<CodeBlockStatusChangedMessage>(
            this, (r, m) => received = m);

        var vm = new CodeBlockViewModel { Status = CodeBlockStatus.Applying };

        vm.MarkAsApplied();

        Assert.Equal(CodeBlockStatus.Applied, vm.Status);
        Assert.Null(vm.ErrorMessage);
        Assert.NotNull(received);
    }

    [Fact]
    public void MarkAsError_SetsStatusAndErrorMessage()
    {
        var vm = new CodeBlockViewModel { Status = CodeBlockStatus.Applying };

        vm.MarkAsError("File not found");

        Assert.Equal(CodeBlockStatus.Error, vm.Status);
        Assert.Equal("File not found", vm.ErrorMessage);
    }

    [Fact]
    public void MarkAsConflict_SetsStatusAndReason()
    {
        var vm = new CodeBlockViewModel { Status = CodeBlockStatus.Pending };

        vm.MarkAsConflict("File was modified");

        Assert.Equal(CodeBlockStatus.Conflict, vm.Status);
        Assert.Equal("File was modified", vm.ErrorMessage);
    }

    [Fact]
    public void ResetToPending_ClearsErrorAndResetsStatus()
    {
        var vm = new CodeBlockViewModel
        {
            Status = CodeBlockStatus.Error,
            ErrorMessage = "Some error"
        };

        vm.ResetToPending();

        Assert.Equal(CodeBlockStatus.Pending, vm.Status);
        Assert.Null(vm.ErrorMessage);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ MODEL CONVERSION TESTS                                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FromModel_CopiesAllProperties()
    {
        var block = new CodeBlock
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            SequenceNumber = 2,
            Content = "test",
            Language = "csharp",
            DisplayLanguage = "C#",
            TargetFilePath = "test.cs",
            BlockType = CodeBlockType.Snippet,
            Status = CodeBlockStatus.Applied,
            ConfidenceScore = 0.8f
        };

        var vm = new CodeBlockViewModel(block);

        Assert.Equal(block.Id, vm.Id);
        Assert.Equal(block.MessageId, vm.MessageId);
        Assert.Equal(block.SequenceNumber, vm.SequenceNumber);
        Assert.Equal(block.Content, vm.Content);
        Assert.Equal(block.Language, vm.Language);
        Assert.Equal(block.DisplayLanguage, vm.DisplayLanguage);
        Assert.Equal(block.TargetFilePath, vm.TargetFilePath);
        Assert.Equal(block.BlockType, vm.BlockType);
        Assert.Equal(block.Status, vm.Status);
        Assert.Equal(block.ConfidenceScore, vm.ConfidenceScore);
    }

    [Fact]
    public void ToModel_CreatesCorrectModel()
    {
        var vm = new CodeBlockViewModel
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Content = "test",
            Language = "python",
            BlockType = CodeBlockType.Command
        };

        var model = vm.ToModel();

        Assert.Equal(vm.Id, model.Id);
        Assert.Equal(vm.Content, model.Content);
        Assert.Equal(vm.Language, model.Language);
        Assert.Equal(vm.BlockType, model.BlockType);
    }

    [Fact]
    public void FromPartialBlock_SetsStreamingTrue()
    {
        var partial = new PartialCodeBlock
        {
            MessageId = Guid.NewGuid(),
            SequenceNumber = 1
        };
        partial.AppendContent("test");

        var vm = new CodeBlockViewModel(partial);

        Assert.True(vm.IsStreaming);
        Assert.Equal("test", vm.Content);
    }
}
