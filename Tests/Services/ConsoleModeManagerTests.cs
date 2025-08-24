using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class ConsoleModeManagerTests : IDisposable
    {
        private readonly Mock<IConsole> _consoleMock;
        private readonly Mock<IConfigManager> _configManagerMock;
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly Mock<IShortcutConfigurationManager> _shortcutManagerMock;
        private readonly Mock<IConsoleModeContentProvider> _mainRendererMock;
        private readonly Mock<IConsoleModeContentProvider> _helpRendererMock;
        private readonly Mock<IConsoleModeContentProvider> _networkRendererMock;
        private readonly ApplicationConfig _testAppConfig;
        private readonly UserPreferences _testUserPreferences;
        private readonly List<IServiceStats> _testStats;
        private readonly List<IConsoleModeContentProvider> _allRenderers;

        public ConsoleModeManagerTests()
        {
            _consoleMock = new Mock<IConsole>();
            _configManagerMock = new Mock<IConfigManager>();
            _loggerMock = new Mock<IAppLogger>();
            _shortcutManagerMock = new Mock<IShortcutConfigurationManager>();
            _mainRendererMock = new Mock<IConsoleModeContentProvider>();
            _helpRendererMock = new Mock<IConsoleModeContentProvider>();
            _networkRendererMock = new Mock<IConsoleModeContentProvider>();

            // Setup test data
            _testAppConfig = new ApplicationConfig();
            _testUserPreferences = new UserPreferences();
            _testStats = new List<IServiceStats>();

            // Setup renderer mocks
            SetupRendererMock(_mainRendererMock, ConsoleMode.Main, "Main Status", TimeSpan.FromMilliseconds(100));
            SetupRendererMock(_helpRendererMock, ConsoleMode.SystemHelp, "System Help", TimeSpan.FromMilliseconds(200));
            SetupRendererMock(_networkRendererMock, ConsoleMode.NetworkStatus, "Network Status", TimeSpan.FromMilliseconds(150));

            // Make main renderer implement IMainStatusRenderer for compatibility
            _mainRendererMock.As<IMainStatusRenderer>();

            _allRenderers = new List<IConsoleModeContentProvider>
            {
                _mainRendererMock.Object,
                _helpRendererMock.Object,
                _networkRendererMock.Object
            };

            // Setup config manager
            _configManagerMock.Setup(x => x.LoadApplicationConfigAsync()).ReturnsAsync(_testAppConfig);
            _configManagerMock.Setup(x => x.LoadUserPreferencesAsync()).ReturnsAsync(_testUserPreferences);

            // Setup console
            _consoleMock.Setup(x => x.WindowWidth).Returns(120);
            _consoleMock.Setup(x => x.WindowHeight).Returns(30);

            // Setup shortcut manager
            _shortcutManagerMock.Setup(x => x.GetDisplayString(ShortcutAction.ShowSystemHelp)).Returns("F1");
            _shortcutManagerMock.Setup(x => x.GetDisplayString(ShortcutAction.ShowNetworkStatus)).Returns("F2");
        }

        private static void SetupRendererMock(Mock<IConsoleModeContentProvider> mock, ConsoleMode mode, string displayName, TimeSpan updateInterval)
        {
            mock.Setup(x => x.Mode).Returns(mode);
            mock.Setup(x => x.DisplayName).Returns(displayName);
            mock.Setup(x => x.PreferredUpdateInterval).Returns(updateInterval);
            mock.Setup(x => x.GetContent(It.IsAny<ConsoleRenderContext>())).Returns(new[] { $"Content from {displayName}" });
            mock.Setup(x => x.TryOpenInExternalEditorAsync()).ReturnsAsync(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup managed resources if needed
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Assert
            manager.Should().NotBeNull();
            manager.CurrentMode.Should().Be(ConsoleMode.Main);
            manager.MainStatusRenderer.Should().NotBeNull();
            _loggerMock.Verify(x => x.Debug("ConsoleModeManager initialized with {0} renderers. Current mode: {1}", 3, ConsoleMode.Main), Times.Once);
            _loggerMock.Verify(x => x.Debug("All required console mode renderers are available: {0}", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleModeManager(null!, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            act.Should().Throw<ArgumentNullException>().WithParameterName("console");
        }

        [Fact]
        public void Constructor_WithNullConfigManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleModeManager(_consoleMock.Object, null!, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            act.Should().Throw<ArgumentNullException>().WithParameterName("configManager");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, null!, _shortcutManagerMock.Object, _allRenderers);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithNullShortcutManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, null!, _allRenderers);
            act.Should().Throw<ArgumentNullException>().WithParameterName("shortcutManager");
        }

        [Fact]
        public void Constructor_WithNullRenderers_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("renderers");
        }

        [Fact]
        public void Constructor_WithMissingMainRenderer_ThrowsInvalidOperationException()
        {
            // Arrange - Remove main renderer
            var incompleteRenderers = new List<IConsoleModeContentProvider> { _helpRendererMock.Object, _networkRendererMock.Object };

            // Act & Assert
            Action act = () => new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, incompleteRenderers);
            act.Should().Throw<InvalidOperationException>().WithMessage("Missing required console mode renderers: Main");
        }

        [Fact]
        public void Constructor_WithMissingSystemHelpRenderer_ThrowsInvalidOperationException()
        {
            // Arrange - Remove help renderer
            var incompleteRenderers = new List<IConsoleModeContentProvider> { _mainRendererMock.Object, _networkRendererMock.Object };

            // Act & Assert
            Action act = () => new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, incompleteRenderers);
            act.Should().Throw<InvalidOperationException>().WithMessage("Missing required console mode renderers: SystemHelp");
        }

        [Fact]
        public void Constructor_WithMissingNetworkStatusRenderer_ThrowsInvalidOperationException()
        {
            // Arrange - Remove network renderer
            var incompleteRenderers = new List<IConsoleModeContentProvider> { _mainRendererMock.Object, _helpRendererMock.Object };

            // Act & Assert
            Action act = () => new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, incompleteRenderers);
            act.Should().Throw<InvalidOperationException>().WithMessage("Missing required console mode renderers: NetworkStatus");
        }

        [Fact]
        public void Constructor_WithMultipleMissingRenderers_ThrowsInvalidOperationException()
        {
            // Arrange - Only provide main renderer
            var incompleteRenderers = new List<IConsoleModeContentProvider> { _mainRendererMock.Object };

            // Act & Assert
            Action act = () => new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, incompleteRenderers);
            act.Should().Throw<InvalidOperationException>().WithMessage("Missing required console mode renderers: SystemHelp, NetworkStatus");
        }

        #endregion

        #region Property Tests

        [Fact]
        public void CurrentMode_InitiallyReturnsMain()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act & Assert
            manager.CurrentMode.Should().Be(ConsoleMode.Main);
        }

        [Fact]
        public void MainStatusRenderer_ReturnsCorrectRenderer()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            var mainRenderer = manager.MainStatusRenderer;

            // Assert
            mainRenderer.Should().NotBeNull();
            mainRenderer.Should().Be(_mainRendererMock.Object);
        }

        #endregion

        #region Toggle Method Tests

        [Fact]
        public void Toggle_FromMainToSystemHelp_SwitchesToSystemHelp()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            manager.Toggle(ConsoleMode.SystemHelp);

            // Assert
            manager.CurrentMode.Should().Be(ConsoleMode.SystemHelp);
            _mainRendererMock.Verify(x => x.Exit(_consoleMock.Object), Times.Once);
            _helpRendererMock.Verify(x => x.Enter(_consoleMock.Object), Times.Once);
        }

        [Fact]
        public void Toggle_FromSystemHelpToSystemHelp_ReturnsToMain()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            manager.SetMode(ConsoleMode.SystemHelp); // First switch to help mode
            _mainRendererMock.Reset();
            _helpRendererMock.Reset();

            // Act
            manager.Toggle(ConsoleMode.SystemHelp);

            // Assert
            manager.CurrentMode.Should().Be(ConsoleMode.Main);
            _helpRendererMock.Verify(x => x.Exit(_consoleMock.Object), Times.Once);
            _mainRendererMock.Verify(x => x.Enter(_consoleMock.Object), Times.Once);
        }

        [Fact]
        public void Toggle_FromMainToNetworkStatus_SwitchesToNetworkStatus()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            manager.Toggle(ConsoleMode.NetworkStatus);

            // Assert
            manager.CurrentMode.Should().Be(ConsoleMode.NetworkStatus);
            _mainRendererMock.Verify(x => x.Exit(_consoleMock.Object), Times.Once);
            _networkRendererMock.Verify(x => x.Enter(_consoleMock.Object), Times.Once);
        }

        [Fact]
        public void Toggle_FromNetworkStatusToNetworkStatus_ReturnsToMain()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            manager.SetMode(ConsoleMode.NetworkStatus); // First switch to network mode
            _mainRendererMock.Reset();
            _networkRendererMock.Reset();

            // Act
            manager.Toggle(ConsoleMode.NetworkStatus);

            // Assert
            manager.CurrentMode.Should().Be(ConsoleMode.Main);
            _networkRendererMock.Verify(x => x.Exit(_consoleMock.Object), Times.Once);
            _mainRendererMock.Verify(x => x.Enter(_consoleMock.Object), Times.Once);
        }

        #endregion

        #region SetMode Method Tests

        [Fact]
        public void SetMode_ToValidMode_SwitchesCorrectly()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            manager.SetMode(ConsoleMode.SystemHelp);

            // Assert
            manager.CurrentMode.Should().Be(ConsoleMode.SystemHelp);
            _loggerMock.Verify(x => x.Debug("Exited console mode: {0}", ConsoleMode.Main), Times.Once);
            _loggerMock.Verify(x => x.Debug("Entered console mode: {0} ({1})", ConsoleMode.SystemHelp, "System Help"), Times.Once);
        }

        [Fact]
        public void SetMode_ToSameMode_LogsDebugAndDoesNothing()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            _loggerMock.Reset(); // Clear constructor logs

            // Act
            manager.SetMode(ConsoleMode.Main); // Already in Main mode

            // Assert
            manager.CurrentMode.Should().Be(ConsoleMode.Main);
            _loggerMock.Verify(x => x.Debug("Already in mode {0}, no change needed", ConsoleMode.Main), Times.Once);
            _mainRendererMock.Verify(x => x.Exit(It.IsAny<IConsole>()), Times.Never);
            _mainRendererMock.Verify(x => x.Enter(It.IsAny<IConsole>()), Times.Never);
        }

        [Fact]
        public void SetMode_ToUnknownMode_LogsWarningAndDoesNothing()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            var unknownMode = (ConsoleMode)999; // Invalid mode

            // Act
            manager.SetMode(unknownMode);

            // Assert
            manager.CurrentMode.Should().Be(ConsoleMode.Main); // Should remain unchanged
            _loggerMock.Verify(x => x.Warning("Attempted to set unknown console mode: {0}", unknownMode), Times.Once);
        }

        [Fact]
        public void SetMode_CallsExitOnPreviousRenderer()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            manager.SetMode(ConsoleMode.SystemHelp);

            // Assert
            _mainRendererMock.Verify(x => x.Exit(_consoleMock.Object), Times.Once);
        }

        [Fact]
        public void SetMode_CallsEnterOnNewRenderer()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            manager.SetMode(ConsoleMode.SystemHelp);

            // Assert
            _helpRendererMock.Verify(x => x.Enter(_consoleMock.Object), Times.Once);
        }

        [Fact]
        public async Task SetMode_ResetsLastUpdateTime()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // First update to set a last update time
            manager.Update(_testStats);

            // Wait a bit to ensure time difference
            await Task.Delay(10);

            // Act
            manager.SetMode(ConsoleMode.SystemHelp);

            // Now update should happen immediately (not skip due to interval)
            _helpRendererMock.Reset();
            manager.Update(_testStats);

            // Assert - Update should have been called (not skipped)
            _helpRendererMock.Verify(x => x.GetContent(It.IsAny<ConsoleRenderContext>()), Times.Once);
        }

        #endregion

        #region Update Method Tests

        [Fact]
        public void Update_WithNullActiveRenderer_LogsWarningAndReturns()
        {
            // Arrange - Create manager and use reflection to set _activeRenderer to null
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Use reflection to set _activeRenderer to null
            var activeRendererField = typeof(ConsoleModeManager).GetField("_activeRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            activeRendererField!.SetValue(manager, null);

            // Act
            manager.Update(_testStats);

            // Assert
            _loggerMock.Verify(x => x.Warning("No active renderer available for update"), Times.Once);
            _consoleMock.Verify(x => x.WriteLines(It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public void Update_BuildsCorrectRenderContext()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            ConsoleRenderContext? capturedContext = null;
            _mainRendererMock.Setup(x => x.GetContent(It.IsAny<ConsoleRenderContext>()))
                .Callback<ConsoleRenderContext>(ctx => capturedContext = ctx)
                .Returns(new[] { "test content" });

            // Act
            manager.Update(_testStats);

            // Assert
            capturedContext.Should().NotBeNull();
            capturedContext!.ServiceStats.Should().BeSameAs(_testStats);
            capturedContext.ApplicationConfig.Should().BeSameAs(_testAppConfig);
            capturedContext.UserPreferences.Should().BeSameAs(_testUserPreferences);
            capturedContext.ConsoleSize.Should().Be((120, 30));
        }

        [Fact]
        public void Update_CallsGetContentOnActiveRenderer()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            manager.Update(_testStats);

            // Assert
            _mainRendererMock.Verify(x => x.GetContent(It.IsAny<ConsoleRenderContext>()), Times.Once);
        }

        [Fact]
        public void Update_WrapsContentWithHeaderAndFooter()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            _mainRendererMock.Setup(x => x.GetContent(It.IsAny<ConsoleRenderContext>())).Returns(new[] { "Original Content" });

            string[]? capturedOutput = null;
            _consoleMock.Setup(x => x.WriteLines(It.IsAny<string[]>()))
                .Callback<string[]>(output => capturedOutput = output);

            // Act
            manager.Update(_testStats);

            // Assert
            capturedOutput.Should().NotBeNull();
            capturedOutput!.Should().Contain("=== MAIN STATUS ==="); // Header
            capturedOutput.Should().Contain("Original Content"); // Original content
            capturedOutput.Should().Contain(line => line.Contains("F1: System Help")); // Footer
            capturedOutput.Should().Contain(line => line.Contains("Ctrl+C: Exit")); // Footer
        }

        [Fact]
        public void Update_CallsWriteLinesOnConsole()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            manager.Update(_testStats);

            // Assert
            _consoleMock.Verify(x => x.WriteLines(It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task Update_UpdatesLastUpdateTime()
        {
            // Arrange
            _mainRendererMock.Setup(x => x.PreferredUpdateInterval).Returns(TimeSpan.FromMilliseconds(1));
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act - First update
            manager.Update(_testStats);

            // Reset and wait
            _mainRendererMock.Reset();
            _consoleMock.Reset();
            await Task.Delay(5);

            // Second update should work (proving time was updated)
            manager.Update(_testStats);

            // Assert
            _mainRendererMock.Verify(x => x.GetContent(It.IsAny<ConsoleRenderContext>()), Times.Once);
        }

        [Fact]
        public void Update_WhenExceptionOccurs_LogsErrorAndFallsBackToMain()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            manager.SetMode(ConsoleMode.SystemHelp); // Switch to help mode

            // Setup help renderer to throw exception
            _helpRendererMock.Setup(x => x.GetContent(It.IsAny<ConsoleRenderContext>()))
                .Throws(new InvalidOperationException("Test exception"));

            // Act
            manager.Update(_testStats);

            // Assert
            _loggerMock.Verify(x => x.Error("Failed to update console mode {0}: {1}", ConsoleMode.SystemHelp, "Test exception"), Times.Once);
            _loggerMock.Verify(x => x.Info("Falling back to Main mode due to render error"), Times.Once);
            manager.CurrentMode.Should().Be(ConsoleMode.Main);
        }

        [Fact]
        public void Update_WhenExceptionInMainMode_DoesNotFallBack()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Setup main renderer to throw exception
            _mainRendererMock.Setup(x => x.GetContent(It.IsAny<ConsoleRenderContext>()))
                .Throws(new InvalidOperationException("Test exception"));

            // Act
            manager.Update(_testStats);

            // Assert
            _loggerMock.Verify(x => x.Error("Failed to update console mode {0}: {1}", ConsoleMode.Main, "Test exception"), Times.Once);
            _loggerMock.Verify(x => x.Info("Falling back to Main mode due to render error"), Times.Never);
            manager.CurrentMode.Should().Be(ConsoleMode.Main); // Should stay in Main
        }

        #endregion

        #region Clear Method Tests

        [Fact]
        public void Clear_CallsConsoleClearAndLogsDebug()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act
            manager.Clear();

            // Assert
            _consoleMock.Verify(x => x.Clear(), Times.Once);
            _loggerMock.Verify(x => x.Debug("Console cleared"), Times.Once);
        }

        #endregion

        #region TryOpenActiveModeInEditorAsync Method Tests

        [Fact]
        public async Task TryOpenActiveModeInEditorAsync_WithNullActiveRenderer_ReturnsFalse()
        {
            // Arrange - Create manager and use reflection to set _activeRenderer to null
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Use reflection to set _activeRenderer to null
            var activeRendererField = typeof(ConsoleModeManager).GetField("_activeRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            activeRendererField!.SetValue(manager, null);

            // Act
            var result = await manager.TryOpenActiveModeInEditorAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Warning("No active renderer available for external editor request"), Times.Once);
        }

        [Fact]
        public async Task TryOpenActiveModeInEditorAsync_WhenRendererReturnsTrue_ReturnsTrue()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            _mainRendererMock.Setup(x => x.TryOpenInExternalEditorAsync()).ReturnsAsync(true);

            // Act
            var result = await manager.TryOpenActiveModeInEditorAsync();

            // Assert
            result.Should().BeTrue();
            _loggerMock.Verify(x => x.Debug("Forwarding external editor request to {0} mode renderer", ConsoleMode.Main), Times.Once);
            _loggerMock.Verify(x => x.Debug("External editor opened successfully from {0} mode", ConsoleMode.Main), Times.Once);
        }

        [Fact]
        public async Task TryOpenActiveModeInEditorAsync_WhenRendererReturnsFalse_ReturnsFalse()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            _mainRendererMock.Setup(x => x.TryOpenInExternalEditorAsync()).ReturnsAsync(false);

            // Act
            var result = await manager.TryOpenActiveModeInEditorAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Debug("Forwarding external editor request to {0} mode renderer", ConsoleMode.Main), Times.Once);
            _loggerMock.Verify(x => x.Warning("External editor failed to open from {0} mode", ConsoleMode.Main), Times.Once);
        }

        [Fact]
        public async Task TryOpenActiveModeInEditorAsync_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            _mainRendererMock.Setup(x => x.TryOpenInExternalEditorAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await manager.TryOpenActiveModeInEditorAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Error("Error opening external editor from {0} mode: {1}", ConsoleMode.Main, "Test exception"), Times.Once);
        }

        [Fact]
        public async Task TryOpenActiveModeInEditorAsync_LogsCorrectMessages()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            manager.SetMode(ConsoleMode.SystemHelp);
            _helpRendererMock.Setup(x => x.TryOpenInExternalEditorAsync()).ReturnsAsync(true);

            // Act
            await manager.TryOpenActiveModeInEditorAsync();

            // Assert
            _loggerMock.Verify(x => x.Debug("Forwarding external editor request to {0} mode renderer", ConsoleMode.SystemHelp), Times.Once);
            _loggerMock.Verify(x => x.Debug("External editor opened successfully from {0} mode", ConsoleMode.SystemHelp), Times.Once);
        }

        #endregion

        #region Footer Generation Tests

        [Fact]
        public void Update_GenerateFooter_ForMainMode_ShowsHelpAndNetworkOptions()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            string[]? capturedOutput = null;
            _consoleMock.Setup(x => x.WriteLines(It.IsAny<string[]>()))
                .Callback<string[]>(output => capturedOutput = output);

            // Act
            manager.Update(_testStats);

            // Assert
            capturedOutput.Should().NotBeNull();
            var footerLine = capturedOutput![capturedOutput.Length - 1];
            footerLine.Should().Contain("F1: System Help");
            footerLine.Should().Contain("F2: Network Status");
            footerLine.Should().Contain("Ctrl+C: Exit");
        }

        [Fact]
        public void Update_GenerateFooter_ForSystemHelpMode_ShowsReturnAndNetworkOptions()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            manager.SetMode(ConsoleMode.SystemHelp);

            string[]? capturedOutput = null;
            _consoleMock.Setup(x => x.WriteLines(It.IsAny<string[]>()))
                .Callback<string[]>(output => capturedOutput = output);

            // Act
            manager.Update(_testStats);

            // Assert
            capturedOutput.Should().NotBeNull();
            var footerLine = capturedOutput![capturedOutput.Length - 1];
            footerLine.Should().Contain("F1: Return to Main");
            footerLine.Should().Contain("F2: Network Status");
            footerLine.Should().Contain("Ctrl+C: Exit");
        }

        [Fact]
        public void Update_GenerateFooter_ForNetworkStatusMode_ShowsHelpAndReturnOptions()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            manager.SetMode(ConsoleMode.NetworkStatus);

            string[]? capturedOutput = null;
            _consoleMock.Setup(x => x.WriteLines(It.IsAny<string[]>()))
                .Callback<string[]>(output => capturedOutput = output);

            // Act
            manager.Update(_testStats);

            // Assert
            capturedOutput.Should().NotBeNull();
            var footerLine = capturedOutput![capturedOutput.Length - 1];
            footerLine.Should().Contain("F1: System Help");
            footerLine.Should().Contain("F2: Return to Main");
            footerLine.Should().Contain("Ctrl+C: Exit");
        }

        [Fact]
        public void Update_GenerateFooter_AlwaysIncludesExitOption()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Test all modes
            var modes = new[] { ConsoleMode.Main, ConsoleMode.SystemHelp, ConsoleMode.NetworkStatus };

            foreach (var mode in modes)
            {
                // Arrange for this mode
                manager.SetMode(mode);
                string[]? capturedOutput = null;
                _consoleMock.Setup(x => x.WriteLines(It.IsAny<string[]>()))
                    .Callback<string[]>(output => capturedOutput = output);

                // Act
                manager.Update(_testStats);

                // Assert
                capturedOutput.Should().NotBeNull($"Output should not be null for mode {mode}");
                var footerLine = capturedOutput![capturedOutput.Length - 1];
                footerLine.Should().Contain("Ctrl+C: Exit", $"Exit option should be present in {mode} mode");
            }
        }

        #endregion

        #region Integration & Edge Case Tests

        [Fact]
        public void Update_WithComplexServiceStats_HandlesCorrectly()
        {
            // Arrange
            var complexStats = new List<IServiceStats>
            {
                Mock.Of<IServiceStats>(s => s.ServiceName == "Service1"),
                Mock.Of<IServiceStats>(s => s.ServiceName == "Service2"),
                Mock.Of<IServiceStats>(s => s.ServiceName == "Service3")
            };

            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            ConsoleRenderContext? capturedContext = null;
            _mainRendererMock.Setup(x => x.GetContent(It.IsAny<ConsoleRenderContext>()))
                .Callback<ConsoleRenderContext>(ctx => capturedContext = ctx)
                .Returns(new[] { "test content" });

            // Act
            manager.Update(complexStats);

            // Assert
            capturedContext.Should().NotBeNull();
            capturedContext!.ServiceStats.Should().BeSameAs(complexStats);
            capturedContext.ServiceStats.Should().HaveCount(3);
        }

        [Fact]
        public void SetMode_MultipleTransitions_MaintainsCorrectState()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);

            // Act - Perform multiple transitions
            manager.SetMode(ConsoleMode.SystemHelp);
            manager.CurrentMode.Should().Be(ConsoleMode.SystemHelp);

            manager.SetMode(ConsoleMode.NetworkStatus);
            manager.CurrentMode.Should().Be(ConsoleMode.NetworkStatus);

            manager.SetMode(ConsoleMode.Main);
            manager.CurrentMode.Should().Be(ConsoleMode.Main);

            manager.SetMode(ConsoleMode.SystemHelp);
            manager.CurrentMode.Should().Be(ConsoleMode.SystemHelp);

            // Assert - Verify all exit/enter calls were made correctly
            _mainRendererMock.Verify(x => x.Exit(_consoleMock.Object), Times.Exactly(2)); // Exited twice (to Help, to Network)
            _helpRendererMock.Verify(x => x.Exit(_consoleMock.Object), Times.Once); // Exited once (to Network)
            _networkRendererMock.Verify(x => x.Exit(_consoleMock.Object), Times.Once); // Exited once (to Main)

            _helpRendererMock.Verify(x => x.Enter(_consoleMock.Object), Times.Exactly(2)); // Entered twice
            _networkRendererMock.Verify(x => x.Enter(_consoleMock.Object), Times.Once); // Entered once
            _mainRendererMock.Verify(x => x.Enter(_consoleMock.Object), Times.Once); // Entered once (from Network)
        }

        [Fact]
        public void Update_WrapContentWithHeaderAndFooter_WithEmptyContent_AddsHeaderAndFooter()
        {
            // Arrange
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            _mainRendererMock.Setup(x => x.GetContent(It.IsAny<ConsoleRenderContext>())).Returns(new string[0]); // Empty content

            string[]? capturedOutput = null;
            _consoleMock.Setup(x => x.WriteLines(It.IsAny<string[]>()))
                .Callback<string[]>(output => capturedOutput = output);

            // Act
            manager.Update(_testStats);

            // Assert
            capturedOutput.Should().NotBeNull();
            capturedOutput!.Should().Contain("=== MAIN STATUS ==="); // Header
            capturedOutput!.Should().Contain(line => line.Contains("Ctrl+C: Exit")); // Footer
            capturedOutput!.Length.Should().BeGreaterThan(2); // At least header + separator + footer
        }

        [Fact]
        public void Update_WrapContentWithHeaderAndFooter_WithMultiLineContent_PreservesContent()
        {
            // Arrange
            var multiLineContent = new[] { "Line 1", "Line 2", "Line 3", "Line 4" };
            var manager = new ConsoleModeManager(_consoleMock.Object, _configManagerMock.Object, _loggerMock.Object, _shortcutManagerMock.Object, _allRenderers);
            _mainRendererMock.Setup(x => x.GetContent(It.IsAny<ConsoleRenderContext>())).Returns(multiLineContent);

            string[]? capturedOutput = null;
            _consoleMock.Setup(x => x.WriteLines(It.IsAny<string[]>()))
                .Callback<string[]>(output => capturedOutput = output);

            // Act
            manager.Update(_testStats);

            // Assert
            capturedOutput.Should().NotBeNull();
            capturedOutput!.Should().Contain("=== MAIN STATUS ==="); // Header
            capturedOutput.Should().Contain("Line 1"); // Original content preserved
            capturedOutput.Should().Contain("Line 2");
            capturedOutput.Should().Contain("Line 3");
            capturedOutput.Should().Contain("Line 4");
            capturedOutput.Should().Contain(line => line.Contains("Ctrl+C: Exit")); // Footer
        }

        #endregion
    }
}
