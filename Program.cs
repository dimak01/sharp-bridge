using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using SharpBridge.Utilities;

namespace SharpBridge
{
    /// <summary>
    /// Main program entry point
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>An asynchronous task representing the execution</returns>
        public static async Task<int> Main(string[] args)
        {
            var commandLineParser = new CommandLineParser();
            return await commandLineParser.ParseAndRunAsync(args, RunApplicationAsync);
        }

        /// <summary>
        /// Runs the main application logic
        /// </summary>
        /// <param name="config">Application configuration</param>
        /// <param name="cts">Cancellation token source</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private static async Task RunApplicationAsync(TrackingReceiverConfig config, CancellationTokenSource cts)
        {
            Console.CursorVisible = false;
            
            DisplayWelcomeMessage();
            
            UdpClient udpClient = null;
            IUdpClientWrapper clientWrapper = null;
            TrackingReceiver trackingReceiver = null;
            
            try
            {
                (trackingReceiver, clientWrapper, udpClient) = CreateTrackingServices(config);
                ITrackingReceiver trackingService = trackingReceiver;
                
                using var perfMonitor = SetupPerformanceMonitoring(trackingService);
                DisplayConnectionInfo(config);
                
                var receiverTask = await StartTrackingAsync(trackingService, perfMonitor, cts.Token);
                await receiverTask;
            }
            catch (SocketException ex)
            {
                HandleSocketException(ex);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                CleanupResources(trackingReceiver, clientWrapper, udpClient);
            }
            
            Console.WriteLine("Application shutting down...");
        }
        
        /// <summary>
        /// Displays the welcome message
        /// </summary>
        private static void DisplayWelcomeMessage()
        {
            Console.WriteLine("Sharp Bridge - VTube Studio iPhone to PC Bridge");
            Console.WriteLine("Inspired by: https://github.com/ovROG/rusty-bridge");
        }
        
        /// <summary>
        /// Creates and initializes tracking services
        /// </summary>
        /// <param name="config">The tracking configuration</param>
        /// <returns>Tuple of initialized services</returns>
        private static (TrackingReceiver receiver, IUdpClientWrapper wrapper, UdpClient client) 
            CreateTrackingServices(TrackingReceiverConfig config)
        {
            Console.WriteLine($"Using local port {config.LocalPort} for listening");
            var udpClient = new UdpClient(config.LocalPort);
            var clientWrapper = new UdpClientWrapper(udpClient);
            var trackingReceiver = new TrackingReceiver(clientWrapper, config);
            
            return (trackingReceiver, clientWrapper, udpClient);
        }
        
        /// <summary>
        /// Sets up performance monitoring for tracking data
        /// </summary>
        /// <param name="trackingService">The tracking service to monitor</param>
        /// <returns>The configured performance monitor</returns>
        private static PerformanceMonitor SetupPerformanceMonitoring(ITrackingReceiver trackingService)
        {
            var perfMonitor = new PerformanceMonitor(uiUpdateIntervalMs: CommandLineDefaults.UiUpdateIntervalMs);
            trackingService.TrackingDataReceived += (sender, data) => perfMonitor.ProcessFrame(data);
            return perfMonitor;
        }
        
        /// <summary>
        /// Displays connection information and initial instructions
        /// </summary>
        /// <param name="config">The tracking configuration</param>
        private static void DisplayConnectionInfo(TrackingReceiverConfig config)
        {
            Console.Clear();
            Console.WriteLine($"Connecting to iPhone at IP: {config.IphoneIpAddress}");
            Console.WriteLine("Waiting for tracking data from iPhone VTube Studio...");
            Console.WriteLine($"IMPORTANT: Make sure port {config.LocalPort} UDP is allowed in your firewall!");
        }
        
        /// <summary>
        /// Starts tracking and monitoring services
        /// </summary>
        /// <param name="trackingService">The tracking service</param>
        /// <param name="perfMonitor">The performance monitor</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task that completes when tracking is cancelled</returns>
        private static async Task<Task> StartTrackingAsync(
            ITrackingReceiver trackingService, 
            PerformanceMonitor perfMonitor,
            CancellationToken cancellationToken)
        {
            var receiverTask = trackingService.RunAsync(cancellationToken);
            await Task.Delay(2000);
            
            Console.Clear();
            perfMonitor.Start();
            
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return receiverTask;
        }
        
        /// <summary>
        /// Handles socket exceptions with appropriate error messages
        /// </summary>
        /// <param name="ex">The socket exception</param>
        private static void HandleSocketException(SocketException ex)
        {
            Console.WriteLine($"Socket error: {ex.Message}");
            Console.WriteLine("This may be because the port is already in use or access is denied.");
            Console.WriteLine("Check if another instance is running or if you need administrative privileges.");
        }
        
        /// <summary>
        /// Cleans up all resources
        /// </summary>
        private static void CleanupResources(
            TrackingReceiver trackingReceiver, 
            IUdpClientWrapper clientWrapper, 
            UdpClient udpClient)
        {
            trackingReceiver?.Dispose();
            clientWrapper?.Dispose();
            udpClient?.Dispose();
            Console.CursorVisible = true;
        }
    }
} 