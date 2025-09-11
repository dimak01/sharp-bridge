using System;
using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Models.Domain;
using Xunit;

namespace SharpBridge.Tests.Models.Domain
{
    public class ParameterExtremumsTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var extremums = new ParameterExtremums();

            // Assert
            extremums.Min.Should().Be(0);
            extremums.Max.Should().Be(0);
            extremums.HasExtremums.Should().BeFalse();
        }

        [Fact]
        public void UpdateExtremums_WithFirstValue_ShouldSetBothMinAndMax()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            var value = 5.5;

            // Act
            extremums.UpdateExtremums(value);

            // Assert
            extremums.Min.Should().Be(value);
            extremums.Max.Should().Be(value);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_WithNewMinimum_ShouldUpdateMin()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(5.0); // First value
            var newMin = 2.0;

            // Act
            extremums.UpdateExtremums(newMin);

            // Assert
            extremums.Min.Should().Be(newMin);
            extremums.Max.Should().Be(5.0);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_WithNewMaximum_ShouldUpdateMax()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(5.0); // First value
            var newMax = 8.0;

            // Act
            extremums.UpdateExtremums(newMax);

            // Assert
            extremums.Min.Should().Be(5.0);
            extremums.Max.Should().Be(newMax);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_WithValueBetweenMinAndMax_ShouldNotChangeExtremums()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(5.0); // First value
            extremums.UpdateExtremums(8.0); // Set max
            extremums.UpdateExtremums(2.0); // Set min
            var middleValue = 6.0;

            // Act
            extremums.UpdateExtremums(middleValue);

            // Assert
            extremums.Min.Should().Be(2.0);
            extremums.Max.Should().Be(8.0);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_WithEqualValues_ShouldMaintainExtremums()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(5.0); // First value
            extremums.UpdateExtremums(8.0); // Set max
            extremums.UpdateExtremums(2.0); // Set min
            var equalValue = 5.0;

            // Act
            extremums.UpdateExtremums(equalValue);

            // Assert
            extremums.Min.Should().Be(2.0);
            extremums.Max.Should().Be(8.0);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_WithNegativeValue_ShouldHandleCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            var negativeValue = -3.5;

            // Act
            extremums.UpdateExtremums(negativeValue);

            // Assert
            extremums.Min.Should().Be(negativeValue);
            extremums.Max.Should().Be(negativeValue);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_WithZeroValue_ShouldHandleCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            var zeroValue = 0.0;

            // Act
            extremums.UpdateExtremums(zeroValue);

            // Assert
            extremums.Min.Should().Be(zeroValue);
            extremums.Max.Should().Be(zeroValue);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_WithLargeValue_ShouldHandleCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            var largeValue = 999999.99;

            // Act
            extremums.UpdateExtremums(largeValue);

            // Assert
            extremums.Min.Should().Be(largeValue);
            extremums.Max.Should().Be(largeValue);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void UpdateExtremums_WithDecimalValue_ShouldHandleCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            var decimalValue = 3.14159;

            // Act
            extremums.UpdateExtremums(decimalValue);

            // Assert
            extremums.Min.Should().Be(decimalValue);
            extremums.Max.Should().Be(decimalValue);
            extremums.HasExtremums.Should().BeTrue();
        }

        [Fact]
        public void Reset_ShouldClearExtremums()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(5.0);
            extremums.UpdateExtremums(8.0);
            extremums.UpdateExtremums(2.0);

            // Act
            extremums.Reset();

            // Assert
            extremums.Min.Should().Be(0);
            extremums.Max.Should().Be(0);
            extremums.HasExtremums.Should().BeFalse();
        }

        [Fact]
        public void Reset_AfterInitialization_ResetsToInitialState()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(10.0);

            // Act
            extremums.Reset();

            // Assert
            extremums.Min.Should().Be(0);
            extremums.Max.Should().Be(0);
            extremums.HasExtremums.Should().BeFalse();
        }

        [Fact]
        public void Reset_MultipleTimes_ShouldWorkCorrectly()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(5.0);
            extremums.UpdateExtremums(8.0);

            // Act & Assert
            extremums.Reset();
            extremums.Min.Should().Be(0);
            extremums.Max.Should().Be(0);
            extremums.HasExtremums.Should().BeFalse();

            extremums.UpdateExtremums(3.0);
            extremums.Min.Should().Be(3.0);
            extremums.Max.Should().Be(3.0);
            extremums.HasExtremums.Should().BeTrue();

            extremums.Reset();
            extremums.Min.Should().Be(0);
            extremums.Max.Should().Be(0);
            extremums.HasExtremums.Should().BeFalse();
        }

        [Fact]
        public void HasExtremums_WithNoValues_ShouldReturnFalse()
        {
            // Arrange
            var extremums = new ParameterExtremums();

            // Assert
            extremums.HasExtremums.Should().BeFalse();
        }

        [Fact]
        public void HasExtremums_WithValues_ShouldReturnTrue()
        {
            // Arrange
            var extremums = new ParameterExtremums();
            extremums.UpdateExtremums(5.0);

            // Assert
            extremums.HasExtremums.Should().BeTrue();
        }
    }
}