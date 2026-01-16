namespace AIntern.Desktop.Tests.ViewModels;

using CommunityToolkit.Mvvm.Messaging;
using Xunit;
using AIntern.Core.Models;
using AIntern.Desktop.Messages;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Unit tests for CodeProposalViewModel (v0.4.1g).
/// </summary>
[Collection("Sequential")]
public class CodeProposalViewModelTests : IDisposable
{
    public CodeProposalViewModelTests()
    {
        WeakReferenceMessenger.Default.Reset();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Reset();
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STATISTICS TESTS                                                         │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void TotalCount_ReturnsCorrectCount()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel());
        vm.CodeBlocks.Add(new CodeBlockViewModel());

        Assert.Equal(2, vm.TotalCount);
    }

    [Fact]
    public void ApplicableCount_ReturnsCorrectCount()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "a.cs"
        });
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.Example
        });
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.Snippet,
            TargetFilePath = "b.cs"
        });

        Assert.Equal(2, vm.ApplicableCount);
    }

    [Fact]
    public void AppliedCount_ReturnsCorrectCount()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            Status = CodeBlockStatus.Applied
        });
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            Status = CodeBlockStatus.Pending
        });

        Assert.Equal(1, vm.AppliedCount);
    }

    [Fact]
    public void IsFullyApplied_TrueWhenAllApplicableApplied()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "a.cs",
            Status = CodeBlockStatus.Applied
        });
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.Example,
            Status = CodeBlockStatus.Pending
        });

        Assert.True(vm.IsFullyApplied);
    }

    [Fact]
    public void IsFullyApplied_FalseWhenNoApplicableBlocks()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.Example
        });

        Assert.False(vm.IsFullyApplied);
    }

    [Fact]
    public void IsPartiallyApplied_TrueWhenSomeApplied()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "a.cs",
            Status = CodeBlockStatus.Applied
        });
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.Snippet,
            TargetFilePath = "b.cs",
            Status = CodeBlockStatus.Pending
        });

        Assert.True(vm.IsPartiallyApplied);
    }

    [Fact]
    public void AffectedFiles_ReturnsDistinctPaths()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel { TargetFilePath = "a.cs" });
        vm.CodeBlocks.Add(new CodeBlockViewModel { TargetFilePath = "b.cs" });
        vm.CodeBlocks.Add(new CodeBlockViewModel { TargetFilePath = "a.cs" });
        vm.CodeBlocks.Add(new CodeBlockViewModel { TargetFilePath = null });

        var files = vm.AffectedFiles.ToList();

        Assert.Equal(2, files.Count);
        Assert.Contains("a.cs", files);
        Assert.Contains("b.cs", files);
    }

    [Fact]
    public void AffectedFileCount_ReturnsCorrectCount()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel { TargetFilePath = "a.cs" });
        vm.CodeBlocks.Add(new CodeBlockViewModel { TargetFilePath = "b.cs" });
        vm.CodeBlocks.Add(new CodeBlockViewModel { TargetFilePath = "a.cs" });

        Assert.Equal(2, vm.AffectedFileCount);
    }

    [Fact]
    public void ProgressText_ShowsCorrectProgress()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "a.cs",
            Status = CodeBlockStatus.Applied
        });
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "b.cs",
            Status = CodeBlockStatus.Pending
        });

        Assert.Equal("1 of 2 applied", vm.ProgressText);
    }

    [Fact]
    public void ProgressText_AllApplied()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "a.cs",
            Status = CodeBlockStatus.Applied
        });

        Assert.Equal("All changes applied", vm.ProgressText);
    }

    [Fact]
    public void ProgressText_NoApplicable()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.Example
        });

        Assert.Equal("No applicable code", vm.ProgressText);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMMAND STATE TESTS                                                      │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void CanApplyAll_TrueWhenPendingBlocks()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "a.cs",
            Status = CodeBlockStatus.Pending
        });

        Assert.True(vm.CanApplyAll);
    }

    [Fact]
    public void CanApplyAll_FalseWhenNoPendingBlocks()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "a.cs",
            Status = CodeBlockStatus.Applied
        });

        Assert.False(vm.CanApplyAll);
    }

    [Fact]
    public void CanRejectAll_TrueWhenPendingBlocks()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel
        {
            Status = CodeBlockStatus.Pending
        });

        Assert.True(vm.CanRejectAll);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ MODEL CONVERSION TESTS                                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void FromProposal_CopiesAllProperties()
    {
        var proposal = new CodeProposal
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Status = ProposalStatus.PartiallyApplied,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            CodeBlocks = new List<CodeBlock>
            {
                new() { Id = Guid.NewGuid(), Content = "test" }
            }
        };

        var vm = new CodeProposalViewModel(proposal);

        Assert.Equal(proposal.Id, vm.Id);
        Assert.Equal(proposal.MessageId, vm.MessageId);
        Assert.Equal(proposal.Status, vm.Status);
        Assert.Single(vm.CodeBlocks);
    }

    [Fact]
    public void ToModel_CreatesCorrectModel()
    {
        var vm = new CodeProposalViewModel
        {
            MessageId = Guid.NewGuid(),
            Status = ProposalStatus.Pending
        };
        vm.CodeBlocks.Add(new CodeBlockViewModel { Content = "test" });

        var model = vm.ToModel();

        Assert.Equal(vm.MessageId, model.MessageId);
        Assert.Equal(vm.Status, model.Status);
        Assert.Single(model.CodeBlocks);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ REJECT ALL TESTS                                                         │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void RejectAll_RejectsAllPendingBlocks()
    {
        var vm = new CodeProposalViewModel();
        vm.CodeBlocks.Add(new CodeBlockViewModel { Status = CodeBlockStatus.Pending });
        vm.CodeBlocks.Add(new CodeBlockViewModel { Status = CodeBlockStatus.Applied });
        vm.CodeBlocks.Add(new CodeBlockViewModel { Status = CodeBlockStatus.Pending });

        vm.RejectAllCommand.Execute(null);

        Assert.Equal(ProposalStatus.Rejected, vm.Status);
        Assert.Equal(2, vm.CodeBlocks.Count(b => b.Status == CodeBlockStatus.Rejected));
        Assert.Equal(1, vm.CodeBlocks.Count(b => b.Status == CodeBlockStatus.Applied));
    }
}
