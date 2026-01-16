using System.Globalization;
using Avalonia.Data.Converters;
using AIntern.Core.Models;

namespace AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PHASE TO TITLE CONVERTER (v0.4.4g)                                      │
// │ Maps BatchApplyPhase to human-readable display titles.                  │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Converts a <see cref="BatchApplyPhase"/> to a human-readable title string.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4g.</para>
/// </remarks>
public class PhaseToTitleConverter : IValueConverter
{
    /// <summary>
    /// Phase-to-title mappings.
    /// </summary>
    private static readonly Dictionary<BatchApplyPhase, string> PhaseTitles = new()
    {
        [BatchApplyPhase.Validating] = "Validating...",
        [BatchApplyPhase.CreatingBackups] = "Creating Backups...",
        [BatchApplyPhase.CreatingDirectories] = "Creating Directories...",
        [BatchApplyPhase.WritingFiles] = "Writing Files...",
        [BatchApplyPhase.Finalizing] = "Finalizing...",
        [BatchApplyPhase.Completed] = "Complete!",
        [BatchApplyPhase.RollingBack] = "Rolling Back..."
    };

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BatchApplyPhase phase)
        {
            return "Processing...";
        }

        return PhaseTitles.TryGetValue(phase, out var title)
            ? title
            : "Processing...";
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("PhaseToTitleConverter is one-way only.");
    }
}
