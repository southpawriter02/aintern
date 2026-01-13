using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Dialogs;

/// <summary>
/// Dialog for confirming unsaved changes before navigation or close.
/// </summary>
/// <remarks>
/// <para>
/// This static helper class creates and shows a modal dialog when the user attempts
/// to navigate away from or close a conversation with unsaved changes.
/// </para>
/// <para>
/// The dialog presents three options:
/// <list type="bullet">
///   <item><description><b>Save</b>: Save changes before proceeding</description></item>
///   <item><description><b>Don't Save</b>: Discard changes and proceed</description></item>
///   <item><description><b>Cancel</b>: Abort the operation and stay</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// if (HasUnsavedChanges)
/// {
///     var result = await UnsavedChangesDialog.ShowAsync(ownerWindow, conversationTitle);
///     switch (result)
///     {
///         case UnsavedChangesDialog.Result.Save:
///             await SaveConversationAsync();
///             break;
///         case UnsavedChangesDialog.Result.DontSave:
///             // Proceed without saving
///             break;
///         case UnsavedChangesDialog.Result.Cancel:
///             return; // Abort operation
///     }
/// }
/// </code>
/// </example>
public static class UnsavedChangesDialog
{
    #region Constants

    /// <summary>
    /// Maximum length for conversation title display in the dialog.
    /// Titles longer than this are truncated with ellipsis.
    /// </summary>
    private const int TitleMaxLength = 40;

    #endregion

    #region Result Enum

    /// <summary>
    /// Result of the unsaved changes dialog.
    /// </summary>
    public enum Result
    {
        /// <summary>User chose to save changes before proceeding.</summary>
        Save,

        /// <summary>User chose to discard changes and proceed.</summary>
        DontSave,

        /// <summary>User cancelled the operation.</summary>
        Cancel
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Shows the unsaved changes dialog and returns the user's choice.
    /// </summary>
    /// <param name="owner">The owner window for modal positioning.</param>
    /// <param name="conversationTitle">
    /// The title of the conversation with unsaved changes.
    /// Truncated to 40 characters if longer.
    /// </param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>The user's choice: Save, DontSave, or Cancel.</returns>
    /// <remarks>
    /// <para>
    /// The dialog is displayed as a modal window centered over the owner.
    /// The Save button receives initial focus as it's the safest default action.
    /// </para>
    /// <para>
    /// Button order follows platform conventions: Cancel, Don't Save, Save (left to right).
    /// </para>
    /// </remarks>
    public static async Task<Result> ShowAsync(
        Window owner,
        string conversationTitle,
        ILogger? logger = null)
    {
        var stopwatch = Stopwatch.StartNew();
        logger?.LogDebug(
            "[ENTER] UnsavedChangesDialog.ShowAsync - Title: {Title}",
            conversationTitle);

        // Truncate title if too long.
        var displayTitle = conversationTitle.Length > TitleMaxLength
            ? conversationTitle[..TitleMaxLength] + "..."
            : conversationTitle;

        // Create result tracking variable.
        var result = Result.Cancel;

        // Create the dialog window.
        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.BorderOnly,
            Background = Application.Current?.FindResource("WindowBackground") as IBrush
                ?? new SolidColorBrush(Color.Parse("#1E1E1E"))
        };

        // Create the content layout.
        var mainPanel = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16
        };

        // Message text.
        var messageText = new TextBlock
        {
            Text = $"Do you want to save changes to \"{displayTitle}\"?",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Application.Current?.FindResource("TextPrimary") as IBrush
                ?? Brushes.White
        };

        // Secondary text.
        var secondaryText = new TextBlock
        {
            Text = "Your changes will be lost if you don't save them.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Application.Current?.FindResource("TextMuted") as IBrush
                ?? Brushes.Gray,
            FontSize = 12
        };

        // Button panel.
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // Cancel button.
        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(16, 8)
        };
        cancelButton.Click += (_, _) =>
        {
            logger?.LogDebug("[INFO] UnsavedChangesDialog - User clicked Cancel");
            result = Result.Cancel;
            dialog.Close();
        };

        // Don't Save button.
        var dontSaveButton = new Button
        {
            Content = "Don't Save",
            Padding = new Thickness(16, 8)
        };
        dontSaveButton.Click += (_, _) =>
        {
            logger?.LogDebug("[INFO] UnsavedChangesDialog - User clicked Don't Save");
            result = Result.DontSave;
            dialog.Close();
        };

        // Save button (primary action).
        var saveButton = new Button
        {
            Content = "Save",
            Padding = new Thickness(16, 8),
            Background = Application.Current?.FindResource("AccentBrush") as IBrush
                ?? new SolidColorBrush(Color.Parse("#007ACC")),
            Foreground = Brushes.White
        };
        saveButton.Click += (_, _) =>
        {
            logger?.LogDebug("[INFO] UnsavedChangesDialog - User clicked Save");
            result = Result.Save;
            dialog.Close();
        };

        // Add buttons to panel (Cancel, Don't Save, Save).
        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(dontSaveButton);
        buttonPanel.Children.Add(saveButton);

        // Assemble the layout.
        mainPanel.Children.Add(messageText);
        mainPanel.Children.Add(secondaryText);
        mainPanel.Children.Add(buttonPanel);

        dialog.Content = mainPanel;

        // Set focus to Save button when dialog opens (safest default).
        dialog.Opened += (_, _) =>
        {
            logger?.LogDebug("[INFO] UnsavedChangesDialog - Dialog opened, focusing Save button");
            saveButton.Focus();
        };

        // Show the dialog.
        logger?.LogInformation(
            "[INFO] UnsavedChangesDialog - Showing dialog for conversation: {Title}",
            displayTitle);

        await dialog.ShowDialog(owner);

        stopwatch.Stop();
        logger?.LogDebug(
            "[EXIT] UnsavedChangesDialog.ShowAsync - Result: {Result}, Duration: {ElapsedMs}ms",
            result, stopwatch.ElapsedMilliseconds);

        return result;
    }

    #endregion
}
