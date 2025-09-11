using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpBridge.Infrastructure.Utilities;
using SharpBridge.Models;
using SharpBridge.Models.Domain;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Models.Domain
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

        [Fact]
        public void InterpolationConverter_EmptyType_ThrowsError()
        {
            // Arrange
            var json = @"{
                ""name"": ""TestParam"",
                ""func"": ""HeadPosX"",
                ""min"": -1.0,
                ""max"": 1.0,
                ""defaultValue"": 0.0,
                ""interpolation"": {
                    ""type"": """"
                }
            }";

            // Act & Assert
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<ParameterRuleDefinition>(json, _jsonOptions));

            Assert.Contains("Type property cannot be null or empty", exception.Message);
        }

        [Fact]
        public void InterpolationConverter_InvalidTokenType_ThrowsError()
        {
            // Arrange
            var json = @"{
                ""name"": ""TestParam"",
                ""func"": ""HeadPosX"",
                ""min"": -1.0,
                ""max"": 1.0,
                ""defaultValue"": 0.0,
                ""interpolation"": ""invalid""
            }";

            // Act & Assert
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<ParameterRuleDefinition>(json, _jsonOptions));

            Assert.Contains("Expected start of object or array", exception.Message);
        }

        [Fact]
        public void InterpolationConverter_WriteMethod_SerializesCorrectly()
        {
            // Arrange
            var linear = new LinearInterpolation();
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0, Y = 0 },
                    new Point { X = 0.42, Y = 0 },
                    new Point { X = 1, Y = 1 }
                }
            };

            // Act
            var linearJson = JsonSerializer.Serialize(linear, _jsonOptions);
            var bezierJson = JsonSerializer.Serialize(bezier, _jsonOptions);

            // Assert
            // When serializing directly, the default serializer is used, not InterpolationConverter
            Assert.Equal("{}", linearJson);
            // BezierInterpolation uses BezierInterpolationConverter which writes compact array
            Assert.Contains("0.42", bezierJson);
            Assert.Contains("0", bezierJson);
        }

        [Fact]
        public void InterpolationConverter_WriteMethod_HandlesNullValue()
        {
            // Arrange
            IInterpolationDefinition? nullValue = null;

            // Act
            var json = JsonSerializer.Serialize(nullValue, _jsonOptions);

            // Assert
            Assert.Equal("null", json);
        }

        [Fact]
        public void InterpolationConverter_WriteMethod_DelegatesToSpecificConverter()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0, Y = 0 },
                    new Point { X = 0.42, Y = 0 },
                    new Point { X = 1, Y = 1 }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(bezier, _jsonOptions);

            // Assert
            // Should use BezierInterpolationConverter which writes compact format
            Assert.Contains("0.42", json);
            Assert.Contains("0", json);
        }

        [Fact]
        public void InterpolationConverter_WriteMethod_ThroughProperty_SerializesCorrectly()
        {
            // Arrange
            var rule = new ParameterRuleDefinition
            {
                Name = "TestParam",
                Func = "HeadPosX",
                Min = -1.0,
                Max = 1.0,
                DefaultValue = 0.0,
                Interpolation = new LinearInterpolation()
            };

            // Act
            var json = JsonSerializer.Serialize(rule, _jsonOptions);

            // Assert
            // Should use InterpolationConverter which adds type property
            Assert.Contains("type", json);
            Assert.Contains("LinearInterpolation", json);
        }


        [Fact]
        public void InterpolationConverter_WriteMethod_WithTypedConverter_DelegatesCorrectly()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0, Y = 0 },
                    new Point { X = 0.42, Y = 0 },
                    new Point { X = 1, Y = 1 }
                }
            };

            // Create options with BezierInterpolationConverter registered
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Act
            var json = JsonSerializer.Serialize(bezier, options);

            // Assert
            // Should use BezierInterpolationConverter which writes compact format
            Assert.Contains("0.42", json);
            Assert.Contains("0", json);
        }

        [Fact]
        public void InterpolationConverter_WriteMethod_WithProperties_CopiesAllProperties()
        {
            // Arrange
            var linear = new LinearInterpolation();
            var converter = new InterpolationConverter();

            // Act
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            converter.Write(writer, linear, _jsonOptions);
            writer.Flush();
            var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            // Assert
            // LinearInterpolation should serialize as an object with type property
            Assert.Contains("type", json);
            Assert.Contains("LinearInterpolation", json);
        }

        [Fact]
        public void InterpolationConverter_WriteMethod_ComplexObject_HandlesCorrectly()
        {
            // Arrange
            var bezier = new BezierInterpolation
            {
                ControlPoints = new List<Point>
                {
                    new Point { X = 0, Y = 0 },
                    new Point { X = 0.25, Y = 0.1 },
                    new Point { X = 0.75, Y = 0.9 },
                    new Point { X = 1, Y = 1 }
                }
            };
            var converter = new InterpolationConverter();

            // Create options without BezierInterpolationConverter to force object serialization
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new InterpolationConverter() }
            };

            // Act
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            converter.Write(writer, bezier, options);
            writer.Flush();
            var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            // Assert
            // Should serialize with type property and control points
            Assert.Contains("type", json);
            Assert.Contains("BezierInterpolation", json);
            Assert.Contains("ControlPoints", json);
        }

        [Fact]
        public void InterpolationConverter_WriteMethod_NullValue_DirectTest()
        {
            // Arrange
            IInterpolationDefinition? nullValue = null;
            var converter = new InterpolationConverter();

            // Act
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            converter.Write(writer, nullValue!, _jsonOptions);
            writer.Flush();
            var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            // Assert
            Assert.Equal("null", json);
        }

        [Fact]
        public void InterpolationConverter_WriteMethod_WithTypedConverter_ConcreteType()
        {
            // Arrange
            var linear = new LinearInterpolation();
            var converter = new InterpolationConverter();

            // Create options with a custom converter for the concrete type LinearInterpolation
            var customConverter = new LinearInterpolationTestConverter();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { converter, customConverter }
            };

            // Act
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            converter.Write(writer, linear, options);
            writer.Flush();
            var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            // Assert
            // Should delegate to the typed converter
            Assert.Contains("test", json);
        }

        // Custom converter for testing the typed converter branch - concrete type
        private class LinearInterpolationTestConverter : JsonConverter<LinearInterpolation>
        {
            public override LinearInterpolation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, LinearInterpolation value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("test", "value");
                writer.WriteEndObject();
            }
        }
    }
}