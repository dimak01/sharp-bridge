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
        private readonly GeneralSettingsConfig _config;
        private readonly TransformationEngineConfig _transformationConfig;
        private readonly ExternalEditorService _service;
        private readonly string _tempConfigPath;

        public ExternalEditorServiceTests()
        {
            _loggerMock = new Mock<IAppLogger>();
            _processLauncherMock = new Mock<IProcessLauncher>();
            _config = new GeneralSettingsConfig
            {
                EditorCommand = "notepad.exe \"%f\""
            };

            // Create a temporary config file for testing
            _tempConfigPath = Path.GetTempFileName();
            File.WriteAllText(_tempConfigPath, "[]");

            _transformationConfig = new TransformationEngineConfig
            {
                ConfigPath = _tempConfigPath
            };

            // Default setup: process launcher succeeds
            _processLauncherMock.Setup(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            _service = new ExternalEditorService(_config, _loggerMock.Object, _processLauncherMock.Object, _transformationConfig);
        }

        public void Dispose()
        {
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Helper method to create ExternalEditorService with custom configuration
        /// </summary>
        private ExternalEditorService CreateService(GeneralSettingsConfig config, TransformationEngineConfig? transformationConfig = null)
        {
            return new ExternalEditorService(config, _loggerMock.Object, _processLauncherMock.Object, transformationConfig ?? _transformationConfig);
        }

        /// <summary>
        /// Helper method to create ExternalEditorService with custom process launcher setup
        /// </summary>
        private ExternalEditorService CreateService(GeneralSettingsConfig config, bool processLauncherReturns, TransformationEngineConfig? transformationConfig = null)
        {
            var processLauncherMock = new Mock<IProcessLauncher>();
            processLauncherMock.Setup(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(processLauncherReturns);
            return new ExternalEditorService(config, _loggerMock.Object, processLauncherMock.Object, transformationConfig ?? _transformationConfig);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new ExternalEditorService(_config, _loggerMock.Object, _processLauncherMock.Object, _transformationConfig);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(null!, _loggerMock.Object, _processLauncherMock.Object, _transformationConfig));

            exception.ParamName.Should().Be("config");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_config, null!, _processLauncherMock.Object, _transformationConfig));

            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullProcessLauncher_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_config, _loggerMock.Object, null!, _transformationConfig));

            exception.ParamName.Should().Be("processLauncher");
        }

        [Fact]
        public void Constructor_WithNullTransformationConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_config, _loggerMock.Object, _processLauncherMock.Object, null!));

            exception.ParamName.Should().Be("transformationEngineConfig");
        }

        #endregion

        #region Editor Command Validation Tests

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithNullEditorCommand_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithNullCommand = new GeneralSettingsConfig { EditorCommand = null! };
            var service = CreateService(configWithNullCommand);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open transformation config in editor: editor command is not configured"), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithEmptyEditorCommand_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithEmptyCommand = new GeneralSettingsConfig { EditorCommand = string.Empty };
            var service = CreateService(configWithEmptyCommand);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open transformation config in editor: editor command is not configured"), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithWhitespaceEditorCommand_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithWhitespaceCommand = new GeneralSettingsConfig { EditorCommand = "   " };
            var service = CreateService(configWithWhitespaceCommand);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open transformation config in editor: editor command is not configured"), Times.Once);
        }

        #endregion

        #region File Path Validation Tests

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithNonExistentFile_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var configWithNonExistentFile = new TransformationEngineConfig { ConfigPath = nonExistentPath };
            var service = CreateService(_config, configWithNonExistentFile);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open transformation config in editor: file does not exist: {0}", nonExistentPath), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithValidFile_OpensFile()
        {
            // Act
            var result = await _service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}",
                It.Is<string>(cmd => cmd.Contains(_tempConfigPath))), Times.Once);
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "notepad.exe"), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("notepad.exe",
                It.Is<string>(args => args.Contains(_tempConfigPath))), Times.Once);
        }

        #endregion

        #region Process Execution Tests

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithProcessLauncherFailure_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var service = CreateService(_config, false); // Process launcher returns false

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Failed to start external editor process"), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithProcessLauncherException_ReturnsFalseAndLogsError()
        {
            // Arrange
            var mockProcessLauncher = new Mock<IProcessLauncher>();
            mockProcessLauncher.Setup(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Test exception"));

            var service = new ExternalEditorService(_config, _loggerMock.Object, mockProcessLauncher.Object, _transformationConfig);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor for transformation config",
                It.IsAny<InvalidOperationException>()), Times.Once);
        }

        #endregion

        #region Placeholder Replacement Tests

        [Fact]
        public async Task TryOpenTransformationConfigAsync_ReplacesPlaceholderWithFilePath()
        {
            // Arrange
            var configWithPlaceholder = new GeneralSettingsConfig { EditorCommand = "editor.exe --file \"%f\" --readonly" };
            var service = CreateService(configWithPlaceholder);

            // Act
            await service.TryOpenTransformationConfigAsync();

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}",
                It.Is<string>(cmd => cmd.Contains(_tempConfigPath) && !cmd.Contains("%f"))), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("editor.exe",
                It.Is<string>(args => args.Contains(_tempConfigPath) && args.Contains("--file") && args.Contains("--readonly"))), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithMultiplePlaceholders_ReplacesAll()
        {
            // Arrange
            var configWithMultiplePlaceholders = new GeneralSettingsConfig { EditorCommand = "editor.exe \"%f\" --backup \"%f.bak\"" };
            var service = CreateService(configWithMultiplePlaceholders);

            // Act
            await service.TryOpenTransformationConfigAsync();

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}",
                It.Is<string>(cmd => cmd.Contains(_tempConfigPath) && !cmd.Contains("%f"))), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("editor.exe",
                It.Is<string>(args => args.Contains(_tempConfigPath) && args.Contains("--backup"))), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithNoPlaceholder_UsesCommandAsIs()
        {
            // Arrange
            var configWithoutPlaceholder = new GeneralSettingsConfig { EditorCommand = "notepad.exe" };
            var service = CreateService(configWithoutPlaceholder);

            // Act
            await service.TryOpenTransformationConfigAsync();

            // Assert
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}", "notepad.exe"), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("notepad.exe", ""), Times.Once);
        }

        #endregion

        #region Command Parsing Tests

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithQuotedExecutable_ParsesCorrectly()
        {
            // Arrange
            var configWithQuotedExe = new GeneralSettingsConfig { EditorCommand = "\"C:\\Program Files\\Editor\\editor.exe\" \"%f\"" };
            var service = CreateService(configWithQuotedExe, false); // Expect failure since editor doesn't exist

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert - Verify command was parsed and executed (will likely fail since editor doesn't exist)
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}",
                It.Is<string>(cmd => cmd.Contains("C:\\Program Files\\Editor\\editor.exe") && cmd.Contains(_tempConfigPath))), Times.Once);
            // Should fail gracefully since the editor doesn't exist
            _loggerMock.Verify(x => x.Warning("Failed to start external editor process"), Times.Once);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithComplexArguments_ParsesCorrectly()
        {
            // Arrange
            var configWithComplexArgs = new GeneralSettingsConfig
            {
                EditorCommand = "code.exe --goto \"%f\":1:1 --new-window --wait"
            };
            var service = CreateService(configWithComplexArgs, false); // Expect failure

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert - Verify command was parsed correctly with complex arguments
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}",
                It.Is<string>(cmd => cmd.Contains("code.exe") && cmd.Contains("--goto") &&
                                     cmd.Contains(_tempConfigPath) && cmd.Contains(":1:1") &&
                                     cmd.Contains("--new-window") && cmd.Contains("--wait"))), Times.Once);
            // Should fail gracefully since code.exe likely doesn't exist
            _loggerMock.Verify(x => x.Warning("Failed to start external editor process"), Times.Once);
            result.Should().BeFalse();
        }

        #endregion

        #region Configuration Examples Tests

        [Theory]
        [InlineData("notepad.exe \"%f\"", "notepad.exe")]
        [InlineData("\"C:\\Program Files\\Notepad++\\notepad++.exe\" \"%f\"", "C:\\Program Files\\Notepad++\\notepad++.exe")]
        [InlineData("code.exe \"%f\"", "code.exe")]
        [InlineData("vim \"%f\"", "vim")]
        [InlineData("nano \"%f\"", "nano")]
        public async Task TryOpenTransformationConfigAsync_WithVariousEditorConfigurations_ParsesExecutableCorrectly(
            string editorCommand, string expectedExecutable)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert - All should succeed since we're mocking the process launcher to return true
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}",
                expectedExecutable), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess(expectedExecutable,
                It.Is<string>(args => args.Contains(_tempConfigPath))), Times.Once);
        }

        #endregion
    }
}