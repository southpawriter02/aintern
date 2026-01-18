// -----------------------------------------------------------------------
// <copyright file="TerminalContextViewModel.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Desktop.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL CONTEXT VIEWMODEL (v0.5.4g)                                    │
// │ ViewModel for terminal output attached to chat context.                 │
// │ Displays capture information and provides context for AI prompts.       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for terminal output attached to chat context.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4g.</para>
/// <para>
/// This ViewModel wraps a <see cref="TerminalOutputCapture"/> and provides:
/// </para>
/// <list type="bullet">
/// <item>Display properties for UI (preview, tokens, truncation)</item>
/// <item>Formatted context string for AI prompts</item>
/// <item>Refresh capability to re-capture output</item>
/// </list>
/// </remarks>
public sealed partial class TerminalContextViewModel : ViewModelBase
{
    // ═══════════════════════════════════════════════════════════════════════
    // DEPENDENCIES
    // ═══════════════════════════════════════════════════════════════════════

    private readonly IOutputCaptureService _captureService;
    private readonly ITerminalService _terminalService;
    private readonly ILogger<TerminalContextViewModel> _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The captured terminal output.
    /// </summary>
    [ObservableProperty]
    private TerminalOutputCapture? _capture;

    /// <summary>
    /// Whether the context is expanded to show full output.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED DISPLAY PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Display name for the context pill.
    /// </summary>
    /// <example>"bash - /project"</example>
    public string DisplayName => Capture?.SessionName ?? "Terminal Output";

    /// <summary>
    /// Short preview of the captured output (100 chars max).
    /// </summary>
    /// <remarks>
    /// Newlines are replaced with spaces for single-line display.
    /// </remarks>
    public string Preview
    {
        get
        {
            if (Capture == null)
                return string.Empty;

            var preview = Capture.Output;
            if (preview.Length > 100)
            {
                preview = preview[..97] + "...";
            }
            return preview.Replace('\n', ' ').Replace('\r', ' ');
        }
    }

    /// <summary>
    /// Full output for expanded view.
    /// </summary>
    public string FullOutput => Capture?.Output ?? string.Empty;

    /// <summary>
    /// Estimated token count for LLM context.
    /// </summary>
    public int EstimatedTokens => Capture?.EstimatedTokens ?? 0;

    /// <summary>
    /// Whether the captured output was truncated.
    /// </summary>
    public bool IsTruncated => Capture?.IsTruncated ?? false;

    /// <summary>
    /// Working directory at capture time.
    /// </summary>
    public string? WorkingDirectory => Capture?.WorkingDirectory;

    /// <summary>
    /// Command that was run (if captured during execution).
    /// </summary>
    public string? Command => Capture?.Command;

    /// <summary>
    /// Whether a command was captured.
    /// </summary>
    public bool HasCommand => !string.IsNullOrEmpty(Command);

    /// <summary>
    /// Capture mode used.
    /// </summary>
    public OutputCaptureMode CaptureMode => Capture?.CaptureMode ?? OutputCaptureMode.FullBuffer;

    /// <summary>
    /// Human-readable capture mode text.
    /// </summary>
    public string CaptureModeText => CaptureMode switch
    {
        OutputCaptureMode.FullBuffer => "Full Buffer",
        OutputCaptureMode.LastCommand => "Last Command",
        OutputCaptureMode.LastNLines => "Last Lines",
        OutputCaptureMode.Selection => "Selection",
        OutputCaptureMode.Manual => "Manual",
        _ => "Unknown"
    };

    /// <summary>
    /// When the capture was taken.
    /// </summary>
    public DateTime CaptureTime => Capture?.CompletedAt ?? DateTime.MinValue;

    /// <summary>
    /// Human-readable capture time.
    /// </summary>
    /// <example>"14:23:45"</example>
    public string CaptureTimeText => CaptureTime == DateTime.MinValue
        ? ""
        : CaptureTime.ToLocalTime().ToString("HH:mm:ss");

    /// <summary>
    /// Formatted context string for AI prompt.
    /// </summary>
    public string ContextString => Capture?.ToContextString() ?? string.Empty;

    /// <summary>
    /// Output line count.
    /// </summary>
    public int LineCount => Capture?.LineCount ?? 0;

    /// <summary>
    /// Original length before truncation.
    /// </summary>
    public int OriginalLength => Capture?.OriginalLength ?? 0;

    /// <summary>
    /// Whether content is different from original due to truncation.
    /// </summary>
    public bool WasTruncated => IsTruncated ||
        (Capture != null && Capture.Output.Length < Capture.OriginalLength);

    /// <summary>
    /// Truncation summary text.
    /// </summary>
    /// <example>"Truncated from 5,000 chars"</example>
    public string TruncationText => WasTruncated
        ? $"Truncated from {OriginalLength:N0} chars"
        : "";

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalContextViewModel"/> class.
    /// </summary>
    /// <param name="captureService">Output capture service.</param>
    /// <param name="terminalService">Terminal service for session info.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public TerminalContextViewModel(
        IOutputCaptureService captureService,
        ITerminalService terminalService,
        ILogger<TerminalContextViewModel> logger)
    {
        _captureService = captureService;
        _terminalService = terminalService;
        _logger = logger;

        _logger.LogDebug("TerminalContextViewModel created");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Toggle the expanded state.
    /// </summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
        _logger.LogTrace("TerminalContext expanded: {Expanded}", IsExpanded);
    }

    /// <summary>
    /// Refresh by re-capturing from the same session with same mode.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Capture == null)
        {
            _logger.LogDebug("Cannot refresh: no capture present");
            return;
        }

        _logger.LogDebug("Refreshing terminal capture from session {SessionId}", Capture.SessionId);

        try
        {
            ClearError();

            var newCapture = await _captureService.CaptureBufferAsync(
                Capture.SessionId,
                Capture.CaptureMode);

            Capture = newCapture;

            _logger.LogInformation("Terminal capture refreshed: {Lines} lines, {Tokens} tokens",
                newCapture.LineCount, newCapture.EstimatedTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh terminal capture");
            SetError(ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PROPERTY CHANGE HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the Capture property changes.
    /// </summary>
    partial void OnCaptureChanged(TerminalOutputCapture? value)
    {
        _logger.LogTrace("Capture changed: {SessionId}, {Mode}",
            value?.SessionId, value?.CaptureMode);

        // Notify all computed properties
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Preview));
        OnPropertyChanged(nameof(FullOutput));
        OnPropertyChanged(nameof(EstimatedTokens));
        OnPropertyChanged(nameof(IsTruncated));
        OnPropertyChanged(nameof(WorkingDirectory));
        OnPropertyChanged(nameof(Command));
        OnPropertyChanged(nameof(HasCommand));
        OnPropertyChanged(nameof(CaptureMode));
        OnPropertyChanged(nameof(CaptureModeText));
        OnPropertyChanged(nameof(CaptureTime));
        OnPropertyChanged(nameof(CaptureTimeText));
        OnPropertyChanged(nameof(ContextString));
        OnPropertyChanged(nameof(LineCount));
        OnPropertyChanged(nameof(OriginalLength));
        OnPropertyChanged(nameof(WasTruncated));
        OnPropertyChanged(nameof(TruncationText));
    }
}
