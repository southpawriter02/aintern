// ============================================================================
// File: TerminalShortcutAction.cs
// Path: src/AIntern.Core/Models/Terminal/TerminalShortcutAction.cs
// Description: Enum defining all available terminal keyboard shortcut actions.
//              Actions are organized by category with number ranges for grouping.
//              Each action maps to a specific terminal or application action.
// Created: 2026-01-18
// AI Intern v0.5.5d - Keyboard Shortcuts System
// ============================================================================

namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalShortcutAction (v0.5.5d)                                             │
// │ Enum defining all available terminal keyboard shortcut actions.             │
// │                                                                              │
// │ Categories:                                                                  │
// │   0-99:    Terminal Panel (toggle, tabs, navigation)                        │
// │   100-199: Terminal Input (PTY pass-through: SIGINT, SIGTSTP, EOF)          │
// │   200-299: Terminal Search (open, close, navigate results)                  │
// │   300-399: Terminal Selection (copy, paste, select all)                     │
// │   400-499: Terminal Scroll (page, line, top, bottom)                        │
// │   500-599: Command Blocks (execute, send to terminal)                       │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Enum defining all available terminal keyboard shortcut actions.
/// Actions are organized by category with number ranges for grouping.
/// </summary>
/// <remarks>
/// <para>
/// Each action corresponds to a specific terminal or application action
/// that can be triggered via keyboard shortcuts.
/// </para>
/// <para>
/// Actions in the Terminal Input category (100-199) are marked with
/// <c>PassToPty = true</c> in their <see cref="KeyBinding"/> to allow
/// the PTY to handle the shortcut instead of the application.
/// </para>
/// <para>Added in v0.5.5d.</para>
/// </remarks>
public enum TerminalShortcutAction
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Terminal Panel (0-99)
    // Actions for managing the terminal panel and tabs.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Show/hide the terminal panel.
    /// Default: Ctrl+` (backtick)
    /// </summary>
    ToggleTerminal = 0,

    /// <summary>
    /// Create a new terminal tab.
    /// Default: Ctrl+Shift+`
    /// </summary>
    NewTerminal = 1,

    /// <summary>
    /// Close the active terminal tab.
    /// Default: Ctrl+Shift+W
    /// </summary>
    CloseTerminal = 2,

    /// <summary>
    /// Switch to the previous terminal tab.
    /// Default: Ctrl+PageUp
    /// </summary>
    PreviousTerminalTab = 3,

    /// <summary>
    /// Switch to the next terminal tab.
    /// Default: Ctrl+PageDown
    /// </summary>
    NextTerminalTab = 4,

    /// <summary>
    /// Switch to terminal tab 1.
    /// Default: Ctrl+Shift+1
    /// </summary>
    SwitchToTerminal1 = 5,

    /// <summary>
    /// Switch to terminal tab 2.
    /// Default: Ctrl+Shift+2
    /// </summary>
    SwitchToTerminal2 = 6,

    /// <summary>
    /// Switch to terminal tab 3.
    /// Default: Ctrl+Shift+3
    /// </summary>
    SwitchToTerminal3 = 7,

    /// <summary>
    /// Switch to terminal tab 4.
    /// Default: Ctrl+Shift+4
    /// </summary>
    SwitchToTerminal4 = 8,

    /// <summary>
    /// Switch to terminal tab 5.
    /// Default: Ctrl+Shift+5
    /// </summary>
    SwitchToTerminal5 = 9,

    /// <summary>
    /// Switch to terminal tab 6.
    /// Default: Ctrl+Shift+6
    /// </summary>
    SwitchToTerminal6 = 10,

    /// <summary>
    /// Switch to terminal tab 7.
    /// Default: Ctrl+Shift+7
    /// </summary>
    SwitchToTerminal7 = 11,

    /// <summary>
    /// Switch to terminal tab 8.
    /// Default: Ctrl+Shift+8
    /// </summary>
    SwitchToTerminal8 = 12,

    /// <summary>
    /// Switch to terminal tab 9.
    /// Default: Ctrl+Shift+9
    /// </summary>
    SwitchToTerminal9 = 13,

    /// <summary>
    /// Maximize or restore the terminal panel.
    /// Default: Ctrl+Shift+M
    /// </summary>
    MaximizeTerminal = 14,

    /// <summary>
    /// Focus the terminal panel.
    /// Default: None (used programmatically)
    /// </summary>
    FocusTerminal = 15,

    // ═══════════════════════════════════════════════════════════════════════════
    // Terminal Input (100-199) - PTY Pass-Through
    // These shortcuts are passed to the PTY for shell handling.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Send SIGINT (interrupt current process).
    /// Default: Ctrl+C (PassToPty = true)
    /// </summary>
    /// <remarks>
    /// Sends the interrupt signal to the foreground process group.
    /// Typically terminates the currently running command.
    /// </remarks>
    SendInterrupt = 100,

    /// <summary>
    /// Send SIGTSTP (suspend current process).
    /// Default: Ctrl+Z (PassToPty = true)
    /// </summary>
    /// <remarks>
    /// Suspends the foreground process. Can be resumed with 'fg' or 'bg'.
    /// </remarks>
    SendSuspend = 101,

    /// <summary>
    /// Send EOF (end of file).
    /// Default: Ctrl+D (PassToPty = true)
    /// </summary>
    /// <remarks>
    /// Signals end-of-input to the shell. On empty line, may close the shell.
    /// </remarks>
    SendEof = 102,

    /// <summary>
    /// Clear the terminal screen.
    /// Default: Ctrl+L (PassToPty = true)
    /// </summary>
    /// <remarks>
    /// Clears the terminal display and redraws the prompt.
    /// Scrollback history is preserved.
    /// </remarks>
    ClearTerminal = 103,

    /// <summary>
    /// Clear line before cursor (Unix line-kill).
    /// Default: Ctrl+U (PassToPty = true)
    /// </summary>
    ClearLineBefore = 104,

    /// <summary>
    /// Clear line after cursor.
    /// Default: Ctrl+K (PassToPty = true)
    /// </summary>
    ClearLineAfter = 105,

    /// <summary>
    /// Delete word before cursor.
    /// Default: Ctrl+W (PassToPty = true)
    /// </summary>
    DeleteWordBefore = 106,

    /// <summary>
    /// Move cursor to line start.
    /// Default: Ctrl+A (PassToPty = true)
    /// </summary>
    MoveToLineStart = 107,

    /// <summary>
    /// Move cursor to line end.
    /// Default: Ctrl+E (PassToPty = true)
    /// </summary>
    MoveToLineEnd = 108,

    // ═══════════════════════════════════════════════════════════════════════════
    // Terminal Search (200-299)
    // Actions for the terminal search feature.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Open the terminal search bar.
    /// Default: Ctrl+F
    /// </summary>
    OpenSearch = 200,

    /// <summary>
    /// Close the terminal search bar.
    /// Default: Escape
    /// </summary>
    CloseSearch = 201,

    /// <summary>
    /// Navigate to the next search result.
    /// Default: F3 (also Enter in search bar)
    /// </summary>
    NextSearchResult = 202,

    /// <summary>
    /// Navigate to the previous search result.
    /// Default: Shift+F3 (also Shift+Enter in search bar)
    /// </summary>
    PreviousSearchResult = 203,

    /// <summary>
    /// Toggle case sensitivity in search.
    /// Default: Alt+C (in search bar)
    /// </summary>
    ToggleSearchCaseSensitive = 204,

    /// <summary>
    /// Toggle regex mode in search.
    /// Default: Alt+R (in search bar)
    /// </summary>
    ToggleSearchRegex = 205,

    // ═══════════════════════════════════════════════════════════════════════════
    // Terminal Selection (300-399)
    // Actions for text selection and clipboard operations.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Copy selected text to clipboard.
    /// Default: Ctrl+Shift+C
    /// </summary>
    /// <remarks>
    /// Uses Ctrl+Shift+C instead of Ctrl+C to avoid conflict with SIGINT.
    /// </remarks>
    Copy = 300,

    /// <summary>
    /// Paste from clipboard.
    /// Default: Ctrl+Shift+V
    /// </summary>
    /// <remarks>
    /// Uses Ctrl+Shift+V instead of Ctrl+V for consistency with Copy.
    /// </remarks>
    Paste = 301,

    /// <summary>
    /// Select all terminal content.
    /// Default: Ctrl+Shift+A
    /// </summary>
    /// <remarks>
    /// Selects all visible content and scrollback buffer.
    /// Uses Ctrl+Shift+A to avoid conflict with Ctrl+A (line start).
    /// </remarks>
    SelectAll = 302,

    /// <summary>
    /// Clear selection without copying.
    /// Default: Escape (when selection active)
    /// </summary>
    ClearSelection = 303,

    // ═══════════════════════════════════════════════════════════════════════════
    // Terminal Scroll (400-499)
    // Actions for scrolling the terminal viewport.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Scroll up one page.
    /// Default: Shift+PageUp
    /// </summary>
    ScrollPageUp = 400,

    /// <summary>
    /// Scroll down one page.
    /// Default: Shift+PageDown
    /// </summary>
    ScrollPageDown = 401,

    /// <summary>
    /// Scroll to the top of the buffer (oldest content).
    /// Default: Shift+Home
    /// </summary>
    ScrollToTop = 402,

    /// <summary>
    /// Scroll to the bottom of the buffer (latest content).
    /// Default: Shift+End
    /// </summary>
    ScrollToBottom = 403,

    /// <summary>
    /// Scroll up one line.
    /// Default: Ctrl+Shift+Up
    /// </summary>
    ScrollLineUp = 404,

    /// <summary>
    /// Scroll down one line.
    /// Default: Ctrl+Shift+Down
    /// </summary>
    ScrollLineDown = 405,

    // ═══════════════════════════════════════════════════════════════════════════
    // Command Blocks (500-599)
    // Actions for chat command block interactions.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Execute the focused command block in the terminal.
    /// Default: Ctrl+Enter
    /// </summary>
    /// <remarks>
    /// Executes the command from a chat message code block in the active terminal.
    /// </remarks>
    ExecuteCommand = 500,

    /// <summary>
    /// Send command to terminal without executing (types but doesn't press Enter).
    /// Default: Ctrl+Shift+Enter
    /// </summary>
    /// <remarks>
    /// Useful for reviewing or modifying the command before execution.
    /// </remarks>
    SendToTerminal = 501,

    /// <summary>
    /// Copy command to clipboard.
    /// Default: Ctrl+Shift+C (on command block)
    /// </summary>
    CopyCommand = 502
}
