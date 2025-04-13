using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Default configuration values for command-line options
    /// </summary>
    public static class CommandLineDefaults
    {
        public const string IphoneIp = "192.168.1.178";
        public const int IphonePort = 21412;
        public const int LocalPort = 21413;
        public const int RequestIntervalSeconds = 1;
        public const int SendForSeconds = 10;
        public const int ReceiveTimeoutMs = 2000;
        public const int UiUpdateIntervalMs = 250;
    }

    /// <summary>
    /// Handles parsing of command-line arguments and configuration
    /// </summary>
    public class CommandLineParser
    {
        /// <summary>
        /// Parses command-line arguments and runs the application with the resulting configuration
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <param name="runAction">The action to run with the parsed configuration</param>
        /// <returns>An exit code representing the result of the operation</returns>
        public async Task<int> ParseAndRunAsync(string[] args, Func<TrackingReceiverConfig, CancellationTokenSource, Task> runAction)
        {
            // Create command line options
            var ipOption = new Option<string>(
                aliases: new[] { "--ip", "-i" },
                description: "iPhone IP address",
                getDefaultValue: () => CommandLineDefaults.IphoneIp);

            var iphonePortOption = new Option<int>(
                aliases: new[] { "--iphone-port", "-p" },
                description: "iPhone port",
                getDefaultValue: () => CommandLineDefaults.IphonePort);

            var localPortOption = new Option<int>(
                aliases: new[] { "--local-port", "-l" },
                description: "Local listening port",
                getDefaultValue: () => CommandLineDefaults.LocalPort);

            var requestIntervalOption = new Option<int>(
                aliases: new[] { "--interval", "-t" },
                description: "Request interval in seconds",
                getDefaultValue: () => CommandLineDefaults.RequestIntervalSeconds);

            var sendForSecondsOption = new Option<int>(
                aliases: new[] { "--send-seconds", "-s" },
                description: "Time to send data for in seconds",
                getDefaultValue: () => CommandLineDefaults.SendForSeconds);

            var receiveTimeoutOption = new Option<int>(
                aliases: new[] { "--timeout", "-r" },
                description: "Receive timeout in milliseconds",
                getDefaultValue: () => CommandLineDefaults.ReceiveTimeoutMs);

            var interactiveOption = new Option<bool>(
                aliases: new[] { "--interactive", "-x" },
                description: "Launch in interactive mode",
                getDefaultValue: () => false);

            // Build the root command
            var rootCommand = new RootCommand("Sharp Bridge - VTube Studio iPhone to PC Bridge");
            rootCommand.AddOption(ipOption);
            rootCommand.AddOption(iphonePortOption);
            rootCommand.AddOption(localPortOption);
            rootCommand.AddOption(requestIntervalOption);
            rootCommand.AddOption(sendForSecondsOption);
            rootCommand.AddOption(receiveTimeoutOption);
            rootCommand.AddOption(interactiveOption);

            rootCommand.SetHandler(async (string ip, int iphonePort, int localPort, int requestInterval, 
                                         int sendForSeconds, int receiveTimeout, bool interactive) =>
            {
                // Handle interactive mode
                string iphoneIpAddress = ip;
                if (interactive || args.Length == 0)
                {
                    Console.WriteLine("Interactive mode enabled.");
                    Console.Write($"Enter your iPhone's IP address (or press Enter for default [{ip}]): ");
                    Console.CursorVisible = true; // Show cursor for input
                    
                    string input = Console.ReadLine();
                    Console.CursorVisible = false; // Hide cursor again
                    
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        iphoneIpAddress = input;
                    }
                }
                
                // Create configuration
                var config = new TrackingReceiverConfig
                {
                    IphoneIpAddress = iphoneIpAddress,
                    IphonePort = iphonePort,
                    LocalPort = localPort,
                    RequestIntervalSeconds = requestInterval,
                    SendForSeconds = sendForSeconds,
                    ReceiveTimeoutMs = receiveTimeout
                };
                
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    Console.WriteLine("Shutdown requested, cleaning up...");
                };
                
                // Run the application with the parsed configuration
                await runAction(config, cts);
            }, 
            ipOption, iphonePortOption, localPortOption, requestIntervalOption, 
            sendForSecondsOption, receiveTimeoutOption, interactiveOption);

            return await rootCommand.InvokeAsync(args);
        }
    }
} 