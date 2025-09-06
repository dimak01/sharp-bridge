using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.Validators;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class TransformationEngineConfigValidatorTests
    {
        private readonly Mock<IConfigFieldValidator> _mockFieldValidator;
        private readonly TransformationEngineConfigValidator _validator;

        public TransformationEngineConfigValidatorTests()
        {
            _mockFieldValidator = new Mock<IConfigFieldValidator>();
            _validator = new TransformationEngineConfigValidator(_mockFieldValidator.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullFieldValidator_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new TransformationEngineConfigValidator(null!));
            exception.ParamName.Should().Be("fieldValidator");
        }

        [Fact]
        public void Constructor_WithValidFieldValidator_InitializesSuccessfully()
        {
            // Arrange
            var mockFieldValidator = new Mock<IConfigFieldValidator>();

            // Act
            var validator = new TransformationEngineConfigValidator(mockFieldValidator.Object);

            // Assert
            validator.Should().NotBeNull();
        }

        #endregion

        #region ValidateSection Tests

        [Fact]
        public void ValidateSection_WithEmptyFieldsList_ReturnsValidResult()
        {
            // Arrange
            var fields = new List<ConfigFieldState>();

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void ValidateSection_WithAllValidFields_ReturnsValidResult()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "C:\\config\\rules.json", true, typeof(string), "Config Path"),
                new("MaxEvaluationIterations", 10, true, typeof(int), "Max Evaluation Iterations")
            };

            _mockFieldValidator.Setup(x => x.ValidateFilePath(It.IsAny<ConfigFieldState>()))
                .Returns((FieldValidationIssue?)null);
            _mockFieldValidator.Setup(x => x.ValidateIntegerRange(It.IsAny<ConfigFieldState>(), 1, 50))
                .Returns((FieldValidationIssue?)null);

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void ValidateSection_WithInvalidFields_ReturnsInvalidResultWithIssues()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", null, true, typeof(string), "Config Path"),
                new("MaxEvaluationIterations", 100, true, typeof(int), "Max Evaluation Iterations")
            };

            var configPathIssue = new FieldValidationIssue("ConfigPath", typeof(string), "Invalid path", "null");
            var iterationsIssue = new FieldValidationIssue("MaxEvaluationIterations", typeof(int), "Value out of range", "100");

            _mockFieldValidator.Setup(x => x.ValidateFilePath(It.IsAny<ConfigFieldState>()))
                .Returns(configPathIssue);
            _mockFieldValidator.Setup(x => x.ValidateIntegerRange(It.IsAny<ConfigFieldState>(), 1, 50))
                .Returns(iterationsIssue);

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Issues.Should().HaveCount(2);
            result.Issues.Should().Contain(i => i.FieldName == "ConfigPath");
            result.Issues.Should().Contain(i => i.FieldName == "MaxEvaluationIterations");
        }

        [Fact]
        public void ValidateSection_WithMixedValidAndInvalidFields_ReturnsInvalidResultWithPartialIssues()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("ConfigPath", "C:\\config\\rules.json", true, typeof(string), "Config Path"),
                new("MaxEvaluationIterations", 100, true, typeof(int), "Max Evaluation Iterations"),
                new("UnknownField", "value", true, typeof(string), "Unknown Field")
            };

            _mockFieldValidator.Setup(x => x.ValidateFilePath(It.IsAny<ConfigFieldState>()))
                .Returns((FieldValidationIssue?)null);
            _mockFieldValidator.Setup(x => x.ValidateIntegerRange(It.IsAny<ConfigFieldState>(), 1, 50))
                .Returns(new FieldValidationIssue("MaxEvaluationIterations", typeof(int), "Value out of range", "100"));

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Issues.Should().HaveCount(2);
            result.Issues.Should().Contain(i => i.FieldName == "MaxEvaluationIterations");
            result.Issues.Should().Contain(i => i.FieldName == "UnknownField");
        }

        #endregion

        #region ValidateSingleField Tests

        [Fact]
        public void ValidateSingleField_WithMissingField_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("ConfigPath", "C:\\config\\rules.json", false, typeof(string), "Config Path");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("ConfigPath");
            issue.Description.Should().Contain("Required field 'Config Path' is missing");
            issue.ProvidedValueText.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithNullValue_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("ConfigPath", null, true, typeof(string), "Config Path");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("ConfigPath");
            issue.Description.Should().Contain("Required field 'Config Path' is missing");
            issue.ProvidedValueText.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithValidConfigPath_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("ConfigPath", "C:\\config\\rules.json", true, typeof(string), "Config Path");

            _mockFieldValidator.Setup(x => x.ValidateFilePath(field))
                .Returns((FieldValidationIssue?)null);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
            _mockFieldValidator.Verify(x => x.ValidateFilePath(field), Times.Once);
        }

        [Fact]
        public void ValidateSingleField_WithValidMaxEvaluationIterations_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("MaxEvaluationIterations", 10, true, typeof(int), "Max Evaluation Iterations");

            _mockFieldValidator.Setup(x => x.ValidateIntegerRange(field, 1, 50))
                .Returns((FieldValidationIssue?)null);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
            _mockFieldValidator.Verify(x => x.ValidateIntegerRange(field, 1, 50), Times.Once);
        }

        [Fact]
        public void ValidateSingleField_WithUnknownField_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", "value", true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("UnknownField");
            issue.Description.Should().Contain("Unknown field 'UnknownField' in TransformationEngineConfig");
            issue.ProvidedValueText.Should().Be("value");
        }

        #endregion

        #region ValidateConfigPath Tests

        [Fact]
        public void ValidateConfigPath_WithValidPath_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("ConfigPath", "C:\\config\\rules.json", true, typeof(string), "Config Path");

            _mockFieldValidator.Setup(x => x.ValidateFilePath(field))
                .Returns((FieldValidationIssue?)null);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
            _mockFieldValidator.Verify(x => x.ValidateFilePath(field), Times.Once);
        }

        [Fact]
        public void ValidateConfigPath_WithInvalidPath_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("ConfigPath", "invalid<>path", true, typeof(string), "Config Path");
            var expectedIssue = new FieldValidationIssue("ConfigPath", typeof(string), "Invalid path", "invalid<>path");

            _mockFieldValidator.Setup(x => x.ValidateFilePath(field))
                .Returns(expectedIssue);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().Be(expectedIssue);
            _mockFieldValidator.Verify(x => x.ValidateFilePath(field), Times.Once);
        }

        #endregion

        #region ValidateMaxEvaluationIterations Tests

        [Fact]
        public void ValidateMaxEvaluationIterations_WithValidValue_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("MaxEvaluationIterations", 25, true, typeof(int), "Max Evaluation Iterations");

            _mockFieldValidator.Setup(x => x.ValidateIntegerRange(field, 1, 50))
                .Returns((FieldValidationIssue?)null);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
            _mockFieldValidator.Verify(x => x.ValidateIntegerRange(field, 1, 50), Times.Once);
        }

        [Fact]
        public void ValidateMaxEvaluationIterations_WithValueTooLow_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("MaxEvaluationIterations", 0, true, typeof(int), "Max Evaluation Iterations");
            var expectedIssue = new FieldValidationIssue("MaxEvaluationIterations", typeof(int), "Value must be between 1 and 50", "0");

            _mockFieldValidator.Setup(x => x.ValidateIntegerRange(field, 1, 50))
                .Returns(expectedIssue);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().Be(expectedIssue);
            _mockFieldValidator.Verify(x => x.ValidateIntegerRange(field, 1, 50), Times.Once);
        }

        [Fact]
        public void ValidateMaxEvaluationIterations_WithValueTooHigh_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("MaxEvaluationIterations", 100, true, typeof(int), "Max Evaluation Iterations");
            var expectedIssue = new FieldValidationIssue("MaxEvaluationIterations", typeof(int), "Value must be between 1 and 50", "100");

            _mockFieldValidator.Setup(x => x.ValidateIntegerRange(field, 1, 50))
                .Returns(expectedIssue);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().Be(expectedIssue);
            _mockFieldValidator.Verify(x => x.ValidateIntegerRange(field, 1, 50), Times.Once);
        }

        #endregion

        #region FormatForDisplay Tests (through validation methods)

        [Fact]
        public void FormatForDisplay_WithNullValue_ReturnsNull()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", null, true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.ProvidedValueText.Should().BeNull();
        }

        [Fact]
        public void FormatForDisplay_WithShortString_ReturnsFullString()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", "short", true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.ProvidedValueText.Should().Be("short");
        }

        [Fact]
        public void FormatForDisplay_WithLongString_TruncatesString()
        {
            // Arrange
            var longString = new string('a', 200); // Longer than 128 chars
            var field = new ConfigFieldState("UnknownField", longString, true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.ProvidedValueText.Should().Be(longString[..125] + "...");
        }

        [Fact]
        public void FormatForDisplay_WithNonStringValue_ReturnsToString()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", 123, true, typeof(int), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.ProvidedValueText.Should().Be("123");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ValidateSingleField_WithEmptyFieldName_HandlesGracefully()
        {
            // Arrange
            var field = new ConfigFieldState("", "value", true, typeof(string), "Test Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("");
        }

        [Fact]
        public void ValidateSingleField_WithBoundaryValues_HandlesCorrectly()
        {
            // Arrange
            var minField = new ConfigFieldState("MaxEvaluationIterations", 1, true, typeof(int), "Max Evaluation Iterations");
            var maxField = new ConfigFieldState("MaxEvaluationIterations", 50, true, typeof(int), "Max Evaluation Iterations");

            _mockFieldValidator.Setup(x => x.ValidateIntegerRange(It.IsAny<ConfigFieldState>(), 1, 50))
                .Returns((FieldValidationIssue?)null);

            // Act
            var (minIsValid, minIssue) = _validator.ValidateSingleField(minField);
            var (maxIsValid, maxIssue) = _validator.ValidateSingleField(maxField);

            // Assert
            minIsValid.Should().BeTrue();
            minIssue.Should().BeNull();
            maxIsValid.Should().BeTrue();
            maxIssue.Should().BeNull();
        }

        #endregion
    }
}
