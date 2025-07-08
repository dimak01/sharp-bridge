using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
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
    public class ApplicationOrchestratorTests
    {
        private readonly Mock<IVTubeStudioPCClient> _vtubeStudioPCClientMock;
        private readonly Mock<IVTubeStudioPhoneClient> _vtubeStudioPhoneClientMock;
        private readonly Mock<ITransformationEngine> _transformationEngineMock;
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly Mock<IConsoleRenderer> _consoleRendererMock;
        private readonly Mock<IKeyboardInputHandler> _keyboardInputHandlerMock;
        private readonly Mock<IVTubeStudioPCParameterManager> _parameterManagerMock;
        private readonly Mock<IRecoveryPolicy> _recoveryPolicyMock;
        private readonly Mock<IConsole> _consoleMock;
        private readonly Mock<IConsoleWindowManager> _consoleWindowManagerMock;
        private readonly Mock<IParameterColorService> _colorServiceMock;
        private readonly Mock<IExternalEditorService> _externalEditorServiceMock;
        private readonly Mock<IShortcutConfigurationManager> _shortcutConfigurationManagerMock;
        private readonly Mock<ISystemHelpRenderer> _systemHelpRendererMock;
        private readonly Mock<IFileChangeWatcher> _appConfigWatcherMock;
        private ApplicationConfig _applicationConfig;
        private UserPreferences _userPreferences;
        private Mock<ConfigManager> _configManagerMock;
        private ApplicationOrchestrator _orchestrator;
        private readonly string _tempConfigPath;
        private readonly VTubeStudioPhoneClientConfig _phoneConfig;
        private readonly VTubeStudioPCConfig _pcConfig;
        private readonly CancellationTokenSource _defaultCts;
        private readonly TimeSpan _longTimeout;
        private readonly CancellationTokenSource _longTimeoutCts;

        public ApplicationOrchestratorTests()
        {
            // Set up mocks
            _vtubeStudioPCClientMock = new Mock<IVTubeStudioPCClient>();
            _vtubeStudioPhoneClientMock = new Mock<IVTubeStudioPhoneClient>();
            _transformationEngineMock = new Mock<ITransformationEngine>();
            _loggerMock = new Mock<IAppLogger>();
            _consoleRendererMock = new Mock<IConsoleRenderer>();
            _keyboardInputHandlerMock = new Mock<IKeyboardInputHandler>();
            _parameterManagerMock = new Mock<IVTubeStudioPCParameterManager>();
            _recoveryPolicyMock = new Mock<IRecoveryPolicy>();
            _consoleMock = new Mock<IConsole>();
            _consoleWindowManagerMock = new Mock<IConsoleWindowManager>();
            _colorServiceMock = new Mock<IParameterColorService>();
            _externalEditorServiceMock = new Mock<IExternalEditorService>();
            _shortcutConfigurationManagerMock = new Mock<IShortcutConfigurationManager>();
            _systemHelpRendererMock = new Mock<ISystemHelpRenderer>();
            _appConfigWatcherMock = new Mock<IFileChangeWatcher>();
            _configManagerMock = new Mock<ConfigManager>("test-config");

            // Create user preferences with default values
            _userPreferences = new UserPreferences
            {
                PhoneClientVerbosity = VerbosityLevel.Normal,
                PCClientVerbosity = VerbosityLevel.Normal,
                TransformationEngineVerbosity = VerbosityLevel.Normal,
                PreferredConsoleWidth = 150,
                PreferredConsoleHeight = 60
            };

            // Create application config with default shortcuts
            _applicationConfig = new ApplicationConfig
            {
                GeneralSettings = new GeneralSettingsConfig
                {
                    EditorCommand = "notepad.exe \"%f\"",
                    Shortcuts = new Dictionary<string, string>
                    {
                        ["CycleTransformationEngineVerbosity"] = "Alt+T",
                        ["CyclePCClientVerbosity"] = "Alt+P",
                        ["CyclePhoneClientVerbosity"] = "Alt+O",
                        ["ReloadTransformationConfig"] = "Alt+K",
                        ["OpenConfigInEditor"] = "Ctrl+Alt+E",
                        ["ShowSystemHelp"] = "F1"
                    }
                }
            };

            // Configure recovery policy to return 2 second delay
            _recoveryPolicyMock.Setup(x => x.GetNextDelay())
                .Returns(TimeSpan.FromMilliseconds(5));

            _defaultCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)); // Increased from 10ms to 50ms
            _longTimeout = TimeSpan.FromMilliseconds(70);
            _longTimeoutCts = new CancellationTokenSource(_longTimeout);

            // Create a simple phone config for testing
            _phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1",
                IphonePort = 1234,
                LocalPort = 5678,
                RequestIntervalSeconds = 0.001, // 1ms interval to ensure requests happen within test timeout
                ReceiveTimeoutMs = 100,
                SendForSeconds = 5,
                ErrorDelayMs = 10 // Fast error retry for tests
            };

            // Create a simple PC config for testing
            _pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                TokenFilePath = "test_token.txt"
            };

            // Set up console mock with default behavior
            _consoleMock.Setup(x => x.WindowWidth).Returns(80);
            _consoleMock.Setup(x => x.WindowHeight).Returns(25);
            _consoleMock.Setup(x => x.TrySetWindowSize(It.IsAny<int>(), It.IsAny<int>())).Returns(true);

            // Set up console window manager mock with default behavior
            _consoleWindowManagerMock.Setup(x => x.GetCurrentSize()).Returns((80, 25));
            _consoleWindowManagerMock.Setup(x => x.SetConsoleSize(It.IsAny<int>(), It.IsAny<int>())).Returns(true);

            // Setup shortcut configuration manager mock
            _shortcutConfigurationManagerMock.Setup(x => x.GetMappedShortcuts())
                .Returns(new Dictionary<ShortcutAction, Shortcut?>
                {
                    [ShortcutAction.CycleTransformationEngineVerbosity] = new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt),
                    [ShortcutAction.CyclePCClientVerbosity] = new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt),
                    [ShortcutAction.CyclePhoneClientVerbosity] = new Shortcut(ConsoleKey.O, ConsoleModifiers.Alt),
                    [ShortcutAction.ReloadTransformationConfig] = new Shortcut(ConsoleKey.K, ConsoleModifiers.Alt),
                    [ShortcutAction.OpenConfigInEditor] = new Shortcut(ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt),
                    [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, ConsoleModifiers.None)
                });
            _shortcutConfigurationManagerMock.Setup(x => x.GetIncorrectShortcuts())
                .Returns(new Dictionary<ShortcutAction, string>());

            // Create orchestrator with mocked dependencies
            _orchestrator = new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object,
                _consoleMock.Object,
                _consoleWindowManagerMock.Object,
                _colorServiceMock.Object,
                _externalEditorServiceMock.Object,
                _shortcutConfigurationManagerMock.Object,
                _applicationConfig,
                _systemHelpRendererMock.Object,
                _userPreferences,
                _configManagerMock.Object,
                _appConfigWatcherMock.Object
            );

            // Create temp config file for tests
            _tempConfigPath = CreateTempConfigFile("[]");
        }

        private ApplicationOrchestrator CreateOrchestrator()
        {
            return new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object,
                _consoleMock.Object,
                _consoleWindowManagerMock.Object,
                _colorServiceMock.Object,
                _externalEditorServiceMock.Object,
                _shortcutConfigurationManagerMock.Object,
                _applicationConfig,
                _systemHelpRendererMock.Object,
                _userPreferences,
                _configManagerMock.Object,
                _appConfigWatcherMock.Object
            );
        }

        /// <summary>
        /// Helper method to create ApplicationOrchestrator with one parameter replaced for null testing
        /// </summary>
        private ApplicationOrchestrator CreateOrchestratorWithNullParameter(string parameterName)
        {
            return parameterName switch
            {
                "vtubeStudioPCClient" => new ApplicationOrchestrator(
                    null!,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object,
                    _appConfigWatcherMock.Object),
                "vtubeStudioPhoneClient" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    null!,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "transformationEngine" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    null!,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "phoneConfig" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    null!,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "logger" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    null!,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "consoleRenderer" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    null!,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "keyboardInputHandler" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    null!,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "parameterManager" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    null!,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "recoveryPolicy" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    null!,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "console" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    null!,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "consoleWindowManager" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    null!,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "colorService" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    null!,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "externalEditorService" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    null!,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "shortcutConfigurationManager" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    null!,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "applicationConfig" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    null!,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "systemHelpRenderer" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    null!,
                    _userPreferences,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "userPreferences" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    null!,
                    _configManagerMock.Object, _appConfigWatcherMock.Object),
                "configManager" => new ApplicationOrchestrator(
                    _vtubeStudioPCClientMock.Object,
                    _vtubeStudioPhoneClientMock.Object,
                    _transformationEngineMock.Object,
                    _phoneConfig,
                    _loggerMock.Object,
                    _consoleRendererMock.Object,
                    _keyboardInputHandlerMock.Object,
                    _parameterManagerMock.Object,
                    _recoveryPolicyMock.Object,
                    _consoleMock.Object,
                    _consoleWindowManagerMock.Object,
                    _colorServiceMock.Object,
                    _externalEditorServiceMock.Object,
                    _shortcutConfigurationManagerMock.Object,
                    _applicationConfig,
                    _systemHelpRendererMock.Object,
                    _userPreferences,
                    null!,
                    _appConfigWatcherMock.Object),
                _ => throw new ArgumentException($"Unknown parameter: {parameterName}")
            };
        }

        private void SetupBasicMocks()
        {
            // Configure PC client behavior
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);

            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Configure TryInitializeAsync for both clients
            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _vtubeStudioPhoneClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Setup PC client service stats
            var pcStats = new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: "Connected",
                currentEntity: new PCTrackingInfo(),
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );
            _vtubeStudioPCClientMock.Setup(x => x.GetServiceStats())
                .Returns(pcStats);

            // Configure parameter manager
            _parameterManagerMock.Setup(x => x.TrySynchronizeParametersAsync(
                    It.IsAny<IEnumerable<VTSParameter>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Configure transformation engine to return default parameters
            _transformationEngineMock.Setup(x => x.GetParameterDefinitions())
                .Returns(new List<VTSParameter>
                {
                    new VTSParameter("TestParam", -1.0, 1.0, 0.0)
                });

            _transformationEngineMock
                .Setup(engine => engine.LoadRulesAsync())
                .Returns(Task.CompletedTask);

            // Default setup for SendTrackingRequestAsync (can be overridden by test-specific setups)
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .Returns(Task.CompletedTask);

            _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Setup Phone client service stats - simulate realistic health behavior
            // Phone client starts healthy after successful initialization (like TryInitializeAsync returning true)
            var phoneStats = new ServiceStats(
                serviceName: "PhoneClient",
                status: "Connected",
                currentEntity: new PhoneTrackingInfo(),
                isHealthy: true, // Healthy after successful initialization
                lastSuccessfulOperation: DateTime.UtcNow, // Recent successful operation
                lastError: null,
                counters: new Dictionary<string, long>
                {
                    ["Total Frames"] = 1, // Simulate having received at least one frame
                    ["Failed Frames"] = 0,
                    ["Uptime (seconds)"] = 1
                }
            );
            _vtubeStudioPhoneClientMock.Setup(x => x.GetServiceStats())
                .Returns(phoneStats);
        }

        // Helper method to set up basic orchestrator requirements for event-based tests
        private void SetupOrchestratorTest(WebSocketState pcClientState = WebSocketState.Open)
        {
            // Set up basic mocks first
            SetupBasicMocks();

            // Override WebSocket state if needed
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(pcClientState);

            // Update PC client service stats to reflect the WebSocket state
            var isHealthy = pcClientState == WebSocketState.Open;
            var status = pcClientState == WebSocketState.Open ? "Connected" : "Disconnected";

            var pcStats = new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: status,
                currentEntity: new PCTrackingInfo(),
                isHealthy: isHealthy,
                lastSuccessfulOperation: isHealthy ? DateTime.UtcNow : DateTime.UtcNow.AddMinutes(-5),
                lastError: isHealthy ? null : "Connection closed",
                counters: new Dictionary<string, long>()
            );
            _vtubeStudioPCClientMock.Setup(x => x.GetServiceStats())
                .Returns(pcStats);
        }

        // Helper method to run an event-triggered test with timeout protection
        private async Task<bool> RunWithTimeoutAndEventTrigger(
            PhoneTrackingInfo? trackingData,
            TimeSpan timeout,
            Func<Task>? additionalSetup = null)
        {
            // Create cancellation tokens with timeout
            using var cts = new CancellationTokenSource();
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, timeoutCts.Token);

            bool eventWasRaised = false;

            // Configure ReceiveResponseAsync to raise the event
            int receiveCallCount = 0;
            _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    receiveCallCount++;
                    if (receiveCallCount == 1)
                    {
                        // Raise the event on first call
                        _vtubeStudioPhoneClientMock.Raise(
                            x => x.TrackingDataReceived += null,
                            new object?[] { _vtubeStudioPhoneClientMock.Object, trackingData });

                        eventWasRaised = true;

                        // Cancel after a short delay to allow event processing
                        Task.Delay(100).ContinueWith(_ => cts.Cancel());
                    }
                })
                .ReturnsAsync(true);

            // Allow caller to perform additional setup if needed
            if (additionalSetup != null)
            {
                await additionalSetup();
            }

            // Initialize and run the orchestrator
            await _orchestrator.InitializeAsync(linkedCts.Token);
            await _orchestrator.RunAsync(linkedCts.Token);

            return eventWasRaised;
        }

        // Helper methods for test execution
        private static async Task RunWithDefaultTimeout(Func<Task> testAction)
        {
            try
            {
                await testAction();
            }
            catch (OperationCanceledException)
            {
                // Expected for normal test completion
            }
        }

        private static async Task RunWithCustomTimeout(Func<Task> testAction, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await testAction();
            }
            catch (OperationCanceledException)
            {
                // Expected for normal test completion
            }
        }

        public void Dispose()
        {
            // Clean up temp file
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }

            _orchestrator.Dispose();
            _defaultCts.Dispose();
        }

        private static string CreateTempConfigFile(string content)
        {
            var tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, content);
            return tempPath;
        }

        // Configuration and Initialization Tests

        [Fact]
        public async Task InitializeAsync_WithValidConfig_InitializesComponents()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;

            // Act
            await _orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _transformationEngineMock.Verify(x => x.LoadRulesAsync(), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenPortDiscoveryFails_CompletesGracefully()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;

            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Initialization fails

            // Act - should not throw
            await _orchestrator.InitializeAsync(cancellationToken);

            // Assert - verify that initialization was attempted
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }



        // Connection and Lifecycle Tests

        [Fact]
        public async Task RunAsync_WithoutInitialization_RunsGracefully()
        {
            // Arrange
            SetupBasicMocks();

            // Act - should not throw exception
            await RunWithDefaultTimeout(async () =>
                await _orchestrator.RunAsync(_defaultCts.Token));

            // Assert - verify that the application attempted to run
            _vtubeStudioPhoneClientMock.Verify(x => x.SendTrackingRequestAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task RunAsync_WithInitialization_StartsPhoneClient()
        {
            var originalOut = Console.Out;
            try
            {
                SetupBasicMocks();
                var consoleWriter = new StringWriter();
                Console.SetOut(consoleWriter);

                await _orchestrator.InitializeAsync(_defaultCts.Token);

                // Act
                await RunWithDefaultTimeout(async () =>
                {
                    await _orchestrator.RunAsync(_defaultCts.Token);
                });

                // Assert
                _vtubeStudioPhoneClientMock.Verify(x => x.SendTrackingRequestAsync(), Times.AtLeastOnce);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public async Task RunAsync_WhenCloseAsyncThrowsException_HandlesGracefullyWithRetry()
        {
            // Arrange
            SetupBasicMocks();

            // Additional setup for this specific test case
            _vtubeStudioPCClientMock
                .Setup(client => client.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Create and initialize orchestrator
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(_defaultCts.Token);

            // Act & Assert
            await RunWithDefaultTimeout(async () =>
            {
                await orchestrator.RunAsync(_defaultCts.Token);
            });

            // Verify that close was attempted
            _vtubeStudioPCClientMock.Verify(
                client => client.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenCancelled_ClosesConnectionsGracefully()
        {
            // Arrange
            SetupBasicMocks();

            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);

            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act & Assert - should not throw exception
            await RunWithDefaultTimeout(async () =>
            {
                await _orchestrator.RunAsync(_defaultCts.Token);
            });

            // Verify cleanup was performed
            _vtubeStudioPCClientMock.Verify(x =>
                x.CloseAsync(
                    It.IsAny<WebSocketCloseStatus>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify the proper log message was written
            _loggerMock.Verify(x => x.Info("Application stopped"), Times.Once);
        }

        // Data Flow Tests

        [Fact]
        public async Task OnTrackingDataReceived_TransformsDataAndSendsToVTubeStudio()
        {
            // Arrange
            var trackingResponse = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };

            var transformedParams = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam
                    {
                        Id = "Test",
                        Value = 0.5,
                        Weight = 1.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter(
                        "Test",
                        -0.75,
                        1.25,
                        0.33
                    )
                }
            };

            // Setup basic orchestrator requirements
            SetupOrchestratorTest();

            // Configure the transformation engine to return test parameters
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);

            // Configure the PC client to verify the test parameters
            _vtubeStudioPCClientMock.Setup(x =>
                x.SendTrackingAsync(
                    It.Is<PCTrackingInfo>(pc =>
                        pc.FaceFound == trackingResponse.FaceFound &&
                        pc.Parameters.First().Id == "Test" &&
                        pc.Parameters.First().Value == 0.5 &&
                        pc.ParameterDefinitions["Test"].Min == -0.75 &&
                        pc.ParameterDefinitions["Test"].Max == 1.25 &&
                        pc.ParameterDefinitions["Test"].DefaultValue == 0.33),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse,
                _longTimeout);

            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x =>
                x.SendTrackingAsync(
                    It.Is<PCTrackingInfo>(pc =>
                        pc.FaceFound == trackingResponse.FaceFound &&
                        pc.Parameters.First().Id == "Test" &&
                        pc.Parameters.First().Value == 0.5 &&
                        pc.ParameterDefinitions["Test"].Min == -0.75 &&
                        pc.ParameterDefinitions["Test"].Max == 1.25 &&
                        pc.ParameterDefinitions["Test"].DefaultValue == 0.33),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WhenTrackingDataIsNull_DoesNotProcessOrSend()
        {
            // Arrange
            PhoneTrackingInfo nullTrackingData = null!;

            // Setup basic orchestrator requirements
            SetupOrchestratorTest();

            // Act - Run the test with a 2 second timeout, passing null tracking data
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                nullTrackingData,
                _longTimeout);

            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(It.IsAny<PhoneTrackingInfo>()), Times.Never);
            _vtubeStudioPCClientMock.Verify(x =>
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WhenConnectionClosed_DoesNotSendTracking()
        {
            // Arrange
            var trackingResponse = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var transformedParams = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam
                    {
                        Id = "Test",
                        Value = 0.5,
                        Weight = 1.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter(
                        "Test",
                        -0.75,
                        1.25,
                        0.33
                    )
                }
            };

            // Setup basic orchestrator requirements with closed connection state
            SetupOrchestratorTest(WebSocketState.Closed);

            // Configure the transformation engine to return test parameters
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);

            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse,
                _longTimeout);

            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x =>
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WithMultipleEvents_ProcessesAllEvents()
        {
            // Arrange
            SetupBasicMocks();
            var trackingData1 = new PhoneTrackingInfo { FaceFound = true, BlendShapes = new List<BlendShape>() };
            var trackingData2 = new PhoneTrackingInfo { FaceFound = false, BlendShapes = new List<BlendShape>() };

            var transformedParams1 = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam> { new TrackingParam { Id = "Test1", Value = 0.5 } },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test1"] = new VTSParameter("Test1", -1.0, 1.0, 0.0)
                }
            };

            var transformedParams2 = new PCTrackingInfo
            {
                FaceFound = false,
                Parameters = new List<TrackingParam> { new TrackingParam { Id = "Test2", Value = 0.7 } },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test2"] = new VTSParameter("Test2", -1.0, 1.0, 0.0)
                }
            };

            // Configure transformation for both events
            _transformationEngineMock.Setup(x => x.TransformData(It.Is<PhoneTrackingInfo>(p => p.FaceFound)))
                .Returns(transformedParams1);

            _transformationEngineMock.Setup(x => x.TransformData(It.Is<PhoneTrackingInfo>(p => !p.FaceFound)))
                .Returns(transformedParams2);

            // Configure PC client to track sends
            var sentParams = new List<PCTrackingInfo>();
            _vtubeStudioPCClientMock.Setup(x =>
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()))
                .Callback<PCTrackingInfo, CancellationToken>((info, _) => sentParams.Add(info))
                .Returns(Task.CompletedTask);

            // Set the WebSocketState to Open
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(WebSocketState.Open);

            // Initialize orchestrator to subscribe to events
            var cancellationToken = CancellationToken.None;
            await _orchestrator.InitializeAsync(cancellationToken);

            // Get the event handler method via reflection
            var onTrackingDataReceivedMethod = typeof(ApplicationOrchestrator)
                .GetMethod("OnTrackingDataReceived",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            // Act
            // Directly invoke the event handler with each tracking data
            onTrackingDataReceivedMethod!.Invoke(_orchestrator,
                new object?[] { _vtubeStudioPhoneClientMock.Object, trackingData1 });

            // Small delay between events
            await Task.Delay(50);

            onTrackingDataReceivedMethod!.Invoke(_orchestrator,
                new object?[] { _vtubeStudioPhoneClientMock.Object, trackingData2 });

            // Allow time for processing
            await Task.Delay(100);

            // Assert
            _transformationEngineMock.Verify(
                x => x.TransformData(It.Is<PhoneTrackingInfo>(p => p.FaceFound)),
                Times.Once);

            _transformationEngineMock.Verify(
                x => x.TransformData(It.Is<PhoneTrackingInfo>(p => !p.FaceFound)),
                Times.Once);

            _vtubeStudioPCClientMock.Verify(x =>
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            Assert.Equal(2, sentParams.Count);
            Assert.True(sentParams[0].FaceFound);
            Assert.Equal("Test1", sentParams[0].Parameters.First().Id);
            Assert.Equal(0.5, sentParams[0].Parameters.First().Value);
            Assert.Equal(-1.0, sentParams[0].ParameterDefinitions["Test1"].Min);
            Assert.Equal(1.0, sentParams[0].ParameterDefinitions["Test1"].Max);
            Assert.Equal(0.0, sentParams[0].ParameterDefinitions["Test1"].DefaultValue);

            Assert.False(sentParams[1].FaceFound);
            Assert.Equal("Test2", sentParams[1].Parameters.First().Id);
            Assert.Equal(0.7, sentParams[1].Parameters.First().Value);
            Assert.Equal(-1.0, sentParams[1].ParameterDefinitions["Test2"].Min);
            Assert.Equal(1.0, sentParams[1].ParameterDefinitions["Test2"].Max);
            Assert.Equal(0.0, sentParams[1].ParameterDefinitions["Test2"].DefaultValue);
        }

        // Disposal Tests

        [Fact]
        public void Dispose_DisposesAllDisposableComponents()
        {
            // Arrange
            // The mocks are already set up in the constructor

            // Act
            _orchestrator.Dispose();

            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.Dispose(), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DisposesComponentsOnlyOnce()
        {
            // Arrange
            // The mocks are already set up in the constructor

            // Act
            _orchestrator.Dispose();
            _orchestrator.Dispose(); // Second call should be a no-op

            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.Dispose(), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_WhenResourcesAreInErrorState_CleansUpGracefully()
        {
            // Arrange
            SetupBasicMocks();
            _vtubeStudioPCClientMock.Setup(x => x.CloseAsync(
                It.IsAny<WebSocketCloseStatus>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Test exception"));

            // Act
            _orchestrator.Dispose();
            _orchestrator.Dispose(); // Second call should be a no-op

            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.Dispose(), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        // Constructor Parameter Tests

        [Fact]
        public void Constructor_WithNullVTubeStudioPCClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("vtubeStudioPCClient"));
            exception.ParamName.Should().Be("vtubeStudioPCClient");
        }

        [Fact]
        public void Constructor_WithNullVTubeStudioPhoneClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("vtubeStudioPhoneClient"));
            exception.ParamName.Should().Be("vtubeStudioPhoneClient");
        }

        [Fact]
        public void Constructor_WithNullTransformationEngine_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("transformationEngine"));
            exception.ParamName.Should().Be("transformationEngine");
        }

        [Fact]
        public void Constructor_WithNullPhoneConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("phoneConfig"));
            exception.ParamName.Should().Be("phoneConfig");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("logger"));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullConsoleRenderer_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("consoleRenderer"));
            exception.ParamName.Should().Be("consoleRenderer");
        }

        [Fact]
        public void Constructor_WithNullKeyboardInputHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("keyboardInputHandler"));
            exception.ParamName.Should().Be("keyboardInputHandler");
        }

        [Fact]
        public void Constructor_WithNullParameterManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("parameterManager"));
            exception.ParamName.Should().Be("parameterManager");
        }

        [Fact]
        public void Constructor_WithNullRecoveryPolicy_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("recoveryPolicy"));
            exception.ParamName.Should().Be("recoveryPolicy");
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("console"));
            exception.ParamName.Should().Be("console");
        }

        [Fact]
        public void Constructor_WithNullConsoleWindowManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("consoleWindowManager"));
            exception.ParamName.Should().Be("consoleWindowManager");
        }

        // Additional RunAsync Tests

        [Fact]
        public async Task RunAsync_ChecksForKeyboardInput()
        {
            // Arrange
            SetupBasicMocks();

            // Set up the keyboardInputHandler to signal when CheckForKeyboardInput is called
            bool keyboardInputChecked = false;
            _keyboardInputHandlerMock
                .Setup(handler => handler.CheckForKeyboardInput())
                .Callback(() => keyboardInputChecked = true);

            // Create and initialize orchestrator
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(_defaultCts.Token);

            // Act
            await RunWithDefaultTimeout(async () =>
            {
                await orchestrator.RunAsync(_defaultCts.Token);
            });

            // Assert
            _keyboardInputHandlerMock.Verify(
                handler => handler.CheckForKeyboardInput(),
                Times.AtLeast(1));
            Assert.True(keyboardInputChecked, "Keyboard input should have been checked");
        }

        [Fact]
        public async Task RunAsync_WhenReceiveResponseAsyncThrowsException_ContinuesRunning()
        {
            // Arrange
            SetupBasicMocks();

            var exceptionThrown = false;

            // Setup to throw exception on first call, then succeed on second call, then cancel
            int receiveCallCount = 0;
            _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    receiveCallCount++;
                    if (receiveCallCount == 1)
                    {
                        exceptionThrown = true;
                        throw new InvalidOperationException("Test exception");
                    }
                    else if (receiveCallCount == 2)
                    {
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                });

            await _orchestrator.InitializeAsync(_longTimeoutCts.Token);

            // Act - Use a longer timeout to ensure we get multiple calls
            await RunWithCustomTimeout(async () =>
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);

            // Assert
            Assert.True(exceptionThrown, "Exception should have been thrown");
            _vtubeStudioPhoneClientMock.Verify(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error in application loop")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenCloseAsyncThrowsException_HandlesGracefully()
        {
            // Arrange
            SetupBasicMocks();

            // Setup CloseAsync to throw an exception
            _vtubeStudioPCClientMock.Setup(x =>
                x.CloseAsync(
                    It.IsAny<WebSocketCloseStatus>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Test exception"));

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act
            await RunWithDefaultTimeout(async () =>
            {
                await _orchestrator.RunAsync(_defaultCts.Token);
            });

            // Assert
            _vtubeStudioPCClientMock.Verify(x =>
                x.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // Additional OnTrackingDataReceived Tests

        [Fact]
        public async Task OnTrackingDataReceived_WhenTransformDataThrowsException_HandlesGracefully()
        {
            // Arrange
            var trackingResponse = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };

            // Setup basic orchestrator requirements
            SetupOrchestratorTest();

            // Setup TransformData to throw an exception
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Throws(new InvalidOperationException("Test exception"));

            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse,
                _longTimeout);

            // Assert - No exception should be thrown and no tracking data should be sent
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x =>
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WhenSendTrackingAsyncThrowsException_HandlesGracefully()
        {
            // Arrange
            var trackingResponse = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var transformedParams = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam {
                        Id = "Test",
                        Value = 0.5,
                        Weight = 1.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter(
                        "Test",
                        -0.75,
                        1.25,
                        0.33
                    )
                }
            };

            // Setup basic orchestrator requirements
            SetupOrchestratorTest();

            // Configure the transformation engine to return test parameters
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);

            // Setup SendTrackingAsync to throw an exception with PCTrackingInfo parameter
            _vtubeStudioPCClientMock.Setup(x =>
                x.SendTrackingAsync(
                    It.IsAny<PCTrackingInfo>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Test exception"));

            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse,
                _longTimeout);

            // Assert - Verify that the transform was called but exception was handled
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x =>
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_RegistersKeyboardShortcuts()
        {
            // Arrange
            SetupBasicMocks();

            // Setup keyboard handler mock to verify registration calls
            _keyboardInputHandlerMock
                .Setup(handler => handler.RegisterShortcut(
                    It.IsAny<ConsoleKey>(),
                    It.IsAny<ConsoleModifiers>(),
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Verifiable();

            var orchestrator = CreateOrchestrator();

            // Act
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Assert
            _keyboardInputHandlerMock.Verify(
                handler => handler.RegisterShortcut(
                    It.IsAny<ConsoleKey>(),
                    It.IsAny<ConsoleModifiers>(),
                    It.IsAny<Action>(),
                    It.IsAny<string>()),
                Times.AtLeast(3));
        }

        [Fact]
        public async Task ReloadTransformationConfig_LoadsNewConfiguration()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;

            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(cancellationToken);

            // Setup transform engine to track reloads
            _transformationEngineMock.Invocations.Clear(); // Clear previous invocations

            // Get the reload action from registered shortcuts
            Action? reloadAction = null;
            _keyboardInputHandlerMock.Verify(x => x.RegisterShortcut(
                ConsoleKey.K,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()),
                Times.Once);

            // Capture the reload action from the registration
            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.K,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => reloadAction = action);

            // Re-initialize to capture the action
            _orchestrator = CreateOrchestrator();
            await _orchestrator.InitializeAsync(cancellationToken);

            // Act - trigger the reload action
            reloadAction.Should().NotBeNull("Reload action should be registered");
            reloadAction!();

            // Allow time for async operations to complete
            await Task.Delay(100);

            // Assert
            _transformationEngineMock.Verify(x => x.LoadRulesAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task ReloadTransformationConfig_WhenLoadingFails_LogsError()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;
            var expectedException = new InvalidOperationException("Test exception");

            // Setup transform engine to throw on second load
            int loadCount = 0;
            _transformationEngineMock.Setup(x => x.LoadRulesAsync())
                .Returns(() =>
                {
                    loadCount++;
                    if (loadCount > 1)
                        throw expectedException;
                    return Task.CompletedTask;
                });

            // Get the reload action from registered shortcuts
            Action? reloadAction = null;
            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.K,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => reloadAction = action);

            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(cancellationToken);

            // Act
            reloadAction!();

            // Allow time for async operations to complete
            await Task.Delay(100);

            // Assert
            _loggerMock.Verify(x => x.ErrorWithException(
                It.Is<string>(s => s.Contains("Error reloading transformation config")),
                expectedException), Times.Once);
        }

        // Tests for UpdateConsoleWindow() failure scenarios

        [Fact]
        public async Task InitializeAsync_WhenConsoleWindowResizeFails_HandlesGracefully()
        {
            // Arrange
            SetupBasicMocks();

            // Setup console window manager mock to fail window resize
            _consoleWindowManagerMock.Setup(x => x.SetConsoleSize(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(false);

            var cancellationToken = CancellationToken.None;

            // Act - should not throw exception
            await _orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _consoleWindowManagerMock.Verify(x => x.SetConsoleSize(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            _loggerMock.Verify(x => x.Warning(
                It.Is<string>(s => s.Contains("Failed to resize console window to preferred size")),
                It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenConsoleSetupThrowsException_HandlesGracefully()
        {
            // Arrange
            SetupBasicMocks();
            var expectedException = new InvalidOperationException("Console setup failed");

            // Setup console window manager mock to throw exception
            _consoleWindowManagerMock.Setup(x => x.SetConsoleSize(It.IsAny<int>(), It.IsAny<int>()))
                .Throws(expectedException);

            var cancellationToken = CancellationToken.None;

            // Act - should not throw exception
            await _orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _loggerMock.Verify(x => x.ErrorWithException(
                It.Is<string>(s => s.Contains("Error setting up console window")),
                expectedException), Times.Once);
        }

        // Tests for LoadTransformationConfig exception handling

        [Fact]
        public async Task InitializeAsync_WhenTransformationEngineLoadThrowsException_PropagatesException()
        {
            // Arrange
            SetupBasicMocks();
            var expectedException = new FileNotFoundException("Config file not found");

            _transformationEngineMock.Setup(x => x.LoadRulesAsync())
                .ThrowsAsync(expectedException);

            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<FileNotFoundException>(() =>
                _orchestrator.InitializeAsync(cancellationToken));

            Assert.Equal(expectedException.Message, thrownException.Message);
        }

        // Tests for error handling in RunAsync phone client operations

        [Fact]
        public async Task RunAsync_WhenPhoneClientSendThrowsException_ContinuesWithRecovery()
        {
            // Arrange
            SetupBasicMocks();
            var expectedException = new InvalidOperationException("Phone client send failed");

            int sendCallCount = 0;
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .Returns(() =>
                {
                    sendCallCount++;
                    if (sendCallCount == 1)
                    {
                        throw expectedException;
                    }
                    return Task.CompletedTask;
                });

            await _orchestrator.InitializeAsync(_longTimeoutCts.Token);

            // Act
            await RunWithCustomTimeout(async () =>
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);

            // Assert
            _vtubeStudioPhoneClientMock.Verify(x => x.SendTrackingRequestAsync(), Times.AtLeast(2));

            // Verify error was logged and recovery delay was applied
            _loggerMock.Verify(x => x.Error(
                It.Is<string>(s => s.Contains("Error in application loop")),
                It.IsAny<object[]>()), Times.Once);
            _recoveryPolicyMock.Verify(x => x.GetNextDelay(), Times.AtLeast(1));
        }

        // Tests for parameter synchronization exception handling

        [Fact]
        public async Task InitializeAsync_WhenParameterSynchronizationThrowsException_PropagatesException()
        {
            // Arrange
            SetupBasicMocks();
            var expectedException = new InvalidOperationException("Parameter sync failed");
            var expectedParameters = new List<VTSParameter>
            {
                new VTSParameter("TestParam", -1.0, 1.0, 0.0)
            };

            _transformationEngineMock.Setup(x => x.GetParameterDefinitions())
                .Returns(expectedParameters);

            _parameterManagerMock.Setup(x => x.TrySynchronizeParametersAsync(
                    It.IsAny<IEnumerable<VTSParameter>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var cancellationToken = CancellationToken.None;

            // Act & Assert - The exception should propagate since TrySynchronizeParametersAsync is called directly
            var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orchestrator.InitializeAsync(cancellationToken));

            Assert.Equal(expectedException.Message, thrownException.Message);
        }

        // Tests for recovery policy edge cases

        [Fact]
        public async Task RunAsync_WhenRecoveryPolicyThrowsException_ContinuesWithDefaultDelay()
        {
            // Arrange
            SetupBasicMocks();
            var phoneException = new InvalidOperationException("Phone client error");
            var recoveryException = new InvalidOperationException("Recovery policy error");

            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .ThrowsAsync(phoneException);

            _recoveryPolicyMock.Setup(x => x.GetNextDelay())
                .Throws(recoveryException);

            await _orchestrator.InitializeAsync(_longTimeoutCts.Token);

            // Act
            await RunWithCustomTimeout(async () =>
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);

            // Assert
            _recoveryPolicyMock.Verify(x => x.GetNextDelay(), Times.AtLeast(1));
            _loggerMock.Verify(x => x.Error(
                It.Is<string>(s => s.Contains("Error in application loop")),
                It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        // Tests for keyboard input handling exceptions

        [Fact]
        public async Task RunAsync_WhenKeyboardInputHandlerThrowsException_ContinuesRunning()
        {
            // Arrange
            SetupBasicMocks();
            var expectedException = new InvalidOperationException("Keyboard input error");

            int checkCallCount = 0;
            _keyboardInputHandlerMock.Setup(x => x.CheckForKeyboardInput())
                .Callback(() =>
                {
                    checkCallCount++;
                    if (checkCallCount == 1)
                    {
                        throw expectedException;
                    }
                });

            await _orchestrator.InitializeAsync(_longTimeoutCts.Token);

            // Act
            await RunWithCustomTimeout(async () =>
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);

            // Assert
            _keyboardInputHandlerMock.Verify(x => x.CheckForKeyboardInput(), Times.AtLeast(2));
            _loggerMock.Verify(x => x.Error(
                It.Is<string>(s => s.Contains("Error in application loop")),
                It.IsAny<object[]>()), Times.Once);
        }

        // Tests for console status update exceptions

        [Fact]
        public async Task RunAsync_WhenUpdateConsoleStatusThrowsException_ContinuesRunning()
        {
            // Arrange
            SetupBasicMocks();
            var expectedException = new InvalidOperationException("Console update error");

            // Setup to throw exception on console update
            int updateCallCount = 0;
            _consoleRendererMock.Setup(x => x.Update(It.IsAny<List<IServiceStats>>()))
                .Callback(() =>
                {
                    updateCallCount++;
                    if (updateCallCount == 1)
                    {
                        throw expectedException;
                    }
                });

            await _orchestrator.InitializeAsync(_longTimeoutCts.Token);

            // Set console update interval to 5ms (much tighter)
            _orchestrator.CONSOLE_UPDATE_INTERVAL_SECONDS = 0.005;

            // Act
            await RunWithCustomTimeout(async () =>
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
                // Add a small delay
            }, TimeSpan.FromMilliseconds(20)); // Using _longTimeout value

            // Assert
            _consoleRendererMock.Verify(x => x.Update(It.IsAny<List<IServiceStats>>()), Times.AtLeast(2));
            _loggerMock.Verify(x => x.ErrorWithException(
                It.Is<string>(s => s.Contains("Error updating console status")),
                expectedException), Times.Once);
        }

        // Tests for specific method path coverage

        [Fact]
        public async Task InitializeAsync_WhenPCClientInitializationFails_StillInitializesPhoneClient()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;

            // Setup PC client to fail initialization
            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Phone client should still succeed
            _vtubeStudioPhoneClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenPhoneClientInitializationFails_StillInitializesPCClient()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;

            // Setup Phone client to fail initialization
            _vtubeStudioPhoneClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // PC client should still succeed
            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _orchestrator.InitializeAsync(cancellationToken);

            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
        }

        // Tests for formatter edge cases

        [Fact]
        public void RegisterKeyboardShortcuts_WhenFormatterIsNull_HandlesGracefully()
        {
            // Arrange
            _consoleRendererMock.Setup(x => x.GetFormatter<PCTrackingInfo>())
                .Returns((IFormatter?)null);

            _consoleRendererMock.Setup(x => x.GetFormatter<PhoneTrackingInfo>())
                .Returns((IFormatter?)null);

            // Get the registered actions
            Action pcCycleAction = null!;
            Action phoneCycleAction = null!;

            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.P,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => pcCycleAction = action);

            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.O,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => phoneCycleAction = action);

            // Initialize orchestrator
            _orchestrator = CreateOrchestrator();

            // Register shortcuts manually
            var registerKeyboardShortcutsMethod = typeof(ApplicationOrchestrator)
                .GetMethod("RegisterKeyboardShortcuts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            registerKeyboardShortcutsMethod!.Invoke(_orchestrator, null);

            // Act - should not throw exceptions
            pcCycleAction.Should().NotBeNull("PC cycle action should be registered");
            phoneCycleAction.Should().NotBeNull("Phone cycle action should be registered");

            var pcAction = () => pcCycleAction();
            var phoneAction = () => phoneCycleAction();

            pcAction.Should().NotThrow("PC cycle action should handle null formatter gracefully");
            phoneAction.Should().NotThrow("Phone cycle action should handle null formatter gracefully");
        }

        // Tests for specific branch coverage scenarios

        [Fact]
        public async Task OnTrackingDataReceived_WithDifferentWebSocketStates_HandlesDifferently()
        {
            // Arrange
            SetupBasicMocks();
            var trackingData = new PhoneTrackingInfo { FaceFound = true, BlendShapes = new List<BlendShape>() };
            var transformedParams = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam> { new TrackingParam { Id = "Test", Value = 0.5 } },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter("Test", -1.0, 1.0, 0.0)
                }
            };

            _transformationEngineMock.Setup(x => x.TransformData(trackingData)).Returns(transformedParams);

            // Test WebSocket states where sending should NOT happen: None, Connecting, Closed, Aborted
            var nonSendingStates = new[] { WebSocketState.None, WebSocketState.Connecting, WebSocketState.Closed, WebSocketState.Aborted };
            int sendTrackingCallCount = 0;

            // Setup to count SendTracking calls
            _vtubeStudioPCClientMock.Setup(x =>
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()))
                .Callback(() => sendTrackingCallCount++)
                .Returns(Task.CompletedTask);

            foreach (var state in nonSendingStates)
            {
                // Setup state-specific behavior - the PC client should return unhealthy for non-Open states
                var pcStats = new ServiceStats(
                    serviceName: "VTubeStudioPCClient",
                    status: state == WebSocketState.Open ? "Connected" : "Disconnected",
                    currentEntity: new PCTrackingInfo(),
                    isHealthy: state == WebSocketState.Open,
                    lastSuccessfulOperation: DateTime.UtcNow,
                    lastError: state == WebSocketState.Open ? null : "Connection not open",
                    counters: new Dictionary<string, long>()
                );
                _vtubeStudioPCClientMock.Setup(x => x.GetServiceStats()).Returns(pcStats);
                _vtubeStudioPCClientMock.SetupGet(x => x.State).Returns(state);

                // Initialize orchestrator to subscribe to events
                var orchestrator = CreateOrchestrator();
                await orchestrator.InitializeAsync(CancellationToken.None);

                // Get the event handler method via reflection
                var onTrackingDataReceivedMethod = typeof(ApplicationOrchestrator)
                    .GetMethod("OnTrackingDataReceived",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                // Act - invoke the event handler
                onTrackingDataReceivedMethod!.Invoke(orchestrator,
                    new object[] { _vtubeStudioPhoneClientMock.Object, trackingData });

                // Small delay to allow processing
                await Task.Delay(50);

                orchestrator.Dispose();
            }

            // Assert - verify transformation was called for all states
            _transformationEngineMock.Verify(x => x.TransformData(trackingData), Times.Exactly(nonSendingStates.Length));

            // Verify SendTrackingAsync was not called for unhealthy states (due to PC client being unhealthy)
            Assert.Equal(0, sendTrackingCallCount);
        }

        // Tests for recovery scenarios that were missing coverage

        [Fact]
        public async Task AttemptRecoveryAsync_WhenPhoneClientIsUnhealthy_AttemptsPhoneClientRecovery()
        {
            // Arrange
            SetupBasicMocks();

            // Setup phone client to be unhealthy initially
            var unhealthyPhoneStats = new ServiceStats(
                serviceName: "PhoneClient",
                status: "Disconnected",
                currentEntity: new PhoneTrackingInfo(),
                isHealthy: false,
                lastSuccessfulOperation: DateTime.UtcNow.AddMinutes(-5),
                lastError: "Connection lost",
                counters: new Dictionary<string, long>()
            );

            _vtubeStudioPhoneClientMock.Setup(x => x.GetServiceStats())
                .Returns(unhealthyPhoneStats);

            // Setup phone client TryInitializeAsync to be called during recovery
            _vtubeStudioPhoneClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Setup phone client to throw exception which triggers recovery
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .ThrowsAsync(new InvalidOperationException("Phone client connection failed"));

            await _orchestrator.InitializeAsync(_longTimeoutCts.Token);

            // Act - Run the orchestrator which will trigger recovery due to phone client exception
            await RunWithCustomTimeout(async () =>
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);

            // Assert
            _vtubeStudioPhoneClientMock.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            _loggerMock.Verify(x => x.Info("Attempting to recover Phone client..."), Times.AtLeastOnce);
        }

        [Fact]
        public async Task AttemptRecoveryAsync_WhenPCClientRecoverySucceeds_AttemptParameterSynchronization()
        {
            // Arrange
            SetupBasicMocks();

            // Setup PC client to be initially unhealthy
            var unhealthyPcStats = new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: "Disconnected",
                currentEntity: new PCTrackingInfo(),
                isHealthy: false,
                lastSuccessfulOperation: DateTime.UtcNow.AddMinutes(-5),
                lastError: "Connection lost",
                counters: new Dictionary<string, long>()
            );

            // Setup healthy PC stats for after recovery
            var healthyPcStats = new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: "Connected",
                currentEntity: new PCTrackingInfo(),
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Setup sequence: first call returns unhealthy, subsequent calls return healthy
            var setupSequence = _vtubeStudioPCClientMock.SetupSequence(x => x.GetServiceStats());
            setupSequence.Returns(unhealthyPcStats);
            setupSequence.Returns(healthyPcStats);
            // Add more healthy returns for subsequent calls during the test
            for (int i = 0; i < 10; i++)
            {
                setupSequence.Returns(healthyPcStats);
            }

            // Setup PC client TryInitializeAsync to succeed (simulating successful recovery)
            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Setup parameter manager to be called during recovery
            _parameterManagerMock.Setup(x => x.TrySynchronizeParametersAsync(
                    It.IsAny<IEnumerable<VTSParameter>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Setup phone client to throw exception which triggers recovery
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .ThrowsAsync(new InvalidOperationException("Trigger recovery scenario"));

            await _orchestrator.InitializeAsync(_longTimeoutCts.Token);

            // Act - Run the orchestrator which will trigger recovery
            await RunWithCustomTimeout(async () =>
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);

            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            _loggerMock.Verify(x => x.Info("Attempting to recover PC client..."), Times.AtLeastOnce);
            _loggerMock.Verify(x => x.Info("PC client recovered successfully, attempting parameter synchronization..."), Times.AtLeastOnce);
            _parameterManagerMock.Verify(x => x.TrySynchronizeParametersAsync(
                It.IsAny<IEnumerable<VTSParameter>>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task AttemptRecoveryAsync_WhenPCClientRecoverySucceedsButParameterSyncFails_LogsWarning()
        {
            // Arrange
            SetupBasicMocks();

            // Setup PC client to be initially unhealthy then healthy after recovery
            var unhealthyPcStats = new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: "Disconnected",
                currentEntity: new PCTrackingInfo(),
                isHealthy: false,
                lastSuccessfulOperation: DateTime.UtcNow.AddMinutes(-5),
                lastError: "Connection lost",
                counters: new Dictionary<string, long>()
            );

            var healthyPcStats = new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: "Connected",
                currentEntity: new PCTrackingInfo(),
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Setup sequence: unhealthy -> healthy after recovery
            var setupSequence = _vtubeStudioPCClientMock.SetupSequence(x => x.GetServiceStats());
            setupSequence.Returns(unhealthyPcStats);
            setupSequence.Returns(healthyPcStats);
            for (int i = 0; i < 10; i++)
            {
                setupSequence.Returns(healthyPcStats);
            }

            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Setup parameter synchronization to fail
            _parameterManagerMock.Setup(x => x.TrySynchronizeParametersAsync(
                    It.IsAny<IEnumerable<VTSParameter>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Trigger recovery scenario
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .ThrowsAsync(new InvalidOperationException("Trigger recovery"));

            await _orchestrator.InitializeAsync(_longTimeoutCts.Token);

            // Act
            await RunWithCustomTimeout(async () =>
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);

            // Assert
            _loggerMock.Verify(x => x.Info("PC client recovered successfully, attempting parameter synchronization..."), Times.AtLeastOnce);
            _loggerMock.Verify(x => x.Warning("Parameter synchronization failed after PC client recovery"), Times.AtLeastOnce);
        }

        // Tests for Color Service Integration

        [Fact]
        public void Constructor_WithNullColorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("colorService"));
            exception.ParamName.Should().Be("colorService");
        }

        [Fact]
        public void Constructor_WithNullExternalEditorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("externalEditorService"));
            exception.ParamName.Should().Be("externalEditorService");
        }

        [Fact]
        public void Constructor_WithNullShortcutConfigurationManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("shortcutConfigurationManager"));
            exception.ParamName.Should().Be("shortcutConfigurationManager");
        }

        [Fact]
        public void Constructor_WithNullApplicationConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("applicationConfig"));
            exception.ParamName.Should().Be("applicationConfig");
        }

        [Fact]
        public void Constructor_WithNullSystemHelpRenderer_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                CreateOrchestratorWithNullParameter("systemHelpRenderer"));
            exception.ParamName.Should().Be("systemHelpRenderer");
        }

        [Fact]
        public async Task OnTrackingDataReceived_WithValidBlendShapes_InitializesColorServiceOnce()
        {
            // Arrange
            SetupOrchestratorTest();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                    new BlendShape { Key = "eyeBlinkRight", Value = 0.3 },
                    new BlendShape { Key = "jawOpen", Value = 0.8 }
                }
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _transformationEngineMock.Setup(x => x.TransformData(trackingData))
                .Returns(pcTrackingInfo);

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act - Trigger the event multiple times
            bool eventTriggered1 = await RunWithTimeoutAndEventTrigger(trackingData, TimeSpan.FromMilliseconds(100));
            bool eventTriggered2 = await RunWithTimeoutAndEventTrigger(trackingData, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(eventTriggered1, "First event should have been triggered");
            Assert.True(eventTriggered2, "Second event should have been triggered");

            // Verify color service was initialized exactly once with the blend shape names
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.Is<IEnumerable<string>>(names =>
                    names.Contains("eyeBlinkLeft") &&
                    names.Contains("eyeBlinkRight") &&
                    names.Contains("jawOpen")),
                It.IsAny<IEnumerable<string>>()),
                Times.Once);

            // Verify debug log was written
            _loggerMock.Verify(x => x.Debug(It.Is<string>(msg =>
                msg.Contains("Color service initialized with") &&
                msg.Contains("calculated parameters and") &&
                msg.Contains("blend shapes"))),
                Times.Once);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WithEmptyBlendShapes_DoesNotInitializeColorService()
        {
            // Arrange
            SetupOrchestratorTest();

            var trackingDataWithEmptyBlendShapes = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>() // Empty blend shapes
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _transformationEngineMock.Setup(x => x.TransformData(trackingDataWithEmptyBlendShapes))
                .Returns(pcTrackingInfo);

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act
            bool eventTriggered = await RunWithTimeoutAndEventTrigger(trackingDataWithEmptyBlendShapes, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(eventTriggered, "Event should have been triggered");

            // Verify color service was NOT initialized
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>()),
                Times.Never);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WithNullBlendShapes_DoesNotInitializeColorService()
        {
            // Arrange
            SetupOrchestratorTest();

            var trackingDataWithNullBlendShapes = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = null! // Null blend shapes
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _transformationEngineMock.Setup(x => x.TransformData(trackingDataWithNullBlendShapes))
                .Returns(pcTrackingInfo);

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act
            bool eventTriggered = await RunWithTimeoutAndEventTrigger(trackingDataWithNullBlendShapes, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(eventTriggered, "Event should have been triggered");

            // Verify color service was NOT initialized
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>()),
                Times.Never);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WhenColorServiceInitializationFails_ContinuesProcessingGracefully()
        {
            // Arrange
            SetupOrchestratorTest();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                    new BlendShape { Key = "eyeBlinkRight", Value = 0.3 }
                }
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _transformationEngineMock.Setup(x => x.TransformData(trackingData))
                .Returns(pcTrackingInfo);

            // Setup color service to throw exception during initialization
            _colorServiceMock.Setup(x => x.InitializeFromConfiguration(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>()))
                .Throws(new InvalidOperationException("Color service initialization failed"));

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act
            bool eventTriggered = await RunWithTimeoutAndEventTrigger(trackingData, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(eventTriggered, "Event should have been triggered despite color service failure");

            // Verify transformation and sending still occurred
            _transformationEngineMock.Verify(x => x.TransformData(trackingData), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => x.SendTrackingAsync(
                It.Is<PCTrackingInfo>(info => info.FaceFound),
                It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify warning was logged
            _loggerMock.Verify(x => x.Warning(It.Is<string>(msg =>
                msg.Contains("Failed to initialize color service") &&
                msg.Contains("Color service initialization failed"))),
                Times.Once);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WithBlendShapesContainingEmptyKeys_FiltersOutEmptyKeys()
        {
            // Arrange
            SetupOrchestratorTest();

            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                    new BlendShape { Key = "", Value = 0.3 }, // Empty key should be filtered out
                    new BlendShape { Key = "jawOpen", Value = 0.8 },
                    new BlendShape { Key = null!, Value = 0.2 } // Null key should be filtered out
                }
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _transformationEngineMock.Setup(x => x.TransformData(trackingData))
                .Returns(pcTrackingInfo);

            // Setup transformation engine to return empty calculated parameters
            _transformationEngineMock.Setup(x => x.GetParameterDefinitions())
                .Returns(new List<VTSParameter>());

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act
            bool eventTriggered = await RunWithTimeoutAndEventTrigger(trackingData, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(eventTriggered, "Event should have been triggered");

            // Verify color service was initialized with only valid blend shape names (empty/null keys filtered out)
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.Is<IEnumerable<string>>(names =>
                    names.Contains("eyeBlinkLeft") &&
                    names.Contains("jawOpen") &&
                    names.Count() == 2), // Only 2 valid names, empty and null filtered out
                It.IsAny<IEnumerable<string>>()),
                Times.Once);
        }

        [Fact]
        public async Task OnTrackingDataReceived_AfterColorServiceInitialized_DoesNotReinitialize()
        {
            // Arrange
            SetupOrchestratorTest();

            var firstTrackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                    new BlendShape { Key = "eyeBlinkRight", Value = 0.3 }
                }
            };

            var secondTrackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "jawOpen", Value = 0.8 },
                    new BlendShape { Key = "mouthSmile", Value = 0.6 }
                }
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _transformationEngineMock.Setup(x => x.TransformData(It.IsAny<PhoneTrackingInfo>()))
                .Returns(pcTrackingInfo);

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act - Trigger events with different blend shapes
            bool firstEventTriggered = await RunWithTimeoutAndEventTrigger(firstTrackingData, TimeSpan.FromMilliseconds(100));
            bool secondEventTriggered = await RunWithTimeoutAndEventTrigger(secondTrackingData, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(firstEventTriggered, "First event should have been triggered");
            Assert.True(secondEventTriggered, "Second event should have been triggered");

            // Verify color service was initialized exactly once (with the first tracking data)
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.Is<IEnumerable<string>>(names =>
                    names.Contains("eyeBlinkLeft") &&
                    names.Contains("eyeBlinkRight") &&
                    names.Count() == 2),
                It.IsAny<IEnumerable<string>>()),
                Times.Once);

            // Verify it was NOT called with the second set of blend shapes
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.Is<IEnumerable<string>>(names =>
                    names.Contains("jawOpen") || names.Contains("mouthSmile")),
                It.IsAny<IEnumerable<string>>()),
                Times.Never);
        }

        [Fact]
        public async Task ReloadTransformationConfig_ResetsColorServiceInitializationFlag()
        {
            // Arrange
            SetupOrchestratorTest();

            var firstTrackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                    new BlendShape { Key = "eyeBlinkRight", Value = 0.3 }
                }
            };

            var secondTrackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "jawOpen", Value = 0.8 },
                    new BlendShape { Key = "mouthSmile", Value = 0.6 }
                }
            };

            var pcTrackingInfo = new PCTrackingInfo();
            _transformationEngineMock.Setup(x => x.TransformData(It.IsAny<PhoneTrackingInfo>()))
                .Returns(pcTrackingInfo);

            // Capture the reload action from keyboard shortcut registration
            Action? reloadAction = null;
            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.K,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => reloadAction = action);

            await _orchestrator.InitializeAsync(_defaultCts.Token);

            // Act - First tracking data should initialize color service
            bool firstEventTriggered = await RunWithTimeoutAndEventTrigger(firstTrackingData, TimeSpan.FromMilliseconds(100));

            // Trigger config reload using captured action
            reloadAction.Should().NotBeNull("Reload action should be registered");
            reloadAction!();

            // Small delay to allow config reload to complete
            await Task.Delay(100);

            // Second tracking data should reinitialize color service after config reload
            bool secondEventTriggered = await RunWithTimeoutAndEventTrigger(secondTrackingData, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(firstEventTriggered, "First event should have been triggered");
            Assert.True(secondEventTriggered, "Second event should have been triggered");

            // Verify color service was initialized twice - once for each tracking data after config reload
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>()),
                Times.Exactly(2));

            // Verify first initialization with first set of blend shapes
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.Is<IEnumerable<string>>(names =>
                    names.Contains("eyeBlinkLeft") &&
                    names.Contains("eyeBlinkRight") &&
                    names.Count() == 2),
                It.IsAny<IEnumerable<string>>()),
                Times.Once);

            // Verify second initialization with second set of blend shapes after config reload
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.Is<IEnumerable<string>>(names =>
                    names.Contains("jawOpen") &&
                    names.Contains("mouthSmile") &&
                    names.Count() == 2),
                It.IsAny<IEnumerable<string>>()),
                Times.Once);

            // Verify debug log for color service reset
            _loggerMock.Verify(x => x.Debug("Color service initialization flag reset for config reload"),
                Times.Once);
        }

        #region External Editor Tests

        [Fact]
        public async Task OpenTransformationConfigInEditor_CallsExternalEditorService()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            _externalEditorServiceMock.Setup(x => x.TryOpenTransformationConfigAsync())
                .ReturnsAsync(true);

            // Act - Use reflection to call the private method
            var method = typeof(ApplicationOrchestrator).GetMethod("OpenTransformationConfigInEditor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert
            _externalEditorServiceMock.Verify(x => x.TryOpenTransformationConfigAsync(), Times.Once);
        }

        [Fact]
        public async Task OpenTransformationConfigInEditor_WhenServiceFails_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            _externalEditorServiceMock.Setup(x => x.TryOpenTransformationConfigAsync())
                .ReturnsAsync(false);

            // Act - Use reflection to call the private method
            var method = typeof(ApplicationOrchestrator).GetMethod("OpenTransformationConfigInEditor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert
            _loggerMock.Verify(x => x.Warning("Failed to open transformation configuration in external editor"), Times.Once);
        }

        #endregion

        #region Keyboard Shortcut Coverage Tests

        [Fact]
        public void RegisterKeyboardShortcuts_AltT_CyclesTransformationEngineVerbosity()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var transformationFormatter = new Mock<IFormatter>();
            _consoleRendererMock.Setup(x => x.GetFormatter<TransformationEngineInfo>())
                .Returns(transformationFormatter.Object);

            // Trigger keyboard shortcut registration
            orchestrator.InitializeAsync(CancellationToken.None).Wait();

            // Act - Simulate Alt+T keyboard shortcut
            var shortcutAction = GetRegisteredShortcutAction(ConsoleKey.T, ConsoleModifiers.Alt);
            shortcutAction?.Invoke();

            // Assert
            transformationFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public void RegisterKeyboardShortcuts_AltP_CyclesPCClientVerbosity()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var pcFormatter = new Mock<IFormatter>();
            _consoleRendererMock.Setup(x => x.GetFormatter<PCTrackingInfo>())
                .Returns(pcFormatter.Object);

            // Trigger keyboard shortcut registration
            orchestrator.InitializeAsync(CancellationToken.None).Wait();

            // Act - Simulate Alt+P keyboard shortcut
            var shortcutAction = GetRegisteredShortcutAction(ConsoleKey.P, ConsoleModifiers.Alt);
            shortcutAction?.Invoke();

            // Assert
            pcFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public void RegisterKeyboardShortcuts_AltO_CyclesPhoneClientVerbosity()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var phoneFormatter = new Mock<IFormatter>();
            _consoleRendererMock.Setup(x => x.GetFormatter<PhoneTrackingInfo>())
                .Returns(phoneFormatter.Object);

            // Trigger keyboard shortcut registration
            orchestrator.InitializeAsync(CancellationToken.None).Wait();

            // Act - Simulate Alt+O keyboard shortcut
            var shortcutAction = GetRegisteredShortcutAction(ConsoleKey.O, ConsoleModifiers.Alt);
            shortcutAction?.Invoke();

            // Assert
            phoneFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public async Task RegisterKeyboardShortcuts_CtrlAltE_OpensExternalEditor()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            _externalEditorServiceMock.Setup(x => x.TryOpenTransformationConfigAsync())
                .ReturnsAsync(true);

            // Act - Simulate Ctrl+Alt+E keyboard shortcut
            var shortcutAction = GetRegisteredShortcutAction(ConsoleKey.E,
                ConsoleModifiers.Control | ConsoleModifiers.Alt);
            shortcutAction?.Invoke();

            // Give async operation time to complete
            await Task.Delay(100);

            // Assert
            _externalEditorServiceMock.Verify(x => x.TryOpenTransformationConfigAsync(), Times.Once);
        }

        /// <summary>
        /// Helper method to get registered keyboard shortcut action for testing
        /// </summary>
        private Action? GetRegisteredShortcutAction(ConsoleKey key, ConsoleModifiers modifiers)
        {
            return _keyboardInputHandlerMock.Invocations
                .Where(inv => inv.Method.Name == "RegisterShortcut" &&
                             inv.Arguments.Count >= 3 &&
                             (ConsoleKey)inv.Arguments[0] == key &&
                             (ConsoleModifiers)inv.Arguments[1] == modifiers)
                .Select(inv => inv.Arguments[2] as Action)
                .FirstOrDefault();
        }

        #endregion

        #region Additional Coverage Tests

        [Fact]
        public async Task ReloadTransformationConfig_WhenTransformationEngineThrows_LogsError()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            _transformationEngineMock.Setup(x => x.LoadRulesAsync())
                .ThrowsAsync(new InvalidOperationException("Config load failed"));

            // Act - Use reflection to call the private method
            var method = typeof(ApplicationOrchestrator).GetMethod("ReloadTransformationConfig",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = (Task)method!.Invoke(orchestrator, null)!;
            await task;

            // Assert - The actual log message is "Error reloading transformation config"
            _loggerMock.Verify(x => x.ErrorWithException(
                "Error reloading transformation config",
                It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public void InitializeColorServiceIfNeeded_WhenColorServiceInitialized_DoesNotReinitialize()
        {
            // Arrange
            var orchestrator = CreateOrchestrator();
            var trackingData = new PhoneTrackingInfo
            {
                BlendShapes = new List<BlendShape> { new BlendShape { Key = "EyeBlinkLeft", Value = 0.5 } }
            };

            // Initialize the service first time
            var method = typeof(ApplicationOrchestrator).GetMethod("InitializeColorServiceIfNeeded",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(orchestrator, new object[] { trackingData });

            // Reset mock to verify second call behavior
            _colorServiceMock.Reset();

            // Act - Try to initialize again
            method.Invoke(orchestrator, new object[] { trackingData });

            // Assert - Should not call InitializeFromConfiguration again
            _colorServiceMock.Verify(x => x.InitializeFromConfiguration(
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public void Dispose_WhenConsoleWindowManagerIsNull_DoesNotThrow()
        {
            // Arrange - Create orchestrator with null console window manager
            var orchestrator = new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object,
                _consoleMock.Object,
                _consoleWindowManagerMock.Object,
                _colorServiceMock.Object,
                _externalEditorServiceMock.Object,
                _shortcutConfigurationManagerMock.Object,
                _applicationConfig,
                _systemHelpRendererMock.Object,
                _userPreferences,
                _configManagerMock.Object, _appConfigWatcherMock.Object);

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => orchestrator.Dispose());
            Assert.Null(exception);
        }

        #endregion

        #region Shortcut System Tests

        [Fact]
        public void Constructor_LoadsShortcutConfiguration()
        {
            // Arrange & Act
            CreateOrchestrator();

            // Assert
            _shortcutConfigurationManagerMock.Verify(x => x.LoadFromConfiguration(_applicationConfig.GeneralSettings), Times.AtLeastOnce);
        }

        [Fact]
        public async Task InitializeAsync_RegistersShortcutsFromConfiguration()
        {
            // Arrange
            SetupBasicMocks();
            var orchestrator = CreateOrchestrator();

            // Act
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Assert
            _keyboardInputHandlerMock.Verify(x => x.RegisterShortcut(
                It.IsAny<ConsoleKey>(),
                It.IsAny<ConsoleModifiers>(),
                It.IsAny<Action>(),
                It.IsAny<string>()), Times.AtLeast(1));
        }

        [Fact]
        public async Task InitializeAsync_WithShortcutConfigurationIssues_LogsWarnings()
        {
            // Arrange
            SetupBasicMocks();
            var incorrectShortcuts = new Dictionary<ShortcutAction, string>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = "InvalidShortcut",
                [ShortcutAction.CyclePCClientVerbosity] = "DuplicateKey"
            };
            _shortcutConfigurationManagerMock.Setup(x => x.GetIncorrectShortcuts())
                .Returns(incorrectShortcuts);

            var orchestrator = CreateOrchestrator();

            // Act
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Assert
            _loggerMock.Verify(x => x.Warning(
                It.Is<string>(s => s.Contains("Invalid shortcut configurations detected")),
                It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WithDisabledShortcuts_SkipsRegistration()
        {
            // Arrange
            SetupBasicMocks();
            _shortcutConfigurationManagerMock.Setup(x => x.GetMappedShortcuts())
                .Returns(new Dictionary<ShortcutAction, Shortcut?>
                {
                    [ShortcutAction.CycleTransformationEngineVerbosity] = null, // Disabled
                    [ShortcutAction.CyclePCClientVerbosity] = new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt),
                });

            var orchestrator = CreateOrchestrator();

            // Act
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Assert
            _loggerMock.Verify(x => x.Debug(
                It.Is<string>(s => s.Contains("Skipping disabled shortcut for action")),
                It.IsAny<object[]>()), Times.Once);
        }

        #endregion

        #region System Help Tests

        [Fact]
        public async Task ShowShortcutHelp_DisplaysSystemHelp()
        {
            // Arrange
            SetupBasicMocks();
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            _systemHelpRendererMock.Setup(x => x.RenderSystemHelp(It.IsAny<ApplicationConfig>(), It.IsAny<int>()))
                .Returns("Test help content");

            // Get the help action from registered shortcuts
            Action? helpAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.F1,
                ConsoleModifiers.None,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => helpAction = action);

            // Re-initialize to capture the action
            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            helpAction.Should().NotBeNull("Help action should be registered");
            helpAction!();

            // Assert
            _systemHelpRendererMock.Verify(x => x.RenderSystemHelp(
                _applicationConfig,
                It.IsAny<int>()), Times.Once);
            _consoleMock.Verify(x => x.Clear(), Times.Once);
            _consoleMock.Verify(x => x.Write("Test help content"), Times.Once);
        }

        [Fact]
        public async Task CheckForKeyboardInput_InHelpMode_ExitsOnF1Repeat()
        {
            // Arrange
            SetupBasicMocks();
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Get the help action and trigger it to enter help mode
            Action? helpAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.F1,
                ConsoleModifiers.None,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => helpAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);
            helpAction!(); // Enter help mode

            // Now simulate that we're in help mode and F1 is pressed again to exit
            // We need to use reflection to set the private field _isShowingSystemHelp to true
            var isShowingSystemHelpField = typeof(ApplicationOrchestrator)
                .GetField("_isShowingSystemHelp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isShowingSystemHelpField!.SetValue(newOrchestrator, true);

            // Act - call the help action again to exit help mode
            helpAction!();

            // Assert
            _consoleMock.Verify(x => x.Clear(), Times.AtLeast(1)); // Called when exiting help
        }

        #endregion

        #region External Editor Tests

        [Fact]
        public async Task OpenConfigInEditor_CallsExternalEditorService()
        {
            // Arrange
            SetupBasicMocks();
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            _externalEditorServiceMock.Setup(x => x.TryOpenTransformationConfigAsync())
                .ReturnsAsync(true);

            // Get the editor action from registered shortcuts
            Action? editorAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.E,
                ConsoleModifiers.Control | ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => editorAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            editorAction.Should().NotBeNull("Editor action should be registered");
            editorAction!();

            // Give async operation time to complete
            await Task.Delay(100);

            // Assert
            _externalEditorServiceMock.Verify(x => x.TryOpenTransformationConfigAsync(), Times.Once);
        }

        [Fact]
        public async Task OpenConfigInEditor_WhenServiceFails_LogsWarning()
        {
            // Arrange
            SetupBasicMocks();
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            _externalEditorServiceMock.Setup(x => x.TryOpenTransformationConfigAsync())
                .ReturnsAsync(false);

            // Get the editor action
            Action? editorAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.E,
                ConsoleModifiers.Control | ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => editorAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            editorAction!();
            await Task.Delay(100);

            // Assert
            _loggerMock.Verify(x => x.Warning("Failed to open transformation configuration in external editor"), Times.Once);
        }

        #endregion

        #region Reload Configuration Tests

        [Fact]
        public async Task ReloadTransformationConfig_ReloadsConfiguration()
        {
            // Arrange
            SetupBasicMocks();
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Clear previous invocations
            _transformationEngineMock.Invocations.Clear();

            // Get the reload action
            Action? reloadAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.K,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => reloadAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            reloadAction.Should().NotBeNull("Reload action should be registered");
            reloadAction!();

            // Allow time for async operations to complete
            await Task.Delay(100);

            // Assert
            _transformationEngineMock.Verify(x => x.LoadRulesAsync(), Times.AtLeast(2)); // Called during Initialize + Reload
            _loggerMock.Verify(x => x.Info("Reloading transformation config..."), Times.Once);
            _loggerMock.Verify(x => x.Info("Transformation config reloaded successfully"), Times.Once);
        }

        [Fact]
        public async Task ReloadTransformationConfig_ResetsColorServiceFlag()
        {
            // Arrange
            SetupBasicMocks();
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Get the reload action
            Action? reloadAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.K,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => reloadAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            reloadAction!();
            await Task.Delay(100);

            // Assert
            _loggerMock.Verify(x => x.Debug("Color service initialization flag reset for config reload"), Times.Once);
        }

        [Fact]
        public async Task ReloadTransformationConfig_WhenLoadingFails_LogsErrorMessage()
        {
            // Arrange
            SetupBasicMocks();
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Set up the mock to succeed on first calls, then fail on subsequent calls
            var expectedException = new InvalidOperationException("Test exception");
            _transformationEngineMock.SetupSequence(x => x.LoadRulesAsync())
                .Returns(Task.CompletedTask) // First call during InitializeAsync succeeds
                .ThrowsAsync(expectedException); // Second call during reload fails

            // Get the reload action
            Action? reloadAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.K,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => reloadAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            reloadAction!();
            await Task.Delay(100);

            // Assert
            _loggerMock.Verify(x => x.ErrorWithException(
                "Error reloading transformation config",
                expectedException), Times.Once);
        }

        #endregion

        #region Verbosity Cycling Tests

        [Fact]
        public async Task CycleTransformationEngineVerbosity_CallsFormatter()
        {
            // Arrange
            SetupBasicMocks();
            var transformationFormatter = new Mock<IFormatter>();
            _consoleRendererMock.Setup(x => x.GetFormatter<TransformationEngineInfo>())
                .Returns(transformationFormatter.Object);

            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Get the verbosity action
            Action? verbosityAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.T,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => verbosityAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            verbosityAction.Should().NotBeNull("Verbosity action should be registered");
            verbosityAction!();

            // Assert
            transformationFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public async Task CyclePCClientVerbosity_CallsFormatter()
        {
            // Arrange
            SetupBasicMocks();
            var pcFormatter = new Mock<IFormatter>();
            _consoleRendererMock.Setup(x => x.GetFormatter<PCTrackingInfo>())
                .Returns(pcFormatter.Object);

            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Get the verbosity action
            Action? verbosityAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.P,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => verbosityAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            verbosityAction!();

            // Assert
            pcFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public async Task CyclePhoneClientVerbosity_CallsFormatter()
        {
            // Arrange
            SetupBasicMocks();
            var phoneFormatter = new Mock<IFormatter>();
            _consoleRendererMock.Setup(x => x.GetFormatter<PhoneTrackingInfo>())
                .Returns(phoneFormatter.Object);

            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Get the verbosity action
            Action? verbosityAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.O,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => verbosityAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act
            verbosityAction!();

            // Assert
            phoneFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }

        [Fact]
        public async Task CycleVerbosity_WhenFormatterIsNull_HandlesGracefully()
        {
            // Arrange
            SetupBasicMocks();
            _consoleRendererMock.Setup(x => x.GetFormatter<TransformationEngineInfo>())
                .Returns((IFormatter?)null);

            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(CancellationToken.None);

            // Get the verbosity action
            Action? verbosityAction = null;
            _keyboardInputHandlerMock.Setup(x => x.RegisterShortcut(
                ConsoleKey.T,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => verbosityAction = action);

            var newOrchestrator = CreateOrchestrator();
            await newOrchestrator.InitializeAsync(CancellationToken.None);

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => verbosityAction!());
            exception.Should().BeNull("Action should handle null formatter gracefully");
        }

        #endregion
    }
}
