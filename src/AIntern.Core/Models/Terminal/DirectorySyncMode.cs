namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIRECTORY SYNC MODE (v0.5.3c)                                           │
// │ Defines how terminal syncs with file explorer/workspace.                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Defines how terminal working directory syncs with the file explorer.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3c.</para>
/// <para>
/// Controls the relationship between the terminal's current working
/// directory and the file explorer or workspace navigation.
/// </para>
/// </remarks>
public enum DirectorySyncMode
{
    /// <summary>
    /// Only sync the currently active/focused terminal tab.
    /// </summary>
    /// <remarks>
    /// Other terminal tabs maintain their own directories independently.
    /// This is the recommended default.
    /// </remarks>
    ActiveTerminalOnly,

    /// <summary>
    /// Sync all terminal tabs that are linked to the current workspace.
    /// </summary>
    /// <remarks>
    /// All terminals in the workspace follow directory navigation.
    /// </remarks>
    AllLinkedTerminals,

    /// <summary>
    /// Never auto-sync. Directory changes are manual only.
    /// </summary>
    /// <remarks>
    /// Terminal directories are fully independent of file explorer.
    /// </remarks>
    Manual
}
