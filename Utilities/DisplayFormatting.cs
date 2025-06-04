using System;

namespace SharpBridge.Utilities;

public static class DisplayFormatting
{
    public static string FormatDuration(TimeSpan duration)
    {
        return $"{(int)duration.TotalHours:D1}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }
} 