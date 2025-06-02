using System;
using Xunit;
using sharp_bridge.Utilities;

namespace sharp_bridge.Tests.Utilities;

public class DisplayFormattingTests
{
    [Theory]
    [InlineData(0, 0, 0, "0:00:00")]      // Zero duration
    [InlineData(0, 0, 5, "0:00:05")]      // Just seconds
    [InlineData(0, 5, 0, "0:05:00")]      // Just minutes
    [InlineData(1, 0, 0, "1:00:00")]      // Just hours
    [InlineData(1, 30, 45, "1:30:45")]    // Hours, minutes, seconds
    [InlineData(10, 0, 0, "10:00:00")]    // Double digit hours
    [InlineData(100, 0, 0, "100:00:00")]  // Triple digit hours
    [InlineData(1000, 0, 0, "1000:00:00")] // Four digit hours
    public void FormatDuration_WithDifferentDurations_ShowsCorrectFormat(int hours, int minutes, int seconds, string expected)
    {
        // Arrange
        var duration = new TimeSpan(hours, minutes, seconds);

        // Act
        var result = DisplayFormatting.FormatDuration(duration);

        // Assert
        Assert.Equal(expected, result);
    }
} 