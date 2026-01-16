namespace AIntern.Services;

using DiffPlex;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INLINE DIFF SERVICE (v0.4.2c)                                            │
// │ Computes character-level inline diffs using DiffPlex.                    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Computes character-level inline diffs using DiffPlex.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2c.</para>
/// <para>
/// This service uses DiffPlex's character-level diff algorithm to identify
/// precisely which characters changed between two versions of a line.
/// The results can then be rendered to highlight inline changes in the UI.
/// </para>
/// </remarks>
public sealed class InlineDiffService : IInlineDiffService
{
    private readonly Differ _differ;
    private readonly ILogger<InlineDiffService>? _logger;

    /// <summary>
    /// Initializes a new instance of the InlineDiffService.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public InlineDiffService(ILogger<InlineDiffService>? logger = null)
    {
        _differ = new Differ();
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<InlineChange> ComputeInlineChanges(
        string originalLine,
        string proposedLine)
    {
        // Handle null inputs gracefully
        originalLine ??= string.Empty;
        proposedLine ??= string.Empty;

        // Quick path for identical lines
        if (originalLine == proposedLine)
        {
            _logger?.LogTrace("Lines are identical, returning empty changes");
            return [];
        }

        // Quick path for completely different lines (one empty)
        if (string.IsNullOrEmpty(originalLine))
        {
            _logger?.LogTrace("Original empty, marking all proposed as Added");
            return
            [
                InlineChange.Added(0, proposedLine)
            ];
        }

        if (string.IsNullOrEmpty(proposedLine))
        {
            _logger?.LogTrace("Proposed empty, marking all original as Removed");
            return
            [
                InlineChange.Removed(0, originalLine)
            ];
        }

        var changes = new List<InlineChange>();

        // Use DiffPlex character-level diff
        var diff = _differ.CreateCharacterDiffs(originalLine, proposedLine, ignoreWhitespace: false);

        int propPos = 0;

        foreach (var block in diff.DiffBlocks)
        {
            // Add unchanged text before this block (from proposed perspective)
            if (block.InsertStartB > propPos)
            {
                var unchangedLength = block.InsertStartB - propPos;
                changes.Add(InlineChange.Unchanged(
                    propPos,
                    proposedLine.Substring(propPos, unchangedLength)));
            }

            // Add removed characters (from original)
            if (block.DeleteCountA > 0)
            {
                changes.Add(InlineChange.Removed(
                    block.DeleteStartA,
                    originalLine.Substring(block.DeleteStartA, block.DeleteCountA)));
            }

            // Add inserted characters (in proposed)
            if (block.InsertCountB > 0)
            {
                changes.Add(InlineChange.Added(
                    block.InsertStartB,
                    proposedLine.Substring(block.InsertStartB, block.InsertCountB)));
            }

            propPos = block.InsertStartB + block.InsertCountB;
        }

        // Add any remaining unchanged text at the end
        if (propPos < proposedLine.Length)
        {
            changes.Add(InlineChange.Unchanged(
                propPos,
                proposedLine.Substring(propPos)));
        }

        _logger?.LogDebug("Computed {Count} inline changes", changes.Count);

        return changes;
    }

    /// <inheritdoc />
    public IReadOnlyList<InlineSegment> GetInlineSegments(
        string content,
        IReadOnlyList<InlineChange> changes,
        DiffSide side)
    {
        // No changes means return the whole content as unchanged
        if (changes == null || changes.Count == 0)
        {
            return
            [
                InlineSegment.Unchanged(content ?? string.Empty)
            ];
        }

        var segments = new List<InlineSegment>();

        foreach (var change in changes)
        {
            // Filter based on which side we're rendering
            // Original side: show Unchanged and Removed (not Added)
            // Proposed side: show Unchanged and Added (not Removed)
            bool includeSegment = change.Type switch
            {
                InlineChangeType.Unchanged => true,
                InlineChangeType.Added => side == DiffSide.Proposed,
                InlineChangeType.Removed => side == DiffSide.Original,
                _ => true
            };

            if (includeSegment && !string.IsNullOrEmpty(change.Text))
            {
                segments.Add(new InlineSegment
                {
                    Text = change.Text,
                    IsChanged = change.Type != InlineChangeType.Unchanged,
                    Type = change.Type
                });
            }
        }

        // If no segments were created, return the content as unchanged
        if (segments.Count == 0)
        {
            return
            [
                InlineSegment.Unchanged(content ?? string.Empty)
            ];
        }

        return segments;
    }
}
