namespace AIntern.Core.Models;

/// <summary>
/// Represents a date/time range for filtering (v0.4.5h).
/// </summary>
/// <param name="Start">The start of the range (inclusive). Null means no lower bound.</param>
/// <param name="End">The end of the range (inclusive). Null means no upper bound.</param>
public sealed record DateTimeRange(DateTime? Start, DateTime? End)
{
    /// <summary>
    /// Checks if a value falls within this range.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is within the range.</returns>
    public bool Contains(DateTime value)
    {
        if (Start.HasValue && value < Start.Value) return false;
        if (End.HasValue && value > End.Value) return false;
        return true;
    }

    /// <summary>
    /// Creates a range for the last N days.
    /// </summary>
    public static DateTimeRange LastNDays(int days) =>
        new(DateTime.UtcNow.AddDays(-days), null);

    /// <summary>
    /// Creates a range for the last N hours.
    /// </summary>
    public static DateTimeRange LastNHours(int hours) =>
        new(DateTime.UtcNow.AddHours(-hours), null);

    /// <summary>
    /// Creates a range for today only.
    /// </summary>
    public static DateTimeRange Today =>
        new(DateTime.UtcNow.Date, null);

    /// <summary>
    /// Creates a range for yesterday only.
    /// </summary>
    public static DateTimeRange Yesterday =>
        new(DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date);
}
