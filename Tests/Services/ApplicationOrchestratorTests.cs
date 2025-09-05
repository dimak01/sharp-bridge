using System;
using System.Collections.Generic;
using System.Linq;
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
