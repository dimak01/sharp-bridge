using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Core.Services;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.Infrastructure;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Models.Configuration;
using Xunit;

namespace SharpBridge.Tests.Core.Services
{
    public class ExternalEditorServiceTests : IDisposable
    {
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly Mock<IProcessLauncher> _processLauncherMock;
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly Mock<IFileChangeWatcher> _mockFileWatcher;
        private readonly GeneralSettingsConfig _config;
        private readonly TransformationEngineConfig _transformationConfig;
        private readonly ExternalEditorService _service;
        private readonly string _tempConfigPath;

        public ExternalEditorServiceTests()
        {
            _loggerMock = new Mock<IAppLogger>();
            _processLauncherMock = new Mock<IProcessLauncher>();
            _mockConfigManager = new Mock<IConfigManager>();
            _mockFileWatcher = new Mock<IFileChangeWatcher>();

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

            // Setup config manager
            _mockConfigManager.Setup(x => x.ApplicationConfigPath).Returns(_tempConfigPath);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(_config);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>()).ReturnsAsync(_transformationConfig);

            // Default setup: process launcher succeeds
            _processLauncherMock.Setup(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            _service = new ExternalEditorService(_mockConfigManager.Object, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object);
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
        private ExternalEditorService CreateService(GeneralSettingsConfig config)
        {
            var configManagerMock = new Mock<IConfigManager>();
            configManagerMock.Setup(x => x.ApplicationConfigPath).Returns(_tempConfigPath);
            configManagerMock.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(config);
            configManagerMock.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>()).ReturnsAsync(_transformationConfig);

            return new ExternalEditorService(configManagerMock.Object, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object);
        }

        /// <summary>
        /// Helper method to create ExternalEditorService with custom process launcher setup
        /// </summary>
        private ExternalEditorService CreateService(GeneralSettingsConfig config, bool processLauncherReturns)
        {
            var processLauncherMock = new Mock<IProcessLauncher>();
            processLauncherMock.Setup(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(processLauncherReturns);

            var configManagerMock = new Mock<IConfigManager>();
            configManagerMock.Setup(x => x.ApplicationConfigPath).Returns(_tempConfigPath);
            configManagerMock.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(config);
            configManagerMock.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>()).ReturnsAsync(_transformationConfig);

            return new ExternalEditorService(configManagerMock.Object, _loggerMock.Object, processLauncherMock.Object, _mockFileWatcher.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new ExternalEditorService(_mockConfigManager.Object, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConfigManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(null!, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object));

            exception.ParamName.Should().Be("configManager");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_mockConfigManager.Object, null!, _processLauncherMock.Object, _mockFileWatcher.Object));

            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullProcessLauncher_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_mockConfigManager.Object, _loggerMock.Object, null!, _mockFileWatcher.Object));

            exception.ParamName.Should().Be("processLauncher");
        }

        [Fact]
        public void Constructor_WithNullConfigWatcher_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ExternalEditorService(_mockConfigManager.Object, _loggerMock.Object, _processLauncherMock.Object, null!));

            exception.ParamName.Should().Be("configWatcher");
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_LogsDisposalMessage()
        {
            // Act
            _service.Dispose();

            // Assert
            _loggerMock.Verify(x => x.Debug("Disposing ExternalEditorService"), Times.Once);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimesSafely()
        {
            // Act
            _service.Dispose();
            _service.Dispose();

            // Assert - Should only log disposal once
            _loggerMock.Verify(x => x.Debug("Disposing ExternalEditorService"), Times.Once);
        }

        #endregion

        #region Configuration Change Tests

        [Fact]
        public async Task OnApplicationConfigChanged_WhenGeneralSettingsChanged_ReloadsConfiguration()
        {
            // Arrange
            var newGeneralSettings = new GeneralSettingsConfig { EditorCommand = "new-editor.exe \"%f\"" };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(newGeneralSettings);

            // Act - Simulate config change event
            _mockFileWatcher.Raise(x => x.FileChanged += null, this, new FileChangeEventArgs("test.json"));

            // Wait a bit for async operation to complete
            await Task.Delay(100);

            // Assert
            _loggerMock.Verify(x => x.Debug("Application config changed, checking if general settings were affected"), Times.Once);
            _loggerMock.Verify(x => x.Info("General settings changed, updating external editor service"), Times.Once);
        }

        [Fact]
        public async Task OnApplicationConfigChanged_WhenGeneralSettingsUnchanged_DoesNotReloadConfiguration()
        {
            // Arrange
            _mockConfigManager.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(_config);

            // Act - Simulate config change event
            _mockFileWatcher.Raise(x => x.FileChanged += null, this, new FileChangeEventArgs("test.json"));

            // Wait a bit for async operation to complete
            await Task.Delay(100);

            // Assert
            _loggerMock.Verify(x => x.Debug("Application config changed, checking if general settings were affected"), Times.Once);
            _loggerMock.Verify(x => x.Info("General settings changed, updating external editor service"), Times.Never);
        }

        [Fact]
        public async Task OnApplicationConfigChanged_WhenConfigLoadingFails_LogsError()
        {
            // Arrange
            _mockConfigManager.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>())
                .ThrowsAsync(new IOException("Config file not found"));

            // Act - Simulate config change event
            _mockFileWatcher.Raise(x => x.FileChanged += null, this, new FileChangeEventArgs("test.json"));

            // Wait a bit for async operation to complete
            await Task.Delay(100);

            // Assert
            _loggerMock.Verify(x => x.Error("Error handling application config change: {0}", "Config file not found"), Times.Once);
        }

        #endregion

        #region Application Config Opening Tests

        [Fact]
        public async Task TryOpenApplicationConfigAsync_WithValidConfig_OpensApplicationConfig()
        {
            // Act
            var result = await _service.TryOpenApplicationConfigAsync();

            // Assert
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Debug("Executing editor command: {0}",
                It.Is<string>(cmd => cmd.Contains(_tempConfigPath))), Times.Once);
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "notepad.exe"), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess("notepad.exe",
                It.Is<string>(args => args.Contains(_tempConfigPath))), Times.Once);
        }

        [Fact]
        public async Task TryOpenApplicationConfigAsync_WithNonExistentFile_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _mockConfigManager.Setup(x => x.ApplicationConfigPath).Returns(nonExistentPath);
            var service = new ExternalEditorService(_mockConfigManager.Object, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object);

            // Act
            var result = await service.TryOpenApplicationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: file does not exist: {1}", "application config", nonExistentPath), Times.Once);
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
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: editor command is not configured", "transformation config"), Times.Once);
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
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: editor command is not configured", "transformation config"), Times.Once);
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
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: editor command is not configured", "transformation config"), Times.Once);
        }

        #endregion

        #region File Path Validation Tests

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithNullFilePath_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithNullPath = new TransformationEngineConfig { ConfigPath = null! };
            var configManagerMock = new Mock<IConfigManager>();
            configManagerMock.Setup(x => x.ApplicationConfigPath).Returns(_tempConfigPath);
            configManagerMock.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(_config);
            configManagerMock.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>()).ReturnsAsync(configWithNullPath);
            var service = new ExternalEditorService(configManagerMock.Object, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: file path is null or empty", "transformation config"), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithEmptyFilePath_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithEmptyPath = new TransformationEngineConfig { ConfigPath = string.Empty };
            var configManagerMock = new Mock<IConfigManager>();
            configManagerMock.Setup(x => x.ApplicationConfigPath).Returns(_tempConfigPath);
            configManagerMock.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(_config);
            configManagerMock.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>()).ReturnsAsync(configWithEmptyPath);
            var service = new ExternalEditorService(configManagerMock.Object, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: file path is null or empty", "transformation config"), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithWhitespaceFilePath_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var configWithWhitespacePath = new TransformationEngineConfig { ConfigPath = "   " };
            var configManagerMock = new Mock<IConfigManager>();
            configManagerMock.Setup(x => x.ApplicationConfigPath).Returns(_tempConfigPath);
            configManagerMock.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(_config);
            configManagerMock.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>()).ReturnsAsync(configWithWhitespacePath);
            var service = new ExternalEditorService(configManagerMock.Object, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: file path is null or empty", "transformation config"), Times.Once);
        }

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithNonExistentFile_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var configWithNonExistentFile = new TransformationEngineConfig { ConfigPath = nonExistentPath };
            var configManagerMock = new Mock<IConfigManager>();
            configManagerMock.Setup(x => x.ApplicationConfigPath).Returns(_tempConfigPath);
            configManagerMock.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(_config);
            configManagerMock.Setup(x => x.LoadSectionAsync<TransformationEngineConfig>()).ReturnsAsync(configWithNonExistentFile);
            var service = new ExternalEditorService(configManagerMock.Object, _loggerMock.Object, _processLauncherMock.Object, _mockFileWatcher.Object);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: file does not exist: {1}", "transformation config", nonExistentPath), Times.Once);
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

            var service = new ExternalEditorService(_mockConfigManager.Object, _loggerMock.Object, mockProcessLauncher.Object, _mockFileWatcher.Object);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.ErrorWithException("Error launching external editor for {0}", It.IsAny<InvalidOperationException>(), "transformation config"), Times.Once);
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

        #region Malformed Editor Command Tests

        [Fact]
        public async Task TryOpenTransformationConfigAsync_WithMalformedQuotedEditorCommand_ReturnsFalseAndLogsWarning()
        {
            // Arrange: EditorCommand is missing a closing quote
            var malformedEditorCommand = "\"C:\\Program Files\\Editor\\editor.exe";
            var config = new GeneralSettingsConfig { EditorCommand = malformedEditorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert: Should reject malformed command and return false
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Malformed editor command: {0}", malformedEditorCommand), Times.Once);
            _processLauncherMock.Verify(x => x.TryStartProcess(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Corner Case Tests

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public async Task TryOpenTransformationConfigAsync_WithEmptyOrWhitespaceEditorCommand_ReturnsFalseAndLogsWarning(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Cannot open {0} in editor: editor command is not configured", "transformation config"), Times.Once);
        }

        [Theory]
        [InlineData("\"C:\\Program Files\\Editor.exe")]
        [InlineData("C:\\Program Files\\Editor.exe\"")]
        [InlineData("\"C:\\Program Files\\Editor.exe\" \"arg1")]
        [InlineData("C:\\Program Files\\Editor.exe\" \"arg1\"")]
        [InlineData("\"C:\\Program Files\\Editor.exe arg1")]
        public async Task TryOpenTransformationConfigAsync_WithUnclosedQuotes_ReturnsFalseAndLogsWarning(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Malformed editor command: {0}", editorCommand), Times.Once);
        }

        [Theory]
        [InlineData("\"C:\\Program Files\\Editor.exe\" \"arg1\" \"arg2")]
        [InlineData("C:\\Editor.exe\" \"arg1\" \"arg2\"")]
        [InlineData("\"C:\\Editor.exe arg1\" \"arg2")]
        public async Task TryOpenTransformationConfigAsync_WithMismatchedQuotes_ReturnsFalseAndLogsWarning(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Malformed editor command: {0}", editorCommand), Times.Once);
        }

        [Theory]
        [InlineData("C:\\Editor.exe    arg1   arg2")]
        [InlineData("C:\\Editor.exe\targ1\t\targ2")]
        [InlineData("C:\\Editor.exe  \t  arg1  \t  arg2")]
        public async Task TryOpenTransformationConfigAsync_WithMultipleSpacesAndTabs_HandlesCorrectly(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert - Should handle multiple spaces and tabs correctly
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "C:\\Editor.exe"), Times.Once);
        }

        [Theory]
        [InlineData("\"C:\\Editor.exe\" \"\" \"arg2\"")]
        [InlineData("\"C:\\Editor.exe\" \"\"")]
        [InlineData("C:\\Editor.exe \"\" \"arg2\"")]
        [InlineData("C:\\Editor.exe \"\"")]
        public async Task TryOpenTransformationConfigAsync_WithQuotedEmptyArgument_HandlesCorrectly(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert - Should handle empty quoted arguments correctly
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", "C:\\Editor.exe"), Times.Once);
        }

        [Theory]
        [InlineData("C:\\Editor.exe\" \"arg with \\\"quote\\\" inside\"")]
        [InlineData("C:\\Editor.exe\" \"arg with \\\"nested\\\" quotes\"")]
        public async Task TryOpenTransformationConfigAsync_WithEmbeddedQuotes_ReturnsFalseAndLogsWarning(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert - Current regex doesn't support escaped quotes, so should fail
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Malformed editor command: {0}", editorCommand), Times.Once);
        }

        [Theory]
        [InlineData("C:\\Editor.exe\" arg1 garbage\"")]
        [InlineData("C:\\Editor.exe\" arg1 \"garbage")]
        [InlineData("C:\\Editor.exe arg1\" garbage")]
        public async Task TryOpenTransformationConfigAsync_WithTrailingGarbage_ReturnsFalseAndLogsWarning(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("Malformed editor command: {0}", editorCommand), Times.Once);
        }

        [Theory]
        [InlineData("C:\\Editor.exe")]
        [InlineData("notepad.exe")]
        [InlineData("vim")]
        public async Task TryOpenTransformationConfigAsync_WithSingleUnquotedToken_HandlesCorrectly(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert - Should handle single unquoted tokens correctly
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", editorCommand), Times.Once);
        }

        [Theory]
        [InlineData("\"C:\\Program Files\\Editor.exe\"")]
        [InlineData("\"notepad.exe\"")]
        [InlineData("\"vim\"")]
        public async Task TryOpenTransformationConfigAsync_WithSingleQuotedToken_HandlesCorrectly(string editorCommand)
        {
            // Arrange
            var config = new GeneralSettingsConfig { EditorCommand = editorCommand };
            var service = CreateService(config);

            // Act
            var result = await service.TryOpenTransformationConfigAsync();

            // Assert - Should handle single quoted tokens correctly
            result.Should().BeTrue();
            var expectedExecutable = editorCommand.Trim('"');
            _loggerMock.Verify(x => x.Info("External editor launched successfully: {0}", expectedExecutable), Times.Once);
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