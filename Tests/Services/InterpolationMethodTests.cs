using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class InterpolationMethodTests
    {
        [Fact]
        public void LinearInterpolationMethod_Interpolate_ReturnsInputValue()
        {
            // Arrange
            var linear = new LinearInterpolationMethod();

            // Act & Assert
            Assert.Equal(0.0, linear.Interpolate(0.0), 6);
            Assert.Equal(0.5, linear.Interpolate(0.5), 6);
            Assert.Equal(1.0, linear.Interpolate(1.0), 6);
            Assert.Equal(0.25, linear.Interpolate(0.25), 6);
            Assert.Equal(0.75, linear.Interpolate(0.75), 6);
        }

        [Fact]
        public void LinearInterpolationMethod_GetDisplayName_ReturnsLinear()
        {
            // Arrange
            var linear = new LinearInterpolationMethod();

            // Act
            var displayName = linear.GetDisplayName();

            // Assert
            Assert.Equal("Linear", displayName);
        }

        [Fact]
        public void BezierInterpolationMethod_WithTwoControlPoints_MatchesLinear()
        {
            // Arrange
            var controlPoints = new List<Point>
            {
                new Point { X = 0.0, Y = 0.0 },
                new Point { X = 1.0, Y = 1.0 }
            };
            var bezier = new BezierInterpolationMethod(controlPoints);

            // Act & Assert
            Assert.Equal(0.0, bezier.Interpolate(0.0), 6);
            Assert.Equal(0.5, bezier.Interpolate(0.5), 6);
            Assert.Equal(1.0, bezier.Interpolate(1.0), 6);
        }

        [Fact]
        public void BezierInterpolationMethod_WithThreeControlPoints_QuadraticCurve()
        {
            // Arrange
            var controlPoints = new List<Point>
            {
                new Point { X = 0.0, Y = 0.0 },
                new Point { X = 0.5, Y = 0.5 },
                new Point { X = 1.0, Y = 1.0 }
            };
            var bezier = new BezierInterpolationMethod(controlPoints);

            // Act & Assert
            Assert.Equal(0.0, bezier.Interpolate(0.0), 6);
            Assert.Equal(0.5, bezier.Interpolate(0.5), 6);
            Assert.Equal(1.0, bezier.Interpolate(1.0), 6);
        }

        [Fact]
        public void BezierInterpolationMethod_WithFourControlPoints_CubicCurve()
        {
            // Arrange
            var controlPoints = new List<Point>
            {
                new Point { X = 0.0, Y = 0.0 },
                new Point { X = 0.3, Y = 0.1 },
                new Point { X = 0.7, Y = 0.9 },
                new Point { X = 1.0, Y = 1.0 }
            };
            var bezier = new BezierInterpolationMethod(controlPoints);

            // Act & Assert
            Assert.Equal(0.0, bezier.Interpolate(0.0), 6);
            Assert.Equal(1.0, bezier.Interpolate(1.0), 6);

            // Test some intermediate values
            var t0_25 = bezier.Interpolate(0.25);
            var t0_75 = bezier.Interpolate(0.75);

            Assert.True(t0_25 > 0.0 && t0_25 < 1.0);
            Assert.True(t0_75 > 0.0 && t0_75 < 1.0);
        }

        [Fact]
        public void BezierInterpolationMethod_WithFiveControlPoints_HigherOrderCurve()
        {
            // Arrange
            var controlPoints = new List<Point>
            {
                new Point { X = 0.0, Y = 0.0 },
                new Point { X = 0.25, Y = 0.1 },
                new Point { X = 0.5, Y = 0.5 },
                new Point { X = 0.75, Y = 0.9 },
                new Point { X = 1.0, Y = 1.0 }
            };
            var bezier = new BezierInterpolationMethod(controlPoints);

            // Act & Assert
            Assert.Equal(0.0, bezier.Interpolate(0.0), 6);
            Assert.Equal(1.0, bezier.Interpolate(1.0), 6);
        }

        [Fact]
        public void BezierInterpolationMethod_GetDisplayName_ReturnsCorrectFormat()
        {
            // Arrange
            var controlPoints = new List<Point>
            {
                new Point { X = 0.0, Y = 0.0 },
                new Point { X = 0.5, Y = 0.5 },
                new Point { X = 1.0, Y = 1.0 }
            };
            var bezier = new BezierInterpolationMethod(controlPoints);

            // Act
            var displayName = bezier.GetDisplayName();

            // Assert
            Assert.Equal("Bezier (3 points)", displayName);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        public void BezierInterpolationMethod_InvalidParameter_ThrowsException(double t)
        {
            // Arrange
            var controlPoints = new List<Point>
            {
                new Point { X = 0.0, Y = 0.0 },
                new Point { X = 1.0, Y = 1.0 }
            };
            var bezier = new BezierInterpolationMethod(controlPoints);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => bezier.Interpolate(t));
        }

        [Fact]
        public void BezierInterpolationMethod_NullControlPoints_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BezierInterpolationMethod(null!));
        }

        [Fact]
        public void BezierInterpolationMethod_TooFewControlPoints_ThrowsException()
        {
            // Arrange
            var controlPoints = new List<Point>
            {
                new Point { X = 0.0, Y = 0.0 }
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new BezierInterpolationMethod(controlPoints));
        }

        [Fact]
        public void BezierInterpolationMethod_TooManyControlPoints_ThrowsException()
        {
            // Arrange
            var controlPoints = Enumerable.Range(0, 10).Select(i => new Point { X = i / 9.0, Y = i / 9.0 }).ToList();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new BezierInterpolationMethod(controlPoints));
        }

        [Fact]
        public void InterpolationMethodFactory_CreateFromDefinition_LinearInterpolation()
        {
            // Arrange
            var definition = new LinearInterpolation();

            // Act
            var method = InterpolationMethodFactory.CreateFromDefinition(definition);

            // Assert
            Assert.NotNull(method);
            Assert.IsType<LinearInterpolationMethod>(method);
            Assert.Equal("Linear", method.GetDisplayName());
        }

        [Fact]
        public void InterpolationMethodFactory_CreateFromDefinition_BezierInterpolation()
        {
            // Arrange
            var definition = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            // Act
            var method = InterpolationMethodFactory.CreateFromDefinition(definition);

            // Assert
            Assert.NotNull(method);
            Assert.IsType<BezierInterpolationMethod>(method);
            Assert.Equal("Bezier (2 points)", method.GetDisplayName());
        }

        [Fact]
        public void InterpolationMethodFactory_CreateFromDefinition_UnsupportedType_ThrowsException()
        {
            // Arrange
            var unsupportedDefinition = new TestInterpolationDefinition();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => InterpolationMethodFactory.CreateFromDefinition(unsupportedDefinition));
        }

        [Fact]
        public void InterpolationMethodFactory_CreateFromDefinition_NullDefinition_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => InterpolationMethodFactory.CreateFromDefinition(null!));
        }

        [Fact]
        public void InterpolationMethodFactory_ValidateDefinition_LinearInterpolation()
        {
            // Arrange
            var definition = new LinearInterpolation();

            // Act
            var isValid = InterpolationMethodFactory.ValidateDefinition(definition);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void InterpolationMethodFactory_ValidateDefinition_ValidBezierInterpolation()
        {
            // Arrange
            var definition = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            // Act
            var isValid = InterpolationMethodFactory.ValidateDefinition(definition);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void InterpolationMethodFactory_ValidateDefinition_InvalidBezierInterpolation()
        {
            // Arrange
            var definition = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 }
                }
            };

            // Act
            var isValid = InterpolationMethodFactory.ValidateDefinition(definition);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void InterpolationMethodFactory_ValidateDefinition_NullDefinition()
        {
            // Act
            var isValid = InterpolationMethodFactory.ValidateDefinition(null!);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void InterpolationMethodFactory_ValidateDefinition_UnsupportedType()
        {
            // Arrange
            var unsupportedDefinition = new TestInterpolationDefinition();

            // Act
            var isValid = InterpolationMethodFactory.ValidateDefinition(unsupportedDefinition);

            // Assert
            Assert.False(isValid);
        }

        // Test helper class for unsupported interpolation types
        private class TestInterpolationDefinition : IInterpolationDefinition
        {
        }
    }
}