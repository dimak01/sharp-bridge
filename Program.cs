using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
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
            Console.WriteLine("Sharp Bridge - VTube Studio iPhone to PC Bridge");
            Console.WriteLine("Inspired by: https://github.com/ovROG/rusty-bridge");
            
            string iphoneIp = "192.168.1.178"; // Default IP
            
            // Simple command-line argument parsing
            if (args.Length > 0)
            {
                iphoneIp = args[0];
            }
            else
            {
                Console.Write("Enter your iPhone's IP address: ");
                string input = Console.ReadLine();
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
                // Create the UDP client
                var udpClient = new UdpClient(0); // 0 = Let OS assign a port
                var localEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
                var localPort = localEndPoint.Port;
                Console.WriteLine($"Listening on local port: {localPort}");
                
                // Create the config
                var config = new TrackingReceiverConfig
                {
                    IphoneIpAddress = iphoneIp,
                    IphonePort = 21412, // Default VTube Studio port
                    RequestIntervalSeconds = 5, // Request tracking data every 5 seconds
                    LocalPort = localPort // Set the local port to be included in the tracking request
                };
                
                // Create and wire up our tracking receiver
                var clientWrapper = new UdpClientWrapper(udpClient);
                using var receiver = new TrackingReceiver(clientWrapper, config);
                
                // Subscribe to tracking data events
                receiver.TrackingDataReceived += OnTrackingDataReceived;
                
                // Run the receiver
                Console.WriteLine("Starting tracking receiver...");
                Console.WriteLine("Waiting for tracking data from iPhone VTube Studio...");
                
                // Start receiver in a separate task
                var receiverTask = receiver.RunAsync(cts.Token);
                
                // Wait for cancellation
                await Task.Delay(Timeout.Infinite, cts.Token);
                
                // Wait for receiver to complete after cancellation
                await receiverTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine("Application shutting down...");
        }
        
        private static void OnTrackingDataReceived(object sender, TrackingResponse data)
        {
            Console.Clear();
            Console.WriteLine("Received tracking data from iPhone:");
            Console.WriteLine($"Timestamp: {data.Timestamp}");
            Console.WriteLine($"Face Found: {data.FaceFound}");
            
            if (data.Rotation != null)
            {
                Console.WriteLine("\nHead Rotation:");
                Console.WriteLine($"  X: {data.Rotation.X:F2}°");
                Console.WriteLine($"  Y: {data.Rotation.Y:F2}°");
                Console.WriteLine($"  Z: {data.Rotation.Z:F2}°");
            }
            
            if (data.Position != null)
            {
                Console.WriteLine("\nHead Position:");
                Console.WriteLine($"  X: {data.Position.X:F2}");
                Console.WriteLine($"  Y: {data.Position.Y:F2}");
                Console.WriteLine($"  Z: {data.Position.Z:F2}");
            }
            
            if (data.BlendShapes != null && data.BlendShapes.Count > 0)
            {
                Console.WriteLine("\nBlend Shapes:");
                foreach (var shape in data.BlendShapes)
                {
                    Console.WriteLine($"  {shape.Key}: {shape.Value:F2}");
                }
            }
            
            Console.WriteLine("\nPress Ctrl+C to exit...");
        }
    }
} 