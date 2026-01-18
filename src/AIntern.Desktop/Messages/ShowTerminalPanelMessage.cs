namespace AIntern.Desktop.Messages;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHOW TERMINAL PANEL MESSAGE (v0.5.3f)                                   │
// │ Message to request showing/focusing the terminal panel.                 │
// └─────────────────────────────────────────────────────────────────────────┘

using CommunityToolkit.Mvvm.Messaging.Messages;

/// <summary>
/// Message to request showing or focusing the terminal panel.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3f.</para>
/// <para>
/// Sent by:
/// <list type="bullet">
///   <item>FileExplorerViewModel when opening terminal from context menu</item>
///   <item>Keyboard shortcut handlers (e.g., Ctrl+`)</item>
///   <item>Any component that needs to show the terminal panel</item>
/// </list>
/// </para>
/// <para>
/// Received by MainWindow which toggles the terminal panel visibility.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Show terminal panel
/// WeakReferenceMessenger.Default.Send(new ShowTerminalPanelMessage());
/// 
/// // Or with explicit visibility
/// WeakReferenceMessenger.Default.Send(new ShowTerminalPanelMessage(true));
/// </code>
/// </example>
public sealed class ShowTerminalPanelMessage : ValueChangedMessage<bool>
{
    /// <summary>
    /// Creates a message to show the terminal panel.
    /// </summary>
    public ShowTerminalPanelMessage() : base(true)
    {
    }

    /// <summary>
    /// Creates a message with explicit visibility.
    /// </summary>
    /// <param name="show">True to show, false to hide.</param>
    public ShowTerminalPanelMessage(bool show) : base(show)
    {
    }
}
