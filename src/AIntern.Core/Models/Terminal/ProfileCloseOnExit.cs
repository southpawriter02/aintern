namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROFILE CLOSE ON EXIT (v0.5.3c)                                         │
// │ Defines behavior when a shell process exits.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Defines behavior when a shell process exits.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3c.</para>
/// <para>
/// Controls whether the terminal tab closes automatically when
/// the shell process terminates, based on exit conditions.
/// </para>
/// </remarks>
public enum ProfileCloseOnExit
{
    /// <summary>
    /// Always close the terminal tab when the shell exits.
    /// </summary>
    /// <remarks>
    /// The tab closes regardless of exit code.
    /// </remarks>
    Always,

    /// <summary>
    /// Close only if the shell exits with code 0 (success).
    /// </summary>
    /// <remarks>
    /// Non-zero exit codes keep the tab open to show error state.
    /// This is the recommended default for development.
    /// </remarks>
    OnCleanExit,

    /// <summary>
    /// Never auto-close the tab.
    /// </summary>
    /// <remarks>
    /// Shows "[Process exited]" message and requires manual close.
    /// Useful for debugging or reviewing command output.
    /// </remarks>
    Never
}
