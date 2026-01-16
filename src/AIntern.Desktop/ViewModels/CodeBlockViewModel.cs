namespace AIntern.Desktop.ViewModels;

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AIntern.Core.Models;
using AIntern.Desktop.Messages;

/// <summary>
/// ViewModel for an individual code block within a chat message (v0.4.1g).
/// </summary>
/// <remarks>
/// <para>
/// Provides observable properties for UI binding, commands for user actions
/// (copy, apply, reject), and streaming support for real-time content updates.
/// </para>
/// </remarks>
public partial class CodeBlockViewModel : ViewModelBase
{
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ OBSERVABLE PROPERTIES                                                    │
    // └─────────────────────────────────────────────────────────────────────────┘

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private Guid _messageId;

    [ObservableProperty]
    private int _sequenceNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineCount))]
    [NotifyPropertyChangedFor(nameof(FormattedLineCount))]
    private string _content = string.Empty;

    [ObservableProperty]
    private string? _language;

    [ObservableProperty]
    private string? _displayLanguage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    [NotifyPropertyChangedFor(nameof(HasTargetPath))]
    [NotifyPropertyChangedFor(nameof(IsApplicable))]
    [NotifyPropertyChangedFor(nameof(ShowApplyButton))]
    private string? _targetFilePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsApplicable))]
    [NotifyPropertyChangedFor(nameof(IsExample))]
    [NotifyPropertyChangedFor(nameof(IsCommand))]
    [NotifyPropertyChangedFor(nameof(IsOutput))]
    [NotifyPropertyChangedFor(nameof(IsConfig))]
    [NotifyPropertyChangedFor(nameof(ShowApplyButton))]
    private CodeBlockType _blockType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusIcon))]
    [NotifyPropertyChangedFor(nameof(ShowApplyButton))]
    [NotifyPropertyChangedFor(nameof(ShowStatusBadge))]
    [NotifyPropertyChangedFor(nameof(IsApplied))]
    [NotifyPropertyChangedFor(nameof(IsRejected))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(HasConflict))]
    private CodeBlockStatus _status = CodeBlockStatus.Pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConfidencePercent))]
    [NotifyPropertyChangedFor(nameof(IsLowConfidence))]
    private float _confidenceScore = 1.0f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowApplyButton))]
    [NotifyPropertyChangedFor(nameof(ShowStreamingIndicator))]
    private bool _isStreaming;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isPathAmbiguous;

    [ObservableProperty]
    private IReadOnlyList<string>? _alternativePaths;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMPUTED PROPERTIES                                                      │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>Number of lines in the content.</summary>
    public int LineCount => string.IsNullOrEmpty(Content) ? 0 : Content.Split('\n').Length;

    /// <summary>Formatted line count for display.</summary>
    public string FormattedLineCount => LineCount == 1 ? "1 line" : $"{LineCount} lines";

    /// <summary>File name extracted from target path.</summary>
    public string? FileName => !string.IsNullOrEmpty(TargetFilePath)
        ? Path.GetFileName(TargetFilePath)
        : null;

    /// <summary>Whether a target path is set.</summary>
    public bool HasTargetPath => !string.IsNullOrEmpty(TargetFilePath);

    /// <summary>Whether this block can be applied to a file.</summary>
    public bool IsApplicable =>
        BlockType is CodeBlockType.CompleteFile or CodeBlockType.Snippet or CodeBlockType.Config
        && HasTargetPath
        && Status == CodeBlockStatus.Pending;

    /// <summary>Whether this is an example/illustration block.</summary>
    public bool IsExample => BlockType == CodeBlockType.Example;

    /// <summary>Whether this is a shell command.</summary>
    public bool IsCommand => BlockType == CodeBlockType.Command;

    /// <summary>Whether this is output/result display.</summary>
    public bool IsOutput => BlockType == CodeBlockType.Output;

    /// <summary>Whether this is a config file.</summary>
    public bool IsConfig => BlockType == CodeBlockType.Config;

    /// <summary>Whether to show the Apply button.</summary>
    public bool ShowApplyButton =>
        (BlockType is CodeBlockType.CompleteFile or CodeBlockType.Snippet or CodeBlockType.Config)
        && HasTargetPath
        && !IsStreaming
        && Status == CodeBlockStatus.Pending;

    /// <summary>Whether to show the status badge.</summary>
    public bool ShowStatusBadge =>
        Status != CodeBlockStatus.Pending
        && Status != CodeBlockStatus.Reviewing;

    /// <summary>Whether to show streaming indicator.</summary>
    public bool ShowStreamingIndicator => IsStreaming;

    /// <summary>Whether block was successfully applied.</summary>
    public bool IsApplied => Status == CodeBlockStatus.Applied;

    /// <summary>Whether block was rejected by user.</summary>
    public bool IsRejected => Status == CodeBlockStatus.Rejected;

    /// <summary>Whether block has an error.</summary>
    public bool HasError => Status == CodeBlockStatus.Error;

    /// <summary>Whether block has a conflict.</summary>
    public bool HasConflict => Status == CodeBlockStatus.Conflict;

    /// <summary>Confidence as percentage string.</summary>
    public string ConfidencePercent => $"{ConfidenceScore * 100:F0}%";

    /// <summary>Whether confidence is below threshold.</summary>
    public bool IsLowConfidence => ConfidenceScore < 0.7f;

    /// <summary>Status text for display.</summary>
    public string StatusText => Status switch
    {
        CodeBlockStatus.Pending => "",
        CodeBlockStatus.Reviewing => "Reviewing",
        CodeBlockStatus.Applying => "Applying...",
        CodeBlockStatus.Applied => "Applied",
        CodeBlockStatus.Rejected => "Rejected",
        CodeBlockStatus.Skipped => "Skipped",
        CodeBlockStatus.Conflict => "Conflict",
        CodeBlockStatus.Error => "Error",
        _ => ""
    };

    /// <summary>Icon key for current status.</summary>
    public string StatusIcon => Status switch
    {
        CodeBlockStatus.Applied => "CheckIcon",
        CodeBlockStatus.Rejected => "CrossIcon",
        CodeBlockStatus.Conflict => "WarningIcon",
        CodeBlockStatus.Error => "ErrorIcon",
        _ => ""
    };

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CONSTRUCTORS                                                             │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Initializes a new instance with default values.
    /// </summary>
    public CodeBlockViewModel()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Initializes from a completed CodeBlock model.
    /// </summary>
    public CodeBlockViewModel(CodeBlock block)
    {
        Id = block.Id;
        MessageId = block.MessageId;
        SequenceNumber = block.SequenceNumber;
        Content = block.Content;
        Language = block.Language;
        DisplayLanguage = block.DisplayLanguage;
        TargetFilePath = block.TargetFilePath;
        BlockType = block.BlockType;
        Status = block.Status;
        ConfidenceScore = block.ConfidenceScore;
    }

    /// <summary>
    /// Initializes from a streaming PartialCodeBlock.
    /// </summary>
    public CodeBlockViewModel(PartialCodeBlock partial)
    {
        Id = partial.Id;
        MessageId = partial.MessageId;
        SequenceNumber = partial.SequenceNumber;
        Content = partial.Content.ToString();
        Language = partial.Language;
        DisplayLanguage = partial.DisplayLanguage;
        TargetFilePath = partial.TargetFilePath;
        IsStreaming = true;
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMMANDS                                                                 │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Copy the code block content to clipboard.
    /// </summary>
    [RelayCommand]
    private void CopyToClipboard()
    {
        WeakReferenceMessenger.Default.Send(
            new CopyToClipboardRequestMessage(Content)
            {
                SourceDescription = $"Code block ({DisplayLanguage ?? "unknown"})"
            });
    }

    /// <summary>
    /// Show diff view for this code block.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanShowDiff))]
    private void ShowDiff()
    {
        Status = CodeBlockStatus.Reviewing;
        WeakReferenceMessenger.Default.Send(new ShowDiffRequestMessage(this));
    }

    private bool CanShowDiff() =>
        HasTargetPath
        && !IsStreaming
        && Status == CodeBlockStatus.Pending;

    /// <summary>
    /// Apply changes from this block directly.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApplyChanges))]
    private void ApplyChanges()
    {
        WeakReferenceMessenger.Default.Send(
            new ApplyChangesRequestMessage(this) { SkipDiffPreview = true });
    }

    private bool CanApplyChanges() =>
        HasTargetPath
        && !IsStreaming
        && Status is CodeBlockStatus.Pending or CodeBlockStatus.Reviewing;

    /// <summary>
    /// Reject this code block.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanReject))]
    public void Reject()
    {
        var oldStatus = Status;
        Status = CodeBlockStatus.Rejected;
        WeakReferenceMessenger.Default.Send(new CodeBlockStatusChangedMessage
        {
            BlockId = Id,
            MessageId = MessageId,
            OldStatus = oldStatus,
            NewStatus = Status
        });
    }

    private bool CanReject() =>
        Status is CodeBlockStatus.Pending or CodeBlockStatus.Reviewing or CodeBlockStatus.Conflict;

    /// <summary>
    /// Select path from alternatives (for ambiguous paths).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectPath))]
    private async Task SelectPathAsync()
    {
        if (AlternativePaths == null || AlternativePaths.Count == 0)
            return;

        var response = await WeakReferenceMessenger.Default.Send(
            new SelectFilePathRequestMessage
            {
                Block = this,
                PossiblePaths = AlternativePaths
            });

        if (!string.IsNullOrEmpty(response))
        {
            TargetFilePath = response;
            IsPathAmbiguous = false;
            AlternativePaths = null;
            ConfidenceScore = 1.0f; // User selected = certain
        }
    }

    private bool CanSelectPath() => IsPathAmbiguous && AlternativePaths?.Count > 0;

    /// <summary>
    /// Toggle expanded state.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STREAMING SUPPORT                                                        │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Append content during streaming.
    /// </summary>
    public void AppendContent(string token)
    {
        Content += token;
    }

    /// <summary>
    /// Update from partial block during streaming.
    /// </summary>
    public void UpdateFromPartial(PartialCodeBlock partial)
    {
        Content = partial.Content.ToString();
        Language = partial.Language;
        DisplayLanguage = partial.DisplayLanguage;
        TargetFilePath = partial.TargetFilePath;
    }

    /// <summary>
    /// Complete streaming with final block data.
    /// </summary>
    public void CompleteStreaming(CodeBlock finalBlock)
    {
        Content = finalBlock.Content;
        Language = finalBlock.Language;
        DisplayLanguage = finalBlock.DisplayLanguage;
        TargetFilePath = finalBlock.TargetFilePath;
        BlockType = finalBlock.BlockType;
        ConfidenceScore = finalBlock.ConfidenceScore;
        IsStreaming = false;

        // Notify commands to re-evaluate CanExecute
        ShowDiffCommand.NotifyCanExecuteChanged();
        ApplyChangesCommand.NotifyCanExecuteChanged();
        RejectCommand.NotifyCanExecuteChanged();
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STATUS UPDATES                                                           │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Mark as applying (in progress).
    /// </summary>
    public void MarkAsApplying()
    {
        Status = CodeBlockStatus.Applying;
    }

    /// <summary>
    /// Mark as successfully applied.
    /// </summary>
    public void MarkAsApplied()
    {
        var oldStatus = Status;
        Status = CodeBlockStatus.Applied;
        ErrorMessage = null;

        WeakReferenceMessenger.Default.Send(new CodeBlockStatusChangedMessage
        {
            BlockId = Id,
            MessageId = MessageId,
            OldStatus = oldStatus,
            NewStatus = Status
        });
    }

    /// <summary>
    /// Mark as error with message.
    /// </summary>
    public void MarkAsError(string error)
    {
        var oldStatus = Status;
        Status = CodeBlockStatus.Error;
        ErrorMessage = error;

        WeakReferenceMessenger.Default.Send(new CodeBlockStatusChangedMessage
        {
            BlockId = Id,
            MessageId = MessageId,
            OldStatus = oldStatus,
            NewStatus = Status,
            ErrorMessage = error
        });
    }

    /// <summary>
    /// Mark as conflict (file changed).
    /// </summary>
    public void MarkAsConflict(string reason)
    {
        var oldStatus = Status;
        Status = CodeBlockStatus.Conflict;
        ErrorMessage = reason;

        WeakReferenceMessenger.Default.Send(new CodeBlockStatusChangedMessage
        {
            BlockId = Id,
            MessageId = MessageId,
            OldStatus = oldStatus,
            NewStatus = Status,
            ErrorMessage = reason
        });
    }

    /// <summary>
    /// Reset to pending state (for retry).
    /// </summary>
    public void ResetToPending()
    {
        Status = CodeBlockStatus.Pending;
        ErrorMessage = null;

        ShowDiffCommand.NotifyCanExecuteChanged();
        ApplyChangesCommand.NotifyCanExecuteChanged();
        RejectCommand.NotifyCanExecuteChanged();
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ MODEL CONVERSION                                                         │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Convert to domain model.
    /// </summary>
    public CodeBlock ToModel() => new()
    {
        Id = Id,
        MessageId = MessageId,
        SequenceNumber = SequenceNumber,
        Content = Content,
        Language = Language,
        DisplayLanguage = DisplayLanguage,
        TargetFilePath = TargetFilePath,
        BlockType = BlockType,
        Status = Status,
        ConfidenceScore = ConfidenceScore
    };
}
