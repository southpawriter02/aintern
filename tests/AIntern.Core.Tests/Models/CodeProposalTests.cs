namespace AIntern.Core.Tests.Models;

using AIntern.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CodeProposal"/> (v0.4.1a).
/// </summary>
public class CodeProposalTests
{
    #region Filtered Views Tests

    [Fact]
    public void ApplicableBlocks_FiltersCorrectly()
    {
        var proposal = new CodeProposal
        {
            CodeBlocks = new[]
            {
                new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "a.cs" },
                new CodeBlock { BlockType = CodeBlockType.Example },
                new CodeBlock { BlockType = CodeBlockType.Snippet, TargetFilePath = "b.cs" }
            }
        };

        Assert.Equal(2, proposal.ApplicableBlocks.Count());
    }

    [Fact]
    public void ExampleBlocks_FiltersCorrectly()
    {
        var proposal = new CodeProposal
        {
            CodeBlocks = new[]
            {
                new CodeBlock { BlockType = CodeBlockType.CompleteFile },
                new CodeBlock { BlockType = CodeBlockType.Example },
                new CodeBlock { BlockType = CodeBlockType.Example }
            }
        };

        Assert.Equal(2, proposal.ExampleBlocks.Count());
    }

    [Fact]
    public void CommandBlocks_FiltersCorrectly()
    {
        var proposal = new CodeProposal
        {
            CodeBlocks = new[]
            {
                new CodeBlock { BlockType = CodeBlockType.Command },
                new CodeBlock { BlockType = CodeBlockType.Snippet }
            }
        };

        Assert.Single(proposal.CommandBlocks);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void ApplicableCount_ReturnsCorrectCount()
    {
        var proposal = new CodeProposal
        {
            CodeBlocks = new[]
            {
                new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "a.cs" },
                new CodeBlock { BlockType = CodeBlockType.Example }
            }
        };

        Assert.Equal(1, proposal.ApplicableCount);
    }

    [Fact]
    public void AffectedFileCount_CountsDistinctFiles()
    {
        var proposal = new CodeProposal
        {
            CodeBlocks = new[]
            {
                new CodeBlock { BlockType = CodeBlockType.Snippet, TargetFilePath = "a.cs" },
                new CodeBlock { BlockType = CodeBlockType.Snippet, TargetFilePath = "a.cs" },
                new CodeBlock { BlockType = CodeBlockType.Snippet, TargetFilePath = "b.cs" }
            }
        };

        Assert.Equal(2, proposal.AffectedFileCount);
    }

    [Fact]
    public void PendingCount_CountsPendingApplicableBlocks()
    {
        var blocks = new[]
        {
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "a.cs", Status = CodeBlockStatus.Pending },
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "b.cs", Status = CodeBlockStatus.Applied }
        };

        var proposal = new CodeProposal { CodeBlocks = blocks };
        Assert.Equal(1, proposal.PendingCount);
    }

    #endregion

    #region UpdateStatus Tests

    [Fact]
    public void UpdateStatus_SetsFullyApplied_WhenAllApplied()
    {
        var blocks = new[]
        {
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "a.cs", Status = CodeBlockStatus.Applied },
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "b.cs", Status = CodeBlockStatus.Applied }
        };

        var proposal = new CodeProposal { CodeBlocks = blocks };
        proposal.UpdateStatus();

        Assert.Equal(ProposalStatus.FullyApplied, proposal.Status);
    }

    [Fact]
    public void UpdateStatus_SetsPartiallyApplied_WhenMixed()
    {
        var blocks = new[]
        {
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "a.cs", Status = CodeBlockStatus.Applied },
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "b.cs", Status = CodeBlockStatus.Pending }
        };

        var proposal = new CodeProposal { CodeBlocks = blocks };
        proposal.UpdateStatus();

        Assert.Equal(ProposalStatus.PartiallyApplied, proposal.Status);
    }

    [Fact]
    public void UpdateStatus_SetsRejected_WhenAllRejected()
    {
        var blocks = new[]
        {
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "a.cs", Status = CodeBlockStatus.Rejected },
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "b.cs", Status = CodeBlockStatus.Rejected }
        };

        var proposal = new CodeProposal { CodeBlocks = blocks };
        proposal.UpdateStatus();

        Assert.Equal(ProposalStatus.Rejected, proposal.Status);
    }

    [Fact]
    public void IsFullyProcessed_TrueWhenNoPending()
    {
        var blocks = new[]
        {
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "a.cs", Status = CodeBlockStatus.Applied },
            new CodeBlock { BlockType = CodeBlockType.CompleteFile, TargetFilePath = "b.cs", Status = CodeBlockStatus.Rejected }
        };

        var proposal = new CodeProposal { CodeBlocks = blocks };
        Assert.True(proposal.IsFullyProcessed);
    }

    #endregion

    #region GetBlocksForFile Tests

    [Fact]
    public void GetBlocksForFile_ReturnsBlocksForPath()
    {
        var proposal = new CodeProposal
        {
            CodeBlocks = new[]
            {
                new CodeBlock { BlockType = CodeBlockType.Snippet, TargetFilePath = "a.cs" },
                new CodeBlock { BlockType = CodeBlockType.Snippet, TargetFilePath = "b.cs" },
                new CodeBlock { BlockType = CodeBlockType.Snippet, TargetFilePath = "a.cs" }
            }
        };

        Assert.Equal(2, proposal.GetBlocksForFile("a.cs").Count());
    }

    #endregion
}
