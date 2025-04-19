using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
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
        
        private bool _isInitialized;
        private bool _isDisposed;
        private string _status = "Initializing";
        private string _transformConfigPath; // Store the config path for reloading
        
        /// <summary>
        /// Creates a new instance of the ApplicationOrchestrator
        /// </summary>
        /// <param name="vtubeStudioPCClient">The VTube Studio PC client</param>
        /// <param name="vtubeStudioPhoneClient">The VTube Studio phone client</param>
        /// <param name="transformationEngine">The transformation engine</param>
        /// <param name="phoneConfig">Configuration for the phone client</param>
        /// <param name="logger">Application logger</param>
        /// <param name="consoleRenderer">Console renderer for displaying status</param>
        public ApplicationOrchestrator(
            IVTubeStudioPCClient vtubeStudioPCClient,
            IVTubeStudioPhoneClient vtubeStudioPhoneClient,
            ITransformationEngine transformationEngine,
            VTubeStudioPhoneClientConfig phoneConfig,
            IAppLogger logger,
            IConsoleRenderer consoleRenderer)
        {
            _vtubeStudioPCClient = vtubeStudioPCClient ?? throw new ArgumentNullException(nameof(vtubeStudioPCClient));
            _vtubeStudioPhoneClient = vtubeStudioPhoneClient ?? throw new ArgumentNullException(nameof(vtubeStudioPhoneClient));
            _transformationEngine = transformationEngine ?? throw new ArgumentNullException(nameof(transformationEngine));
            _phoneConfig = phoneConfig ?? throw new ArgumentNullException(nameof(phoneConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _consoleRenderer = consoleRenderer ?? throw new ArgumentNullException(nameof(consoleRenderer));
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
            
            _logger.Info("Using transformation config: {0}", transformConfigPath);
            
            // Store the config path for reloading
            _transformConfigPath = transformConfigPath;
            
            await InitializeTransformationEngine(transformConfigPath);
            await DiscoverAndConnectToVTubeStudio(cancellationToken);
            await AuthenticateWithVTubeStudio(cancellationToken);
            
            _logger.Info("Application initialized successfully");
            _isInitialized = true;
        }

        /// <summary>
        /// Starts the data flow between components and runs until cancelled
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task that completes when the orchestrator is stopped</returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            EnsureInitialized();
            
            _logger.Info("Starting application...");
            
            try
            {
                SubscribeToEvents();
                await RunUntilCancelled(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Operation was canceled, shutting down...");
            }
            finally
            {
                await PerformCleanup(cancellationToken);
            }
            
            _logger.Info("Application stopped");
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ApplicationOrchestrator must be initialized before running");
            }
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
        
        private async Task DiscoverAndConnectToVTubeStudio(CancellationToken cancellationToken)
        {
            _logger.Info("Discovering VTube Studio port...");
            int port = await _vtubeStudioPCClient.DiscoverPortAsync(cancellationToken);
            if (port <= 0)
            {
                throw new InvalidOperationException("Could not discover VTube Studio port. Is VTube Studio running?");
            }
            _logger.Info("Discovered VTube Studio on port {0}", port);
            
            _logger.Info("Connecting to VTube Studio...");
            await _vtubeStudioPCClient.ConnectAsync(cancellationToken);
        }
        
        private async Task AuthenticateWithVTubeStudio(CancellationToken cancellationToken)
        {
            _logger.Info("Authenticating with VTube Studio...");
            bool authenticated = await _vtubeStudioPCClient.AuthenticateAsync(cancellationToken);
            if (!authenticated)
            {
                throw new InvalidOperationException("Could not authenticate with VTube Studio. Check if authentication was approved.");
            }
            _logger.Info("Successfully authenticated with VTube Studio");
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
            _status = "Running";
            _logger.Info("Starting main application loop...");
            
            // Initialize timing variables
            var nextRequestTime = DateTime.UtcNow;
            var nextStatusUpdateTime = DateTime.UtcNow;
            
            try
            {
                _consoleRenderer.ClearConsole();

                // Send initial tracking request
                await _vtubeStudioPhoneClient.SendTrackingRequestAsync();
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Check if it's time to send another tracking request
                        if (DateTime.UtcNow >= nextRequestTime)
                        {
                            await _vtubeStudioPhoneClient.SendTrackingRequestAsync();
                            
                            // Set the next request time based on phone configuration
                            double requestIntervalSeconds = _phoneConfig?.RequestIntervalSeconds ?? 5.0;
                            nextRequestTime = DateTime.UtcNow.AddSeconds(requestIntervalSeconds);
                        }
                        
                        // Try to receive tracking data
                        bool dataReceived = await _vtubeStudioPhoneClient.ReceiveResponseAsync(cancellationToken);
                        
                        
                        // Check for keyboard input every tick of the loop
                        CheckForKeyboardInput();

                        // Check if it's time to update console status
                        if (DateTime.UtcNow >= nextStatusUpdateTime)
                        {
                            UpdateConsoleStatus();
                            
                            nextStatusUpdateTime = DateTime.UtcNow.AddSeconds(0.1f); // Update status every 0.1 seconds
                        }
                        
                        // If no data was received, add a small delay to prevent CPU spinning
                        if (!dataReceived)
                        {
                            await Task.Delay(10, cancellationToken);
                        }
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                    {
                        _status = $"Error: {ex.Message}";
                        _logger.Error("Error in application loop: {0}", ex.Message);
                        await Task.Delay(1000, cancellationToken); // Add delay on error before retrying
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _status = "Cancellation requested";
                _logger.Info("Operation was canceled, shutting down...");
            }
            finally
            {
                _status = "Stopped";
            }
        }
        
        private async Task PerformCleanup(CancellationToken cancellationToken)
        {
            UnsubscribeFromEvents();
            await CloseVTubeStudioConnection();
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
                // Get statistics from both clients
                var phoneStats = _vtubeStudioPhoneClient.GetServiceStats();
                var pcStats = _vtubeStudioPCClient.GetServiceStats();
                
                // Create a list of all service stats to display
                var allStats = new List<IServiceStats>();
                
                // Add stats to the list - this works because of covariance with the 'out' parameter in IServiceStats<out T>
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
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true); // true means don't echo to screen
                
                // Check for Alt+P combination for PC client
                if (keyInfo.Key == ConsoleKey.P && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt))
                {
                    // Cycle verbosity specifically for PC client formatter
                    var pcFormatter = _consoleRenderer.GetFormatter<PCTrackingInfo>();
                    if (pcFormatter != null)
                    {
                        pcFormatter.CycleVerbosity();
                        _status = $"PC client verbosity changed to {pcFormatter.CurrentVerbosity}";
                    }
                }
                
                // Check for Alt+O combination for Phone client
                if (keyInfo.Key == ConsoleKey.O && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt))
                {
                    // Cycle verbosity specifically for Phone client formatter
                    var phoneFormatter = _consoleRenderer.GetFormatter<PhoneTrackingInfo>();
                    if (phoneFormatter != null)
                    {
                        phoneFormatter.CycleVerbosity();
                        _status = $"Phone client verbosity changed to {phoneFormatter.CurrentVerbosity}";
                    }
                }
                
                // Check for Alt+R combination to reload transformation config
                if (keyInfo.Key == ConsoleKey.K && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt))
                {
                    // Reload transformation config
                    ReloadTransformationConfig();
                }
            }
        }
        
        /// <summary>
        /// Reloads the transformation configuration
        /// </summary>
        private async void ReloadTransformationConfig()
        {
            try
            {
                if (string.IsNullOrEmpty(_transformConfigPath))
                {
                    _status = "Error: No transformation config path available for reload";
                    return;
                }
                
                _status = "Reloading transformation config...";
                _logger.Info("Reloading transformation config...");
                
                // Use a lock or semaphore here if there are concurrency concerns
                await InitializeTransformationEngine(_transformConfigPath);
                
                _status = "Transformation config reloaded successfully";
                _logger.Info("Transformation config reloaded successfully");
            }
            catch (Exception ex)
            {
                _status = $"Error reloading config: {ex.Message}";
                _logger.ErrorWithException("Error reloading transformation config", ex);
            }
        }

        /// <summary>
        /// Handles tracking data received from the iPhone
        /// </summary>
        private async void OnTrackingDataReceived(object sender, PhoneTrackingInfo trackingData)
        {
            try
            {
                if (trackingData == null)
                {
                    return;
                }
                
                // Transform tracking data
                IEnumerable<TrackingParam> parameters = _transformationEngine.TransformData(trackingData);
                
                // Create PCTrackingInfo to encapsulate the transformed data
                var pcTrackingInfo = new PCTrackingInfo
                {
                    Parameters = parameters,
                    FaceFound = trackingData.FaceFound
                };
                
                // Send to VTube Studio if connection is open
                if (_vtubeStudioPCClient.State == System.Net.WebSockets.WebSocketState.Open)
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
                    _vtubeStudioPCClient?.Dispose();
                    _vtubeStudioPhoneClient?.Dispose();
                }
                
                _isDisposed = true;
            }
        }
    }
} 