using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;

namespace SharpBridge.Services
{
    /// <summary>
    /// Console-based service for handling first-time setup of missing configuration fields
    /// </summary>
    public class FirstTimeSetupService : IFirstTimeSetupService
    {
        private readonly IConsole _console;

        /// <summary>
        /// Initializes a new instance of FirstTimeSetupService
        /// </summary>
        /// <param name="console">Console interface for user interaction</param>
        public FirstTimeSetupService(IConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        /// <summary>
        /// Prompts the user to provide values for missing configuration fields
        /// </summary>
        /// <param name="missingFields">Collection of fields that need to be configured</param>
        /// <param name="currentConfig">Current application configuration (immutable)</param>
        /// <returns>Task with setup result and updated configuration if successful</returns>
        public async Task<(bool Success, ApplicationConfig? UpdatedConfig)> RunSetupAsync(IEnumerable<MissingField> missingFields, ApplicationConfig currentConfig)
        {
            if (missingFields == null || !missingFields.Any())
            {
                return (true, null); // Nothing to set up
            }

            if (currentConfig == null)
            {
                throw new ArgumentNullException(nameof(currentConfig));
            }

            var fieldList = missingFields.ToList();
            var workingConfig = currentConfig; // Start with current config
            bool setupSuccessful = true;

            // Display initial setup screen
            DisplaySetupIntroduction(fieldList);

            var totalSteps = fieldList.Count;
            var currentStep = 0;

            foreach (var field in fieldList)
            {
                currentStep++;
                DisplayProgressIndicator(currentStep, totalSteps, field);

                try
                {
                    var (success, updatedConfig) = field switch
                    {
                        MissingField.PhoneIpAddress => await SetupPhoneIpAddressAsync(workingConfig),
                        MissingField.PhonePort => await SetupPhonePortAsync(workingConfig),
                        MissingField.PCHost => await SetupPCHostAsync(workingConfig),
                        MissingField.PCPort => await SetupPCPortAsync(workingConfig),
                        _ => (false, workingConfig)
                    };

                    if (!success)
                    {
                        setupSuccessful = false;
                        break;
                    }

                    // Update working config with the new configuration
                    workingConfig = updatedConfig;
                }
                catch (Exception ex)
                {
                    DisplayErrorMessage($"Error during setup: {ex.Message}");
                    setupSuccessful = false;
                    break;
                }
            }

            // Display completion screen
            DisplaySetupCompletion(setupSuccessful);

            return (setupSuccessful, setupSuccessful ? workingConfig : null);
        }

        /// <summary>
        /// Prompts for and sets up the iPhone IP address
        /// </summary>
        private async Task<(bool Success, ApplicationConfig UpdatedConfig)> SetupPhoneIpAddressAsync(ApplicationConfig config)
        {
            while (true)
            {
                // Display the setup screen for iPhone IP address
                var setupScreen = BuildPhoneIpAddressSetupScreen();
                _console.WriteLines(setupScreen);

                var input = _console.ReadLine();

                // Allow user to cancel setup
                if (IsCancellationRequest(input))
                {
                    DisplaySetupCancellation();
                    return (false, config);
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    DisplayValidationError("IP address cannot be empty. Please try again.");
                    continue;
                }

                if (!IPAddress.TryParse(input.Trim(), out _))
                {
                    DisplayValidationError("Invalid IP address format. Please enter a valid IP address (e.g., 192.168.1.200).");
                    continue;
                }

                // Create new config with updated IP address
                var updatedConfig = WithPhoneIpAddress(config, input.Trim());

                DisplayFieldSuccess($"✓ iPhone IP address set to: {input.Trim()}");
                return (true, updatedConfig);
            }
        }

        /// <summary>
        /// Prompts for and sets up the iPhone port
        /// </summary>
        private Task<(bool Success, ApplicationConfig UpdatedConfig)> SetupPhonePortAsync(ApplicationConfig config)
        {
            while (true)
            {
                // Display the setup screen for iPhone port
                var setupScreen = BuildPhonePortSetupScreen();
                _console.WriteLines(setupScreen);

                var input = _console.ReadLine();

                // Allow empty input to use default
                if (string.IsNullOrWhiteSpace(input))
                {
                    input = "21412";
                }

                if (!int.TryParse(input.Trim(), out var port) || port <= 0 || port > 65535)
                {
                    DisplayValidationError("Invalid port number. Please enter a number between 1 and 65535.");
                    continue;
                }

                // Create new config with updated port
                var updatedConfig = WithPhonePort(config, port);

                DisplayFieldSuccess($"✓ iPhone port set to: {port}");
                return Task.FromResult((true, updatedConfig));
            }
        }

        /// <summary>
        /// Prompts for and sets up the PC host address
        /// </summary>
        private Task<(bool Success, ApplicationConfig UpdatedConfig)> SetupPCHostAsync(ApplicationConfig config)
        {
            while (true)
            {
                // Display the setup screen for PC host
                var setupScreen = BuildPCHostSetupScreen();
                _console.WriteLines(setupScreen);

                var input = _console.ReadLine();

                // Allow empty input to use default
                var host = string.IsNullOrWhiteSpace(input) ? "localhost" : input.Trim();

                // Create new config with updated host
                var updatedConfig = WithPCHost(config, host);

                DisplayFieldSuccess($"✓ PC host set to: {host}");
                return Task.FromResult((true, updatedConfig));
            }
        }

        /// <summary>
        /// Prompts for and sets up the PC port
        /// </summary>
        private Task<(bool Success, ApplicationConfig UpdatedConfig)> SetupPCPortAsync(ApplicationConfig config)
        {
            while (true)
            {
                // Display the setup screen for PC port
                var setupScreen = BuildPCPortSetupScreen();
                _console.WriteLines(setupScreen);

                var input = _console.ReadLine();

                // Allow empty input to use default
                if (string.IsNullOrWhiteSpace(input))
                {
                    input = "8001";
                }

                if (!int.TryParse(input.Trim(), out var port) || port <= 0 || port > 65535)
                {
                    DisplayValidationError("Invalid port number. Please enter a number between 1 and 65535.");
                    continue;
                }

                // Create new config with updated port
                var updatedConfig = WithPCPort(config, port);

                DisplayFieldSuccess($"✓ PC port set to: {port}");
                return Task.FromResult((true, updatedConfig));
            }
        }

        /// <summary>
        /// Creates a copy of ApplicationConfig with updated PhoneClient IP address
        /// </summary>
        private static ApplicationConfig WithPhoneIpAddress(ApplicationConfig original, string ipAddress)
        {
            var cloned = ConfigCloner.Clone(original);
            cloned.PhoneClient.IphoneIpAddress = ipAddress;
            return cloned;
        }

        /// <summary>
        /// Creates a copy of ApplicationConfig with updated PhoneClient port
        /// </summary>
        private static ApplicationConfig WithPhonePort(ApplicationConfig original, int port)
        {
            var cloned = ConfigCloner.Clone(original);
            cloned.PhoneClient.IphonePort = port;
            return cloned;
        }

        /// <summary>
        /// Creates a copy of ApplicationConfig with updated PCClient host
        /// </summary>
        private static ApplicationConfig WithPCHost(ApplicationConfig original, string host)
        {
            var cloned = ConfigCloner.Clone(original);
            cloned.PCClient.Host = host;
            return cloned;
        }

        /// <summary>
        /// Creates a copy of ApplicationConfig with updated PCClient port
        /// </summary>
        private static ApplicationConfig WithPCPort(ApplicationConfig original, int port)
        {
            var cloned = ConfigCloner.Clone(original);
            cloned.PCClient.Port = port;
            return cloned;
        }

        /// <summary>
        /// Displays the setup introduction screen
        /// </summary>
        private void DisplaySetupIntroduction(List<MissingField> missingFields)
        {
            var lines = new List<string>
            {
                "",
                "=== Sharp Bridge First-Time Setup ===",
                "",
                "Some required configuration fields need to be set up before Sharp Bridge can start.",
                "Please provide the following information:",
                ""
            };

            // Add list of fields that will be configured
            foreach (var field in missingFields)
            {
                var fieldName = field switch
                {
                    MissingField.PhoneIpAddress => "📱 iPhone IP Address",
                    MissingField.PhonePort => "📱 iPhone Port",
                    MissingField.PCHost => "💻 PC Host Address",
                    MissingField.PCPort => "💻 PC Port",
                    _ => field.ToString()
                };
                lines.Add($"  • {fieldName}");
            }

            lines.Add("");
            _console.WriteLines(lines.ToArray());
        }

        /// <summary>
        /// Displays the setup completion screen
        /// </summary>
        private void DisplaySetupCompletion(bool successful)
        {
            var lines = new List<string> { "" };

            if (successful)
            {
                lines.AddRange(new[]
                {
                    "✓ Setup completed successfully!",
                    "Sharp Bridge will now continue with initialization...",
                });
            }
            else
            {
                lines.AddRange(new[]
                {
                    "✗ Setup was cancelled or failed.",
                    "Sharp Bridge cannot start without the required configuration.",
                });
            }

            lines.Add("");
            _console.WriteLines(lines.ToArray());
        }

        /// <summary>
        /// Builds the iPhone IP address setup screen
        /// </summary>
        private static string[] BuildPhoneIpAddressSetupScreen()
        {
            return new[]
            {
                "",
                "📱 iPhone IP Address Setup",
                "",
                "Enter the IP address of your iPhone running VTube Studio.",
                "You can find this in VTube Studio > Settings > General > Network.",
                "Example: 192.168.1.200",
                "",
                "iPhone IP Address: "
            };
        }

        /// <summary>
        /// Builds the iPhone port setup screen
        /// </summary>
        private static string[] BuildPhonePortSetupScreen()
        {
            return new[]
            {
                "",
                "📱 iPhone Port Setup",
                "",
                "Enter the port number that VTube Studio is using on your iPhone.",
                "The default is usually 21412, but check VTube Studio > Settings > General > Network.",
                "",
                "iPhone Port (default: 21412): "
            };
        }

        /// <summary>
        /// Builds the PC host setup screen
        /// </summary>
        private static string[] BuildPCHostSetupScreen()
        {
            return new[]
            {
                "",
                "💻 PC VTube Studio Host Setup",
                "",
                "Enter the host address where VTube Studio is running on your PC.",
                "Use 'localhost' if VTube Studio is on the same computer as Sharp Bridge.",
                "Leave empty to use 'localhost' as default.",
                "",
                "PC Host (default: localhost): "
            };
        }

        /// <summary>
        /// Builds the PC port setup screen
        /// </summary>
        private static string[] BuildPCPortSetupScreen()
        {
            return new[]
            {
                "",
                "💻 PC VTube Studio Port Setup",
                "",
                "Enter the port number that VTube Studio API is using on your PC.",
                "The default is usually 8001. Check VTube Studio > Settings > General > API.",
                "",
                "PC Port (default: 8001): "
            };
        }

        /// <summary>
        /// Displays a progress indicator showing current step and field being configured
        /// </summary>
        private void DisplayProgressIndicator(int currentStep, int totalSteps, MissingField field)
        {
            var fieldName = field switch
            {
                MissingField.PhoneIpAddress => "iPhone IP Address",
                MissingField.PhonePort => "iPhone Port",
                MissingField.PCHost => "PC Host",
                MissingField.PCPort => "PC Port",
                _ => "Unknown Field"
            };

            var progressBar = GenerateProgressBar(currentStep, totalSteps);

            var lines = new[]
            {
                "",
                "═══════════════════════════════════════════════════════════════════════",
                $"  FIRST-TIME SETUP - Step {currentStep} of {totalSteps}",
                $"  Configuring: {fieldName}",
                "",
                $"  Progress: {progressBar} ({currentStep}/{totalSteps})",
                "═══════════════════════════════════════════════════════════════════════",
                ""
            };

            _console.WriteLines(lines);
        }

        /// <summary>
        /// Generates a visual progress bar
        /// </summary>
        private static string GenerateProgressBar(int current, int total, int width = 20)
        {
            var progress = (double)current / total;
            var filled = (int)(progress * width);
            var empty = width - filled;

            return $"[{new string('█', filled)}{new string('░', empty)}]";
        }

        /// <summary>
        /// Checks if user input indicates they want to cancel setup
        /// </summary>
        private static bool IsCancellationRequest(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var trimmed = input.Trim();
            return trimmed.Equals("cancel", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Displays setup cancellation message
        /// </summary>
        private void DisplaySetupCancellation()
        {
            var lines = new[]
            {
                "",
                "⚠️  Setup cancelled by user.",
                "",
                "The application will continue with the current configuration,",
                "but some features may not work properly until the missing",
                "fields are configured.",
                "",
                "You can restart the application to run setup again, or",
                "manually edit the configuration file.",
                "",
                "Press Enter to continue..."
            };
            _console.WriteLines(lines);
            _console.ReadLine(); // Wait for user acknowledgment
        }

        /// <summary>
        /// Displays a validation error message
        /// </summary>
        private void DisplayValidationError(string message)
        {
            var lines = new[]
            {
                "",
                $"❌ {message}",
                "Press Enter to try again...",
                "",
                "💡 Tip: Type 'cancel', 'quit', or 'exit' to skip setup"
            };
            _console.WriteLines(lines);
            _console.ReadLine(); // Wait for user to press Enter
        }

        /// <summary>
        /// Displays a field success message
        /// </summary>
        private void DisplayFieldSuccess(string message)
        {
            var lines = new[]
            {
                "",
                message,
                "",
                "Press Enter to continue...",
                ""
            };
            _console.WriteLines(lines);
            _console.ReadLine(); // Wait for user to press Enter
        }

        /// <summary>
        /// Displays an error message
        /// </summary>
        private void DisplayErrorMessage(string message)
        {
            var lines = new[]
            {
                "",
                $"❌ {message}",
                "Press Enter to continue...",
                ""
            };
            _console.WriteLines(lines);
            _console.ReadLine(); // Wait for user to press Enter
        }
    }
}
