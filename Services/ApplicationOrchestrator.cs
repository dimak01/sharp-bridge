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
        private readonly IConsoleRenderer _consoleRenderer;
        private readonly IKeyboardInputHandler _keyboardInputHandler;
        private readonly IVTubeStudioPCParameterManager _parameterManager;
        private readonly IRecoveryPolicy _recoveryPolicy;
        private readonly IConsole _console;
        private readonly ConsoleWindowManager _consoleWindowManager;
        private readonly IParameterColorService _colorService;
        
        // Preferred console dimensions
        private const int PREFERRED_CONSOLE_WIDTH = 150;
        private const int PREFERRED_CONSOLE_HEIGHT = 60;
        
        private bool _isDisposed;
        private string _transformConfigPath = string.Empty; // Store the config path for reloading
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
        /// <param name="consoleRenderer">Console renderer for displaying status</param>
        /// <param name="keyboardInputHandler">Keyboard input handler</param>
        /// <param name="parameterManager">VTube Studio PC parameter manager</param>
        /// <param name="recoveryPolicy">Policy for determining recovery attempt timing</param>
        /// <param name="console">Console abstraction for window management</param>
        /// <param name="colorService">Parameter color service for colored console output</param>
        public ApplicationOrchestrator(
            IVTubeStudioPCClient vtubeStudioPCClient,
            IVTubeStudioPhoneClient vtubeStudioPhoneClient,
            ITransformationEngine transformationEngine,
            VTubeStudioPhoneClientConfig phoneConfig,
            IAppLogger logger,
            IConsoleRenderer consoleRenderer,
            IKeyboardInputHandler keyboardInputHandler,
            IVTubeStudioPCParameterManager parameterManager,
            IRecoveryPolicy recoveryPolicy,
            IConsole console,
            IParameterColorService colorService)
        {
            _vtubeStudioPCClient = vtubeStudioPCClient ?? throw new ArgumentNullException(nameof(vtubeStudioPCClient));
            _vtubeStudioPhoneClient = vtubeStudioPhoneClient ?? throw new ArgumentNullException(nameof(vtubeStudioPhoneClient));
            _transformationEngine = transformationEngine ?? throw new ArgumentNullException(nameof(transformationEngine));
            _phoneConfig = phoneConfig ?? throw new ArgumentNullException(nameof(phoneConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _consoleRenderer = consoleRenderer ?? throw new ArgumentNullException(nameof(consoleRenderer));
            _keyboardInputHandler = keyboardInputHandler ?? throw new ArgumentNullException(nameof(keyboardInputHandler));
            _parameterManager = parameterManager ?? throw new ArgumentNullException(nameof(parameterManager));
            _recoveryPolicy = recoveryPolicy ?? throw new ArgumentNullException(nameof(recoveryPolicy));
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
            
            // Initialize console window manager
            _consoleWindowManager = new ConsoleWindowManager(_console);
        }

        /// <summary>
        /// Initializes components and establishes connections
        /// </summary>
        /// <param name="transformConfigPath">Path to the transformation configuration file</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task that completes when initialization and connection are done</returns>
        public async Task InitializeAsync(string transformConfigPath, CancellationToken cancellationToken)
        {
            ValidateInitializationParameters(transformConfigPath);
            
            // Set preferred console window size
            SetupConsoleWindow();
            
            _logger.Info("Using transformation config: {0}", transformConfigPath);
            
            // Store the config path for reloading
            _transformConfigPath = transformConfigPath;
            
            await InitializeTransformationEngine(transformConfigPath);
            
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
                
                bool success = _consoleWindowManager.SetTemporarySize(PREFERRED_CONSOLE_WIDTH, PREFERRED_CONSOLE_HEIGHT);
                if (success)
                {
                    _logger.Info("Console window resized to preferred size: {0}x{1}", PREFERRED_CONSOLE_WIDTH, PREFERRED_CONSOLE_HEIGHT);
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

        private void ValidateInitializationParameters(string transformConfigPath)
        {
            if (string.IsNullOrWhiteSpace(transformConfigPath))
            {
                throw new ArgumentException("Transform configuration path cannot be null or empty", nameof(transformConfigPath));
            }
            
            if (!File.Exists(transformConfigPath))
            {
                throw new FileNotFoundException($"Transform configuration file not found: {transformConfigPath}");
            }
        }
        
        private async Task InitializeTransformationEngine(string transformConfigPath)
        {
            await _transformationEngine.LoadRulesAsync(transformConfigPath);
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
        }
        
        private void UnsubscribeFromEvents()
        {
            _vtubeStudioPhoneClient.TrackingDataReceived -= OnTrackingDataReceived;
        }
        
        private async Task RunUntilCancelled(CancellationToken cancellationToken)
        {
            _logger.Info("Starting main application loop...");
            
            // Initialize timing variables
            var nextRequestTime = DateTime.UtcNow;
            var nextStatusUpdateTime = DateTime.UtcNow;
            
            _consoleRenderer.ClearConsole();

            // Send initial tracking request                
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRecoveryIfNeeded(cancellationToken);
                    nextRequestTime = await ProcessTrackingRequestIfNeeded(nextRequestTime, cancellationToken);
                    var dataReceived = await ProcessDataReceiving(cancellationToken);
                    ProcessKeyboardInput();
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
        /// Processes keyboard input checking
        /// </summary>
        private void ProcessKeyboardInput()
        {
            CheckForKeyboardInput();
        }

        /// <summary>
        /// Processes console status update if it's time to do so
        /// </summary>
        private DateTime ProcessConsoleUpdateIfNeeded(DateTime nextStatusUpdateTime)
        {
            if (DateTime.UtcNow >= nextStatusUpdateTime)
            {
                UpdateConsoleStatus();
                return DateTime.UtcNow.AddSeconds(0.1f); // Update status every 0.1 seconds
            }
            
            return nextStatusUpdateTime;
        }

        /// <summary>
        /// Processes idle delay if no data was received to prevent CPU spinning
        /// </summary>
        private async Task ProcessIdleDelayIfNeeded(bool dataReceived, CancellationToken cancellationToken)
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
            
            // Restore original console window size
            try
            {
                _consoleWindowManager?.RestoreOriginalSize();
                _logger.Info("Console window size restored to original dimensions");
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error restoring console window size", ex);
            }
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
                
                // Display all stats using our new covariance-enabled Update method
                _consoleRenderer.Update(allStats);
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error updating console status", ex);
            }
        }

        /// <summary>
        /// Checks for keyboard input and handles key combinations
        /// </summary>
        private void CheckForKeyboardInput()
        {
            _keyboardInputHandler.CheckForKeyboardInput();
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
                await InitializeTransformationEngine(_transformConfigPath);
                
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
        /// Registers the keyboard shortcuts for the application
        /// </summary>
        private void RegisterKeyboardShortcuts()
        {
            // Register Alt+T to cycle Transformation Engine verbosity
            _keyboardInputHandler.RegisterShortcut(
                ConsoleKey.T, 
                ConsoleModifiers.Alt, 
                () => {
                    var transformationFormatter = _consoleRenderer.GetFormatter<TransformationEngineInfo>();
                    if (transformationFormatter != null)
                    {
                        transformationFormatter.CycleVerbosity();
                    }
                },
                "Cycle Transformation Engine verbosity"
            );
            
            // Register Alt+P to cycle PC client verbosity
            _keyboardInputHandler.RegisterShortcut(
                ConsoleKey.P, 
                ConsoleModifiers.Alt, 
                () => {
                    var pcFormatter = _consoleRenderer.GetFormatter<PCTrackingInfo>();
                    if (pcFormatter != null)
                    {
                        pcFormatter.CycleVerbosity();
                    }
                },
                "Cycle PC client verbosity"
            );
            
            // Register Alt+O to cycle Phone client verbosity
            _keyboardInputHandler.RegisterShortcut(
                ConsoleKey.O, 
                ConsoleModifiers.Alt, 
                () => {
                    var phoneFormatter = _consoleRenderer.GetFormatter<PhoneTrackingInfo>();
                    if (phoneFormatter != null)
                    {
                        phoneFormatter.CycleVerbosity();
                    }
                },
                "Cycle Phone client verbosity"
            );
            
            // Register Alt+K to reload transformation config
            _keyboardInputHandler.RegisterShortcut(
                ConsoleKey.K, 
                ConsoleModifiers.Alt, 
                () => _ = ReloadTransformationConfig(),
                "Reload transformation configuration"
            );
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
                    // Dispose console window manager first to restore window size
                    _consoleWindowManager?.Dispose();
                    
                    _vtubeStudioPCClient.Dispose();
                    _vtubeStudioPhoneClient.Dispose();
                }
                
                _isDisposed = true;
            }
        }
    }
} 