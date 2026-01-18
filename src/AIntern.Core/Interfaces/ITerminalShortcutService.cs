// ============================================================================
// File: ITerminalShortcutService.cs
// Path: src/AIntern.Core/Interfaces/ITerminalShortcutService.cs
// Description: Interface for managing and handling keyboard shortcuts.
//              Provides lookup, binding management, and conflict detection.
// Created: 2026-01-18
// AI Intern v0.5.5d - Keyboard Shortcuts System
// ============================================================================

namespace AIntern.Core.Interfaces;

using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ ITerminalShortcutService (v0.5.5d)                                           │
// │ Service for managing and handling keyboard shortcuts.                       │
// │                                                                              │
// │ Responsibilities:                                                           │
// │   - Binding lookup by key combination or action                             │
// │   - Category-based filtering for settings UI                                │
// │   - Custom binding management with persistence                              │
// │   - Conflict detection and resolution                                       │
// │   - Reset to defaults                                                       │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for managing and handling keyboard shortcuts.
/// </summary>
/// <remarks>
/// <para>
/// This service manages the mapping between keyboard combinations and actions.
/// It supports:
/// </para>
/// <list type="bullet">
///   <item><description>Lookup by key combination (for handling key events)</description></item>
///   <item><description>Lookup by action (for displaying current bindings)</description></item>
///   <item><description>Custom binding management with persistence</description></item>
///   <item><description>Conflict detection to prevent duplicate key assignments</description></item>
/// </list>
/// <para>Added in v0.5.5d.</para>
/// </remarks>
public interface ITerminalShortcutService
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Binding Retrieval
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets all registered keyboard bindings.
    /// </summary>
    /// <returns>Read-only list of all bindings.</returns>
    IReadOnlyList<KeyBinding> GetAllBindings();

    /// <summary>
    /// Gets bindings filtered by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>Read-only list of bindings in the category.</returns>
    IReadOnlyList<KeyBinding> GetBindingsByCategory(string category);

    /// <summary>
    /// Gets all unique binding categories.
    /// </summary>
    /// <returns>Read-only list of category names.</returns>
    /// <remarks>
    /// Categories are returned in alphabetical order.
    /// </remarks>
    IReadOnlyList<string> GetCategories();

    /// <summary>
    /// Gets the binding for a specific action.
    /// </summary>
    /// <param name="action">The action to look up.</param>
    /// <returns>The binding, or null if not found.</returns>
    KeyBinding? GetBinding(TerminalShortcutAction action);

    // ═══════════════════════════════════════════════════════════════════════════
    // Key Event Handling
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tries to match a key combination to an action.
    /// </summary>
    /// <param name="key">The pressed key (string name, e.g., "C", "F3").</param>
    /// <param name="modifiers">Active modifier keys.</param>
    /// <param name="action">The matched action, if any.</param>
    /// <returns>True if a binding was matched.</returns>
    /// <remarks>
    /// <para>
    /// This is the primary method for handling keyboard events.
    /// Call this in <c>OnKeyDown</c> to check if a key combination maps to an action.
    /// </para>
    /// <para>
    /// If a match is found, check <see cref="KeyBinding.PassToPty"/> to determine
    /// if the event should be passed to the PTY or handled by the application.
    /// </para>
    /// </remarks>
    bool TryGetAction(string key, KeyModifierFlags modifiers, out TerminalShortcutAction action);

    /// <summary>
    /// Gets the binding that matches a key combination.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="modifiers">The modifiers to look up.</param>
    /// <returns>The matching binding, or null if not found.</returns>
    KeyBinding? GetBindingByKey(string key, KeyModifierFlags modifiers);

    // ═══════════════════════════════════════════════════════════════════════════
    // Custom Binding Management
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates a key binding to a new key combination.
    /// </summary>
    /// <param name="action">The action to rebind.</param>
    /// <param name="key">The new key.</param>
    /// <param name="modifiers">The new modifiers.</param>
    /// <returns>True if binding was updated successfully.</returns>
    /// <remarks>
    /// <para>
    /// Returns <c>false</c> if:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>The action is not registered</description></item>
    ///   <item><description>The binding is not customizable</description></item>
    ///   <item><description>The key combination conflicts with an existing binding</description></item>
    /// </list>
    /// <para>
    /// Custom bindings are automatically persisted to settings.
    /// </para>
    /// </remarks>
    bool UpdateBinding(TerminalShortcutAction action, string key, KeyModifierFlags modifiers);

    /// <summary>
    /// Resets a binding to its default key combination.
    /// </summary>
    /// <param name="action">The action to reset.</param>
    /// <remarks>
    /// If the action is not registered, this method does nothing.
    /// </remarks>
    void ResetBinding(TerminalShortcutAction action);

    /// <summary>
    /// Resets all bindings to their default key combinations.
    /// </summary>
    /// <remarks>
    /// This clears all custom bindings from settings.
    /// </remarks>
    void ResetAllBindings();

    // ═══════════════════════════════════════════════════════════════════════════
    // Conflict Detection
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks if a key combination conflicts with existing bindings.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="modifiers">The modifiers to check.</param>
    /// <param name="exclude">Optional action to exclude from conflict check.</param>
    /// <returns>True if there's a conflict.</returns>
    /// <remarks>
    /// Use <paramref name="exclude"/> when checking a key combination for an
    /// action that's being updated (to avoid self-conflict).
    /// </remarks>
    bool HasConflict(string key, KeyModifierFlags modifiers, TerminalShortcutAction? exclude = null);

    /// <summary>
    /// Gets the binding that would conflict with a key combination.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="modifiers">The modifiers to check.</param>
    /// <param name="exclude">Optional action to exclude from conflict check.</param>
    /// <returns>The conflicting binding, or null if no conflict.</returns>
    KeyBinding? GetConflictingBinding(string key, KeyModifierFlags modifiers, TerminalShortcutAction? exclude = null);

    // ═══════════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Event raised when bindings are changed.
    /// </summary>
    /// <remarks>
    /// Subscribe to this event to update UI when bindings change.
    /// </remarks>
    event EventHandler? BindingsChanged;
}
