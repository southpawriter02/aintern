// -----------------------------------------------------------------------
// <copyright file="TerminalContextViewModelFactory.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Desktop.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL CONTEXT VIEWMODEL FACTORY (v0.5.4g)                            │
// │ Factory for creating TerminalContextViewModel with proper DI.           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Factory for creating <see cref="TerminalContextViewModel"/> instances with proper DI.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4g.</para>
/// <para>
/// Provides factory methods for different capture scenarios:
/// </para>
/// <list type="bullet">
/// <item><see cref="Create"/> - From existing capture</item>
/// <item><see cref="CreateFromFullBufferAsync"/> - Full buffer capture</item>
/// <item><see cref="CreateFromLastLinesAsync"/> - Last N lines</item>
/// <item><see cref="CreateFromSelectionAsync"/> - User selection</item>
/// </list>
/// </remarks>
public sealed class TerminalContextViewModelFactory
{
    // ═══════════════════════════════════════════════════════════════════════
    // DEPENDENCIES
    // ═══════════════════════════════════════════════════════════════════════

    private readonly IOutputCaptureService _captureService;
    private readonly ITerminalService _terminalService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TerminalContextViewModelFactory> _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalContextViewModelFactory"/> class.
    /// </summary>
    public TerminalContextViewModelFactory(
        IOutputCaptureService captureService,
        ITerminalService terminalService,
        ILoggerFactory loggerFactory)
    {
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<TerminalContextViewModelFactory>();

        _logger.LogDebug("TerminalContextViewModelFactory initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FACTORY METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Create a ViewModel from an existing capture.
    /// </summary>
    /// <param name="capture">The captured terminal output.</param>
    /// <returns>A configured ViewModel.</returns>
    public TerminalContextViewModel Create(TerminalOutputCapture capture)
    {
        ArgumentNullException.ThrowIfNull(capture);

        var viewModel = new TerminalContextViewModel(
            _captureService,
            _terminalService,
            _loggerFactory.CreateLogger<TerminalContextViewModel>())
        {
            Capture = capture
        };

        _logger.LogTrace("Created TerminalContextViewModel for session {SessionId}, mode {Mode}",
            capture.SessionId, capture.CaptureMode);

        return viewModel;
    }

    /// <summary>
    /// Create a ViewModel and capture the full buffer.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A ViewModel with captured output.</returns>
    public async Task<TerminalContextViewModel> CreateFromFullBufferAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Capturing full buffer from session {SessionId}", sessionId);

        var capture = await _captureService.CaptureBufferAsync(
            sessionId,
            OutputCaptureMode.FullBuffer,
            ct: ct);

        _logger.LogInformation("Full buffer captured: {Lines} lines, {Tokens} tokens",
            capture.LineCount, capture.EstimatedTokens);

        return Create(capture);
    }

    /// <summary>
    /// Create a ViewModel and capture last N lines.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="lineCount">Number of lines to capture (default: 50).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A ViewModel with captured output.</returns>
    public async Task<TerminalContextViewModel> CreateFromLastLinesAsync(
        Guid sessionId,
        int lineCount = 50,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Capturing last {LineCount} lines from session {SessionId}",
            lineCount, sessionId);

        var capture = await _captureService.CaptureBufferAsync(
            sessionId,
            OutputCaptureMode.LastNLines,
            lineCount,
            ct);

        _logger.LogInformation("Last {LineCount} lines captured: {ActualLines} lines, {Tokens} tokens",
            lineCount, capture.LineCount, capture.EstimatedTokens);

        return Create(capture);
    }

    /// <summary>
    /// Create a ViewModel from user selection.
    /// </summary>
    /// <param name="sessionId">Terminal session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A ViewModel with captured selection, or null if nothing selected.</returns>
    public async Task<TerminalContextViewModel?> CreateFromSelectionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Capturing selection from session {SessionId}", sessionId);

        var capture = await _captureService.CaptureSelectionAsync(sessionId, ct);

        if (capture == null)
        {
            _logger.LogDebug("No selection to capture from session {SessionId}", sessionId);
            return null;
        }

        _logger.LogInformation("Selection captured: {Lines} lines, {Tokens} tokens",
            capture.LineCount, capture.EstimatedTokens);

        return Create(capture);
    }

    /// <summary>
    /// Create a ViewModel from the active terminal session's full buffer.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A ViewModel with captured output, or null if no active session.</returns>
    public async Task<TerminalContextViewModel?> CreateFromActiveSessionAsync(
        CancellationToken ct = default)
    {
        var activeSession = _terminalService.ActiveSession;
        if (activeSession == null)
        {
            _logger.LogDebug("No active terminal session for capture");
            return null;
        }

        return await CreateFromFullBufferAsync(activeSession.Id, ct);
    }
}
