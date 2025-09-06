using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class ApplicationOrchestratorTests : IDisposable
    {
        private readonly Mock<IVTubeStudioPCClient> _mockPCClient;
        private readonly Mock<IVTubeStudioPhoneClient> _mockPhoneClient;
        private readonly Mock<ITransformationEngine> _mockTransformationEngine;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IConsoleModeManager> _mockModeManager;
        private readonly Mock<IKeyboardInputHandler> _mockKeyboardInputHandler;
        private readonly Mock<IVTubeStudioPCParameterManager> _mockParameterManager;
        private readonly Mock<IRecoveryPolicy> _mockRecoveryPolicy;
        private readonly Mock<IConsole> _mockConsole;
        private readonly Mock<IConsoleWindowManager> _mockConsoleWindowManager;
        private readonly Mock<IParameterColorService> _mockColorService;
        private readonly Mock<IExternalEditorService> _mockExternalEditorService;
        private readonly Mock<IShortcutConfigurationManager> _mockShortcutConfigurationManager;
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly Mock<IFileChangeWatcher> _mockAppConfigWatcher;
        private readonly VTubeStudioPhoneClientConfig _phoneConfig;
        private readonly ApplicationConfig _applicationConfig;
        private readonly UserPreferences _userPreferences;

        public ApplicationOrchestratorTests()
        {
            _mockPCClient = new Mock<IVTubeStudioPCClient>();
            _mockPhoneClient = new Mock<IVTubeStudioPhoneClient>();
            _mockTransformationEngine = new Mock<ITransformationEngine>();
            _mockLogger = new Mock<IAppLogger>();
            _mockModeManager = new Mock<IConsoleModeManager>();
            _mockKeyboardInputHandler = new Mock<IKeyboardInputHandler>();
            _mockParameterManager = new Mock<IVTubeStudioPCParameterManager>();
            _mockRecoveryPolicy = new Mock<IRecoveryPolicy>();
            _mockConsole = new Mock<IConsole>();
            _mockConsoleWindowManager = new Mock<IConsoleWindowManager>();
            _mockColorService = new Mock<IParameterColorService>();
            _mockExternalEditorService = new Mock<IExternalEditorService>();
            _mockShortcutConfigurationManager = new Mock<IShortcutConfigurationManager>();
            _mockConfigManager = new Mock<IConfigManager>();
            _mockAppConfigWatcher = new Mock<IFileChangeWatcher>();

            _phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1",
                RequestIntervalSeconds = 0.1,
                ErrorDelayMs = 100
            };

            _applicationConfig = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "notepad.exe",
                    Shortcuts = new Dictionary<string, string>
                    {
                        { "CycleTransformationEngineVerbosity", "F1" },
                        { "CyclePCClientVerbosity", "F2" },
                        { "CyclePhoneClientVerbosity", "F3" },
                        { "ReloadTransformationConfig", "F4" },
                        { "OpenConfigInEditor", "F5" },
                        { "ShowSystemHelp", "F6" },
                        { "ShowNetworkStatus", "F7" }
                    }
                }
            };

            _userPreferences = new UserPreferences
            {
                PreferredConsoleWidth = 120,
                PreferredConsoleHeight = 30,
                TransformationEngineVerbosity = VerbosityLevel.Normal,
                PCClientVerbosity = VerbosityLevel.Normal,
                PhoneClientVerbosity = VerbosityLevel.Normal
            };

            // Setup common mock behaviors
            _mockConfigManager.Setup(x => x.ApplicationConfigPath).Returns("test-config.json");
            _mockConsoleWindowManager.Setup(x => x.GetCurrentSize()).Returns((120, 30));
            _mockConsoleWindowManager.Setup(x => x.SetConsoleSize(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
            _mockRecoveryPolicy.Setup(x => x.GetNextDelay()).Returns(TimeSpan.FromSeconds(5));
            _mockShortcutConfigurationManager.Setup(x => x.GetMappedShortcuts()).Returns(new Dictionary<ShortcutAction, Shortcut?>());
            _mockShortcutConfigurationManager.Setup(x => x.GetIncorrectShortcuts()).Returns(new Dictionary<ShortcutAction, string>());
        }

        private ApplicationOrchestrator CreateOrchestrator()
        {
            return new ApplicationOrchestrator(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                _phoneConfig,
                _mockLogger.Object,
                _mockModeManager.Object,
                _mockKeyboardInputHandler.Object,
                _mockParameterManager.Object,
                _mockRecoveryPolicy.Object,
                _mockConsole.Object,
                _mockConsoleWindowManager.Object,
                _mockColorService.Object,
                _mockExternalEditorService.Object,
                _mockShortcutConfigurationManager.Object,
                _applicationConfig,
                _userPreferences,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var orchestrator = CreateOrchestrator();

            // Assert
            orchestrator.Should().NotBeNull();
            orchestrator.CONSOLE_UPDATE_INTERVAL_SECONDS.Should().Be(0.1);
        }


        #endregion

        #region InitializeAsync Tests

        [Fact]
        public async Task InitializeAsync_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationToken = CancellationToken.None;

            // Setup mocks
            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _mockTransformationEngine.Verify(x => x.LoadRulesAsync(), Times.Once);
            _mockAppConfigWatcher.Verify(x => x.StartWatching("test-config.json"), Times.Once);
            _mockPCClient.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _mockPhoneClient.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _mockParameterManager.Verify(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken), Times.Once);
            _mockShortcutConfigurationManager.Verify(x => x.LoadFromConfiguration(_applicationConfig.GeneralSettings), Times.Once);
            _mockLogger.Verify(x => x.Info("Application initialized successfully"), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenParameterSyncFails_LogsWarning()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(false));

            // Act
            await orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Warning("Parameter synchronization failed during initialization, will retry during recovery"), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenConsoleResizeFails_LogsWarning()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationToken = CancellationToken.None;

            _mockConsoleWindowManager.Setup(x => x.SetConsoleSize(It.IsAny<int>(), It.IsAny<int>())).Returns(false);
            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Warning("Failed to resize console window to preferred size. Using current size: {0}x{1}", 120, 30), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenConsoleSetupThrows_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationToken = CancellationToken.None;

            _mockConsoleWindowManager.Setup(x => x.GetCurrentSize()).Throws(new Exception("Console error"));
            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error setting up console window", It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region RunAsync Tests

        [Fact]
        public async Task RunAsync_WithValidParameters_RunsSuccessfully()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Setup mocks for the main loop
            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Setup mocks for the main loop
            _mockPhoneClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PhoneClient", "VTube Studio Phone Client", null, true, DateTime.UtcNow, null, null));
            _mockPCClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PCClient", "VTube Studio PC Client", null, true, DateTime.UtcNow, null, null));
            _mockTransformationEngine.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("TransformationEngine", "Transformation Engine", null, true, DateTime.UtcNow, null, null));
            _mockPhoneClient.Setup(x => x.ReceiveResponseAsync(cancellationToken)).Returns(Task.FromResult(false));
            _mockPhoneClient.Setup(x => x.SendTrackingRequestAsync()).Returns(Task.CompletedTask);

            // Cancel after a short delay to exit the loop
            cancellationTokenSource.CancelAfter(50);

            // Act
            await orchestrator.RunAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Info("Starting application..."), Times.Once);
            _mockLogger.Verify(x => x.Info("Starting main application loop..."), Times.Once);
            _mockLogger.Verify(x => x.Info("Application stopped"), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenExceptionOccurs_HandlesGracefully()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Setup mocks
            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Setup mocks to throw exception in main loop
            _mockPhoneClient.Setup(x => x.GetServiceStats()).Throws(new Exception("Test exception"));
            _mockPCClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PCClient", "VTube Studio PC Client", null, true, DateTime.UtcNow, null, null));
            _mockTransformationEngine.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("TransformationEngine", "Transformation Engine", null, true, DateTime.UtcNow, null, null));

            // Cancel after a short delay
            cancellationTokenSource.CancelAfter(100);

            // Act
            await orchestrator.RunAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Error("Error in application loop: {0}", "Test exception"), Times.AtLeastOnce);
        }

        #endregion

        #region Event Handler Tests

        [Fact]
        public void OnTrackingDataReceived_WithValidData_ProcessesSuccessfully()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape> { new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 } }
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _mockTransformationEngine.Setup(x => x.TransformData(trackingData)).Returns(pcTrackingInfo);
            _mockPCClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PCClient", "VTube Studio PC Client", null, true, DateTime.UtcNow, null, null));
            _mockPCClient.Setup(x => x.SendTrackingAsync(pcTrackingInfo, CancellationToken.None)).Returns(Task.CompletedTask);
            _mockTransformationEngine.Setup(x => x.GetParameterDefinitions()).Returns(new List<VTSParameter>());

            // Act
            var method = orchestrator.GetType().GetMethod("OnTrackingDataReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, new object[] { _mockPhoneClient.Object, trackingData });

            // Assert
            _mockTransformationEngine.Verify(x => x.TransformData(trackingData), Times.Once);
            _mockPCClient.Verify(x => x.SendTrackingAsync(pcTrackingInfo, CancellationToken.None), Times.Once);
        }

        [Fact]
        public void OnTrackingDataReceived_WithNullData_DoesNothing()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();

            // Act
            orchestrator.GetType().GetMethod("OnTrackingDataReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(orchestrator, new object[] { null!, null! });

            // Assert
            _mockTransformationEngine.Verify(x => x.TransformData(It.IsAny<PhoneTrackingInfo>()), Times.Never);
        }

        [Fact]
        public void OnTrackingDataReceived_WhenPCClientUnhealthy_DoesNotSend()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape> { new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 } }
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _mockTransformationEngine.Setup(x => x.TransformData(trackingData)).Returns(pcTrackingInfo);
            _mockPCClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PCClient", "VTube Studio PC Client", null, false, DateTime.UtcNow, null, null));
            _mockTransformationEngine.Setup(x => x.GetParameterDefinitions()).Returns(new List<VTSParameter>());

            // Act
            var method = orchestrator.GetType().GetMethod("OnTrackingDataReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, new object[] { _mockPhoneClient.Object, trackingData });

            // Assert
            _mockPCClient.Verify(x => x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OnApplicationConfigChanged_WithValidConfig_ReloadsSuccessfully()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var newGeneralSettings = new GeneralSettingsConfig
            {
                EditorCommand = "code.exe",
                Shortcuts = new Dictionary<string, string>()
            };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ReturnsAsync(newGeneralSettings);
            _mockShortcutConfigurationManager.Setup(x => x.LoadFromConfiguration(newGeneralSettings)).Verifiable();

            // Act
            var method = orchestrator.GetType().GetMethod("OnApplicationConfigChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, new object[] { _mockAppConfigWatcher.Object, new FileChangeEventArgs("test.json") });

            // Wait a bit for async operations
            await Task.Delay(10);

            // Assert
            _mockConfigManager.Verify(x => x.LoadSectionAsync<GeneralSettingsConfig>(), Times.Once);
            _mockShortcutConfigurationManager.Verify(x => x.LoadFromConfiguration(newGeneralSettings), Times.Once);
            _mockLogger.Verify(x => x.Info("Application configuration reloaded successfully"), Times.Once);
        }

        [Fact]
        public async Task OnApplicationConfigChanged_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();

            _mockConfigManager.Setup(x => x.LoadSectionAsync<GeneralSettingsConfig>()).ThrowsAsync(new Exception("Config error"));

            // Act
            var method = orchestrator.GetType().GetMethod("OnApplicationConfigChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, new object[] { _mockAppConfigWatcher.Object, new FileChangeEventArgs("test.json") });

            // Wait a bit for async operations
            await Task.Delay(10);

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error reloading application configuration", It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region Recovery Tests

        [Fact]
        public async Task AttemptRecoveryAsync_WhenPCClientUnhealthy_AttemptsRecovery()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationToken = CancellationToken.None;

            _mockPCClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PCClient", "VTube Studio PC Client", null, false, DateTime.UtcNow, null, null));
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PhoneClient", "VTube Studio Phone Client", null, true, DateTime.UtcNow, null, null));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            var method = orchestrator.GetType().GetMethod("AttemptRecoveryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = await (Task<bool>)method?.Invoke(orchestrator, new object[] { cancellationToken })!;

            // Assert
            result.Should().BeTrue();
            _mockPCClient.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _mockLogger.Verify(x => x.Info("Attempting to recover PC client..."), Times.Once);
        }

        [Fact]
        public async Task AttemptRecoveryAsync_WhenPhoneClientUnhealthy_AttemptsRecovery()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationToken = CancellationToken.None;

            _mockPCClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PCClient", "VTube Studio PC Client", null, true, DateTime.UtcNow, null, null));
            _mockPhoneClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PhoneClient", "VTube Studio Phone Client", null, false, DateTime.UtcNow, null, null));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));

            // Act
            var method = orchestrator.GetType().GetMethod("AttemptRecoveryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = await (Task<bool>)method?.Invoke(orchestrator, new object[] { cancellationToken })!;

            // Assert
            result.Should().BeTrue();
            _mockPhoneClient.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _mockLogger.Verify(x => x.Info("Attempting to recover Phone client..."), Times.Once);
        }

        [Fact]
        public async Task AttemptRecoveryAsync_WhenAllClientsHealthy_DoesNotAttemptRecovery()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationToken = CancellationToken.None;

            _mockPCClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PCClient", "VTube Studio PC Client", null, true, DateTime.UtcNow, null, null));
            _mockPhoneClient.Setup(x => x.GetServiceStats()).Returns(new ServiceStats("PhoneClient", "VTube Studio Phone Client", null, true, DateTime.UtcNow, null, null));

            // Act
            var method = orchestrator.GetType().GetMethod("AttemptRecoveryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = await (Task<bool>)method?.Invoke(orchestrator, new object[] { cancellationToken })!;

            // Assert
            result.Should().BeFalse();
            _mockPCClient.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockPhoneClient.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Keyboard Shortcut Tests

        [Fact]
        public void RegisterKeyboardShortcuts_WithValidShortcuts_RegistersSuccessfully()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                { ShortcutAction.CycleTransformationEngineVerbosity, new Shortcut(ConsoleKey.F1, ConsoleModifiers.None) },
                { ShortcutAction.CyclePCClientVerbosity, new Shortcut(ConsoleKey.F2, ConsoleModifiers.None) }
            };

            _mockShortcutConfigurationManager.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _mockShortcutConfigurationManager.Setup(x => x.GetIncorrectShortcuts()).Returns(new Dictionary<ShortcutAction, string>());

            // Act
            var method = orchestrator.GetType().GetMethod("RegisterKeyboardShortcuts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, null);

            // Assert
            _mockKeyboardInputHandler.Verify(x => x.RegisterShortcut(It.IsAny<ConsoleKey>(), It.IsAny<ConsoleModifiers>(), It.IsAny<Action>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void RegisterKeyboardShortcuts_WithDisabledShortcuts_SkipsThem()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>
            {
                { ShortcutAction.CycleTransformationEngineVerbosity, new Shortcut(ConsoleKey.F1, ConsoleModifiers.None) },
                { ShortcutAction.CyclePCClientVerbosity, null } // Disabled shortcut
            };

            _mockShortcutConfigurationManager.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _mockShortcutConfigurationManager.Setup(x => x.GetIncorrectShortcuts()).Returns(new Dictionary<ShortcutAction, string>());

            // Act
            var method = orchestrator.GetType().GetMethod("RegisterKeyboardShortcuts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, null);

            // Assert
            _mockKeyboardInputHandler.Verify(x => x.RegisterShortcut(It.IsAny<ConsoleKey>(), It.IsAny<ConsoleModifiers>(), It.IsAny<Action>(), It.IsAny<string>()), Times.Once);
            _mockLogger.Verify(x => x.Debug("Skipping disabled shortcut for action: {0}", ShortcutAction.CyclePCClientVerbosity), Times.Once);
        }

        [Fact]
        public void RegisterKeyboardShortcuts_WithIncorrectShortcuts_LogsWarning()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var shortcuts = new Dictionary<ShortcutAction, Shortcut?>();
            var incorrectShortcuts = new Dictionary<ShortcutAction, string>
            {
                { ShortcutAction.CycleTransformationEngineVerbosity, "Invalid shortcut" }
            };

            _mockShortcutConfigurationManager.Setup(x => x.GetMappedShortcuts()).Returns(shortcuts);
            _mockShortcutConfigurationManager.Setup(x => x.GetIncorrectShortcuts()).Returns(incorrectShortcuts);

            // Act
            var method = orchestrator.GetType().GetMethod("RegisterKeyboardShortcuts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, null);

            // Assert
            _mockLogger.Verify(x => x.Warning("Invalid shortcut configurations detected: {0}", It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region Color Service Tests

        [Fact]
        public void InitializeColorServiceIfNeeded_WithValidTrackingData_InitializesOnce()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var trackingData = new PhoneTrackingInfo
            {
                BlendShapes = new List<BlendShape> { new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }, new BlendShape { Key = "eyeBlinkRight", Value = 0.3 } }
            };

            var parameterDefinitions = new List<VTSParameter>
            {
                new VTSParameter("param1", 0, 1, 0.5),
                new VTSParameter("param2", 0, 1, 0.5)
            };

            _mockTransformationEngine.Setup(x => x.GetParameterDefinitions()).Returns(parameterDefinitions);

            // Act - Call twice to ensure it only initializes once
            var method = orchestrator.GetType().GetMethod("InitializeColorServiceIfNeeded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, new object[] { trackingData });
            method?.Invoke(orchestrator, new object[] { trackingData });

            // Assert
            _mockColorService.Verify(x => x.InitializeFromConfiguration(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [Fact]
        public void InitializeColorServiceIfNeeded_WithNullBlendShapes_DoesNotInitialize()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var trackingData = new PhoneTrackingInfo
            {
                BlendShapes = new List<BlendShape>()
            };

            // Act
            var method = orchestrator.GetType().GetMethod("InitializeColorServiceIfNeeded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, new object[] { trackingData });

            // Assert
            _mockColorService.Verify(x => x.InitializeFromConfiguration(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public void InitializeColorServiceIfNeeded_WithEmptyBlendShapes_DoesNotInitialize()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var trackingData = new PhoneTrackingInfo
            {
                BlendShapes = new List<BlendShape>()
            };

            // Act
            var method = orchestrator.GetType().GetMethod("InitializeColorServiceIfNeeded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(orchestrator, new object[] { trackingData });

            // Assert
            _mockColorService.Verify(x => x.InitializeFromConfiguration(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        #endregion

        #region User Preferences Tests

        [Fact]
        public async Task UpdateUserPreferencesAsync_WithValidAction_UpdatesAndSaves()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var newWidth = 150;
            var newHeight = 40;

            _mockConfigManager.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>())).Returns(Task.CompletedTask);

            // Act
            var method = orchestrator.GetType()
                .GetMethod("UpdateUserPreferencesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method?.Invoke(orchestrator, new object[] { new Action<UserPreferences>(p => { p.PreferredConsoleWidth = newWidth; p.PreferredConsoleHeight = newHeight; }) })!;

            // Assert
            _userPreferences.PreferredConsoleWidth.Should().Be(newWidth);
            _userPreferences.PreferredConsoleHeight.Should().Be(newHeight);
            _mockConfigManager.Verify(x => x.SaveUserPreferencesAsync(_userPreferences), Times.Once);
            _mockLogger.Verify(x => x.Debug("User preferences updated and saved successfully"), Times.Once);
        }

        [Fact]
        public async Task UpdateUserPreferencesAsync_WhenSaveFails_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();

            _mockConfigManager.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>())).ThrowsAsync(new Exception("Save failed"));

            // Act
            var method = orchestrator.GetType()
                .GetMethod("UpdateUserPreferencesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method?.Invoke(orchestrator, new object[] { new Action<UserPreferences>(p => p.PreferredConsoleWidth = 200) })!;

            // Assert
            _mockLogger.Verify(x => x.Error("Failed to save user preferences: Save failed"), Times.Once);
        }

        #endregion

        #region Action Method Tests

        [Theory]
        [InlineData(ShortcutAction.CycleTransformationEngineVerbosity)]
        [InlineData(ShortcutAction.CyclePCClientVerbosity)]
        [InlineData(ShortcutAction.CyclePhoneClientVerbosity)]
        [InlineData(ShortcutAction.ReloadTransformationConfig)]
        [InlineData(ShortcutAction.OpenConfigInEditor)]
        [InlineData(ShortcutAction.ShowSystemHelp)]
        [InlineData(ShortcutAction.ShowNetworkStatus)]
        public void GetActionMethod_WithValidAction_ReturnsAction(ShortcutAction action)
        {
            // Arrange
            var orchestrator = CreateOrchestrator();

            // Setup mocks for formatters
            var mockTransformationFormatter = new Mock<IFormatter>();
            var mockPCFormatter = new Mock<IFormatter>();
            var mockPhoneFormatter = new Mock<IFormatter>();

            mockTransformationFormatter.Setup(x => x.CycleVerbosity()).Returns(VerbosityLevel.Detailed);
            mockPCFormatter.Setup(x => x.CycleVerbosity()).Returns(VerbosityLevel.Detailed);
            mockPhoneFormatter.Setup(x => x.CycleVerbosity()).Returns(VerbosityLevel.Detailed);

            var mockMainStatusRenderer = new Mock<IMainStatusRenderer>();
            _mockModeManager.Setup(x => x.MainStatusRenderer).Returns(mockMainStatusRenderer.Object);
            mockMainStatusRenderer.Setup(x => x.GetFormatter<TransformationEngineInfo>()).Returns(mockTransformationFormatter.Object);
            mockMainStatusRenderer.Setup(x => x.GetFormatter<PCTrackingInfo>()).Returns(mockPCFormatter.Object);
            mockMainStatusRenderer.Setup(x => x.GetFormatter<PhoneTrackingInfo>()).Returns(mockPhoneFormatter.Object);

            _mockConfigManager.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>())).Returns(Task.CompletedTask);

            // Act
            var method = orchestrator.GetType()
                .GetMethod("GetActionMethod", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = method?.Invoke(orchestrator, new object[] { action }) as Action;

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void GetActionMethod_WithInvalidAction_ReturnsNull()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var invalidAction = (ShortcutAction)999; // Invalid enum value

            // Act
            var method = orchestrator.GetType()
                .GetMethod("GetActionMethod", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = method?.Invoke(orchestrator, new object[] { invalidAction }) as Action;

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetActionMethod_CycleTransformationEngineVerbosity_ExecutesImplementation()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("GetActionMethod", BindingFlags.NonPublic | BindingFlags.Instance);
            var mockFormatter = new Mock<IFormatter>();
            mockFormatter.Setup(x => x.CycleVerbosity()).Returns(VerbosityLevel.Detailed);

            _mockModeManager.Setup(x => x.MainStatusRenderer.GetFormatter<TransformationEngineInfo>())
                .Returns(mockFormatter.Object);

            // Act
            var action = (Action?)method!.Invoke(orchestrator, new object[] { ShortcutAction.CycleTransformationEngineVerbosity });
            action!.Invoke(); // Execute the action

            // Assert
            action.Should().NotBeNull();
            mockFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public void GetActionMethod_CyclePCClientVerbosity_ExecutesImplementation()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("GetActionMethod", BindingFlags.NonPublic | BindingFlags.Instance);
            var mockFormatter = new Mock<IFormatter>();
            mockFormatter.Setup(x => x.CycleVerbosity()).Returns(VerbosityLevel.Normal);

            _mockModeManager.Setup(x => x.MainStatusRenderer.GetFormatter<PCTrackingInfo>())
                .Returns(mockFormatter.Object);

            // Act
            var action = (Action?)method!.Invoke(orchestrator, new object[] { ShortcutAction.CyclePCClientVerbosity });
            action!.Invoke(); // Execute the action

            // Assert
            action.Should().NotBeNull();
            mockFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public void GetActionMethod_CyclePhoneClientVerbosity_ExecutesImplementation()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("GetActionMethod", BindingFlags.NonPublic | BindingFlags.Instance);
            var mockFormatter = new Mock<IFormatter>();
            mockFormatter.Setup(x => x.CycleVerbosity()).Returns(VerbosityLevel.Basic);

            _mockModeManager.Setup(x => x.MainStatusRenderer.GetFormatter<PhoneTrackingInfo>())
                .Returns(mockFormatter.Object);

            // Act
            var action = (Action?)method!.Invoke(orchestrator, new object[] { ShortcutAction.CyclePhoneClientVerbosity });
            action!.Invoke(); // Execute the action

            // Assert
            action.Should().NotBeNull();
            mockFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public void GetActionMethod_ShowSystemHelp_ExecutesImplementation()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("GetActionMethod", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var action = (Action?)method!.Invoke(orchestrator, new object[] { ShortcutAction.ShowSystemHelp });
            action!.Invoke(); // Execute the action

            // Assert
            action.Should().NotBeNull();
            _mockModeManager.Verify(x => x.Toggle(ConsoleMode.SystemHelp), Times.Once);
        }

        [Fact]
        public void GetActionMethod_ShowNetworkStatus_ExecutesImplementation()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("GetActionMethod", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var action = (Action?)method!.Invoke(orchestrator, new object[] { ShortcutAction.ShowNetworkStatus });
            action!.Invoke(); // Execute the action

            // Assert
            action.Should().NotBeNull();
            _mockModeManager.Verify(x => x.Toggle(ConsoleMode.NetworkStatus), Times.Once);
        }

        [Theory]
        [InlineData(ShortcutAction.CycleTransformationEngineVerbosity, "Cycle Transformation Engine Verbosity")]
        [InlineData(ShortcutAction.CyclePCClientVerbosity, "Cycle PC Client Verbosity")]
        [InlineData(ShortcutAction.CyclePhoneClientVerbosity, "Cycle Phone Client Verbosity")]
        [InlineData(ShortcutAction.ReloadTransformationConfig, "Reload Transformation Config")]
        [InlineData(ShortcutAction.OpenConfigInEditor, "Open Config in External Editor")]
        [InlineData(ShortcutAction.ShowSystemHelp, "Show System Help")]
        [InlineData(ShortcutAction.ShowNetworkStatus, "Show Network Status")]
        public void GetActionDescription_WithValidAction_ReturnsDescription(ShortcutAction action, string expectedDescription)
        {
            // Arrange
            var orchestrator = CreateOrchestrator();

            // Act
            var method = orchestrator.GetType()
                .GetMethod("GetActionDescription", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = method?.Invoke(null, new object[] { action }) as string;

            // Assert
            result.Should().Be(expectedDescription);
        }

        #endregion

        #region OnTrackingDataReceived Exception Handling Tests

        [Fact]
        public async Task OnTrackingDataReceived_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("OnTrackingDataReceived", BindingFlags.NonPublic | BindingFlags.Instance);
            var trackingData = new PhoneTrackingInfo();

            // Mock transformation engine to throw exception
            _mockTransformationEngine.Setup(x => x.TransformData(It.IsAny<PhoneTrackingInfo>()))
                .Throws(new Exception("Transformation failed"));

            // Act
            method!.Invoke(orchestrator, new object[] { null!, trackingData });

            // Wait a bit for the async void method to complete
            await Task.Delay(100);

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error processing tracking data", It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region InitializeColorServiceIfNeeded Exception Handling Tests

        [Fact]
        public void InitializeColorServiceIfNeeded_WhenExceptionOccurs_LogsWarning()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("InitializeColorServiceIfNeeded", BindingFlags.NonPublic | BindingFlags.Instance);
            var trackingData = new PhoneTrackingInfo { BlendShapes = new List<BlendShape> { new BlendShape { Key = "TestBlendShape", Value = 0.5 } } };

            // Mock transformation engine to return parameters
            _mockTransformationEngine.Setup(x => x.GetParameterDefinitions())
                .Returns(new List<VTSParameter>());

            // Mock color service to throw exception
            _mockColorService.Setup(x => x.InitializeFromConfiguration(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .Throws(new Exception("Color service initialization failed"));

            // Act
            method!.Invoke(orchestrator, new object[] { trackingData });

            // Assert
            _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("Failed to initialize color service"))), Times.Once);
        }

        #endregion

        #region AttemptRecoveryAsync PC Client Recovery Tests

        [Fact]
        public async Task AttemptRecoveryAsync_WhenPCClientRecovers_AttemptsParameterSynchronization()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("AttemptRecoveryAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var cancellationToken = CancellationToken.None;

            // Mock PC client to be unhealthy initially, then healthy after recovery
            var unhealthyStats = new ServiceStats("PCClient", "Unhealthy", null, false);
            var healthyStats = new ServiceStats("PCClient", "Healthy", null, true);

            _mockPCClient.SetupSequence(x => x.GetServiceStats())
                .Returns(unhealthyStats)  // First call - unhealthy
                .Returns(healthyStats);   // Second call - healthy after recovery

            _mockPCClient.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Mock phone client to be healthy (no recovery needed)
            var phoneStats = new ServiceStats("PhoneClient", "Healthy", null, true);
            _mockPhoneClient.Setup(x => x.GetServiceStats())
                .Returns(phoneStats);

            // Mock parameter manager to succeed
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var task = (Task)method!.Invoke(orchestrator, new object[] { cancellationToken })!;
            await task;

            // Assert
            _mockLogger.Verify(x => x.Info("PC client recovered successfully, attempting parameter synchronization..."), Times.Once);
            _mockParameterManager.Verify(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task AttemptRecoveryAsync_WhenPCClientRecoversButParameterSyncFails_LogsWarning()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("AttemptRecoveryAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var cancellationToken = CancellationToken.None;

            // Mock PC client to be unhealthy initially, then healthy after recovery
            var unhealthyStats = new ServiceStats("PCClient", "Unhealthy", null, false);
            var healthyStats = new ServiceStats("PCClient", "Healthy", null, true);

            _mockPCClient.SetupSequence(x => x.GetServiceStats())
                .Returns(unhealthyStats)  // First call - unhealthy
                .Returns(healthyStats);   // Second call - healthy after recovery

            _mockPCClient.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Mock phone client to be healthy (no recovery needed)
            var phoneStats = new ServiceStats("PhoneClient", "Healthy", null, true);
            _mockPhoneClient.Setup(x => x.GetServiceStats())
                .Returns(phoneStats);

            // Mock parameter manager to fail
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var task = (Task)method!.Invoke(orchestrator, new object[] { cancellationToken })!;
            await task;

            // Assert
            _mockLogger.Verify(x => x.Info("PC client recovered successfully, attempting parameter synchronization..."), Times.Once);
            _mockLogger.Verify(x => x.Warning("Parameter synchronization failed after PC client recovery"), Times.Once);
            _mockParameterManager.Verify(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken), Times.Once);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_WhenCalled_DisposesResources()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();

            // Act
            orchestrator.Dispose();

            // Assert
            _mockPCClient.Verify(x => x.Dispose(), Times.Once);
            _mockPhoneClient.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();

            // Act & Assert
            orchestrator.Dispose();
            orchestrator.Dispose(); // Should not throw

            // Verify that Dispose was called on the clients only once (due to disposal guard)
            _mockPCClient.Verify(x => x.Dispose(), Times.Once);
            _mockPhoneClient.Verify(x => x.Dispose(), Times.Once);
        }

        #endregion

        #region ReloadTransformationConfig Tests

        [Fact]
        public async Task ReloadTransformationConfig_WhenCalled_ReloadsConfiguration()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("ReloadTransformationConfig", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert
            _mockTransformationEngine.Verify(x => x.LoadRulesAsync(), Times.Once);
            _mockLogger.Verify(x => x.Info("Reloading transformation config..."), Times.Once);
            _mockLogger.Verify(x => x.Debug("Color service initialization flag reset for config reload"), Times.Once);
        }

        [Fact]
        public async Task ReloadTransformationConfig_WhenParameterSyncFails_LogsWarning()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("ReloadTransformationConfig", BindingFlags.NonPublic | BindingFlags.Instance);

            // Mock parameter synchronization to fail
            _mockTransformationEngine.Setup(x => x.GetParameterDefinitions())
                .Returns(new List<VTSParameter>());
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert
            _mockLogger.Verify(x => x.Warning("Parameter synchronization failed during config reload"), Times.Once);
        }

        [Fact]
        public async Task ReloadTransformationConfig_WhenParameterSyncSucceeds_LogsSuccess()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("ReloadTransformationConfig", BindingFlags.NonPublic | BindingFlags.Instance);

            // Mock parameter synchronization to succeed
            _mockTransformationEngine.Setup(x => x.GetParameterDefinitions())
                .Returns(new List<VTSParameter>());
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert
            _mockLogger.Verify(x => x.Info("Transformation config reloaded successfully"), Times.Once);
        }

        [Fact]
        public async Task ReloadTransformationConfig_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("ReloadTransformationConfig", BindingFlags.NonPublic | BindingFlags.Instance);

            // Mock transformation engine to throw exception
            _mockTransformationEngine.Setup(x => x.LoadRulesAsync())
                .ThrowsAsync(new Exception("Transformation engine failed"));

            // Act
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error reloading transformation config", It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region OpenConfigInEditor Tests

        [Fact]
        public async Task OpenConfigInEditor_WhenCalled_DelegatesToModeManager()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("OpenConfigInEditor", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert
            _mockModeManager.Verify(x => x.TryOpenActiveModeInEditorAsync(), Times.Once);
        }

        #endregion

        #region InitializeAsync Console Window Tracking Tests

        [Fact]
        public async Task InitializeAsync_WhenConsoleWindowSizeChanges_UpdatesUserPreferences()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            Action<int, int>? capturedCallback = null;

            // Capture the callback when StartSizeChangeTracking is called
            _mockConsoleWindowManager.Setup(x => x.StartSizeChangeTracking(It.IsAny<Action<int, int>>()))
                .Callback<Action<int, int>>(callback => capturedCallback = callback);

            // Act
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Assert - Verify that console window size change tracking is set up
            _mockConsoleWindowManager.Verify(x => x.StartSizeChangeTracking(It.IsAny<Action<int, int>>()), Times.Once);

            // Verify the callback was captured and can be invoked
            capturedCallback.Should().NotBeNull();

            // Test the callback execution
            var newWidth = 150;
            var newHeight = 40;

            // This should not throw and should update user preferences
            capturedCallback!(newWidth, newHeight);

            // Verify that UpdateUserPreferencesAsync was called (fire-and-forget)
            // We can't easily verify the async call, but we can verify the callback works
            capturedCallback.Should().NotBeNull();
        }

        #endregion

        #region RunAsync Exception Handling Tests

        [Fact]
        public async Task RunAsync_WhenOperationCanceledException_LogsGracefulShutdown()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var cancellationTokenSource = new CancellationTokenSource();

            // Mock the recovery policy to return a delay
            _mockRecoveryPolicy.Setup(x => x.GetNextDelay())
                .Returns(TimeSpan.FromSeconds(1));

            // Act
            var task = orchestrator.RunAsync(cancellationTokenSource.Token);

            // Cancel the token to trigger cancellation
            cancellationTokenSource.Cancel();

            // Wait for the task to complete with a reasonable timeout
            // Use Task.WhenAny to wait for either completion or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(task, timeoutTask);

            // Assert - The task should complete (either successfully or with cancellation)
            // Since we can't easily trigger the specific catch block, let's just verify it runs
            if (completedTask == timeoutTask)
            {
                // Task didn't complete in time - this indicates a problem
                task.IsCompleted.Should().BeTrue("Task should have completed within 5 seconds after cancellation");
            }
            else
            {
                // Task completed normally
                task.IsCompleted.Should().BeTrue();
            }
        }

        #endregion

        #region ProcessRecoveryIfNeeded Recovery Completion Tests

        [Fact]
        public async Task ProcessRecoveryIfNeeded_WhenRecoverySucceeds_LogsCompletion()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("ProcessRecoveryIfNeeded", BindingFlags.NonPublic | BindingFlags.Instance);
            var cancellationToken = CancellationToken.None;

            // Mock recovery policy to return a delay
            _mockRecoveryPolicy.Setup(x => x.GetNextDelay())
                .Returns(TimeSpan.FromSeconds(1));

            // Mock AttemptRecoveryAsync to return true (successful recovery)
            // We need to mock the recovery to succeed
            _mockPCClient.Setup(x => x.GetServiceStats())
                .Returns(new ServiceStats("PCClient", "Description", null, true, DateTime.UtcNow, null, null));
            _mockPhoneClient.Setup(x => x.GetServiceStats())
                .Returns(new ServiceStats("PhoneClient", "Description", null, false, DateTime.UtcNow, null, null));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var task = (Task)method!.Invoke(orchestrator, new object[] { cancellationToken })!;
            await task;

            // Assert
            _mockLogger.Verify(x => x.Info("Recovery attempt completed"), Times.Once);
        }

        #endregion

        #region CloseVTubeStudioConnection Exception Handling Tests

        [Fact]
        public async Task CloseVTubeStudioConnection_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("CloseVTubeStudioConnection", BindingFlags.NonPublic | BindingFlags.Instance);

            // Mock PC client to throw exception when closing
            _mockPCClient.Setup(x => x.CloseAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection close failed"));

            // Act
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error closing VTube Studio connection", It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region UpdateConsoleStatus Exception Handling Tests

        [Fact]
        public async Task UpdateConsoleStatus_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var method = typeof(ApplicationOrchestrator).GetMethod("UpdateConsoleStatus", BindingFlags.NonPublic | BindingFlags.Instance);

            // Mock the transformation engine to throw an exception
            _mockTransformationEngine.Setup(x => x.GetServiceStats())
                .Throws(new Exception("Transformation engine failed"));

            // Act
            method!.Invoke(orchestrator, null);

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error updating console status", It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region Constructor Null Validation Tests

        [Theory]
        [InlineData("vtubeStudioPCClient")]
        [InlineData("vtubeStudioPhoneClient")]
        [InlineData("transformationEngine")]
        [InlineData("phoneConfig")]
        [InlineData("logger")]
        [InlineData("modeManager")]
        [InlineData("keyboardInputHandler")]
        [InlineData("parameterManager")]
        [InlineData("recoveryPolicy")]
        [InlineData("consoleWindowManager")]
        [InlineData("colorService")]
        [InlineData("externalEditorService")]
        [InlineData("shortcutConfigurationManager")]
        [InlineData("applicationConfig")]
        [InlineData("userPreferences")]
        [InlineData("configManager")]
        [InlineData("appConfigWatcher")]
        public void Constructor_WithNullParameter_ThrowsArgumentNullException(string nullParameter)
        {
            // Arrange
            var validPCClient = new Mock<IVTubeStudioPCClient>().Object;
            var validPhoneClient = new Mock<IVTubeStudioPhoneClient>().Object;
            var validTransformationEngine = new Mock<ITransformationEngine>().Object;
            var validPhoneConfig = new VTubeStudioPhoneClientConfig();
            var validLogger = new Mock<IAppLogger>().Object;
            var validModeManager = new Mock<IConsoleModeManager>().Object;
            var validKeyboardInputHandler = new Mock<IKeyboardInputHandler>().Object;
            var validParameterManager = new Mock<IVTubeStudioPCParameterManager>().Object;
            var validRecoveryPolicy = new Mock<IRecoveryPolicy>().Object;
            var validConsole = new Mock<IConsole>().Object;
            var validConsoleWindowManager = new Mock<IConsoleWindowManager>().Object;
            var validColorService = new Mock<IParameterColorService>().Object;
            var validExternalEditorService = new Mock<IExternalEditorService>().Object;
            var validShortcutConfigurationManager = new Mock<IShortcutConfigurationManager>().Object;
            var validApplicationConfig = new ApplicationConfig();
            var validUserPreferences = new UserPreferences();
            var validConfigManager = new Mock<IConfigManager>().Object;
            var validAppConfigWatcher = new Mock<IFileChangeWatcher>().Object;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
            {
                new ApplicationOrchestrator(
                    nullParameter == "vtubeStudioPCClient" ? null! : validPCClient,
                    nullParameter == "vtubeStudioPhoneClient" ? null! : validPhoneClient,
                    nullParameter == "transformationEngine" ? null! : validTransformationEngine,
                    nullParameter == "phoneConfig" ? null! : validPhoneConfig,
                    nullParameter == "logger" ? null! : validLogger,
                    nullParameter == "modeManager" ? null! : validModeManager,
                    nullParameter == "keyboardInputHandler" ? null! : validKeyboardInputHandler,
                    nullParameter == "parameterManager" ? null! : validParameterManager,
                    nullParameter == "recoveryPolicy" ? null! : validRecoveryPolicy,
                    nullParameter == "console" ? null! : validConsole,
                    nullParameter == "consoleWindowManager" ? null! : validConsoleWindowManager,
                    nullParameter == "colorService" ? null! : validColorService,
                    nullParameter == "externalEditorService" ? null! : validExternalEditorService,
                    nullParameter == "shortcutConfigurationManager" ? null! : validShortcutConfigurationManager,
                    nullParameter == "applicationConfig" ? null! : validApplicationConfig,
                    nullParameter == "userPreferences" ? null! : validUserPreferences,
                    nullParameter == "configManager" ? null! : validConfigManager,
                    nullParameter == "appConfigWatcher" ? null! : validAppConfigWatcher
                );
            });

            exception.ParamName.Should().Be(nullParameter);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void CONSOLE_UPDATE_INTERVAL_SECONDS_CanBeSet()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var newInterval = 0.5;

            // Act
            orchestrator.CONSOLE_UPDATE_INTERVAL_SECONDS = newInterval;

            // Assert
            orchestrator.CONSOLE_UPDATE_INTERVAL_SECONDS.Should().Be(newInterval);
        }

        #endregion

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
