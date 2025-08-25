using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Services
{
    /// <summary>
    /// Orchestrates the application lifecycle and coordinates component interactions
    /// </summary>
    public class ApplicationOrchestrator : IApplicationOrchestrator, IDisposable
    {
        private readonly IVTubeStudioPCClient _vtubeStudioPCClient;
        private readonly IVTubeStudioPhoneClient _vtubeStudioPhoneClient;
        private readonly ITransformationEngine _transformationEngine;
        private readonly VTubeStudioPhoneClientConfig _phoneConfig;
        private readonly IAppLogger _logger;
        private readonly IConsoleModeManager _modeManager;
        private readonly IKeyboardInputHandler _keyboardInputHandler;
        private readonly IVTubeStudioPCParameterManager _parameterManager;
        private readonly IRecoveryPolicy _recoveryPolicy;
        private readonly IConsoleWindowManager _consoleWindowManager;
        private readonly IParameterColorService _colorService;
        private readonly IExternalEditorService _externalEditorService;
        private readonly IShortcutConfigurationManager _shortcutConfigurationManager;
        private readonly IConfigManager _configManager;
        private readonly IFileChangeWatcher _appConfigWatcher;
        private readonly UserPreferences _userPreferences;
        private ApplicationConfig _applicationConfig;

        /// <summary>
        /// Gets or sets the interval in seconds between console status updates
        /// </summary>
        public double CONSOLE_UPDATE_INTERVAL_SECONDS { get; set; } = 0.1;

        private bool _isDisposed;
        private DateTime _nextRecoveryAttempt = DateTime.UtcNow;
        private bool _colorServiceInitialized = false; // Track if color service has been initialized

        /// <summary>
        /// Creates a new instance of the ApplicationOrchestrator
        /// </summary>
        /// <param name="vtubeStudioPCClient">The VTube Studio PC client</param>
        /// <param name="vtubeStudioPhoneClient">The VTube Studio phone client</param>
        /// <param name="transformationEngine">The transformation engine</param>
        /// <param name="phoneConfig">Configuration for the phone client</param>
        /// <param name="logger">Application logger</param>
        /// <param name="modeManager">Console mode manager for UI mode switching</param>
        /// <param name="keyboardInputHandler">Keyboard input handler</param>
        /// <param name="parameterManager">VTube Studio PC parameter manager</param>
        /// <param name="recoveryPolicy">Policy for determining recovery attempt timing</param>
        /// <param name="console">Console abstraction for window management</param>
        /// <param name="consoleWindowManager">Console window manager for size management and tracking</param>
        /// <param name="colorService">Parameter color service for colored console output</param>
        /// <param name="externalEditorService">Service for opening files in external editors</param>
        /// <param name="shortcutConfigurationManager">Manager for keyboard shortcut configurations</param>
        /// <param name="applicationConfig">Application configuration containing shortcut definitions</param>
        /// <param name="userPreferences">User preferences for console dimensions and verbosity levels</param>
        /// <param name="configManager">Configuration manager for saving user preferences</param>
        /// <param name="appConfigWatcher">File change watcher for application configuration</param>
        public ApplicationOrchestrator(
            IVTubeStudioPCClient vtubeStudioPCClient,
            IVTubeStudioPhoneClient vtubeStudioPhoneClient,
            ITransformationEngine transformationEngine,
            VTubeStudioPhoneClientConfig phoneConfig,
            IAppLogger logger,
            IConsoleModeManager modeManager,
            IKeyboardInputHandler keyboardInputHandler,
            IVTubeStudioPCParameterManager parameterManager,
            IRecoveryPolicy recoveryPolicy,
            IConsole console,
            IConsoleWindowManager consoleWindowManager,
            IParameterColorService colorService,
            IExternalEditorService externalEditorService,
            IShortcutConfigurationManager shortcutConfigurationManager,
            ApplicationConfig applicationConfig,
            UserPreferences userPreferences,
            IConfigManager configManager,
            IFileChangeWatcher appConfigWatcher)
        {
            _vtubeStudioPCClient = vtubeStudioPCClient ?? throw new ArgumentNullException(nameof(vtubeStudioPCClient));
            _vtubeStudioPhoneClient = vtubeStudioPhoneClient ?? throw new ArgumentNullException(nameof(vtubeStudioPhoneClient));
            _transformationEngine = transformationEngine ?? throw new ArgumentNullException(nameof(transformationEngine));
            _phoneConfig = phoneConfig ?? throw new ArgumentNullException(nameof(phoneConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
            _keyboardInputHandler = keyboardInputHandler ?? throw new ArgumentNullException(nameof(keyboardInputHandler));
            _parameterManager = parameterManager ?? throw new ArgumentNullException(nameof(parameterManager));
            _recoveryPolicy = recoveryPolicy ?? throw new ArgumentNullException(nameof(recoveryPolicy));
            _consoleWindowManager = consoleWindowManager ?? throw new ArgumentNullException(nameof(consoleWindowManager));
            _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
            _externalEditorService = externalEditorService ?? throw new ArgumentNullException(nameof(externalEditorService));
            _shortcutConfigurationManager = shortcutConfigurationManager ?? throw new ArgumentNullException(nameof(shortcutConfigurationManager));
            _applicationConfig = applicationConfig ?? throw new ArgumentNullException(nameof(applicationConfig));
            _userPreferences = userPreferences ?? throw new ArgumentNullException(nameof(userPreferences));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _appConfigWatcher = appConfigWatcher ?? throw new ArgumentNullException(nameof(appConfigWatcher));

            // Load shortcut configuration
            _shortcutConfigurationManager.LoadFromConfiguration(_applicationConfig.GeneralSettings);
        }

        /// <summary>
        /// Initializes components and establishes connections
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task that completes when initialization and connection are done</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Set preferred console window size
            SetupConsoleWindow();

            // Start console size change tracking
            _consoleWindowManager.StartSizeChangeTracking((width, height) =>
            {
                // Update preferences and save asynchronously (fire-and-forget)
                _ = UpdateUserPreferencesAsync(prefs =>
                {
                    prefs.PreferredConsoleWidth = width;
                    prefs.PreferredConsoleHeight = height;
                });
            });

            await InitializeTransformationEngine();

            // Start watching application config file for hot reload
            _appConfigWatcher.StartWatching(_configManager.ApplicationConfigPath);
            _logger.Info("Started watching application config file for hot reload: {0}", _configManager.ApplicationConfigPath);

            // Initialize clients directly during startup
            _logger.Info("Attempting initial client connections...");
            await _vtubeStudioPCClient.TryInitializeAsync(cancellationToken);
            await _vtubeStudioPhoneClient.TryInitializeAsync(cancellationToken);

            // Register keyboard shortcuts
            RegisterKeyboardShortcuts();

            // Attempt to synchronize VTube Studio parameters (non-fatal if it fails)
            var parameterSyncSuccess = await TrySynchronizeParametersAsync(cancellationToken);
            if (!parameterSyncSuccess)
            {
                _logger.Warning("Parameter synchronization failed during initialization, will retry during recovery");
            }

            _logger.Info("Application initialized successfully");
        }

        /// <summary>
        /// Sets up the console window with preferred dimensions
        /// </summary>
        private void SetupConsoleWindow()
        {
            try
            {
                var currentSize = _consoleWindowManager.GetCurrentSize();
                _logger.Info("Current console size: {0}x{1}", currentSize.width, currentSize.height);

                bool success = _consoleWindowManager.SetConsoleSize(_userPreferences.PreferredConsoleWidth, _userPreferences.PreferredConsoleHeight);
                if (success)
                {
                    _logger.Info("Console window resized to preferred size: {0}x{1}", _userPreferences.PreferredConsoleWidth, _userPreferences.PreferredConsoleHeight);
                }
                else
                {
                    _logger.Warning("Failed to resize console window to preferred size. Using current size: {0}x{1}",
                        currentSize.width, currentSize.height);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error setting up console window", ex);
            }
        }

        /// <summary>
        /// Starts the data flow between components and runs until cancelled
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task that completes when the orchestrator is stopped</returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Starting application...");

            try
            {
                SubscribeToEvents();
                await RunUntilCancelled(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Operation was canceled, shutting down gracefully...");
            }
            finally
            {
                await PerformCleanup(cancellationToken);
            }

            _logger.Info("Application stopped");
        }

        private async Task InitializeTransformationEngine()
        {
            await _transformationEngine.LoadRulesAsync();
        }

        /// <summary>
        /// Attempts to synchronize VTube Studio parameters based on loaded transformation rules
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if synchronization was successful, false otherwise</returns>
        private async Task<bool> TrySynchronizeParametersAsync(CancellationToken cancellationToken)
        {
            var requiredParameters = _transformationEngine.GetParameterDefinitions();
            var success = await _parameterManager.TrySynchronizeParametersAsync(requiredParameters, cancellationToken);

            if (success)
            {
                _logger.Info("VTube Studio parameters synchronized successfully");
            }
            else
            {
                _logger.Warning("Failed to synchronize VTube Studio parameters");
            }

            return success;
        }

        private void SubscribeToEvents()
        {
            _vtubeStudioPhoneClient.TrackingDataReceived += OnTrackingDataReceived;
            _appConfigWatcher.FileChanged += OnApplicationConfigChanged;
        }

        private void UnsubscribeFromEvents()
        {
            _vtubeStudioPhoneClient.TrackingDataReceived -= OnTrackingDataReceived;
            _appConfigWatcher.FileChanged -= OnApplicationConfigChanged;
        }

        private async Task RunUntilCancelled(CancellationToken cancellationToken)
        {
            _logger.Info("Starting main application loop...");

            // Initialize timing variables
            var nextRequestTime = DateTime.UtcNow;
            var nextStatusUpdateTime = DateTime.UtcNow;

            _modeManager.Clear();

            // Send initial tracking request                
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRecoveryIfNeeded(cancellationToken);
                    nextRequestTime = await ProcessTrackingRequestIfNeeded(nextRequestTime, cancellationToken);
                    var dataReceived = await ProcessDataReceiving(cancellationToken);
                    _keyboardInputHandler.CheckForKeyboardInput();
                    _consoleWindowManager.ProcessSizeChanges();
                    nextStatusUpdateTime = ProcessConsoleUpdateIfNeeded(nextStatusUpdateTime);
                    await ProcessIdleDelayIfNeeded(dataReceived, cancellationToken);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    await HandleMainLoopError(ex, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Processes recovery attempt if it's time to do so
        /// </summary>
        private async Task ProcessRecoveryIfNeeded(CancellationToken cancellationToken)
        {
            if (DateTime.UtcNow >= _nextRecoveryAttempt)
            {
                var recoveryAttempted = await AttemptRecoveryAsync(cancellationToken);
                if (recoveryAttempted)
                {
                    _logger.Info("Recovery attempt completed");
                }
                _nextRecoveryAttempt = DateTime.UtcNow.Add(_recoveryPolicy.GetNextDelay());
            }
        }

        /// <summary>
        /// Processes tracking request if it's time to send one
        /// </summary>
        private async Task<DateTime> ProcessTrackingRequestIfNeeded(DateTime nextRequestTime, CancellationToken cancellationToken)
        {
            if (DateTime.UtcNow >= nextRequestTime)
            {
                // Only send requests if the phone client is healthy
                var phoneStats = _vtubeStudioPhoneClient.GetServiceStats();
                if (phoneStats.IsHealthy)
                {
                    await _vtubeStudioPhoneClient.SendTrackingRequestAsync();
                }

                // Set the next request time based on phone configuration
                return DateTime.UtcNow.AddSeconds(_phoneConfig.RequestIntervalSeconds);
            }

            return nextRequestTime;
        }

        /// <summary>
        /// Processes data receiving from the phone client
        /// </summary>
        private async Task<bool> ProcessDataReceiving(CancellationToken cancellationToken)
        {
            return await _vtubeStudioPhoneClient.ReceiveResponseAsync(cancellationToken);
        }

        /// <summary>
        /// Processes console status update if it's time to do so
        /// </summary>
        private DateTime ProcessConsoleUpdateIfNeeded(DateTime nextStatusUpdateTime)
        {
            if (DateTime.UtcNow >= nextStatusUpdateTime)
            {
                UpdateConsoleStatus();
                return DateTime.UtcNow.AddSeconds(CONSOLE_UPDATE_INTERVAL_SECONDS);
            }

            return nextStatusUpdateTime;
        }

        /// <summary>
        /// Processes idle delay if no data was received to prevent CPU spinning
        /// </summary>
        private static async Task ProcessIdleDelayIfNeeded(bool dataReceived, CancellationToken cancellationToken)
        {
            if (!dataReceived)
            {
                await Task.Delay(10, cancellationToken);
            }
        }

        /// <summary>
        /// Handles errors that occur in the main application loop
        /// </summary>
        private async Task HandleMainLoopError(Exception ex, CancellationToken cancellationToken)
        {
            _logger.Error("Error in application loop: {0}", ex.Message);
            await Task.Delay(_phoneConfig.ErrorDelayMs, cancellationToken);
        }

        private async Task PerformCleanup(CancellationToken cancellationToken)
        {
            UnsubscribeFromEvents();
            await CloseVTubeStudioConnection();

            // Console size is now preserved between application runs
            // No restoration needed
        }

        private async Task CloseVTubeStudioConnection()
        {
            try
            {
                await _vtubeStudioPCClient.CloseAsync(
                    System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                    "Application shutting down",
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error closing VTube Studio connection", ex);
            }
        }

        /// <summary>
        /// Updates the console with the current status of all components
        /// </summary>
        private void UpdateConsoleStatus()
        {
            try
            {
                // Get statistics from all components
                var transformationStats = _transformationEngine.GetServiceStats();
                var phoneStats = _vtubeStudioPhoneClient.GetServiceStats();
                var pcStats = _vtubeStudioPCClient.GetServiceStats();

                // Create a list of all service stats to display
                var allStats = new List<IServiceStats>();

                // Add stats to the list - transformation engine first, then clients
                if (transformationStats != null) allStats.Add(transformationStats);
                if (phoneStats != null) allStats.Add(phoneStats);
                if (pcStats != null) allStats.Add(pcStats);

                // Delegate to mode manager for rendering
                _modeManager.Update(allStats);
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error updating console status", ex);
            }
        }

        /// <summary>
        /// Reloads the transformation configuration
        /// </summary>
        private async Task ReloadTransformationConfig()
        {
            try
            {
                _logger.Info("Reloading transformation config...");

                // Use a lock or semaphore here if there are concurrency concerns
                await InitializeTransformationEngine();

                // Reset color service initialization flag so it will be reinitialized with new config
                _colorServiceInitialized = false;
                _logger.Debug("Color service initialization flag reset for config reload");

                // Attempt to synchronize VTube Studio parameters (non-fatal if it fails)
                var parameterSyncSuccess = await TrySynchronizeParametersAsync(CancellationToken.None);
                if (!parameterSyncSuccess)
                {
                    _logger.Warning("Parameter synchronization failed during config reload");
                }

                _logger.Info("Transformation config reloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error reloading transformation config", ex);
            }
        }

        /// <summary>
        /// Opens the appropriate configuration file in external editor based on current context
        /// </summary>
        private async Task OpenConfigInEditor()
        {
            // Delegate to mode manager to forward to the active renderer
            await _modeManager.TryOpenActiveModeInEditorAsync();
        }

        /// <summary>
        /// Handles tracking data received from the iPhone
        /// </summary>
        private async void OnTrackingDataReceived(object? sender, PhoneTrackingInfo trackingData)
        {
            try
            {
                if (trackingData == null)
                {
                    return;
                }

                // Initialize color service on first successful tracking data with blend shapes
                InitializeColorServiceIfNeeded(trackingData);

                // Transform tracking data
                PCTrackingInfo pcTrackingInfo = _transformationEngine.TransformData(trackingData);

                // Copy the FaceFound property from the original data
                pcTrackingInfo.FaceFound = trackingData.FaceFound;

                // Send to VTube Studio if PC client is healthy
                var pcStats = _vtubeStudioPCClient.GetServiceStats();
                if (pcStats.IsHealthy)
                {
                    await _vtubeStudioPCClient.SendTrackingAsync(pcTrackingInfo, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error processing tracking data", ex);
            }
        }

        /// <summary>
        /// Handles application configuration file changes
        /// </summary>
        private async void OnApplicationConfigChanged(object? sender, FileChangeEventArgs e)
        {
            try
            {
                _logger.Info("Application configuration file changed: {0}", e.FilePath);

                // Reload the application configuration
                var newApplicationConfig = await _configManager.LoadApplicationConfigAsync();

                // Update the internal reference
                _applicationConfig = newApplicationConfig;

                // Reload shortcut configuration with new settings
                _shortcutConfigurationManager.LoadFromConfiguration(newApplicationConfig.GeneralSettings);

                // Re-register keyboard shortcuts with new configuration
                RegisterKeyboardShortcuts();

                _logger.Info("Application configuration reloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error reloading application configuration", ex);
            }
        }

        /// <summary>
        /// Initializes the color service with transformation expressions and blend shape names
        /// from the first successful tracking data. This is called once per application run.
        /// </summary>
        /// <param name="trackingData">Phone tracking data containing blend shapes</param>
        private void InitializeColorServiceIfNeeded(PhoneTrackingInfo trackingData)
        {
            // Only initialize once and only if we have valid tracking data with blend shapes
            if (_colorServiceInitialized || trackingData?.BlendShapes == null || trackingData.BlendShapes.Count == 0)
            {
                return;
            }

            try
            {
                // Get calculated parameter names from the transformation engine
                var calculatedParameterNames = _transformationEngine.GetParameterDefinitions()
                    .Select(p => p.Name)
                    .Where(name => !string.IsNullOrEmpty(name));

                // Extract blend shape names from the tracking data
                var blendShapeNames = trackingData.BlendShapes.Select(bs => bs.Key).Where(key => !string.IsNullOrEmpty(key));

                // Initialize the color service
                _colorService.InitializeFromConfiguration(blendShapeNames, calculatedParameterNames);
                _colorServiceInitialized = true;

                _logger.Debug($"Color service initialized with {calculatedParameterNames.Count()} calculated parameters and {blendShapeNames.Count()} blend shapes");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to initialize color service: {ex.Message}");
                // Color service failure is not critical - continue without coloring
            }
        }

        /// <summary>
        /// Registers the keyboard shortcuts for the application based on configuration
        /// </summary>
        private void RegisterKeyboardShortcuts()
        {
            var mappedShortcuts = _shortcutConfigurationManager.GetMappedShortcuts();

            // Register each configured shortcut
            foreach (var (action, shortcut) in mappedShortcuts)
            {
                if (shortcut == null)
                {
                    _logger.Debug("Skipping disabled shortcut for action: {0}", action);
                    continue;
                }

                var actionMethod = GetActionMethod(action);
                var description = GetActionDescription(action);

                if (actionMethod != null)
                {
                    _keyboardInputHandler.RegisterShortcut(shortcut.Key, shortcut.Modifiers, actionMethod, description);
                    _logger.Debug("Registered shortcut {0}+{1} for action: {2}", shortcut.Modifiers, shortcut.Key, action);
                }
            }

            // Log any configuration issues using new status system
            var incorrectShortcuts = _shortcutConfigurationManager.GetIncorrectShortcuts();
            if (incorrectShortcuts.Count > 0)
            {
                var invalidActions = incorrectShortcuts.Keys.Select(action => $"{action}: {incorrectShortcuts[action]}");
                _logger.Warning("Invalid shortcut configurations detected: {0}", string.Join(", ", invalidActions));
            }
        }

        /// <summary>
        /// Gets the action method for a specific shortcut action
        /// </summary>
        private Action? GetActionMethod(ShortcutAction action)
        {
            return action switch
            {
                ShortcutAction.CycleTransformationEngineVerbosity => () =>
                {
                    var transformationFormatter = _modeManager.MainStatusRenderer.GetFormatter<TransformationEngineInfo>();
                    var newVerbosity = transformationFormatter?.CycleVerbosity();
                    if (newVerbosity.HasValue)
                    {
                        _ = UpdateUserPreferencesAsync(prefs => prefs.TransformationEngineVerbosity = newVerbosity.Value);
                    }
                }
                ,
                ShortcutAction.CyclePCClientVerbosity => () =>
                {
                    var pcFormatter = _modeManager.MainStatusRenderer.GetFormatter<PCTrackingInfo>();
                    var newVerbosity = pcFormatter?.CycleVerbosity();
                    if (newVerbosity.HasValue)
                    {
                        _ = UpdateUserPreferencesAsync(prefs => prefs.PCClientVerbosity = newVerbosity.Value);
                    }
                }
                ,
                ShortcutAction.CyclePhoneClientVerbosity => () =>
                {
                    var phoneFormatter = _modeManager.MainStatusRenderer.GetFormatter<PhoneTrackingInfo>();
                    var newVerbosity = phoneFormatter?.CycleVerbosity();
                    if (newVerbosity.HasValue)
                    {
                        _ = UpdateUserPreferencesAsync(prefs => prefs.PhoneClientVerbosity = newVerbosity.Value);
                    }
                }
                ,
                ShortcutAction.ReloadTransformationConfig => () => _ = ReloadTransformationConfig(),
                ShortcutAction.OpenConfigInEditor => () => _ = OpenConfigInEditor(),
                ShortcutAction.ShowSystemHelp => () => _modeManager.Toggle(ConsoleMode.SystemHelp),
                ShortcutAction.ShowNetworkStatus => () => _modeManager.Toggle(ConsoleMode.NetworkStatus),
                _ => null
            };
        }

        /// <summary>
        /// Gets the description for a specific shortcut action using Description attributes
        /// </summary>
        private static string GetActionDescription(ShortcutAction action)
        {
            return AttributeHelper.GetDescription(action);
        }

        /// <summary>
        /// Updates user preferences and saves them asynchronously.
        /// Fire-and-forget operation with error logging.
        /// </summary>
        private async Task UpdateUserPreferencesAsync(Action<UserPreferences> updateAction)
        {
            try
            {
                // Apply the update directly to the DI instance
                updateAction(_userPreferences);

                // Save to file (fire-and-forget)
                await _configManager.SaveUserPreferencesAsync(_userPreferences);

                _logger.Debug("User preferences updated and saved successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save user preferences: {ex.Message}");
                // Continue execution - preferences saving failure is not critical
            }
        }

        /// <summary>
        /// Attempts to recover unhealthy components
        /// </summary>
        private async Task<bool> AttemptRecoveryAsync(CancellationToken cancellationToken)
        {
            bool recoveryAttempted = false;
            bool pcClientRecovered = false;

            // Check PC client health
            var pcStats = _vtubeStudioPCClient.GetServiceStats();
            if (!pcStats.IsHealthy)
            {
                _logger.Info("Attempting to recover PC client...");
                await _vtubeStudioPCClient.TryInitializeAsync(cancellationToken);
                recoveryAttempted = true;

                // Check if PC client recovery was successful
                var newPcStats = _vtubeStudioPCClient.GetServiceStats();
                pcClientRecovered = newPcStats.IsHealthy;
            }

            // Check Phone client health
            var phoneStats = _vtubeStudioPhoneClient.GetServiceStats();
            if (!phoneStats.IsHealthy)
            {
                _logger.Info("Attempting to recover Phone client...");
                await _vtubeStudioPhoneClient.TryInitializeAsync(cancellationToken);
                recoveryAttempted = true;
            }

            // If PC client was successfully recovered, try to synchronize parameters
            if (pcClientRecovered)
            {
                _logger.Info("PC client recovered successfully, attempting parameter synchronization...");
                var parameterSyncSuccess = await TrySynchronizeParametersAsync(cancellationToken);
                if (!parameterSyncSuccess)
                {
                    _logger.Warning("Parameter synchronization failed after PC client recovery");
                }
            }

            return recoveryAttempted;
        }

        /// <summary>
        /// Disposes managed and unmanaged resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed and unmanaged resources
        /// </summary>
        /// <param name="disposing">Whether this is being called from Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Console window manager will be disposed by DI container
                    // which will automatically stop size tracking and restore window size
                    _vtubeStudioPCClient.Dispose();
                    _vtubeStudioPhoneClient.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}