namespace AIntern.Desktop.Messages;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ OPEN TERMINAL MESSAGE (v0.5.3f)                                         │
// │ Message to request creating a new terminal with specific profile.       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Message to request opening a new terminal session with specific settings.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3f.</para>
/// <para>
/// Sent by:
/// <list type="bullet">
///   <item>FileExplorerViewModel context menu commands</item>
///   <item>Shell selector dialog</item>
///   <item>Any component that needs to create a terminal</item>
/// </list>
/// </para>
/// <para>
/// Received by TerminalPanelViewModel which creates the session via ITerminalService.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// WeakReferenceMessenger.Default.Send(new OpenTerminalMessage
/// {
///     ProfileId = selectedProfile.Id,
///     WorkingDirectory = "/home/user/projects",
///     WorkspaceId = currentWorkspace?.Id
/// });
/// </code>
/// </example>
public sealed class OpenTerminalMessage
{
    /// <summary>
    /// Profile ID to use for the new terminal.
    /// </summary>
    /// <remarks>
    /// If null, the default profile will be used.
    /// </remarks>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// Working directory for the new terminal.
    /// </summary>
    /// <remarks>
    /// If empty, the shell's default behavior is used (typically home directory).
    /// </remarks>
    public string WorkingDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Optional workspace ID to link the terminal session to.
    /// </summary>
    /// <remarks>
    /// When set, enables bi-directional directory synchronization
    /// between the terminal and file explorer.
    /// </remarks>
    public Guid? WorkspaceId { get; init; }

    /// <summary>
    /// Returns a string representation of the message.
    /// </summary>
    public override string ToString() =>
        $"OpenTerminalMessage(Profile={ProfileId?.ToString()[..8] ?? "default"}, Dir={WorkingDirectory})";
}
