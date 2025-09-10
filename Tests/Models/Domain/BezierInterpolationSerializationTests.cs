using System;
using System.Collections.Generic;
using System.Text.Json;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Models.Domain
{
    /// <summary>
    /// Unit tests for BezierInterpolation serialization with compact flat array format
    /// </summary>
    public class BezierInterpolationSerializationTests
    {
        [Fact]
        public void Serialize_BezierInterpolation_WritesCompactArray()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 0.42, Y = 0.0 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            var options = new JsonSerializerOptions
            {
                Converters = { new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Act
            var json = JsonSerializer.Serialize(bezier, options);

            // Assert
            Assert.Equal("[0.42,0]", json);
        }

        [Fact]
        public void Deserialize_CompactArray_ReadsBezierInterpolation()
        {
            // Arrange
            var json = "[0,0,0.42,0,1,1]";
            var options = new JsonSerializerOptions
            {
                Converters = { new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Act
            var result = JsonSerializer.Deserialize<IInterpolationDefinition>(json, options);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BezierInterpolation>(result);
            var bezier = (BezierInterpolation)result;
            Assert.Equal(5, bezier.ControlPoints.Count);
            Assert.Equal(0.0, bezier.ControlPoints[0].X);
            Assert.Equal(0.0, bezier.ControlPoints[0].Y);
            Assert.Equal(0.0, bezier.ControlPoints[1].X);
            Assert.Equal(0.0, bezier.ControlPoints[1].Y);
            Assert.Equal(0.42, bezier.ControlPoints[2].X);
            Assert.Equal(0.0, bezier.ControlPoints[2].Y);
            Assert.Equal(1.0, bezier.ControlPoints[3].X);
            Assert.Equal(1.0, bezier.ControlPoints[3].Y);
            Assert.Equal(1.0, bezier.ControlPoints[4].X);
            Assert.Equal(1.0, bezier.ControlPoints[4].Y);
        }



        [Fact]
        public void Deserialize_ObjectFormat_FlatArrayControlPoints_ReadsBezierInterpolation()
        {
            // Arrange - This matches the actual JSON format from the user's example
            var json = "{\"type\":\"BezierInterpolation\",\"controlPoints\":[0.42,0,1,1]}";
            var options = new JsonSerializerOptions
            {
                Converters = { new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Act
            var result = JsonSerializer.Deserialize<IInterpolationDefinition>(json, options);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BezierInterpolation>(result);
            var bezier = (BezierInterpolation)result;
            Assert.Equal(4, bezier.ControlPoints.Count); // 2 original + 2 implicit points

            // Verify the middle control points (the ones from the JSON)
            Assert.Equal(0.42, bezier.ControlPoints[1].X);
            Assert.Equal(0.0, bezier.ControlPoints[1].Y);
            Assert.Equal(1.0, bezier.ControlPoints[2].X);
            Assert.Equal(1.0, bezier.ControlPoints[2].Y);

            // Verify implicit start and end points
            Assert.Equal(0.0, bezier.ControlPoints[0].X);
            Assert.Equal(0.0, bezier.ControlPoints[0].Y);
            Assert.Equal(1.0, bezier.ControlPoints[3].X);
            Assert.Equal(1.0, bezier.ControlPoints[3].Y);
        }

        [Fact]
        public void Deserialize_OddNumberOfValues_ThrowsException()
        {
            // Arrange
            var json = "[0,0,0.42,0,1]"; // 5 values - odd number
            var options = new JsonSerializerOptions
            {
                Converters = { new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Act & Assert
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<IInterpolationDefinition>(json, options));
            Assert.Contains("even number of values", exception.Message);
        }

        [Fact]
        public void Deserialize_NonNumericValues_ThrowsException()
        {
            // Arrange
            var json = "[0,0,\"invalid\",0,1,1]";
            var options = new JsonSerializerOptions
            {
                Converters = { new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Act & Assert
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<IInterpolationDefinition>(json, options));
            Assert.Contains("Expected number", exception.Message);
        }

        [Fact]
        public void Serialize_ComplexBezierCurve_WritesCompactArray()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 0.1, Y = 0.9 },
                    new Point { X = 0.3, Y = 0.1 },
                    new Point { X = 0.5, Y = 0.8 },
                    new Point { X = 0.7, Y = 0.2 },
                    new Point { X = 0.9, Y = 0.1 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            var options = new JsonSerializerOptions
            {
                Converters = { new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Act
            var json = JsonSerializer.Serialize(bezier, options);

            // Assert
            Assert.Equal("[0.1,0.9,0.3,0.1,0.5,0.8,0.7,0.2,0.9,0.1]", json);
        }

        [Fact]
        public void Deserialize_ComplexBezierCurve_ReadsCorrectly()
        {
            // Arrange
            var json = "[0,0,0.1,0.9,0.3,0.1,0.5,0.8,0.7,0.2,0.9,0.1,1,1]";
            var options = new JsonSerializerOptions
            {
                Converters = { new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Act
            var result = JsonSerializer.Deserialize<IInterpolationDefinition>(json, options);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BezierInterpolation>(result);
            var bezier = (BezierInterpolation)result;
            Assert.Equal(9, bezier.ControlPoints.Count);

            // Verify first and last points
            Assert.Equal(0.0, bezier.ControlPoints[0].X);
            Assert.Equal(0.0, bezier.ControlPoints[0].Y);
            Assert.Equal(1.0, bezier.ControlPoints[8].X);
            Assert.Equal(1.0, bezier.ControlPoints[8].Y);
        }
    }
}