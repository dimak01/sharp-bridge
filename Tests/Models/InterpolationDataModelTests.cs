using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Models
{
    public class InterpolationDataModelTests
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public InterpolationDataModelTests()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(), new InterpolationConverter(), new BezierInterpolationConverter() }
            };
        }

        [Fact]
        public void LinearInterpolation_Serialization_WorksCorrectly()
        {
            // Arrange
            var linear = new LinearInterpolation();

            // Act
            var json = JsonSerializer.Serialize(linear, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<LinearInterpolation>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.IsType<LinearInterpolation>(deserialized);
        }

        [Fact]
        public void BezierInterpolation_Serialization_WorksCorrectly()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 0.3, Y = 0.1 },
                    new Point { X = 0.7, Y = 0.9 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(bezier, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<BezierInterpolation>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.IsType<BezierInterpolation>(deserialized);
            Assert.Equal(4, deserialized.ControlPoints.Count);
            Assert.Equal(0.0, deserialized.ControlPoints[0].X);
            Assert.Equal(0.0, deserialized.ControlPoints[0].Y);
            Assert.Equal(1.0, deserialized.ControlPoints[3].X);
            Assert.Equal(1.0, deserialized.ControlPoints[3].Y);
        }

        [Fact]
        public void Point_Validation_XAndYMustBeZeroToOne()
        {
            // Arrange
            var point = new Point { X = 0.5, Y = 0.7 };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(point, new ValidationContext(point), results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(-0.1, 0.5)]
        [InlineData(1.1, 0.5)]
        [InlineData(0.5, -0.1)]
        [InlineData(0.5, 1.1)]
        public void Point_Validation_RejectsOutOfRangeValues(double x, double y)
        {
            // Arrange
            var point = new Point { X = x, Y = y };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(point, new ValidationContext(point), results, true);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void BezierInterpolation_Validation_RequiresMinimumTwoControlPoints()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 }
                }
            };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(bezier, new ValidationContext(bezier), results, true);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("at least 2 control points"));
        }

        [Fact]
        public void BezierInterpolation_Validation_AcceptsValidControlPoints()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0.0, Y = 0.0 },
                    new Point { X = 1.0, Y = 1.0 }
                }
            };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(bezier, new ValidationContext(bezier), results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public void BezierInterpolation_Validation_RejectsTooManyControlPoints()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = Enumerable.Range(0, 10).Select(i => new Point { X = i / 9.0, Y = i / 9.0 }).ToList()
            };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(bezier, new ValidationContext(bezier), results, true);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("maximum 8 control points"));
        }

        [Fact]
        public void ParameterRuleDefinition_BackwardCompatibility_MissingInterpolationDefaultsToNull()
        {
            // Arrange
            var json = @"{
                ""name"": ""TestParam"",
                ""func"": ""HeadPosX"",
                ""min"": -1.0,
                ""max"": 1.0,
                ""defaultValue"": 0.0
            }";

            // Act
            var rule = JsonSerializer.Deserialize<ParameterRuleDefinition>(json, _jsonOptions);

            // Assert
            Assert.NotNull(rule);
            Assert.Null(rule.Interpolation);
        }

        [Fact]
        public void ParameterRuleDefinition_WithLinearInterpolation_DeserializesCorrectly()
        {
            // Arrange
            var json = @"{
                ""name"": ""TestParam"",
                ""func"": ""HeadPosX"",
                ""min"": -1.0,
                ""max"": 1.0,
                ""defaultValue"": 0.0,
                ""interpolation"": {
                    ""type"": ""LinearInterpolation""
                }
            }";

            // Act
            var rule = JsonSerializer.Deserialize<ParameterRuleDefinition>(json, _jsonOptions);

            // Assert
            Assert.NotNull(rule);
            Assert.NotNull(rule.Interpolation);
            Assert.IsType<LinearInterpolation>(rule.Interpolation!);
        }

        [Fact]
        public void ParameterRuleDefinition_WithBezierInterpolation_DeserializesCorrectly()
        {
            // Arrange
            var json = @"{
                ""name"": ""TestParam"",
                ""func"": ""HeadPosX"",
                ""min"": -1.0,
                ""max"": 1.0,
                ""defaultValue"": 0.0,
                ""interpolation"": {
                    ""type"": ""BezierInterpolation"",
                    ""controlPoints"": [0.3,0.1,0.7,0.9]
                }
            }";

            // Act
            var rule = JsonSerializer.Deserialize<ParameterRuleDefinition>(json, _jsonOptions);

            // Assert
            Assert.NotNull(rule);
            Assert.NotNull(rule.Interpolation);
            Assert.IsType<BezierInterpolation>(rule.Interpolation);

            var bezier = (BezierInterpolation)rule.Interpolation!;
            Assert.Equal(4, bezier.ControlPoints.Count); // 2 original + 2 implicit points
        }

        [Fact]
        public void InterpolationConverter_UnknownType_ThrowsHelpfulError()
        {
            // Arrange
            var json = @"{
                ""name"": ""TestParam"",
                ""func"": ""HeadPosX"",
                ""min"": -1.0,
                ""max"": 1.0,
                ""defaultValue"": 0.0,
                ""interpolation"": {
                    ""type"": ""UnknownInterpolation""
                }
            }";

            // Act & Assert
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<ParameterRuleDefinition>(json, _jsonOptions));

            Assert.Contains("Unknown interpolation type", exception.Message);
            Assert.Contains("Available types", exception.Message);
        }

        [Fact]
        public void InterpolationConverter_MissingType_ThrowsError()
        {
            // Arrange
            var json = @"{
                ""name"": ""TestParam"",
                ""func"": ""HeadPosX"",
                ""min"": -1.0,
                ""max"": 1.0,
                ""defaultValue"": 0.0,
                ""interpolation"": {
                    ""ControlPoints"": []
                }
            }";

            // Act & Assert
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<ParameterRuleDefinition>(json, _jsonOptions));

            Assert.Contains("Missing 'type' property", exception.Message);
        }
    }
}