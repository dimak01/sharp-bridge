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
        private string _iphoneIp;
        
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
        /// <param name="iphoneIp">IP address of the iPhone</param>
        /// <param name="transformConfigPath">Path to the transformation configuration file</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task that completes when initialization and connection are done</returns>
        public async Task InitializeAsync(string iphoneIp, string transformConfigPath, CancellationToken cancellationToken)
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(iphoneIp))
            {
                throw new ArgumentException("iPhone IP address cannot be null or empty", nameof(iphoneIp));
            }
            
            if (string.IsNullOrWhiteSpace(transformConfigPath))
            {
                throw new ArgumentException("Transform configuration path cannot be null or empty", nameof(transformConfigPath));
            }
            
            if (!File.Exists(transformConfigPath))
            {
                throw new FileNotFoundException($"Transform configuration file not found: {transformConfigPath}");
            }
            
            // Store iPhone IP for later use
            _iphoneIp = iphoneIp;
            
            // Log initialization
            Console.WriteLine($"Initializing application with iPhone IP: {iphoneIp}");
            Console.WriteLine($"Using transformation config: {transformConfigPath}");
            
            // Initialize transformation engine
            await _transformationEngine.LoadRulesAsync(transformConfigPath);
            
            // Attempt to discover VTube Studio port
            Console.WriteLine("Discovering VTube Studio port...");
            int port = await _vtubeStudioPCClient.DiscoverPortAsync(cancellationToken);
            if (port <= 0)
            {
                throw new InvalidOperationException("Could not discover VTube Studio port. Is VTube Studio running?");
            }
            Console.WriteLine($"Discovered VTube Studio on port {port}");
            
            // Connect to VTube Studio
            Console.WriteLine("Connecting to VTube Studio...");
            await _vtubeStudioPCClient.ConnectAsync(cancellationToken);
            
            // Authenticate with VTube Studio
            Console.WriteLine("Authenticating with VTube Studio...");
            bool authenticated = await _vtubeStudioPCClient.AuthenticateAsync(cancellationToken);
            if (!authenticated)
            {
                throw new InvalidOperationException("Could not authenticate with VTube Studio. Check if authentication was approved.");
            }
            Console.WriteLine("Successfully authenticated with VTube Studio");
            
            // Subscribe to the tracking data events
            _vtubeStudioPhoneClient.TrackingDataReceived += OnTrackingDataReceived;
            
            // Log successful initialization
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
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ApplicationOrchestrator must be initialized before running");
            }
            
            Console.WriteLine("Starting application...");
            
            try
            {
                // Start the iPhone tracking receiver
                await Task.WhenAll(
                    _vtubeStudioPhoneClient.RunAsync(_iphoneIp, cancellationToken),
                    // Wait indefinitely (until cancellation) using a simple way
                    Task.Delay(Timeout.Infinite, cancellationToken)
                );
            }
            catch (OperationCanceledException)
            {
                // This is expected, just clean up
                Console.WriteLine("Operation was canceled, shutting down...");
            }
            finally
            {
                // Clean up event handlers
                _vtubeStudioPhoneClient.TrackingDataReceived -= OnTrackingDataReceived;
                
                // Gracefully close connections
                try
                {
                    await _vtubeStudioPCClient.CloseAsync(
                        System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                        "Application shutting down",
                        CancellationToken.None); // Use a new token to ensure this completes
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing VTube Studio connection: {ex.Message}");
                }
            }
            
            Console.WriteLine("Application stopped");
        }

        /// <summary>
        /// Handles tracking data received from the iPhone
        /// </summary>
        private async void OnTrackingDataReceived(object sender, TrackingResponse trackingData)
        {
            try
            {
                // Skip if no data or face not found
                if (trackingData == null)
                {
                    return;
                }
                
                // Transform the tracking data
                IEnumerable<TrackingParam> parameters = _transformationEngine.TransformData(trackingData);
                
                // Send to VTube Studio if connection is open
                if (_vtubeStudioPCClient.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    await _vtubeStudioPCClient.SendTrackingAsync(
                        parameters, 
                        trackingData.FaceFound, 
                        CancellationToken.None); // Using None as this should complete quickly
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
                    // Dispose managed resources
                    _vtubeStudioPCClient?.Dispose();
                    _vtubeStudioPhoneClient?.Dispose();
                }
                
                _isDisposed = true;
            }
        }
    }
} 