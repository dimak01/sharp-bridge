using System;
using FluentAssertions;
using SharpBridge.Models;
using Xunit;

namespace SharpBridge.Tests.Models
{
    public class ParameterExtremumsTests
    {
        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Act
            var extremums = new ParameterExtremums();

            // Assert
            extremums.Min.Should().Be(0);
            extremums.Max.Should().Be(0);
            extremums.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public void UpdateExtremums_FirstValue_InitializesExtremums()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            const double firstValue = 0.5;

            // Act
            extremums.UpdateExtremums(firstValue);

            // Assert
            extremums.Min.Should().Be(firstValue);
            extremums.Max.Should().Be(firstValue);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_NewMinimum_UpdatesMin()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(0.5); // Initialize with 0.5
            const double newMin = -0.3;

            // Act
            extremums.UpdateExtremums(newMin);

            // Assert
            extremums.Min.Should().Be(newMin);
            extremums.Max.Should().Be(0.5);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_NewMaximum_UpdatesMax()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(0.5); // Initialize with 0.5
            const double newMax = 0.8;

            // Act
            extremums.UpdateExtremums(newMax);

            // Assert
            extremums.Min.Should().Be(0.5);
            extremums.Max.Should().Be(newMax);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_ValueBetweenMinMax_NoChange()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(-0.5); // Initialize with -0.5
            extremums.UpdateExtremums(0.8); // Set max to 0.8
            const double middleValue = 0.2;

            // Act
            extremums.UpdateExtremums(middleValue);

            // Assert
            extremums.Min.Should().Be(-0.5);
            extremums.Max.Should().Be(0.8);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_EqualToMin_NoChange()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(-0.5); // Initialize with -0.5
            extremums.UpdateExtremums(0.8); // Set max to 0.8
            const double equalToMin = -0.5;

            // Act
            extremums.UpdateExtremums(equalToMin);

            // Assert
            extremums.Min.Should().Be(-0.5);
            extremums.Max.Should().Be(0.8);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_EqualToMax_NoChange()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(-0.5); // Initialize with -0.5
            extremums.UpdateExtremums(0.8); // Set max to 0.8
            const double equalToMax = 0.8;

            // Act
            extremums.UpdateExtremums(equalToMax);

            // Assert
            extremums.Min.Should().Be(-0.5);
            extremums.Max.Should().Be(0.8);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_NegativeValues_TracksCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();

            // Act
            extremums.UpdateExtremums(-1.0);
            extremums.UpdateExtremums(-0.5);
            extremums.UpdateExtremums(-0.8);

            // Assert
            extremums.Min.Should().Be(-1.0);
            extremums.Max.Should().Be(-0.5);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_ZeroValues_TracksCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();

            // Act
            extremums.UpdateExtremums(0.0);
            extremums.UpdateExtremums(0.5);
            extremums.UpdateExtremums(-0.3);

            // Assert
            extremums.Min.Should().Be(-0.3);
            extremums.Max.Should().Be(0.5);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_LargeValues_TracksCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();

            // Act
            extremums.UpdateExtremums(1000.0);
            extremums.UpdateExtremums(500.0);
            extremums.UpdateExtremums(1500.0);

            // Assert
            extremums.Min.Should().Be(500.0);
            extremums.Max.Should().Be(1500.0);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_DecimalPrecision_TracksCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();

            // Act
            extremums.UpdateExtremums(0.123456789);
            extremums.UpdateExtremums(0.123456788);
            extremums.UpdateExtremums(0.123456790);

            // Assert
            extremums.Min.Should().Be(0.123456788);
            extremums.Max.Should().Be(0.123456790);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void Reset_AfterInitialization_ResetsToInitialState()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(0.5);
            extremums.UpdateExtremums(-0.3);
            extremums.UpdateExtremums(0.8);

            // Act
            extremums.Reset();

            // Assert
            extremums.Min.Should().Be(-0.3); // Min value is preserved
            extremums.Max.Should().Be(0.8);  // Max value is preserved
            extremums.IsInitialized.Should().BeFalse(); // Only IsInitialized is reset
        }

        [Fact]
        public void Reset_WithoutInitialization_RemainsInInitialState()
        {
            // Arrange
            var extremums = new ParameterExtremums();

            // Act
            extremums.Reset();

            // Assert
            extremums.Min.Should().Be(0);
            extremums.Max.Should().Be(0);
            extremums.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public void UpdateExtremums_AfterReset_ReinitializesCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(0.5);
            extremums.UpdateExtremums(-0.3);
            extremums.Reset();
            const double newValue = 0.8;

            // Act
            extremums.UpdateExtremums(newValue);

            // Assert
            extremums.Min.Should().Be(newValue);
            extremums.Max.Should().Be(newValue);
            extremums.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_MultipleResets_WorksCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();

            // Act & Assert - First cycle
            extremums.UpdateExtremums(0.5);
            extremums.UpdateExtremums(-0.3);
            extremums.Min.Should().Be(-0.3);
            extremums.Max.Should().Be(0.5);

            extremums.Reset();
            extremums.IsInitialized.Should().BeFalse();

            // Second cycle
            extremums.UpdateExtremums(1.0);
            extremums.UpdateExtremums(-1.0);
            extremums.Min.Should().Be(-1.0);
            extremums.Max.Should().Be(1.0);

            extremums.Reset();
            extremums.IsInitialized.Should().BeFalse();

            // Third cycle
            extremums.UpdateExtremums(0.0);
            extremums.Min.Should().Be(0.0);
            extremums.Max.Should().Be(0.0);
            extremums.IsInitialized.Should().BeTrue();
        }
    }
}