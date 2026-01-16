using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROPOSAL VALIDATION RESULT TESTS (v0.4.4a)                               │
// │ Unit tests for ProposalValidationResult model.                           │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class ProposalValidationResultTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Filter Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Errors_FiltersCorrectly()
    {
        var result = new ProposalValidationResult
        {
            Issues = new[]
            {
                new ValidationIssue { Severity = ValidationSeverity.Error },
                new ValidationIssue { Severity = ValidationSeverity.Warning },
                new ValidationIssue { Severity = ValidationSeverity.Error }
            }
        };

        Assert.Equal(2, result.Errors.Count());
    }

    [Fact]
    public void Warnings_FiltersCorrectly()
    {
        var result = new ProposalValidationResult
        {
            Issues = new[]
            {
                new ValidationIssue { Severity = ValidationSeverity.Warning },
                new ValidationIssue { Severity = ValidationSeverity.Error }
            }
        };

        Assert.Single(result.Warnings);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Summary Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HasErrors_ReturnsTrue_WhenErrorsExist()
    {
        var result = new ProposalValidationResult
        {
            Issues = new[]
            {
                new ValidationIssue { Severity = ValidationSeverity.Error }
            }
        };

        Assert.True(result.HasErrors);
    }

    [Fact]
    public void HasWarnings_ReturnsTrue_WhenWarningsExist()
    {
        var result = new ProposalValidationResult
        {
            Issues = new[]
            {
                new ValidationIssue { Severity = ValidationSeverity.Warning }
            }
        };

        Assert.True(result.HasWarnings);
    }

    [Fact]
    public void SummaryMessage_ValidNoWarnings()
    {
        var result = new ProposalValidationResult { IsValid = true };

        Assert.Equal("Proposal is valid and ready to apply.", result.SummaryMessage);
    }

    [Fact]
    public void SummaryMessage_ValidWithWarnings()
    {
        var result = new ProposalValidationResult
        {
            IsValid = true,
            Issues = new[]
            {
                new ValidationIssue { Severity = ValidationSeverity.Warning },
                new ValidationIssue { Severity = ValidationSeverity.Warning }
            }
        };

        Assert.Equal("Proposal is valid with 2 warning(s).", result.SummaryMessage);
    }

    [Fact]
    public void SummaryMessage_Invalid()
    {
        var result = new ProposalValidationResult
        {
            IsValid = false,
            Issues = new[]
            {
                new ValidationIssue { Severity = ValidationSeverity.Error },
                new ValidationIssue { Severity = ValidationSeverity.Error }
            }
        };

        Assert.Equal("Proposal has 2 error(s) that must be resolved.", result.SummaryMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Query Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetIssuesForPath_FiltersCorrectly()
    {
        var result = new ProposalValidationResult
        {
            Issues = new[]
            {
                new ValidationIssue { Path = "file1.cs" },
                new ValidationIssue { Path = "file2.cs" },
                new ValidationIssue { Path = "file1.cs" }
            }
        };

        Assert.Equal(2, result.GetIssuesForPath("file1.cs").Count());
    }

    [Fact]
    public void GetIssuesForOperation_FiltersCorrectly()
    {
        var opId = Guid.NewGuid();

        var result = new ProposalValidationResult
        {
            Issues = new[]
            {
                new ValidationIssue { OperationId = opId },
                new ValidationIssue { OperationId = Guid.NewGuid() },
                new ValidationIssue { OperationId = opId }
            }
        };

        Assert.Equal(2, result.GetIssuesForOperation(opId).Count());
    }

    [Fact]
    public void GetIssuesByType_FiltersCorrectly()
    {
        var result = new ProposalValidationResult
        {
            Issues = new[]
            {
                new ValidationIssue { Type = ValidationIssueType.FileExists },
                new ValidationIssue { Type = ValidationIssueType.InvalidPath },
                new ValidationIssue { Type = ValidationIssueType.FileExists }
            }
        };

        Assert.Equal(2, result.GetIssuesByType(ValidationIssueType.FileExists).Count());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Method Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Valid_CreatesValidResult()
    {
        var result = ProposalValidationResult.Valid();

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Invalid_CreatesInvalidResult()
    {
        var issue = ValidationIssue.Error("file.cs", ValidationIssueType.FileExists, "Already exists");

        var result = ProposalValidationResult.Invalid(issue);

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void InvalidWithError_CreatesWithSingleError()
    {
        var result = ProposalValidationResult.InvalidWithError(
            "file.cs",
            ValidationIssueType.PermissionDenied,
            "Access denied");

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Access denied", result.Errors.First().Message);
    }
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ VALIDATION ISSUE TESTS                                                   │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class ValidationIssueTests
{
    [Fact]
    public void Error_CreatesErrorIssue()
    {
        var issue = ValidationIssue.Error(
            "file.cs",
            ValidationIssueType.FileExists,
            "File already exists",
            suggestedFix: "Delete the existing file");

        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal("file.cs", issue.Path);
        Assert.Equal(ValidationIssueType.FileExists, issue.Type);
        Assert.Equal("File already exists", issue.Message);
        Assert.Equal("Delete the existing file", issue.SuggestedFix);
    }

    [Fact]
    public void Warning_CreatesWarningIssue()
    {
        var issue = ValidationIssue.Warning(
            "file.cs",
            ValidationIssueType.EmptyContent,
            "Content is empty");

        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Info_CreatesInfoIssue()
    {
        var issue = ValidationIssue.Info(
            "file.cs",
            ValidationIssueType.DirectoryExists,
            "Directory already exists");

        Assert.Equal(ValidationSeverity.Info, issue.Severity);
    }
}
