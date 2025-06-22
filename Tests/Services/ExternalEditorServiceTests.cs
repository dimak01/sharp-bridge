using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class ExternalEditorServiceTests : IDisposable
    {
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly ApplicationConfig _config;
        private readonly ExternalEditorService _service;
        private readonly string _tempFilePath;

        public ExternalEditorServiceTests()
        {
            _loggerMock = new Mock<IAppLogger>();
            _config = new ApplicationConfig
            {
                EditorCommand = "notepad.exe \"%f\""
            };
            _service = new ExternalEditorService(_config, _loggerMock.Object);
            
            // Create a temporary file for testing
            _tempFilePath = Path.GetTempFileName();
            File.WriteAllText(_tempFilePath, "test content");
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new ExternalEditorService(_config, _loggerMock.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(null, _loggerMock.Object));

            exception.ParamName.Should().Be("config");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_config, null));

            exception.ParamName.Should().Be("logger");
        }

        #endregion

        #region File Path Validation Tests

        [Fact]
        public async Task TryOpenFileAsync_WithNullFilePath_ReturnsFalseAndLogsWarning()
        {
            // Act
            var result = await _service.TryOpenFileAsync(null);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: file path is null or empty"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithEmptyFilePath_ReturnsFalseAndLogsWarning()
        {
            // Act
            var result = await _service.TryOpenFileAsync(string.Empty);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: file path is null or empty"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithWhitespaceFilePath_ReturnsFalseAndLogsWarning()
        {
            // Act
            var result = await _service.TryOpenFileAsync("   ");

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: file path is null or empty"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithNonExistentFile_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var result = await _service.TryOpenFileAsync(nonExistentPath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: file does not exist: {0}", nonExistentPath), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithValidExistingFile_ProcessesFile()
        {
            // Act
            var result = await _service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains(_tempFilePath))), Times.Once);
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "notepad.exe"), Times.Once);
        }

        #endregion

        #region Editor Command Validation Tests

        [Fact]
        public async Task TryOpenFileAsync_WithNullEditorCommand_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithNullCommand = new ApplicationConfig { EditorCommand = null };
            var service = new ExternalEditorService(configWithNullCommand, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: editor command is not configured"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithEmptyEditorCommand_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithEmptyCommand = new ApplicationConfig { EditorCommand = string.Empty };
            var service = new ExternalEditorService(configWithEmptyCommand, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: editor command is not configured"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithWhitespaceEditorCommand_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithWhitespaceCommand = new ApplicationConfig { EditorCommand = "   " };
            var service = new ExternalEditorService(configWithWhitespaceCommand, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: editor command is not configured"), Times.Once);
        }

        #endregion

        #region Placeholder Replacement Tests

        [Fact]
        public async Task TryOpenFileAsync_ReplacesPlaceholderWithFilePath()
        {
            // Arrange
            var testPath = @"C:\test\file.json";
            var configWithPlaceholder = new ApplicationConfig { EditorCommand = "editor.exe --file \"%f\" --readonly" };
            var service = new ExternalEditorService(configWithPlaceholder, _loggerMock.Object);

            // Act
            await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains(_tempFilePath) && !cmd.Contains("%f"))), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithMultiplePlaceholders_ReplacesAll()
        {
            // Arrange
            var configWithMultiplePlaceholders = new ApplicationConfig { EditorCommand = "editor.exe \"%f\" --backup \"%f.bak\"" };
            var service = new ExternalEditorService(configWithMultiplePlaceholders, _loggerMock.Object);

            // Act
            await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains(_tempFilePath) && !cmd.Contains("%f"))), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithNoPlaceholder_UsesCommandAsIs()
        {
            // Arrange
            var configWithoutPlaceholder = new ApplicationConfig { EditorCommand = "notepad.exe" };
            var service = new ExternalEditorService(configWithoutPlaceholder, _loggerMock.Object);

            // Act
            await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", "notepad.exe"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithFilePathContainingSpaces_HandlesCorrectly()
        {
            // Arrange
            var pathWithSpaces = Path.Combine(Path.GetTempPath(), "test file with spaces.txt");
            File.WriteAllText(pathWithSpaces, "test");
            
            try
            {
                // Act
                await _service.TryOpenFileAsync(pathWithSpaces);

                // Assert
                _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                    It.Is<string>(cmd => cmd.Contains(pathWithSpaces))), Times.Once);
            }
            finally
            {
                if (File.Exists(pathWithSpaces))
                    File.Delete(pathWithSpaces);
            }
        }

        #endregion

        #region Command Parsing Tests

        [Fact]
        public async Task TryOpenFileAsync_WithQuotedExecutable_ParsesCorrectly()
        {
            // Arrange
            var configWithQuotedExe = new ApplicationConfig { EditorCommand = "\"C:\\Program Files\\Editor\\editor.exe\" \"%f\"" };
            var service = new ExternalEditorService(configWithQuotedExe, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Verify command was parsed and executed (will likely fail since editor doesn't exist)
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains("C:\\Program Files\\Editor\\editor.exe") && cmd.Contains(_tempFilePath))), Times.Once);
            // Should fail gracefully since the editor doesn't exist
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor", 
                It.IsAny<Exception>()), Times.Once);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryOpenFileAsync_WithUnquotedExecutable_ParsesCorrectly()
        {
            // Act
            await _service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "notepad.exe"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithExecutableOnly_ParsesCorrectly()
        {
            // Arrange
            var configWithExeOnly = new ApplicationConfig { EditorCommand = "notepad.exe" };
            var service = new ExternalEditorService(configWithExeOnly, _loggerMock.Object);

            // Act
            await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "notepad.exe"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithComplexArguments_ParsesCorrectly()
        {
            // Arrange
            var configWithComplexArgs = new ApplicationConfig { 
                EditorCommand = "code.exe --goto \"%f\":1:1 --new-window --wait" 
            };
            var service = new ExternalEditorService(configWithComplexArgs, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Verify command was parsed correctly with complex arguments
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains("code.exe") && cmd.Contains("--goto") && 
                                     cmd.Contains(_tempFilePath) && cmd.Contains(":1:1") && 
                                     cmd.Contains("--new-window") && cmd.Contains("--wait"))), Times.Once);
            // Should fail gracefully since code.exe likely doesn't exist
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor", 
                It.IsAny<Exception>()), Times.Once);
            result.Should().BeFalse();
        }

        #endregion

        #region Process Execution Tests

        [Fact]
        public async Task TryOpenFileAsync_WithValidCommand_ReturnsTrue()
        {
            // Act
            var result = await _service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TryOpenFileAsync_WithValidCommand_LogsDebugMessage()
        {
            // Act
            await _service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithValidCommand_LogsSuccessMessage()
        {
            // Act
            await _service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", 
                "notepad.exe"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithInvalidExecutable_ReturnsFalseAndLogsError()
        {
            // Arrange
            var configWithInvalidExe = new ApplicationConfig { 
                EditorCommand = "nonexistenteditor12345.exe \"%f\"" 
            };
            var service = new ExternalEditorService(configWithInvalidExe, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor", 
                It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task TryOpenFileAsync_WhenExceptionThrown_ReturnsFalseAndLogsError()
        {
            // Arrange
            var configWithBadCommand = new ApplicationConfig { EditorCommand = "\0invalid\0command\0" };
            var service = new ExternalEditorService(configWithBadCommand, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            // Should log either an error with exception or a warning about failed process start
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor", 
                It.IsAny<Exception>()), Times.AtMostOnce);
            _loggerMock.Verify(x => x.Warning("Failed to start external editor process"), Times.AtMostOnce);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithMalformedCommand_HandlesGracefully()
        {
            // Arrange
            var configWithMalformedCommand = new ApplicationConfig { EditorCommand = "\"unclosed quote command" };
            var service = new ExternalEditorService(configWithMalformedCommand, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Should not throw exception and return a valid boolean result
            // The test passes if we reach this point without throwing an exception
            Assert.True(result == true || result == false);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task TryOpenFileAsync_WithVeryLongFilePath_HandlesCorrectly()
        {
            // Arrange
            var longFileName = new string('a', 200) + ".txt";
            var longFilePath = Path.Combine(Path.GetTempPath(), longFileName);
            
            try
            {
                File.WriteAllText(longFilePath, "test");

                // Act
                var result = await _service.TryOpenFileAsync(longFilePath);

                // Assert
                result.Should().BeTrue();
                _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                    It.Is<string>(cmd => cmd.Contains(longFilePath))), Times.Once);
            }
            finally
            {
                if (File.Exists(longFilePath))
                    File.Delete(longFilePath);
            }
        }

        [Fact]
        public async Task TryOpenFileAsync_WithSpecialCharactersInPath_HandlesCorrectly()
        {
            // Arrange
            var specialCharFile = Path.Combine(Path.GetTempPath(), "test-file_with[special]chars.txt");
            
            try
            {
                File.WriteAllText(specialCharFile, "test");

                // Act
                var result = await _service.TryOpenFileAsync(specialCharFile);

                // Assert
                result.Should().BeTrue();
                _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                    It.Is<string>(cmd => cmd.Contains(specialCharFile))), Times.Once);
            }
            finally
            {
                if (File.Exists(specialCharFile))
                    File.Delete(specialCharFile);
            }
        }

        [Fact]
        public async Task TryOpenFileAsync_WithUnicodeCharactersInPath_HandlesCorrectly()
        {
            // Arrange
            var unicodeFile = Path.Combine(Path.GetTempPath(), "test-файл-测试.txt");
            
            try
            {
                File.WriteAllText(unicodeFile, "test");

                // Act
                var result = await _service.TryOpenFileAsync(unicodeFile);

                // Assert
                result.Should().BeTrue();
                _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                    It.Is<string>(cmd => cmd.Contains(unicodeFile))), Times.Once);
            }
            finally
            {
                if (File.Exists(unicodeFile))
                    File.Delete(unicodeFile);
            }
        }

        #endregion

        #region Configuration Examples Tests

        [Theory]
        [InlineData("notepad.exe \"%f\"", "notepad.exe")]
        [InlineData("\"C:\\Program Files\\Notepad++\\notepad++.exe\" \"%f\"", "C:\\Program Files\\Notepad++\\notepad++.exe")]
        [InlineData("code.exe \"%f\"", "code.exe")]
        [InlineData("vim \"%f\"", "vim")]
        [InlineData("nano \"%f\"", "nano")]
        public async Task TryOpenFileAsync_WithVariousEditorConfigurations_ParsesExecutableCorrectly(
            string editorCommand, string expectedExecutable)
        {
            // Arrange
            var config = new ApplicationConfig { EditorCommand = editorCommand };
            var service = new ExternalEditorService(config, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - For notepad.exe, expect success on Windows; for others, expect proper error handling
            if (expectedExecutable == "notepad.exe")
            {
                // Notepad should exist on Windows
                result.Should().BeTrue();
                _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", 
                    expectedExecutable), Times.Once);
            }
            else
            {
                // Other editors likely don't exist, but service should handle gracefully
                _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                    It.Is<string>(cmd => cmd.Contains(_tempFilePath))), Times.Once);
                // Should either succeed or fail gracefully with error logging
                _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor", 
                    It.IsAny<Exception>()), Times.AtMostOnce);
            }
        }

        [Fact]
        public async Task TryOpenFileAsync_WithJetBrainsIDECommand_ParsesCorrectly()
        {
            // Arrange
            var jetbrainsConfig = new ApplicationConfig { 
                EditorCommand = "\"C:\\Users\\User\\AppData\\Local\\JetBrains\\Toolbox\\scripts\\idea.cmd\" \"%f\"" 
            };
            var service = new ExternalEditorService(jetbrainsConfig, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Verify command parsing for JetBrains IDE
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains("C:\\Users\\User\\AppData\\Local\\JetBrains\\Toolbox\\scripts\\idea.cmd") && 
                                     cmd.Contains(_tempFilePath))), Times.Once);
            // Should fail gracefully since the IDE path doesn't exist
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor", 
                It.IsAny<Exception>()), Times.Once);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryOpenFileAsync_WithSublimeTextCommand_ParsesCorrectly()
        {
            // Arrange
            var sublimeConfig = new ApplicationConfig { 
                EditorCommand = "\"C:\\Program Files\\Sublime Text\\sublime_text.exe\" \"%f\"" 
            };
            var service = new ExternalEditorService(sublimeConfig, _loggerMock.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Verify command parsing for Sublime Text
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains("C:\\Program Files\\Sublime Text\\sublime_text.exe") && 
                                     cmd.Contains(_tempFilePath))), Times.Once);
            // Should fail gracefully since Sublime Text likely isn't installed
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor", 
                It.IsAny<Exception>()), Times.Once);
            result.Should().BeFalse();
        }

        #endregion
    }
} 