using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.Remediation;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Configuration.Services.Remediation
{
    public class GeneralSettingsConfigRemediationServiceTests
    {
        private readonly Mock<IShortcutParser> _mockShortcutParser;
        private readonly GeneralSettingsConfigRemediationService _service;

        public GeneralSettingsConfigRemediationServiceTests()
        {
            _mockShortcutParser = new Mock<IShortcutParser>();
            _service = new GeneralSettingsConfigRemediationService(_mockShortcutParser.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange
            var mockShortcutParser = new Mock<IShortcutParser>();

            // Act
            var service = new GeneralSettingsConfigRemediationService(mockShortcutParser.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullShortcutParser_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new GeneralSettingsConfigRemediationService(null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("shortcutParser");
        }

        #endregion

        #region Remediate Tests - Silent Defaults Path

        [Fact]
        public async Task Remediate_WithMissingEditorCommand_AppliesDefaultsAndReturnsSucceeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
                // EditorCommand is missing
            };

            SetupDefaultShortcutParser();

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            var config = (GeneralSettingsConfig)result.UpdatedConfig!;
            config.EditorCommand.Should().Be("notepad.exe \"%f\"");
            config.Shortcuts.Should().NotBeNull();
        }

        [Fact]
        public async Task Remediate_WithMissingShortcuts_AppliesDefaultsAndReturnsSucceeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command")
                // Shortcuts is missing
            };

            SetupDefaultShortcutParser();

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            var config = (GeneralSettingsConfig)result.UpdatedConfig!;
            config.EditorCommand.Should().Be("code.exe \"%f\"");
            config.Shortcuts.Should().NotBeNull();
            config.Shortcuts.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Remediate_WithBothFieldsMissing_AppliesDefaultsAndReturnsSucceeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>();
            // Both fields are missing

            SetupDefaultShortcutParser();

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            var config = (GeneralSettingsConfig)result.UpdatedConfig!;
            config.EditorCommand.Should().Be("notepad.exe \"%f\"");
            config.Shortcuts.Should().NotBeNull();
            config.Shortcuts.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Remediate_WithNullEditorCommand_AppliesDefaultsAndReturnsSucceeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", null, true, typeof(string), "External Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            SetupDefaultShortcutParser();

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            var config = (GeneralSettingsConfig)result.UpdatedConfig!;
            config.EditorCommand.Should().Be("notepad.exe \"%f\"");
        }

        [Fact]
        public async Task Remediate_WithNullShortcuts_AppliesDefaultsAndReturnsSucceeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", null, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            SetupDefaultShortcutParser();

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            var config = (GeneralSettingsConfig)result.UpdatedConfig!;
            config.Shortcuts.Should().NotBeNull();
            config.Shortcuts.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Remediate_WithNotPresentEditorCommand_AppliesDefaultsAndReturnsSucceeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", false, typeof(string), "External Editor Command"), // Not present
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            SetupDefaultShortcutParser();

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            var config = (GeneralSettingsConfig)result.UpdatedConfig!;
            config.EditorCommand.Should().Be("notepad.exe \"%f\"");
        }

        [Fact]
        public async Task Remediate_WithNotPresentShortcuts_AppliesDefaultsAndReturnsSucceeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, false, typeof(Dictionary<string, string>), "Keyboard Shortcuts") // Not present
            };

            SetupDefaultShortcutParser();

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            var config = (GeneralSettingsConfig)result.UpdatedConfig!;
            config.Shortcuts.Should().NotBeNull();
            config.Shortcuts.Should().NotBeEmpty();
        }

        #endregion

        #region Remediate Tests - No Remediation Path

        [Fact]
        public async Task Remediate_WithAllFieldsPresent_ReturnsNoRemediationNeeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            result.Result.Should().Be(RemediationResult.NoRemediationNeeded);
            result.UpdatedConfig.Should().BeNull();
        }

        #endregion

        #region IsAnyFieldMissing Tests

        [Fact]
        public void IsAnyFieldMissing_WithMissingEditorCommand_ReturnsTrue()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
                // EditorCommand is missing
            };

            // Act
            var result = IsAnyFieldMissing(fieldsState);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAnyFieldMissing_WithMissingShortcuts_ReturnsTrue()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command")
                // Shortcuts is missing
            };

            // Act
            var result = IsAnyFieldMissing(fieldsState);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAnyFieldMissing_WithBothFieldsMissing_ReturnsTrue()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>();

            // Act
            var result = IsAnyFieldMissing(fieldsState);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAnyFieldMissing_WithNullEditorCommand_ReturnsTrue()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", null, false, typeof(string), "External Editor Command"), // Not present
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            // Act
            var result = IsAnyFieldMissing(fieldsState);


            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAnyFieldMissing_WithNullShortcuts_ReturnsTrue()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", null, false, typeof(Dictionary<string, string>), "Keyboard Shortcuts") // Not present
            };

            // Act
            var result = IsAnyFieldMissing(fieldsState);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAnyFieldMissing_WithNotPresentEditorCommand_ReturnsTrue()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", false, typeof(string), "External Editor Command"), // Not present
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            // Act
            var result = IsAnyFieldMissing(fieldsState);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAnyFieldMissing_WithNotPresentShortcuts_ReturnsTrue()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, false, typeof(Dictionary<string, string>), "Keyboard Shortcuts") // Not present
            };

            // Act
            var result = IsAnyFieldMissing(fieldsState);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAnyFieldMissing_WithAllFieldsPresent_ReturnsFalse()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            // Act
            var result = IsAnyFieldMissing(fieldsState);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ApplyDefaultsToMissingFields Tests

        [Fact]
        public void ApplyDefaultsToMissingFields_WithMissingEditorCommand_ReplacesWithDefault()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", null, true, typeof(string), "External Editor Command"), // Missing
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            SetupDefaultShortcutParser();

            // Act
            var result = ApplyDefaultsToMissingFields(fieldsState);

            // Assert
            result.Should().HaveCount(2);
            var editorCommandField = result.First(f => f.FieldName == "EditorCommand");
            editorCommandField.Value.Should().Be("notepad.exe \"%f\"");
            editorCommandField.IsPresent.Should().BeTrue();
        }

        [Fact]
        public void ApplyDefaultsToMissingFields_WithMissingShortcuts_ReplacesWithDefault()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", null, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts") // Missing
            };

            SetupDefaultShortcutParser();

            // Act
            var result = ApplyDefaultsToMissingFields(fieldsState);

            // Assert
            result.Should().HaveCount(2);
            var shortcutsField = result.First(f => f.FieldName == "Shortcuts");
            shortcutsField.Value.Should().BeOfType<Dictionary<string, string>>();
            shortcutsField.IsPresent.Should().BeTrue();
        }

        [Fact]
        public void ApplyDefaultsToMissingFields_WithBothFieldsMissing_AddsBothDefaults()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>();

            SetupDefaultShortcutParser();

            // Act
            var result = ApplyDefaultsToMissingFields(fieldsState);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.FieldName == "EditorCommand");
            result.Should().Contain(f => f.FieldName == "Shortcuts");

            var editorCommandField = result.First(f => f.FieldName == "EditorCommand");
            editorCommandField.Value.Should().Be("notepad.exe \"%f\"");
            editorCommandField.IsPresent.Should().BeTrue();

            var shortcutsField = result.First(f => f.FieldName == "Shortcuts");
            shortcutsField.Value.Should().BeOfType<Dictionary<string, string>>();
            shortcutsField.IsPresent.Should().BeTrue();
        }

        [Fact]
        public void ApplyDefaultsToMissingFields_WithNotPresentEditorCommand_ReplacesWithDefault()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", false, typeof(string), "External Editor Command"), // Not present
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            SetupDefaultShortcutParser();

            // Act
            var result = ApplyDefaultsToMissingFields(fieldsState);

            // Assert
            result.Should().HaveCount(2);
            var editorCommandField = result.First(f => f.FieldName == "EditorCommand");
            editorCommandField.Value.Should().Be("notepad.exe \"%f\"");
            editorCommandField.IsPresent.Should().BeTrue();
        }

        [Fact]
        public void ApplyDefaultsToMissingFields_WithNotPresentShortcuts_ReplacesWithDefault()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, false, typeof(Dictionary<string, string>), "Keyboard Shortcuts") // Not present
            };

            SetupDefaultShortcutParser();

            // Act
            var result = ApplyDefaultsToMissingFields(fieldsState);

            // Assert
            result.Should().HaveCount(2);
            var shortcutsField = result.First(f => f.FieldName == "Shortcuts");
            shortcutsField.Value.Should().BeOfType<Dictionary<string, string>>();
            shortcutsField.IsPresent.Should().BeTrue();
        }

        [Fact]
        public void ApplyDefaultsToMissingFields_WithAllFieldsPresent_ReturnsOriginalFields()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            // Act
            var result = ApplyDefaultsToMissingFields(fieldsState);

            // Assert
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(fieldsState);
        }

        #endregion

        #region CreateDefaultShortcutsDictionary Tests

        [Fact]
        public void CreateDefaultShortcutsDictionary_CallsShortcutParserForEachShortcut()
        {
            // Arrange
            _mockShortcutParser.Setup(x => x.FormatShortcut(It.IsAny<Shortcut>()))
                .Returns("Ctrl+T");

            // Act
            var result = CreateDefaultShortcutsDictionary();

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            _mockShortcutParser.Verify(x => x.FormatShortcut(It.IsAny<Shortcut>()), Times.AtLeastOnce);
        }

        [Fact]
        public void CreateDefaultShortcutsDictionary_ReturnsFormattedShortcuts()
        {
            // Arrange
            _mockShortcutParser.Setup(x => x.FormatShortcut(It.IsAny<Shortcut>()))
                .Returns("Ctrl+T");

            // Act
            var result = CreateDefaultShortcutsDictionary();

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Values.Should().AllBe("Ctrl+T");
        }

        #endregion

        #region CreateConfigFromFieldStates Tests

        [Fact]
        public void CreateConfigFromFieldStates_WithValidFields_CreatesCorrectConfig()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            // Act
            var result = CreateConfigFromFieldStates(fieldsState);

            // Assert
            result.Should().NotBeNull();
            result.EditorCommand.Should().Be("code.exe \"%f\"");
            result.Shortcuts.Should().NotBeNull();
            result.Shortcuts.Should().ContainKey("Test");
            result.Shortcuts["Test"].Should().Be("Ctrl+T");
        }

        [Fact]
        public void CreateConfigFromFieldStates_WithNullFields_CreatesEmptyConfig()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", null, true, typeof(string), "External Editor Command"),
                new("Shortcuts", null, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts")
            };

            // Act
            var result = CreateConfigFromFieldStates(fieldsState);

            // Assert
            result.Should().NotBeNull();
            // When fields have null values, they're not processed, so config keeps default values
            result.EditorCommand.Should().Be("notepad.exe \"%f\""); // Default value from GeneralSettingsConfig
            result.Shortcuts.Should().NotBeNull(); // Default value is empty dictionary
            result.Shortcuts.Should().BeEmpty();
        }

        [Fact]
        public void CreateConfigFromFieldStates_WithNotPresentFields_CreatesEmptyConfig()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", false, typeof(string), "External Editor Command"), // Not present
                new("Shortcuts", new Dictionary<string, string> { ["Test"] = "Ctrl+T" }, false, typeof(Dictionary<string, string>), "Keyboard Shortcuts") // Not present
            };

            // Act
            var result = CreateConfigFromFieldStates(fieldsState);

            // Assert
            result.Should().NotBeNull();
            result.EditorCommand.Should().Be("notepad.exe \"%f\""); // Default value from GeneralSettingsConfig
            result.Shortcuts.Should().NotBeNull(); // Default value is empty dictionary
            result.Shortcuts.Should().BeEmpty();
        }

        [Fact]
        public void CreateConfigFromFieldStates_WithEmptyFields_CreatesEmptyConfig()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>();

            // Act
            var result = CreateConfigFromFieldStates(fieldsState);

            // Assert
            result.Should().NotBeNull();
            result.EditorCommand.Should().Be("notepad.exe \"%f\""); // Default value from GeneralSettingsConfig
            result.Shortcuts.Should().NotBeNull(); // Default value is empty dictionary
            result.Shortcuts.Should().BeEmpty();
        }

        [Fact]
        public void CreateConfigFromFieldStates_WithMixedValidAndInvalidFields_CreatesPartialConfig()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("EditorCommand", "code.exe \"%f\"", true, typeof(string), "External Editor Command"),
                new("Shortcuts", null, true, typeof(Dictionary<string, string>), "Keyboard Shortcuts"), // Invalid
                new("UnknownField", "test", true, typeof(string), "Unknown Field") // Unknown field
            };

            // Act
            var result = CreateConfigFromFieldStates(fieldsState);

            // Assert
            result.Should().NotBeNull();
            result.EditorCommand.Should().Be("code.exe \"%f\"");
            result.Shortcuts.Should().NotBeNull(); // Default value is empty dictionary
            result.Shortcuts.Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private void SetupDefaultShortcutParser()
        {
            _mockShortcutParser.Setup(x => x.FormatShortcut(It.IsAny<Shortcut>()))
                .Returns("Ctrl+T");
        }

        private static bool IsAnyFieldMissing(List<ConfigFieldState> fields)
        {
            return typeof(GeneralSettingsConfigRemediationService)
                .GetMethod("IsAnyFieldMissing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { fields }) as bool? ?? false;
        }

        private List<ConfigFieldState> ApplyDefaultsToMissingFields(List<ConfigFieldState> fields)
        {
            return typeof(GeneralSettingsConfigRemediationService)
                .GetMethod("ApplyDefaultsToMissingFields", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_service, new object[] { fields }) as List<ConfigFieldState> ?? new List<ConfigFieldState>();
        }

        private Dictionary<string, string> CreateDefaultShortcutsDictionary()
        {
            return typeof(GeneralSettingsConfigRemediationService)
                .GetMethod("CreateDefaultShortcutsDictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_service, new object[] { }) as Dictionary<string, string> ?? new Dictionary<string, string>();
        }

        private static GeneralSettingsConfig CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
        {
            return typeof(GeneralSettingsConfigRemediationService)
                .GetMethod("CreateConfigFromFieldStates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { fieldsState }) as GeneralSettingsConfig ?? new GeneralSettingsConfig();
        }

        #endregion
    }
}
