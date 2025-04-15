using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;

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
            Console.WriteLine("SharpBridge Application");
            
            // Parse command line arguments (temporary simple version)
            string iphoneIp = args.Length > 0 ? args[0] : "127.0.0.1";
            string transformConfigPath = args.Length > 1 ? args[1] : "Configs/default-transform.json";
            
            // Setup DI container
            var services = new ServiceCollection();
            services.AddSharpBridgeServices();
            
            // Configure options
            services.ConfigureVTubeStudioPhoneClient(options => 
            {
                // Default configuration - will be replaced with command-line args or config file
                options.ReceiveTimeoutMs = 100;
                options.SendForSeconds = 3;
            });
            
            using var serviceProvider = services.BuildServiceProvider();
            
            // Create cancellation token for application shutdown
            using var cts = new CancellationTokenSource();
            
            // Handle Ctrl+C
            Console.CancelKeyPress += (sender, e) => 
            {
                Console.WriteLine("Shutting down...");
                cts.Cancel();
                e.Cancel = true; // Prevent default process termination
            };
            
            try
            {
                // Get orchestrator from DI container
                var orchestrator = serviceProvider.GetRequiredService<IApplicationOrchestrator>();
                
                // Initialize and run the application
                await orchestrator.InitializeAsync(iphoneIp, transformConfigPath, cts.Token);
                await orchestrator.RunAsync(cts.Token);
                
                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Application was canceled.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }
    }
} 