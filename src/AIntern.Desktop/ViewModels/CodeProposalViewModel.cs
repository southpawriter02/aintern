namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AIntern.Core.Models;
using AIntern.Desktop.Messages;

/// <summary>
/// ViewModel for a complete code proposal containing multiple blocks (v0.4.1g).
/// </summary>
/// <remarks>
/// <para>
/// Manages a collection of CodeBlockViewModels and provides aggregate
/// statistics and bulk actions (apply all, reject all).
/// </para>
/// </remarks>
public partial class CodeProposalViewModel : ViewModelBase
{
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ OBSERVABLE PROPERTIES                                                    │
    // └─────────────────────────────────────────────────────────────────────────┘

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private Guid _messageId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFullyApplied))]
    [NotifyPropertyChangedFor(nameof(IsPartiallyApplied))]
    [NotifyPropertyChangedFor(nameof(CanApplyAll))]
    [NotifyPropertyChangedFor(nameof(CanRejectAll))]
    private ProposalStatus _status = ProposalStatus.Pending;

    [ObservableProperty]
    private DateTime _createdAt;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COLLECTIONS                                                              │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// All code blocks in this proposal.
    /// </summary>
    public ObservableCollection<CodeBlockViewModel> CodeBlocks { get; } = new();

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMPUTED PROPERTIES                                                      │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>Total number of code blocks.</summary>
    public int TotalCount => CodeBlocks.Count;

    /// <summary>Number of applicable (can be applied) blocks.</summary>
    public int ApplicableCount => CodeBlocks.Count(b =>
        b.BlockType is CodeBlockType.CompleteFile or CodeBlockType.Snippet or CodeBlockType.Config
        && b.HasTargetPath);

    /// <summary>Number of blocks already applied.</summary>
    public int AppliedCount => CodeBlocks.Count(b => b.Status == CodeBlockStatus.Applied);

    /// <summary>Number of pending blocks.</summary>
    public int PendingCount => CodeBlocks.Count(b => b.Status == CodeBlockStatus.Pending);

    /// <summary>Number of blocks with errors.</summary>
    public int ErrorCount => CodeBlocks.Count(b =>
        b.Status is CodeBlockStatus.Error or CodeBlockStatus.Conflict);

    /// <summary>Whether any blocks can be applied.</summary>
    public bool HasApplicableBlocks => ApplicableCount > 0;

    /// <summary>Whether all applicable blocks have been applied.</summary>
    public bool IsFullyApplied =>
        ApplicableCount > 0 && AppliedCount == ApplicableCount;

    /// <summary>Whether some but not all blocks have been applied.</summary>
    public bool IsPartiallyApplied =>
        AppliedCount > 0 && AppliedCount < ApplicableCount;

    /// <summary>Whether ApplyAll command can execute.</summary>
    public bool CanApplyAll =>
        PendingCount > 0 && Status != ProposalStatus.FullyApplied;

    /// <summary>Whether RejectAll command can execute.</summary>
    public bool CanRejectAll =>
        PendingCount > 0 && Status != ProposalStatus.Rejected;

    /// <summary>Progress text for display.</summary>
    public string ProgressText
    {
        get
        {
            if (ApplicableCount == 0) return "No applicable code";
            if (IsFullyApplied) return "All changes applied";
            return $"{AppliedCount} of {ApplicableCount} applied";
        }
    }

    /// <summary>List of affected file paths.</summary>
    public IEnumerable<string> AffectedFiles =>
        CodeBlocks
            .Where(b => !string.IsNullOrEmpty(b.TargetFilePath))
            .Select(b => b.TargetFilePath!)
            .Distinct()
            .OrderBy(p => p);

    /// <summary>Number of affected files.</summary>
    public int AffectedFileCount => AffectedFiles.Count();

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CONSTRUCTORS                                                             │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Initializes a new instance with default values.
    /// </summary>
    public CodeProposalViewModel()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CodeBlocks.CollectionChanged += OnCodeBlocksChanged;
    }

    /// <summary>
    /// Initializes from a CodeProposal model.
    /// </summary>
    public CodeProposalViewModel(CodeProposal proposal) : this()
    {
        Id = proposal.Id;
        MessageId = proposal.MessageId;
        Status = proposal.Status;
        CreatedAt = proposal.CreatedAt;

        foreach (var block in proposal.CodeBlocks)
        {
            CodeBlocks.Add(new CodeBlockViewModel(block));
        }
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMMANDS                                                                 │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Apply all pending applicable blocks.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApplyAll))]
    private void ApplyAll()
    {
        Status = ProposalStatus.PartiallyApplied;

        // Send message for each pending applicable block
        foreach (var block in CodeBlocks.Where(b =>
            b.Status == CodeBlockStatus.Pending && b.IsApplicable))
        {
            WeakReferenceMessenger.Default.Send(
                new ApplyChangesRequestMessage(block) { SkipDiffPreview = true });
        }
    }

    /// <summary>
    /// Reject all pending blocks.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRejectAll))]
    private void RejectAll()
    {
        Status = ProposalStatus.Rejected;

        foreach (var block in CodeBlocks.Where(b =>
            b.Status == CodeBlockStatus.Pending))
        {
            block.Reject();
        }

        NotifyStatsChanged();
    }

    /// <summary>
    /// Show summary dialog for this proposal.
    /// </summary>
    [RelayCommand]
    private void ShowSummary()
    {
        // Will be handled by parent view or messenger in v0.4.2
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COLLECTION MANAGEMENT                                                    │
    // └─────────────────────────────────────────────────────────────────────────┘

    private void OnCodeBlocksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Subscribe to status changes on new blocks
        if (e.NewItems != null)
        {
            foreach (CodeBlockViewModel block in e.NewItems)
            {
                block.PropertyChanged += OnBlockPropertyChanged;
            }
        }

        // Unsubscribe from removed blocks
        if (e.OldItems != null)
        {
            foreach (CodeBlockViewModel block in e.OldItems)
            {
                block.PropertyChanged -= OnBlockPropertyChanged;
            }
        }

        NotifyStatsChanged();
    }

    private void OnBlockPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodeBlockViewModel.Status))
        {
            NotifyStatsChanged();
            UpdateProposalStatus();
        }
    }

    private void NotifyStatsChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ApplicableCount));
        OnPropertyChanged(nameof(AppliedCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(HasApplicableBlocks));
        OnPropertyChanged(nameof(IsFullyApplied));
        OnPropertyChanged(nameof(IsPartiallyApplied));
        OnPropertyChanged(nameof(CanApplyAll));
        OnPropertyChanged(nameof(CanRejectAll));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(AffectedFiles));
        OnPropertyChanged(nameof(AffectedFileCount));

        ApplyAllCommand.NotifyCanExecuteChanged();
        RejectAllCommand.NotifyCanExecuteChanged();
    }

    private void UpdateProposalStatus()
    {
        if (IsFullyApplied)
        {
            Status = ProposalStatus.FullyApplied;
        }
        else if (CodeBlocks.All(b => b.Status == CodeBlockStatus.Rejected))
        {
            Status = ProposalStatus.Rejected;
        }
        else if (IsPartiallyApplied)
        {
            Status = ProposalStatus.PartiallyApplied;
        }
        else if (ErrorCount > 0)
        {
            // Keep current status but could track error state
        }
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ MODEL CONVERSION                                                         │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Convert to domain model.
    /// </summary>
    public CodeProposal ToModel() => new()
    {
        Id = Id,
        MessageId = MessageId,
        Status = Status,
        CreatedAt = CreatedAt,
        CodeBlocks = CodeBlocks.Select(b => b.ToModel()).ToList()
    };
}
