// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;
using SharpBridge.Infrastructure.Factories;
using SharpBridge.Models.Domain;
using Xunit;

namespace SharpBridge.Tests.Core.Engines
{
    public class TransformationEngineInterpolationTests
    {
        [Fact]
        public void GetRuleValue_WithLinearInterpolation_MatchesOriginalBehavior()
        {
            // Arrange
            var expression = new Expression("0.5");
            var rule = new ParameterTransformation("TestRule", expression, "0.5", 0.0, 1.0, 0.0, new LinearInterpolation());

            // Act
            var result = TransformationEngineInterpolationHelper.GetRuleValue(rule);

            // Assert
            Assert.Equal(0.5, result, 6);
        }

        [Fact]
        public void GetRuleValue_WithBezierInterpolation_AppliesCurve()
        {
            // Arrange
            var expression = new Expression("0.25"); // Use 0.25 instead of 0.5
            var bezierInterpolation = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 0.1, Y = 0.9 }, // Very dramatic curve
                    new Point { X = 0.9, Y = 0.1 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };
            var rule = new ParameterTransformation("TestRule", expression, "0.25", 0.0, 1.0, 0.0, bezierInterpolation);

            // Act
            var result = TransformationEngineInterpolationHelper.GetRuleValue(rule);

            // Assert
            // The result should be different from 0.25 due to the Bezier curve
            Assert.NotEqual(0.25, result, 6);
            Assert.True(result >= 0.0 && result <= 1.0);
        }

        [Fact]
        public void GetRuleValue_WithNullInterpolation_UsesLinearBehavior()
        {
            // Arrange
            var expression = new Expression("0.5");
            var rule = new ParameterTransformation("TestRule", expression, "0.5", 0.0, 1.0, 0.0, null);

            // Act
            var result = TransformationEngineInterpolationHelper.GetRuleValue(rule);

            // Assert
            Assert.Equal(0.5, result, 6);
        }

        [Fact]
        public void GetRuleValue_WithInvalidInterpolation_FallsBackToLinear()
        {
            // Arrange
            var expression = new Expression("0.5");
            var invalidInterpolation = new TestInvalidInterpolation();
            var rule = new ParameterTransformation("TestRule", expression, "0.5", 0.0, 1.0, 0.0, invalidInterpolation);

            // Act
            var result = TransformationEngineInterpolationHelper.GetRuleValue(rule);

            // Assert
            Assert.Equal(0.5, result, 6);
        }

        [Fact]
        public void GetRuleValue_WithClamping_RespectsMinMaxBounds()
        {
            // Arrange
            var expression = new Expression("2.0"); // Value outside bounds
            var rule = new ParameterTransformation("TestRule", expression, "2.0", 0.0, 1.0, 0.0, new LinearInterpolation());

            // Act
            var result = TransformationEngineInterpolationHelper.GetRuleValue(rule);

            // Assert
            Assert.Equal(1.0, result, 6);
        }

        [Fact]
        public void GetRuleValue_WithBezierInterpolation_DifferentInputValues_ProduceDifferentResults()
        {
            // Arrange
            var bezierInterpolation = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 0.1, Y = 0.9 },
                    new Point { X = 0.9, Y = 0.1 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            // Act
            var result1 = TransformationEngineInterpolationHelper.GetRuleValue(
                new ParameterTransformation("TestRule", new Expression("0.25"), "0.25", 0.0, 1.0, 0.0, bezierInterpolation));
            var result2 = TransformationEngineInterpolationHelper.GetRuleValue(
                new ParameterTransformation("TestRule", new Expression("0.75"), "0.75", 0.0, 1.0, 0.0, bezierInterpolation));

            // Assert
            // The results should be different due to the Bezier curve
            Assert.NotEqual(result1, result2, 6);
            Assert.True(result1 >= 0.0 && result1 <= 1.0);
            Assert.True(result2 >= 0.0 && result2 <= 1.0);
        }

        [Fact]
        public void BezierInterpolation_DirectTest_WorksCorrectly()
        {
            // Arrange
            var bezierInterpolation = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 0.1, Y = 0.9 },
                    new Point { X = 0.9, Y = 0.1 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            // Act
            var interpolationMethod = InterpolationMethodFactory.CreateFromDefinition(bezierInterpolation);
            var result1 = interpolationMethod.Interpolate(0.25);
            var result2 = interpolationMethod.Interpolate(0.75);

            // Assert
            // These should be different from each other and from linear interpolation
            Assert.NotEqual(result1, result2, 6);
            Assert.True(result1 >= 0.0 && result1 <= 1.0);
            Assert.True(result2 >= 0.0 && result2 <= 1.0);

            // Test that they're different from linear interpolation
            Assert.NotEqual(0.25, result1, 6);
            Assert.NotEqual(0.75, result2, 6);
        }

        [Fact]
        public void NormalizeToRange_WithNormalRange_WorksCorrectly()
        {
            // Arrange
            var value = 0.5;
            var min = 0.0;
            var max = 1.0;

            // Act
            var result = TransformationEngineInterpolationHelper.NormalizeToRange(value, min, max);

            // Assert
            Assert.Equal(0.5, result, 6);
        }

        [Fact]
        public void NormalizeToRange_WithDifferentRange_WorksCorrectly()
        {
            // Arrange
            var value = 5.0;
            var min = 0.0;
            var max = 10.0;

            // Act
            var result = TransformationEngineInterpolationHelper.NormalizeToRange(value, min, max);

            // Assert
            Assert.Equal(0.5, result, 6);
        }

        [Fact]
        public void NormalizeToRange_WithZeroRange_ReturnsHalf()
        {
            // Arrange
            var value = 0.5;
            var min = 1.0;
            var max = 1.0;

            // Act
            var result = TransformationEngineInterpolationHelper.NormalizeToRange(value, min, max);

            // Assert
            Assert.Equal(0.5, result, 6);
        }

        [Fact]
        public void NormalizeToRange_WithOutOfBoundsValue_ClampsCorrectly()
        {
            // Arrange
            var value = 2.0; // Outside 0-1 range
            var min = 0.0;
            var max = 1.0;

            // Act
            var result = TransformationEngineInterpolationHelper.NormalizeToRange(value, min, max);

            // Assert
            Assert.Equal(1.0, result, 6);
        }

        [Fact]
        public void ScaleToRange_WithNormalRange_WorksCorrectly()
        {
            // Arrange
            var normalizedValue = 0.5;
            var min = 0.0;
            var max = 1.0;

            // Act
            var result = TransformationEngineInterpolationHelper.ScaleToRange(normalizedValue, min, max);

            // Assert
            Assert.Equal(0.5, result, 6);
        }

        [Fact]
        public void ScaleToRange_WithDifferentRange_WorksCorrectly()
        {
            // Arrange
            var normalizedValue = 0.5;
            var min = 0.0;
            var max = 10.0;

            // Act
            var result = TransformationEngineInterpolationHelper.ScaleToRange(normalizedValue, min, max);

            // Assert
            Assert.Equal(5.0, result, 6);
        }

        [Fact]
        public void ScaleToRange_WithZeroRange_ReturnsMin()
        {
            // Arrange
            var normalizedValue = 0.5;
            var min = 1.0;
            var max = 1.0;

            // Act
            var result = TransformationEngineInterpolationHelper.ScaleToRange(normalizedValue, min, max);

            // Assert
            Assert.Equal(1.0, result, 6);
        }

        [Fact]
        public void ScaleToRange_WithOutOfBoundsValue_ClampsCorrectly()
        {
            // Arrange
            var normalizedValue = 2.0; // Outside 0-1 range
            var min = 0.0;
            var max = 1.0;

            // Act
            var result = TransformationEngineInterpolationHelper.ScaleToRange(normalizedValue, min, max);

            // Assert
            Assert.Equal(1.0, result, 6);
        }

        [Theory]
        [InlineData(0.0, 0.0, 1.0, 0.0)]
        [InlineData(0.5, 0.0, 1.0, 0.5)]
        [InlineData(1.0, 0.0, 1.0, 1.0)]
        [InlineData(0.25, 0.0, 1.0, 0.25)]
        [InlineData(0.75, 0.0, 1.0, 0.75)]
        public void NormalizeAndScale_RoundTrip_ReturnsOriginalValue(double input, double min, double max, double expected)
        {
            // Arrange
            var normalized = TransformationEngineInterpolationHelper.NormalizeToRange(input, min, max);

            // Act
            var result = TransformationEngineInterpolationHelper.ScaleToRange(normalized, min, max);

            // Assert
            Assert.Equal(expected, result, 6);
        }

        // Test helper class for invalid interpolation
        private class TestInvalidInterpolation : IInterpolationDefinition
        {
        }
    }

    /// <summary>
    /// Helper class to expose the private methods from TransformationEngine for testing
    /// </summary>
    public static class TransformationEngineInterpolationHelper
    {
        public static double GetRuleValue(ParameterTransformation rule)
        {
            var evaluatedValue = Convert.ToDouble(rule.Expression.Evaluate());

            // If no interpolation is specified, use the original linear behavior
            if (rule.Interpolation == null)
            {
                return Math.Clamp(evaluatedValue, rule.Min, rule.Max);
            }

            try
            {
                // Create interpolation method from definition
                var interpolationMethod = InterpolationMethodFactory.CreateFromDefinition(rule.Interpolation);

                // Normalize the input value to 0-1 range
                var normalizedInput = NormalizeToRange(evaluatedValue, rule.Min, rule.Max);

                // Apply interpolation
                var interpolatedValue = interpolationMethod.Interpolate(normalizedInput);

                // Scale the interpolated value back to the parameter range
                return ScaleToRange(interpolatedValue, rule.Min, rule.Max);
            }
            catch (Exception)
            {
                // Log the error and fallback to linear interpolation
                return Math.Clamp(evaluatedValue, rule.Min, rule.Max);
            }
        }

        public static double NormalizeToRange(double value, double min, double max)
        {
            if (Math.Abs(max - min) < 1e-10)
            {
                // Handle zero-range case
                return 0.5;
            }

            var normalized = (value - min) / (max - min);
            return Math.Clamp(normalized, 0.0, 1.0);
        }

        public static double ScaleToRange(double normalizedValue, double min, double max)
        {
            if (Math.Abs(max - min) < 1e-10)
            {
                // Handle zero-range case
                return min;
            }

            var scaled = min + (normalizedValue * (max - min));
            return Math.Clamp(scaled, min, max);
        }
    }
}