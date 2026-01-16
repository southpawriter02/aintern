namespace AIntern.Core.Models;

/// <summary>
/// Represents a collection of code blocks from a single LLM response,
/// forming a cohesive proposal for code changes (v0.4.1a).
/// </summary>
public sealed class CodeProposal
{
    /// <summary>
    /// Unique identifier for this proposal.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The message ID that generated this proposal.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// When this proposal was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// All code blocks in this proposal.
    /// </summary>
    public IReadOnlyList<CodeBlock> CodeBlocks { get; init; } = Array.Empty<CodeBlock>();

    /// <summary>
    /// Overall status of the proposal.
    /// </summary>
    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    // === Filtered Views ===

    /// <summary>
    /// Code blocks that can be applied (have target paths and are applicable types).
    /// </summary>
    public IEnumerable<CodeBlock> ApplicableBlocks =>
        CodeBlocks.Where(b => b.IsApplicable);

    /// <summary>
    /// Code blocks classified as examples (not meant to be applied).
    /// </summary>
    public IEnumerable<CodeBlock> ExampleBlocks =>
        CodeBlocks.Where(b => b.BlockType == CodeBlockType.Example);

    /// <summary>
    /// Code blocks classified as commands.
    /// </summary>
    public IEnumerable<CodeBlock> CommandBlocks =>
        CodeBlocks.Where(b => b.BlockType == CodeBlockType.Command);

    /// <summary>
    /// Code blocks classified as output/logs.
    /// </summary>
    public IEnumerable<CodeBlock> OutputBlocks =>
        CodeBlocks.Where(b => b.BlockType == CodeBlockType.Output);

    /// <summary>
    /// Code blocks that have not been processed yet.
    /// </summary>
    public IEnumerable<CodeBlock> PendingBlocks =>
        CodeBlocks.Where(b => b.Status == CodeBlockStatus.Pending && b.IsApplicable);

    // === Statistics ===

    /// <summary>
    /// Number of applicable code blocks.
    /// </summary>
    public int ApplicableCount => ApplicableBlocks.Count();

    /// <summary>
    /// Number of blocks that have been applied.
    /// </summary>
    public int AppliedCount => CodeBlocks.Count(b => b.Status == CodeBlockStatus.Applied);

    /// <summary>
    /// Number of blocks that have been rejected.
    /// </summary>
    public int RejectedCount => CodeBlocks.Count(b => b.Status == CodeBlockStatus.Rejected);

    /// <summary>
    /// Number of blocks still pending.
    /// </summary>
    public int PendingCount => PendingBlocks.Count();

    /// <summary>
    /// Whether all applicable blocks have been processed.
    /// </summary>
    public bool IsFullyProcessed => ApplicableBlocks.All(
        b => b.Status is not CodeBlockStatus.Pending);

    /// <summary>
    /// Unique target files in this proposal.
    /// </summary>
    public IEnumerable<string> TargetFiles =>
        ApplicableBlocks
            .Select(b => b.TargetFilePath!)
            .Distinct();

    /// <summary>
    /// Number of unique files affected.
    /// </summary>
    public int AffectedFileCount => TargetFiles.Count();

    // === Helper Methods ===

    /// <summary>
    /// Gets code blocks targeting a specific file.
    /// </summary>
    public IEnumerable<CodeBlock> GetBlocksForFile(string filePath) =>
        ApplicableBlocks.Where(b => 
            string.Equals(b.TargetFilePath, filePath, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Recalculates the overall status based on individual block statuses.
    /// </summary>
    public void UpdateStatus()
    {
        if (!ApplicableBlocks.Any())
        {
            Status = ProposalStatus.Pending;
            return;
        }

        if (ApplicableBlocks.All(b => b.Status == CodeBlockStatus.Applied))
        {
            Status = ProposalStatus.FullyApplied;
        }
        else if (ApplicableBlocks.All(b => b.Status == CodeBlockStatus.Rejected))
        {
            Status = ProposalStatus.Rejected;
        }
        else if (ApplicableBlocks.Any(b => b.Status != CodeBlockStatus.Pending))
        {
            Status = ProposalStatus.PartiallyApplied;
        }
        else
        {
            Status = ProposalStatus.Pending;
        }
    }
}

/// <summary>
/// Overall status of a code proposal (v0.4.1a).
/// </summary>
public enum ProposalStatus
{
    /// <summary>
    /// No blocks have been processed yet.
    /// </summary>
    Pending,

    /// <summary>
    /// Some blocks have been applied, others pending.
    /// </summary>
    PartiallyApplied,

    /// <summary>
    /// All applicable blocks have been applied.
    /// </summary>
    FullyApplied,

    /// <summary>
    /// User rejected the entire proposal.
    /// </summary>
    Rejected,

    /// <summary>
    /// Proposal has expired (e.g., file changed since proposal).
    /// </summary>
    Expired
}
