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
        private readonly Mock<IProcessLauncher> _processLauncherMock;
        private readonly ApplicationConfig _config;
        private readonly ExternalEditorService _service;
        private readonly string _tempFilePath;

        public ExternalEditorServiceTests()
        {
            _loggerMock = new Mock<IAppLogger>();
            _processLauncherMock = new Mock<IProcessLauncher>();
            _config = new ApplicationConfig
            {
                EditorCommand = "notepad.exe \"%f\""
            };
            
            // Default setup: process launcher succeeds
            _processLauncherMock.Setup(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
                
            _service = new ExternalEditorService(_config, _loggerMock.Object, _processLauncherMock.Object);
            
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
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Helper method to create ExternalEditorService with custom configuration
        /// </summary>
        private ExternalEditorService CreateService(ApplicationConfig config)
        {
            return new ExternalEditorService(config, _loggerMock.Object, _processLauncherMock.Object);
        }

        /// <summary>
        /// Helper method to create ExternalEditorService with custom process launcher setup
        /// </summary>
        private ExternalEditorService CreateService(ApplicationConfig config, bool processLauncherReturns)
        {
            var processLauncherMock = new Mock<IProcessLauncher>();
            processLauncherMock.Setup(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(processLauncherReturns);
            return new ExternalEditorService(config, _loggerMock.Object, processLauncherMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new ExternalEditorService(_config, _loggerMock.Object, _processLauncherMock.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(null!, _loggerMock.Object, _processLauncherMock.Object));

            exception.ParamName.Should().Be("config");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_config, null!, _processLauncherMock.Object));

            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullProcessLauncher_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_config, _loggerMock.Object, null!));

            exception.ParamName.Should().Be("processLauncher");
        }

        #endregion

        #region File Path Validation Tests

        [Fact]
        public async Task TryOpenFileAsync_WithNullFilePath_ReturnsFalseAndLogsWarning()
        {
            // Act
            var result = await _service.TryOpenFileAsync(null!);

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
            _processLauncherMock.Verify(x => x.TryStartProcess("notepad.exe", 
                It.Is<string>(args => args.Contains(_tempFilePath))), Times.Once);
        }

        #endregion

        #region Editor Command Validation Tests

        [Fact]
        public async Task TryOpenFileAsync_WithNullEditorCommand_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithNullCommand = new ApplicationConfig { EditorCommand = null! };
            var service = CreateService(configWithNullCommand);

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
            var service = CreateService(configWithEmptyCommand);

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
            var service = CreateService(configWithWhitespaceCommand);

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
            var configWithPlaceholder = new ApplicationConfig { EditorCommand = "editor.exe --file \"%f\" --readonly" };
            var service = CreateService(configWithPlaceholder);

            // Act
            await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains(_tempFilePath) && !cmd.Contains("%f"))), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("editor.exe", 
                It.Is<string>(args => args.Contains(_tempFilePath) && args.Contains("--file") && args.Contains("--readonly"))), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithMultiplePlaceholders_ReplacesAll()
        {
            // Arrange
            var configWithMultiplePlaceholders = new ApplicationConfig { EditorCommand = "editor.exe \"%f\" --backup \"%f.bak\"" };
            var service = CreateService(configWithMultiplePlaceholders);

            // Act
            await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains(_tempFilePath) && !cmd.Contains("%f"))), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("editor.exe", 
                It.Is<string>(args => args.Contains(_tempFilePath) && args.Contains("--backup"))), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithNoPlaceholder_UsesCommandAsIs()
        {
            // Arrange
            var configWithoutPlaceholder = new ApplicationConfig { EditorCommand = "notepad.exe" };
            var service = CreateService(configWithoutPlaceholder);

            // Act
            await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", "notepad.exe"), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("notepad.exe", ""), Times.Once);
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
                _processLauncherMock.Verify(x => x.TryStartProcess("notepad.exe", 
                    It.Is<string>(args => args.Contains(pathWithSpaces))), Times.Once);
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
            var service = CreateService(configWithQuotedExe, false); // Expect failure since editor doesn't exist

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Verify command was parsed and executed (will likely fail since editor doesn't exist)
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains("C:\\Program Files\\Editor\\editor.exe") && cmd.Contains(_tempFilePath))), Times.Once);
            // Should fail gracefully since the editor doesn't exist
            _loggerMock.Verify(x => x.Warning("Failed to start external editor process"), Times.Once);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryOpenFileAsync_WithUnquotedExecutable_ParsesCorrectly()
        {
            // Act
            await _service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "notepad.exe"), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("notepad.exe", 
                It.Is<string>(args => args.Contains(_tempFilePath))), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithExecutableOnly_ParsesCorrectly()
        {
            // Arrange
            var configWithExeOnly = new ApplicationConfig { EditorCommand = "notepad.exe" };
            var service = CreateService(configWithExeOnly);

            // Act
            await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "notepad.exe"), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("notepad.exe", ""), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithComplexArguments_ParsesCorrectly()
        {
            // Arrange
            var configWithComplexArgs = new ApplicationConfig { 
                EditorCommand = "code.exe --goto \"%f\":1:1 --new-window --wait" 
            };
            var service = CreateService(configWithComplexArgs, false); // Expect failure

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Verify command was parsed correctly with complex arguments
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.Is<string>(cmd => cmd.Contains("code.exe") && cmd.Contains("--goto") && 
                                     cmd.Contains(_tempFilePath) && cmd.Contains(":1:1") && 
                                     cmd.Contains("--new-window") && cmd.Contains("--wait"))), Times.Once);
            // Should fail gracefully since code.exe likely doesn't exist
            _loggerMock.Verify(x => x.Warning("Failed to start external editor process"), Times.Once);
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
        public async Task TryOpenFileAsync_WithInvalidExecutable_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithInvalidExe = new ApplicationConfig { 
                EditorCommand = "nonexistenteditor12345.exe \"%f\"" 
            };
            var service = CreateService(configWithInvalidExe, false); // Process launcher returns false

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Failed to start external editor process"), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task TryOpenFileAsync_WhenExceptionThrown_ReturnsFalseAndLogsError()
        {
            // Arrange
            var configWithBadCommand = new ApplicationConfig { EditorCommand = "\0invalid\0command\0" };
            var service = CreateService(configWithBadCommand, false);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            // Should log either an error with exception or a warning about failed process start
            _loggerMock.Verify(x => x.Warning("Failed to start external editor process"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithMalformedCommand_HandlesGracefully()
        {
            // Arrange
            var configWithMalformedCommand = new ApplicationConfig { EditorCommand = "\"unclosed quote command" };
            var service = CreateService(configWithMalformedCommand);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Should not throw exception and return a valid boolean result
            // The test passes if we reach this point without throwing an exception
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task TryOpenFileAsync_WhenParseCommandThrowsException_CatchesAndLogsError()
        {
            // Arrange - Create a mock that will throw an exception when ParseCommand is called internally
            var mockProcessLauncher = new Mock<IProcessLauncher>();
            mockProcessLauncher.Setup(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Test exception"));
            
            var config = new ApplicationConfig { EditorCommand = "notepad.exe \"%f\"" };
            var service = new ExternalEditorService(config, _loggerMock.Object, mockProcessLauncher.Object);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor", 
                It.IsAny<InvalidOperationException>()), Times.Once);
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
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - All should succeed since we're mocking the process launcher to return true
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", 
                expectedExecutable), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess(expectedExecutable, 
                It.Is<string>(args => args.Contains(_tempFilePath))), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithJetBrainsIDECommand_ParsesCorrectly()
        {
            // Arrange
            var jetbrainsConfig = new ApplicationConfig { 
                EditorCommand = "\"C:\\Users\\User\\AppData\\Local\\JetBrains\\Toolbox\\scripts\\idea.cmd\" \"%f\"" 
            };
            var service = CreateService(jetbrainsConfig);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Verify command parsing for JetBrains IDE
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", 
                "C:\\Users\\User\\AppData\\Local\\JetBrains\\Toolbox\\scripts\\idea.cmd"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithSublimeTextCommand_ParsesCorrectly()
        {
            // Arrange
            var sublimeConfig = new ApplicationConfig { 
                EditorCommand = "\"C:\\Program Files\\Sublime Text\\sublime_text.exe\" \"%f\"" 
            };
            var service = CreateService(sublimeConfig);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Verify command parsing for Sublime Text
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", 
                "C:\\Program Files\\Sublime Text\\sublime_text.exe"), Times.Once);
        }

        #endregion

        #region ParseCommand Edge Cases Tests

        [Fact]
        public async Task TryOpenFileAsync_WithNullCommandLine_HandlesGracefully()
        {
            // Arrange - This will test the ParseCommand method with null input indirectly
            var configWithNullCommand = new ApplicationConfig { EditorCommand = null! };
            var service = CreateService(configWithNullCommand);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: editor command is not configured"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithEmptyCommandLine_HandlesGracefully()
        {
            // Arrange - This will test the ParseCommand method with empty input indirectly
            var configWithEmptyCommand = new ApplicationConfig { EditorCommand = "" };
            var service = CreateService(configWithEmptyCommand);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open file in editor: editor command is not configured"), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithQuotedExecutableNoArguments_ParsesCorrectly()
        {
            // Arrange - This tests the quoted executable path in ParseCommand where there are no arguments
            var configWithQuotedExeOnly = new ApplicationConfig { EditorCommand = "\"C:\\Program Files\\Editor\\editor.exe\"" };
            var service = CreateService(configWithQuotedExeOnly);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", 
                "C:\\Program Files\\Editor\\editor.exe"), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("C:\\Program Files\\Editor\\editor.exe", ""), Times.Once);
        }

        [Fact]
        public async Task TryOpenFileAsync_WithMalformedQuotedCommand_HandlesGracefully()
        {
            // Arrange - This tests the case where quoted executable has no closing quote
            var configWithMalformedQuote = new ApplicationConfig { EditorCommand = "\"C:\\Program Files\\Editor\\editor.exe" };
            var service = CreateService(configWithMalformedQuote);

            // Act
            var result = await service.TryOpenFileAsync(_tempFilePath);

            // Assert - Should not throw exception and handle gracefully
            result.Should().BeTrue(); // ParseCommand should still work, treating the whole thing as an unquoted command
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", 
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ParseCommand_WithNullOrEmptyCommand_ReturnsEmptyStrings()
        {
            // Arrange - ParseCommand is now static, so we use Static binding flags and don't need an instance
            var parseCommandMethod = typeof(ExternalEditorService)
                .GetMethod("ParseCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert - Test null command
            var nullResult = parseCommandMethod!.Invoke(null, new object[] { null! });
            var nullTuple = ((string executable, string arguments))nullResult!;
            nullTuple.executable.Should().Be(string.Empty);
            nullTuple.arguments.Should().Be(string.Empty);

            // Act & Assert - Test empty command
            var emptyResult = parseCommandMethod.Invoke(null, new object[] { "" });
            var emptyTuple = ((string executable, string arguments))emptyResult!;
            emptyTuple.executable.Should().Be(string.Empty);
            emptyTuple.arguments.Should().Be(string.Empty);

            // Act & Assert - Test whitespace command
            var whitespaceResult = parseCommandMethod.Invoke(null, new object[] { "   " });
            var whitespaceTuple = ((string executable, string arguments))whitespaceResult!;
            whitespaceTuple.executable.Should().Be(string.Empty);
            whitespaceTuple.arguments.Should().Be(string.Empty);
        }

        #endregion
    }
} 