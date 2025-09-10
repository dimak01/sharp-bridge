using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces.Core.Orchestrators;

namespace SharpBridge
{
    /// <summary>
    /// Main program entry point
    /// </summary>
    public static class Program
    {
        // Windows API functions for ANSI support
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        /// <summary>
        /// Application entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>An asynchronous task representing the execution</returns>
        public static async Task<int> Main(string[] args)
        {
            // Enable ANSI color support for console
            try
            {
                // Enable virtual terminal processing for ANSI support
                if (OperatingSystem.IsWindows())
                {
                    var handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE
                    if (GetConsoleMode(handle, out uint mode))
                    {
                        mode |= 0x0004; // ENABLE_VIRTUAL_TERMINAL_PROCESSING
                        SetConsoleMode(handle, mode);
                    }
                }
            }
            catch
            {
                // Ignore errors - ANSI colors will just not work
            }

            Console.WriteLine("Preparing to start Sharp Bridge...");

            // Setup DI container
            var services = new ServiceCollection();
            services.AddSharpBridgeServices("./Configs");

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
                await orchestrator.InitializeAsync(cts.Token);
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