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
    public class GeneralSettingsConfigValidatorTests
    {
        private readonly GeneralSettingsConfigValidator _validator;
        private readonly Mock<IConfigFieldValidator> _mockFieldValidator;

        public GeneralSettingsConfigValidatorTests()
        {
            _mockFieldValidator = new Mock<IConfigFieldValidator>();
            _validator = new GeneralSettingsConfigValidator(_mockFieldValidator.Object);
        }

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
                new("EditorCommand", "notepad.exe", true, typeof(string), "Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { { "F1", "Help" } }, true, typeof(Dictionary<string, string>), "Shortcuts")
            };

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
                new("EditorCommand", null, true, typeof(string), "Editor Command"),
                new("Shortcuts", new Dictionary<string, string>(), true, typeof(Dictionary<string, string>), "Shortcuts")
            };

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Issues.Should().HaveCount(2);
            result.Issues.Should().Contain(i => i.FieldName == "EditorCommand");
            result.Issues.Should().Contain(i => i.FieldName == "Shortcuts");
        }

        [Fact]
        public void ValidateSection_WithMixedValidAndInvalidFields_ReturnsInvalidResultWithPartialIssues()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("EditorCommand", "notepad.exe", true, typeof(string), "Editor Command"),
                new("Shortcuts", null, true, typeof(Dictionary<string, string>), "Shortcuts"),
                new("UnknownField", "value", true, typeof(string), "Unknown Field")
            };

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Issues.Should().HaveCount(2);
            result.Issues.Should().Contain(i => i.FieldName == "Shortcuts");
            result.Issues.Should().Contain(i => i.FieldName == "UnknownField");
        }

        #endregion

        #region ValidateSingleField Tests

        [Fact]
        public void ValidateSingleField_WithMissingField_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("EditorCommand", "notepad.exe", false, typeof(string), "Editor Command");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("EditorCommand");
            issue.Description.Should().Contain("Required field 'Editor Command' is missing");
            issue.ProvidedValueText.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithNullValue_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("EditorCommand", null, true, typeof(string), "Editor Command");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("EditorCommand");
            issue.Description.Should().Contain("Required field 'Editor Command' is missing");
            issue.ProvidedValueText.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithValidEditorCommand_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("EditorCommand", "notepad.exe", true, typeof(string), "Editor Command");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithValidShortcuts_ReturnsValid()
        {
            // Arrange
            var shortcuts = new Dictionary<string, string> { { "F1", "Help" } };
            var field = new ConfigFieldState("Shortcuts", shortcuts, true, typeof(Dictionary<string, string>), "Shortcuts");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
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
            issue.Description.Should().Contain("Unknown field 'UnknownField' in GeneralSettingsConfig");
            issue.ProvidedValueText.Should().Be("value");
        }

        #endregion

        #region ValidateEditorCommand Tests

        [Fact]
        public void ValidateEditorCommand_WithEmptyString_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("EditorCommand", "", true, typeof(string), "Editor Command");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("EditorCommand");
            issue.Description.Should().Contain("External editor command cannot be null or empty");
            issue.ProvidedValueText.Should().Be("");
        }

        [Fact]
        public void ValidateEditorCommand_WithWhitespaceString_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("EditorCommand", "   ", true, typeof(string), "Editor Command");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("EditorCommand");
            issue.Description.Should().Contain("External editor command cannot be null or empty");
            issue.ProvidedValueText.Should().Be("   ");
        }

        [Fact]
        public void ValidateEditorCommand_WithNonStringValue_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("EditorCommand", 123, true, typeof(string), "Editor Command");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("EditorCommand");
            issue.Description.Should().Contain("External editor command cannot be null or empty");
            issue.ProvidedValueText.Should().Be("123");
        }

        #endregion

        #region ValidateShortcuts Tests

        [Fact]
        public void ValidateShortcuts_WithValidDictionary_ReturnsValid()
        {
            // Arrange
            var shortcuts = new Dictionary<string, string> { { "F1", "Help" }, { "F2", "Save" } };
            var field = new ConfigFieldState("Shortcuts", shortcuts, true, typeof(Dictionary<string, string>), "Shortcuts");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
        }

        [Fact]
        public void ValidateShortcuts_WithNullValue_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("Shortcuts", null, true, typeof(Dictionary<string, string>), "Shortcuts");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("Shortcuts");
            issue.Description.Should().Contain("Required field 'Shortcuts' is missing");
            issue.ProvidedValueText.Should().BeNull();
        }

        [Fact]
        public void ValidateShortcuts_WithEmptyDictionary_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("Shortcuts", new Dictionary<string, string>(), true, typeof(Dictionary<string, string>), "Shortcuts");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("Shortcuts");
            issue.Description.Should().Contain("Keyboard shortcuts dictionary cannot be null or empty");
            issue.ProvidedValueText.Should().NotBeNull();
        }

        [Fact]
        public void ValidateShortcuts_WithNonDictionaryValue_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("Shortcuts", "not a dictionary", true, typeof(Dictionary<string, string>), "Shortcuts");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("Shortcuts");
            issue.Description.Should().Contain("Keyboard shortcuts dictionary cannot be null or empty");
            issue.ProvidedValueText.Should().Be("not a dictionary");
        }

        #endregion

        #region FormatForDisplay Tests (through validation methods)

        [Fact]
        public void FormatForDisplay_WithNullValue_ReturnsNull()
        {
            // Arrange
            var field = new ConfigFieldState("EditorCommand", null, true, typeof(string), "Editor Command");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.ProvidedValueText.Should().BeNull();
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
            issue!.FieldName.Should().Be("UnknownField");
            issue.Description.Should().Contain("Unknown field 'UnknownField' in GeneralSettingsConfig");
            issue.ProvidedValueText.Should().Be(longString[..125] + "...");
        }

        [Fact]
        public void FormatForDisplay_WithNonStringValue_ReturnsToString()
        {
            // Arrange
            var field = new ConfigFieldState("EditorCommand", 123, true, typeof(string), "Editor Command");

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

        #endregion
    }
}
