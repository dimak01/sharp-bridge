using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
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
        
        private bool _isInitialized;
        private bool _isDisposed;
        
        /// <summary>
        /// Creates a new instance of the ApplicationOrchestrator
        /// </summary>
        /// <param name="vtubeStudioPCClient">The VTube Studio PC client</param>
        /// <param name="vtubeStudioPhoneClient">The VTube Studio phone client</param>
        /// <param name="transformationEngine">The transformation engine</param>
        public ApplicationOrchestrator(
            IVTubeStudioPCClient vtubeStudioPCClient,
            IVTubeStudioPhoneClient vtubeStudioPhoneClient,
            ITransformationEngine transformationEngine)
        {
            _vtubeStudioPCClient = vtubeStudioPCClient ?? throw new ArgumentNullException(nameof(vtubeStudioPCClient));
            _vtubeStudioPhoneClient = vtubeStudioPhoneClient ?? throw new ArgumentNullException(nameof(vtubeStudioPhoneClient));
            _transformationEngine = transformationEngine ?? throw new ArgumentNullException(nameof(transformationEngine));
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
            
            Console.WriteLine($"Using transformation config: {transformConfigPath}");
            
            await InitializeTransformationEngine(transformConfigPath);
            await DiscoverAndConnectToVTubeStudio(cancellationToken);
            await AuthenticateWithVTubeStudio(cancellationToken);
            
            Console.WriteLine("Application initialized successfully");
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
            
            Console.WriteLine("Starting application...");
            
            try
            {
                SubscribeToEvents();
                await RunUntilCancelled(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was canceled, shutting down...");
            }
            finally
            {
                await PerformCleanup(cancellationToken);
            }
            
            Console.WriteLine("Application stopped");
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
            Console.WriteLine("Discovering VTube Studio port...");
            int port = await _vtubeStudioPCClient.DiscoverPortAsync(cancellationToken);
            if (port <= 0)
            {
                throw new InvalidOperationException("Could not discover VTube Studio port. Is VTube Studio running?");
            }
            Console.WriteLine($"Discovered VTube Studio on port {port}");
            
            Console.WriteLine("Connecting to VTube Studio...");
            await _vtubeStudioPCClient.ConnectAsync(cancellationToken);
        }
        
        private async Task AuthenticateWithVTubeStudio(CancellationToken cancellationToken)
        {
            Console.WriteLine("Authenticating with VTube Studio...");
            bool authenticated = await _vtubeStudioPCClient.AuthenticateAsync(cancellationToken);
            if (!authenticated)
            {
                throw new InvalidOperationException("Could not authenticate with VTube Studio. Check if authentication was approved.");
            }
            Console.WriteLine("Successfully authenticated with VTube Studio");
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
            await Task.WhenAll(
                _vtubeStudioPhoneClient.RunAsync(cancellationToken),
                Task.Delay(Timeout.Infinite, cancellationToken)
            );
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
                Console.WriteLine($"Error closing VTube Studio connection: {ex.Message}");
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
                
                IEnumerable<TrackingParam> parameters = _transformationEngine.TransformData(trackingData);
                
                if (_vtubeStudioPCClient.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    await _vtubeStudioPCClient.SendTrackingAsync(
                        parameters, 
                        trackingData.FaceFound, 
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing tracking data: {ex.Message}");
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