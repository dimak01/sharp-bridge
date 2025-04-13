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
            // Parse command-line arguments and get configuration
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
            // Set up console to reduce flickering
            Console.CursorVisible = false;
            
            Console.WriteLine("Sharp Bridge - VTube Studio iPhone to PC Bridge");
            Console.WriteLine("Inspired by: https://github.com/ovROG/rusty-bridge");
            
            try
            {
                // Create the UDP client with the specific port from config
                Console.WriteLine($"Using local port {config.LocalPort} for listening");
                var udpClient = new UdpClient(config.LocalPort);
                
                // Create and wire up our tracking receiver
                var clientWrapper = new UdpClientWrapper(udpClient);
                using var receiver = new TrackingReceiver(clientWrapper, config);
                
                // Create performance monitor with built-in console display
                using var perfMonitor = new PerformanceMonitor(uiUpdateIntervalMs: CommandLineDefaults.UiUpdateIntervalMs);
                
                // Subscribe to tracking data events
                receiver.TrackingDataReceived += (sender, data) => perfMonitor.ProcessFrame(data);
                
                // Clear the console before starting any output to ensure clean display
                Console.Clear();
                
                // Show a simple waiting message before giving control to performance monitor
                Console.WriteLine($"Connecting to iPhone at IP: {config.IphoneIpAddress}");
                Console.WriteLine("Waiting for tracking data from iPhone VTube Studio...");
                Console.WriteLine($"IMPORTANT: Make sure port {config.LocalPort} UDP is allowed in your firewall!");
                
                // Start the receiver in the background
                var receiverTask = receiver.RunAsync(cts.Token);
                
                // Allow time for initial messages to be seen
                await Task.Delay(2000);
                
                // Clear again before starting the performance monitor to avoid text overlap
                Console.Clear();
                
                // Start the performance monitor
                perfMonitor.Start();
                
                // Wait for cancellation
                await Task.Delay(Timeout.Infinite, cts.Token);
                
                // Wait for receiver to complete after cancellation
                await receiverTask;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error: {ex.Message}");
                Console.WriteLine("This may be because the port is already in use or access is denied.");
                Console.WriteLine("Check if another instance is running or if you need administrative privileges.");
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
                // Restore console state
                Console.CursorVisible = true;
            }
            
            Console.WriteLine("Application shutting down...");
        }
    }
} 