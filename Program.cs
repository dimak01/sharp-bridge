using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
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
            Console.WriteLine("SharpBridge Application");

            // Parse command line arguments
            var options = await CommandLineParser.ParseAsync(args);

            // Setup DI container
            var services = new ServiceCollection();
            services.AddSharpBridgeServices(
                options.ConfigDirectory,
                options.PCConfigFilename,
                options.PhoneConfigFilename);

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
                await orchestrator.InitializeAsync(options.TransformConfigPath, cts.Token);
                await orchestrator.RunAsync(cts.Token);

                return 0;
            }
            catch (OperationCanceledException)
            {
                await Console.Out.WriteLineAsync("Application was canceled.");
                return 0;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                await Console.Error.WriteLineAsync(ex.StackTrace);
                return 1;
            }
        }
    }
}