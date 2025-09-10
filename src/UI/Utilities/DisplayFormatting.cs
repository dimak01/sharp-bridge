using System;

namespace SharpBridge.UI.Utilities;

/// <summary>
/// Provides utility methods for formatting values for display
/// </summary>
public static class DisplayFormatting
{
    /// <summary>
    /// Formats a TimeSpan into a human-readable duration string (H:MM:SS)
    /// </summary>
    /// <param name="duration">The time span to format</param>
    /// <returns>Formatted duration string</returns>
    public static string FormatDuration(TimeSpan duration)
    {
        return $"{(int)duration.TotalHours:D1}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }
} 