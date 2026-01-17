namespace AIntern.Desktop.Models;

/// <summary>
/// Defines the UI context where a keyboard shortcut is active (v0.4.5f).
/// Context-specific shortcuts take priority over global shortcuts.
/// </summary>
public enum ShortcutContext
{
    /// <summary>
    /// Shortcut is active in all contexts.
    /// </summary>
    Global = 0,

    /// <summary>
    /// Chat text input is focused.
    /// </summary>
    ChatInput = 1,

    /// <summary>
    /// Chat message list/panel is focused.
    /// </summary>
    ChatView = 2,

    /// <summary>
    /// A code block is focused or hovered.
    /// </summary>
    CodeBlock = 3,

    /// <summary>
    /// Diff viewer panel is active.
    /// </summary>
    DiffViewer = 4,

    /// <summary>
    /// File tree/browser is focused.
    /// </summary>
    FileTree = 5,

    /// <summary>
    /// Settings panel is open.
    /// </summary>
    Settings = 6,

    /// <summary>
    /// A modal dialog is open.
    /// </summary>
    Modal = 7,

    /// <summary>
    /// Change history panel is focused.
    /// </summary>
    ChangeHistory = 8,

    /// <summary>
    /// Editor panel is focused.
    /// </summary>
    Editor = 9
}

/// <summary>
/// Categories for grouping shortcuts in the settings UI (v0.4.5f).
/// </summary>
public enum ShortcutCategory
{
    /// <summary>
    /// General application shortcuts.
    /// </summary>
    General,

    /// <summary>
    /// Navigation between views/panels.
    /// </summary>
    Navigation,

    /// <summary>
    /// Code block operations.
    /// </summary>
    CodeBlocks,

    /// <summary>
    /// Diff viewer operations.
    /// </summary>
    DiffViewer,

    /// <summary>
    /// Chat and messaging.
    /// </summary>
    Chat,

    /// <summary>
    /// File operations.
    /// </summary>
    FileOperations,

    /// <summary>
    /// History and undo operations.
    /// </summary>
    History,

    /// <summary>
    /// Editor operations.
    /// </summary>
    Editor
}
