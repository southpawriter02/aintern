using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PROPOSAL TESTS (v0.4.4a)                                       │
// │ Unit tests for FileTreeProposal model.                                   │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class FileTreeProposalTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Constructor / Default Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var proposal = new FileTreeProposal();

        Assert.NotEqual(Guid.Empty, proposal.Id);
        Assert.Equal(string.Empty, proposal.RootPath);
        Assert.Empty(proposal.Operations);
        Assert.Equal(FileTreeProposalStatus.Pending, proposal.Status);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Count Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FileCount_ReturnsCorrectCount()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Type = FileOperationType.Create },
                new FileOperation { Type = FileOperationType.Create },
                new FileOperation { Type = FileOperationType.Modify }
            }
        };

        Assert.Equal(2, proposal.FileCount);
    }

    [Fact]
    public void ModifyCount_ReturnsCorrectCount()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Type = FileOperationType.Create },
                new FileOperation { Type = FileOperationType.Modify },
                new FileOperation { Type = FileOperationType.Modify }
            }
        };

        Assert.Equal(2, proposal.ModifyCount);
    }

    [Fact]
    public void Directories_ReturnsUniqueDirectories()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Type = FileOperationType.Create, Path = "src/Models/User.cs" },
                new FileOperation { Type = FileOperationType.Create, Path = "src/Models/Product.cs" },
                new FileOperation { Type = FileOperationType.Create, Path = "src/Services/UserService.cs" }
            }
        };

        var dirs = proposal.Directories.ToList();
        Assert.Equal(2, dirs.Count);
        Assert.Contains("src/Models", dirs);
        Assert.Contains("src/Services", dirs);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Selection Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SelectedOperations_FiltersCorrectly()
    {
        var op1 = new FileOperation { IsSelected = true };
        var op2 = new FileOperation { IsSelected = false };
        var op3 = new FileOperation { IsSelected = true };

        var proposal = new FileTreeProposal
        {
            Operations = new[] { op1, op2, op3 }
        };

        Assert.Equal(2, proposal.SelectedCount);
        Assert.False(proposal.AllSelected);
        Assert.True(proposal.HasSelectedOperations);
    }

    [Fact]
    public void SelectAll_SelectsAllOperations()
    {
        var op1 = new FileOperation { IsSelected = false };
        var op2 = new FileOperation { IsSelected = false };

        var proposal = new FileTreeProposal
        {
            Operations = new[] { op1, op2 }
        };

        proposal.SelectAll();

        Assert.True(op1.IsSelected);
        Assert.True(op2.IsSelected);
        Assert.True(proposal.AllSelected);
    }

    [Fact]
    public void DeselectAll_DeselectsAllOperations()
    {
        var op1 = new FileOperation { IsSelected = true };
        var op2 = new FileOperation { IsSelected = true };

        var proposal = new FileTreeProposal
        {
            Operations = new[] { op1, op2 }
        };

        proposal.DeselectAll();

        Assert.False(op1.IsSelected);
        Assert.False(op2.IsSelected);
        Assert.False(proposal.HasSelectedOperations);
    }

    [Fact]
    public void ToggleSelection_TogglesCorrectOperation()
    {
        var op = new FileOperation { IsSelected = true };

        var proposal = new FileTreeProposal
        {
            Operations = new[] { op }
        };

        proposal.ToggleSelection(op.Id);
        Assert.False(op.IsSelected);

        proposal.ToggleSelection(op.Id);
        Assert.True(op.IsSelected);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Size Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TotalSizeBytes_SumsAllContent()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Content = "Hello" },  // 5 bytes
                new FileOperation { Content = "World" }   // 5 bytes
            }
        };

        Assert.Equal(10, proposal.TotalSizeBytes);
    }

    [Fact]
    public void TotalLineCount_SumsAllLines()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Content = "Line1\nLine2" },
                new FileOperation { Content = "Single" }
            }
        };

        Assert.Equal(3, proposal.TotalLineCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Query Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ContainsPath_ReturnsTrueForExistingPath()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Path = "src/Test.cs" }
            }
        };

        Assert.True(proposal.ContainsPath("src/Test.cs"));
        Assert.True(proposal.ContainsPath("SRC/TEST.CS")); // Case-insensitive
        Assert.False(proposal.ContainsPath("other/file.cs"));
    }

    [Fact]
    public void GetOperation_ReturnsCorrectOperation()
    {
        var op = new FileOperation { Path = "src/Test.cs" };

        var proposal = new FileTreeProposal
        {
            Operations = new[] { op }
        };

        var found = proposal.GetOperation("src/Test.cs");
        Assert.Same(op, found);
    }

    [Fact]
    public void GetOperationById_ReturnsCorrectOperation()
    {
        var op = new FileOperation { Path = "src/Test.cs" };

        var proposal = new FileTreeProposal
        {
            Operations = new[] { op }
        };

        var found = proposal.GetOperationById(op.Id);
        Assert.Same(op, found);
    }

    [Fact]
    public void GetOperationsInDirectory_FiltersCorrectly()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Path = "src/Models/User.cs" },
                new FileOperation { Path = "src/Models/Product.cs" },
                new FileOperation { Path = "src/Services/UserService.cs" }
            }
        };

        var modelsOps = proposal.GetOperationsInDirectory("src/Models").ToList();
        Assert.Equal(2, modelsOps.Count);
    }

    [Fact]
    public void GetOperationsByType_FiltersCorrectly()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Type = FileOperationType.Create },
                new FileOperation { Type = FileOperationType.Modify },
                new FileOperation { Type = FileOperationType.Create }
            }
        };

        var creates = proposal.GetOperationsByType(FileOperationType.Create).ToList();
        Assert.Equal(2, creates.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Status Filter Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void PendingOperations_FiltersByStatus()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Status = FileOperationStatus.Pending },
                new FileOperation { Status = FileOperationStatus.Applied },
                new FileOperation { Status = FileOperationStatus.Pending }
            }
        };

        Assert.Equal(2, proposal.PendingOperations.Count());
    }

    [Fact]
    public void AppliedOperations_FiltersByStatus()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new[]
            {
                new FileOperation { Status = FileOperationStatus.Pending },
                new FileOperation { Status = FileOperationStatus.Applied },
                new FileOperation { Status = FileOperationStatus.Applied }
            }
        };

        Assert.Equal(2, proposal.AppliedOperations.Count());
    }
}
