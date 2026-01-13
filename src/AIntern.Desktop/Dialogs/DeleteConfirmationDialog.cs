using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Dialogs;

/// <summary>
/// Dialog for confirming deletion of a conversation.
/// </summary>
/// <remarks>
/// <para>
/// This static helper class creates and shows a modal dialog when the user attempts
/// to delete a conversation. The dialog emphasizes that deletion is permanent and
/// cannot be undone.
/// </para>
/// <para>
/// The dialog presents two options:
/// <list type="bullet">
///   <item><description><b>Cancel</b>: Abort the deletion (default focus)</description></item>
///   <item><description><b>Delete</b>: Permanently delete the conversation</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var confirmed = await DeleteConfirmationDialog.ShowAsync(ownerWindow, conversationTitle);
/// if (confirmed)
/// {
///     await DeleteConversationAsync(conversationId);
/// }
/// </code>
/// </example>
public static class DeleteConfirmationDialog
{
    #region Constants

    /// <summary>
    /// Maximum length for conversation title display in the dialog.
    /// Titles longer than this are truncated with ellipsis.
    /// </summary>
    private const int TitleMaxLength = 40;

    #endregion

    #region Public Methods

    /// <summary>
    /// Shows the delete confirmation dialog and returns whether the user confirmed.
    /// </summary>
    /// <param name="owner">The owner window for modal positioning.</param>
    /// <param name="conversationTitle">
    /// The title of the conversation to delete.
    /// Truncated to 40 characters if longer.
    /// </param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>
    /// <c>true</c> if the user confirmed deletion; <c>false</c> if cancelled.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The dialog is displayed as a modal window centered over the owner.
    /// The Cancel button receives initial focus as it's the safer default action.
    /// </para>
    /// <para>
    /// The Delete button uses destructive styling (red background) to indicate
    /// the irreversible nature of the action.
    /// </para>
    /// </remarks>
    public static async Task<bool> ShowAsync(
        Window owner,
        string conversationTitle,
        ILogger? logger = null)
    {
        var stopwatch = Stopwatch.StartNew();
        logger?.LogDebug(
            "[ENTER] DeleteConfirmationDialog.ShowAsync - Title: {Title}",
            conversationTitle);

        // Truncate title if too long.
        var displayTitle = conversationTitle.Length > TitleMaxLength
            ? conversationTitle[..TitleMaxLength] + "..."
            : conversationTitle;

        // Create result tracking variable.
        var confirmed = false;

        // Create the dialog window.
        var dialog = new Window
        {
            Title = "Delete Conversation",
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
            Text = $"Delete \"{displayTitle}\"?",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = Application.Current?.FindResource("TextPrimary") as IBrush
                ?? Brushes.White
        };

        // Warning text (destructive color).
        var warningText = new TextBlock
        {
            Text = "This action cannot be undone. The conversation and all its messages will be permanently deleted.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Application.Current?.FindResource("DestructiveForeground") as IBrush
                ?? new SolidColorBrush(Color.Parse("#F48771")),
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

        // Cancel button (default focus for safety).
        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(16, 8)
        };
        cancelButton.Click += (_, _) =>
        {
            logger?.LogDebug("[INFO] DeleteConfirmationDialog - User clicked Cancel");
            confirmed = false;
            dialog.Close();
        };

        // Delete button (destructive styling).
        var deleteButton = new Button
        {
            Content = "Delete",
            Padding = new Thickness(16, 8)
        };

        // Apply destructive theme from resources.
        if (Application.Current?.FindResource("DestructiveButton") is ControlTheme destructiveTheme)
        {
            deleteButton.Theme = destructiveTheme;
        }
        else
        {
            // Fallback styling if theme not found.
            deleteButton.Background = new SolidColorBrush(Color.Parse("#DC3545"));
            deleteButton.Foreground = Brushes.White;
        }
        deleteButton.Click += (_, _) =>
        {
            logger?.LogDebug("[INFO] DeleteConfirmationDialog - User clicked Delete");
            confirmed = true;
            dialog.Close();
        };

        // Add buttons to panel (Cancel, Delete).
        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(deleteButton);

        // Assemble the layout.
        mainPanel.Children.Add(messageText);
        mainPanel.Children.Add(warningText);
        mainPanel.Children.Add(buttonPanel);

        dialog.Content = mainPanel;

        // Set focus to Cancel button when dialog opens (safer default).
        dialog.Opened += (_, _) =>
        {
            logger?.LogDebug("[INFO] DeleteConfirmationDialog - Dialog opened, focusing Cancel button");
            cancelButton.Focus();
        };

        // Show the dialog.
        logger?.LogInformation(
            "[INFO] DeleteConfirmationDialog - Showing dialog for conversation: {Title}",
            displayTitle);

        await dialog.ShowDialog(owner);

        stopwatch.Stop();
        logger?.LogDebug(
            "[EXIT] DeleteConfirmationDialog.ShowAsync - Confirmed: {Confirmed}, Duration: {ElapsedMs}ms",
            confirmed, stopwatch.ElapsedMilliseconds);

        return confirmed;
    }

    #endregion
}
