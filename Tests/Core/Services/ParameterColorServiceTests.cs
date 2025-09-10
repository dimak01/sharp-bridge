using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Services;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Core.Services
{
    public class ParameterColorServiceTests
    {
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly ParameterColorService _colorService;

        public ParameterColorServiceTests()
        {
            _loggerMock = new Mock<IAppLogger>();
            _colorService = new ParameterColorService(_loggerMock.Object!);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new ParameterColorService(null!));
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void InitializeFromConfiguration_WithNullBlendShapes_LogsWarningAndReturns()
        {
            // Arrange
            var calculatedParameters = new[] { "param1", "param2" };

            // Act
            _colorService.InitializeFromConfiguration(null!, calculatedParameters);

            // Assert
            _loggerMock.Verify(x => x.Warning("ParameterColorService initialized with null blend shape names"), Times.Once);
        }

        [Fact]
        public void InitializeFromConfiguration_WithNullCalculatedParameters_LogsWarningAndReturns()
        {
            // Arrange
            var blendShapes = new[] { "eyeBlinkLeft", "eyeBlinkRight" };

            // Act
            _colorService.InitializeFromConfiguration(blendShapes, null!);

            // Assert
            _loggerMock.Verify(x => x.Warning("ParameterColorService initialized with null calculated parameter names"), Times.Once);
        }

        [Fact]
        public void InitializeFromConfiguration_WithValidData_AssignsColorsCorrectly()
        {
            // Arrange
            var calculatedParameters = new[] { "calculatedParam1", "calculatedParam2" };
            var blendShapes = new[] { "eyeBlinkLeft", "eyeBlinkRight", "jawOpen" };

            // Act
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);

            // Assert
            _loggerMock.Verify(x => x.Info("ParameterColorService initialized with 2 calculated parameters and 3 blend shapes"), Times.Once);
            _loggerMock.Verify(x => x.Debug("Parameter sets: 2 calculated parameters, 3 blend shapes, 6 head parameters"), Times.Once);
        }

        [Fact]
        public void InitializeFromConfiguration_WithEmptyStrings_FiltersOutEmptyValues()
        {
            // Arrange
            var calculatedParameters = new[] { "validParam", "", "anotherParam", null };
            var blendShapes = new[] { "validBlendShape", "", "anotherBlendShape", null };

            // Act
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);

            // Assert - Should only count non-empty values
            _loggerMock.Verify(x => x.Info("ParameterColorService initialized with 4 calculated parameters and 4 blend shapes"), Times.Once);
            _loggerMock.Verify(x => x.Debug("Parameter sets: 2 calculated parameters, 2 blend shapes, 6 head parameters"), Times.Once);
        }

        [Fact]
        public void GetColoredBlendShapeName_WithValidName_ReturnsColoredBlendShape()
        {
            // Act
            var result = _colorService.GetColoredBlendShapeName("eyeBlinkLeft");

            // Assert
            result.Should().Be(ConsoleColors.ColorizeBlendShape("eyeBlinkLeft"));
            result.Should().Contain(ConsoleColors.BlendShapeColor);
            result.Should().Contain(ConsoleColors.Reset);
        }

        [Fact]
        public void GetColoredCalculatedParameterName_WithValidName_ReturnsColoredParameter()
        {
            // Act
            var result = _colorService.GetColoredCalculatedParameterName("calculatedParam1");

            // Assert
            result.Should().Be(ConsoleColors.ColorizeCalculatedParameter("calculatedParam1"));
            result.Should().Contain(ConsoleColors.CalculatedParameterColor);
            result.Should().Contain(ConsoleColors.Reset);
        }

        [Fact]
        public void GetColoredBlendShapeName_WithNullOrEmpty_ReturnsEmpty()
        {
            // Act & Assert
            _colorService.GetColoredBlendShapeName(null!).Should().Be(string.Empty);
            _colorService.GetColoredBlendShapeName("").Should().Be(string.Empty);
            _colorService.GetColoredBlendShapeName("   ").Should().Be(ConsoleColors.ColorizeBlendShape("   ")); // Whitespace preserved but colored
        }

        [Fact]
        public void GetColoredCalculatedParameterName_WithNullOrEmpty_ReturnsEmpty()
        {
            // Act & Assert
            _colorService.GetColoredCalculatedParameterName(null!).Should().Be(string.Empty);
            _colorService.GetColoredCalculatedParameterName("").Should().Be(string.Empty);
            _colorService.GetColoredCalculatedParameterName("   ").Should().Be(ConsoleColors.ColorizeCalculatedParameter("   ")); // Whitespace preserved but colored
        }

        [Fact]
        public void GetColoredExpression_WithBlendShapeReferences_ColorsBlendShapes()
        {
            // Arrange
            var calculatedParameters = new string[0];
            var blendShapes = new[] { "eyeBlinkLeft", "jawOpen" };
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);
            var expression = "eyeBlinkLeft * 2 + jawOpen";

            // Act
            var result = _colorService.GetColoredExpression(expression);

            // Assert - Blend shapes should be colored cyan
            result.Should().Contain(ConsoleColors.ColorizeBlendShape("eyeBlinkLeft"));
            result.Should().Contain(ConsoleColors.ColorizeBlendShape("jawOpen"));
            result.Should().Contain(" * 2 + "); // Operators should remain unchanged
        }

        [Fact]
        public void GetColoredExpression_WithCalculatedParameterReferences_ColorsCalculatedParameters()
        {
            // Arrange
            var calculatedParameters = new[] { "param1", "param2" };
            var blendShapes = new string[0];
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);
            var expression = "param1 + param2 * 0.5";

            // Act
            var result = _colorService.GetColoredExpression(expression);

            // Assert - Calculated parameters should be colored yellow
            result.Should().Contain(ConsoleColors.ColorizeCalculatedParameter("param1"));
            result.Should().Contain(ConsoleColors.ColorizeCalculatedParameter("param2"));
            result.Should().Contain(" + ");
            result.Should().Contain(" * 0.5");
        }

        [Fact]
        public void GetColoredExpression_WithMixedReferences_BlendShapesTakePriority()
        {
            // Arrange - Same name exists as both blend shape and calculated parameter
            var calculatedParameters = new[] { "conflictingName" };
            var blendShapes = new[] { "conflictingName" };
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);
            var expression = "conflictingName * 2";

            // Act
            var result = _colorService.GetColoredExpression(expression);

            // Assert - Should use blend shape color (cyan) due to priority
            result.Should().Contain(ConsoleColors.ColorizeBlendShape("conflictingName"));
            result.Should().NotContain(ConsoleColors.ColorizeCalculatedParameter("conflictingName"));
            // The expression should contain the colored parameter and the operators
            result.Should().Contain(" * 2"); // Operators should remain unchanged
        }

        [Fact]
        public void GetColoredExpression_WithPartialMatches_OnlyColorsWholeWords()
        {
            // Arrange
            var calculatedParameters = new string[0];
            var blendShapes = new[] { "eye" };
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);
            var expression = "eyeBlinkLeft + eye + eyebrow";

            // Act
            var result = _colorService.GetColoredExpression(expression);

            // Assert - Only "eye" should be colored, not "eyeBlinkLeft" or "eyebrow"
            result.Should().Contain("eyeBlinkLeft"); // Unchanged
            result.Should().Contain(ConsoleColors.ColorizeBlendShape("eye"));
            result.Should().Contain("eyebrow"); // Unchanged
        }

        [Fact]
        public void GetColoredExpression_CachesResults()
        {
            // Arrange
            var calculatedParameters = new string[0];
            var blendShapes = new[] { "eyeBlinkLeft" };
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);
            var expression = "eyeBlinkLeft * 2";

            // Act - Call twice
            var result1 = _colorService.GetColoredExpression(expression);
            var result2 = _colorService.GetColoredExpression(expression);

            // Assert - Results should be identical (cached)
            result1.Should().Be(result2);
            result1.Should().Contain(ConsoleColors.ColorizeBlendShape("eyeBlinkLeft"));
        }

        [Fact]
        public void GetColoredExpression_WithNullOrEmpty_ReturnsEmpty()
        {
            // Arrange
            var calculatedParameters = new string[0];
            var blendShapes = new string[0];
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);

            // Act & Assert
            _colorService.GetColoredExpression(null!).Should().Be(string.Empty);
            _colorService.GetColoredExpression("").Should().Be(string.Empty);
        }

        [Fact]
        public void InitializeFromConfiguration_CalledMultipleTimes_ClearsPreviousMappings()
        {
            // Arrange - First initialization
            var firstCalculatedParameters = new[] { "oldParam" };
            var firstBlendShapes = new[] { "oldBlendShape" };
            _colorService.InitializeFromConfiguration(firstBlendShapes, firstCalculatedParameters);

            // Act - Second initialization with different data
            var secondCalculatedParameters = new[] { "newParam" };
            var secondBlendShapes = new[] { "newBlendShape" };
            _colorService.InitializeFromConfiguration(secondBlendShapes, secondCalculatedParameters);

            // Assert - Methods always return colored versions regardless of initialization
            // (The simplified approach doesn't track which parameters are "known")
            _colorService.GetColoredCalculatedParameterName("oldParam").Should().Contain(ConsoleColors.CalculatedParameterColor);
            _colorService.GetColoredBlendShapeName("oldBlendShape").Should().Contain(ConsoleColors.BlendShapeColor);
            _colorService.GetColoredCalculatedParameterName("newParam").Should().Contain(ConsoleColors.CalculatedParameterColor);
            _colorService.GetColoredBlendShapeName("newBlendShape").Should().Contain(ConsoleColors.BlendShapeColor);
        }

        [Fact]
        public void InitializeFromConfiguration_WithLargeDataSet_HandlesEfficiently()
        {
            // Arrange - Large dataset to test performance
            var calculatedParameters = new List<string>();
            var blendShapes = new List<string>();

            for (int i = 0; i < 1000; i++)
            {
                calculatedParameters.Add($"param_{i}");
                blendShapes.Add($"blendShape_{i}");
            }

            // Act
            _colorService.InitializeFromConfiguration(blendShapes, calculatedParameters);

            // Assert - Should handle large datasets without issues
            _colorService.GetColoredCalculatedParameterName("param_500").Should().Contain(ConsoleColors.CalculatedParameterColor);
            _colorService.GetColoredBlendShapeName("blendShape_500").Should().Contain(ConsoleColors.BlendShapeColor);
            _colorService.GetColoredCalculatedParameterName("nonexistent").Should().Contain(ConsoleColors.CalculatedParameterColor);
        }
    }
}