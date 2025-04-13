using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;

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
        public static async Task Main(string[] args)
        {
            // Set up console to reduce flickering
            Console.CursorVisible = false;
            
            Console.WriteLine("Sharp Bridge - VTube Studio iPhone to PC Bridge");
            Console.WriteLine("Inspired by: https://github.com/ovROG/rusty-bridge");
            
            string iphoneIp = "192.168.1.178"; // Default IP - DO NOT CHANGE!!!
            
            // Simple command-line argument parsing
            if (args.Length > 0)
            {
                iphoneIp = args[0];
            }
            else
            {
                Console.Write("Enter your iPhone's IP address: ");
                Console.CursorVisible = true; // Show cursor for input
                string input = Console.ReadLine();
                Console.CursorVisible = false; // Hide cursor again
                if (!string.IsNullOrWhiteSpace(input))
                {
                    iphoneIp = input;
                }
            }
            
            Console.WriteLine($"Connecting to iPhone at IP: {iphoneIp}");
            Console.WriteLine("Press Ctrl+C to exit...");
            
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested, cleaning up...");
            };
            
            try
            {
                // Create the config with our static port
                var config = new TrackingReceiverConfig
                {
                    IphoneIpAddress = iphoneIp,
                    IphonePort = 21412, // Default VTube Studio port
                    // Using default values matched to Rust implementation
                };
                
                // Create the UDP client with the specific port from config
                Console.WriteLine($"Using local port {config.LocalPort} for listening");
                var udpClient = new UdpClient(config.LocalPort);
                
                // Create and wire up our tracking receiver
                var clientWrapper = new UdpClientWrapper(udpClient);
                using var receiver = new TrackingReceiver(clientWrapper, config);
                
                // Create performance monitor with built-in console display
                using var perfMonitor = new PerformanceMonitor(uiUpdateIntervalMs: 250);
                
                // Subscribe to tracking data events
                receiver.TrackingDataReceived += (sender, data) => perfMonitor.ProcessFrame(data);
                
                // Clear the console before starting any output to ensure clean display
                Console.Clear();
                
                // Show a simple waiting message before giving control to performance monitor
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