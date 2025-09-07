using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;

namespace SharpBridge.Services.Remediation
{
    /// <summary>
    /// Remediation service for VTubeStudioPhoneClientConfig configuration sections.
    /// </summary>
    public class VTubeStudioPhoneClientConfigRemediationService : IConfigSectionRemediationService
    {
        private readonly IConfigSectionValidator _validator;
        private readonly IConsole _console;

        /// <summary>
        /// Initializes a new instance of the VTubeStudioPhoneClientConfigRemediationService class.
        /// </summary>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="console">Console abstraction for I/O</param>
        public VTubeStudioPhoneClientConfigRemediationService(
            IConfigSectionValidatorsFactory validatorsFactory,
            IConsole console)
        {
            _validator = validatorsFactory.GetValidator(ConfigSectionTypes.VTubeStudioPhoneClientConfig);
            _console = console;
        }

        /// <summary>
        /// Remediates configuration issues for a VTubeStudioPhoneClientConfig section by fixing missing or invalid fields.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>A tuple indicating the remediation result and the updated configuration section (if changes were made)</returns>
        public async Task<(RemediationResult Result, IConfigSection? UpdatedConfig)> Remediate(List<ConfigFieldState> fieldsState)
        {
            var workingFields = new List<ConfigFieldState>(fieldsState);

            // First validation: check if section is already valid
            var initialValidation = _validator.ValidateSection(workingFields);
            if (initialValidation.IsValid)
            {
                // Section was already valid, no remediation needed
                return (RemediationResult.NoRemediationNeeded, null);
            }

            // Show splash screen and wait for user to start
            var isFirstTimeSetup = IsFirstTimeSetup(workingFields);
            var splash = BuildSplashLines(initialValidation.Issues, isFirstTimeSetup);
            _console.WriteLines(splash);
            _console.ReadLine();

            // Remediate each issue - each method will loop until the field is valid
            foreach (var issue in initialValidation.Issues)
            {
                await RemediateFieldUntilValidAsync(issue, workingFields);
            }

            // Final validation: ensure all fields are now valid
            var finalValidation = _validator.ValidateSection(workingFields);
            if (finalValidation.IsValid)
            {
                // Section was successfully remediated
                var config = CreateConfigFromFieldStates(workingFields);
                return (RemediationResult.Succeeded, config);
            }
            else
            {
                // This shouldn't happen since RemediateFieldUntilValidAsync should fix each field
                // But if it does, we should return Failed
                return (RemediationResult.Failed, null);
            }
        }

        private static bool IsEligibleForPassThru(List<ConfigFieldState> workingFields, string activeFieldName)
        {
            return false;
        }

        private async Task RemediateFieldUntilValidAsync(
            FieldValidationIssue initialIssue,
            List<ConfigFieldState> workingFields)
        {
            var activeFieldName = initialIssue.FieldName;

            if (IsEligibleForPassThru(workingFields, activeFieldName))
                return;

            var notes = GetFieldNotes(activeFieldName);
            string? lastError = null;

            while (true)
            {
                // Get current field state (or create a placeholder state for prompting)
                var current = workingFields.FirstOrDefault(f => f.FieldName == activeFieldName)
                              ?? new ConfigFieldState(activeFieldName, null, false, initialIssue.ExpectedType, initialIssue.Description);

                // Render frame focused on the active field with optional notes and persistent error
                var frame = BuildFieldFrame(current, notes, lastError);
                _console.WriteLines(frame);
                var input = _console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    // Check if there's a default for this field
                    var defaultValue = GetFieldDefault(activeFieldName);
                    if (defaultValue != null)
                    {
                        // Use default value - convert to string for parsing
                        input = defaultValue.ToString();
                    }
                    else
                    {
                        // No default available - set error and continue prompting same field
                        lastError = "Input cannot be empty. Please provide a value.";
                        continue;
                    }
                }

                // Parse basic type
                if (!TryParseInput(current.ExpectedType, input!, out var parsed, out var parseError))
                {
                    lastError = parseError;
                    continue;
                }

                // Validate this single field using the validator
                var candidate = new ConfigFieldState(current.FieldName, parsed, true, current.ExpectedType, current.Description);
                var (isValid, issue) = _validator.ValidateSingleField(candidate);
                if (!isValid)
                {
                    lastError = issue?.Description ?? "Invalid value.";
                    continue;
                }

                // Apply update to working set and exit field loop
                var existing = workingFields.FirstOrDefault(f => f.FieldName == current.FieldName);
                if (existing != null)
                {
                    var idx = workingFields.IndexOf(existing);
                    workingFields[idx] = candidate;
                }
                else
                {
                    workingFields.Add(candidate);
                }

                // Clear screen after successful field remediation for clean transition
                _console.Clear();

                await Task.CompletedTask;
                return;
            }
        }

        private static string[] BuildSplashLines(List<FieldValidationIssue> issues, bool isFirstTimeSetup)
        {
            var lines = new List<string>
            {
                isFirstTimeSetup
                    ? "=== Welcome! Let's complete the initial setup for your iPhone tracking ==="
                    : "=== VTube Studio Phone Client Configuration - Remediation ===",
                isFirstTimeSetup
                    ? "We'll guide you through the required fields to get started:"
                    : "The following fields need attention:"
            };

            foreach (var issue in issues)
            {
                var detail = string.IsNullOrWhiteSpace(issue.ProvidedValueText)
                    ? issue.Description
                    : $"{issue.Description} (provided: '{ConsoleColors.Colorize(issue.ProvidedValueText, ConsoleColors.Error)}')";
                lines.Add($" - {ConsoleColors.ColorizeBasicType(detail)}");
            }

            lines.Add("");
            lines.Add(isFirstTimeSetup
                ? "Press Enter to start first-time setup..."
                : "Press Enter to start remediation...");
            return lines.ToArray();
        }

        private static bool IsFirstTimeSetup(List<ConfigFieldState> fields)
        {
            // Treat as first-time if all required fields are missing (not present or null)
            bool Missing(string name)
            {
                var f = fields.FirstOrDefault(x => x.FieldName == name);
                return f == null || !f.IsPresent || f.Value == null;
            }

            var missingIp = Missing("IphoneIpAddress");
            var missingIphonePort = Missing("IphonePort");
            var missingLocalPort = Missing("LocalPort");

            return missingIp && missingIphonePort && missingLocalPort;
        }

        private static readonly Dictionary<string, string[]> FieldNotes = new()
        {
            ["IphoneIpAddress"] = new[]
            {
                "To find your iPhone's IP address:",
                "1. Open VTube Studio app on your iPhone",
                "2. Go to \u001b[96m'Settings'\u001b[0m (Gear Icon) → Scroll all the way down to \u001b[96m'3rd Party PC Clients'\u001b[0m",
                "3. Tap on \u001b[96m'Show IP List'\u001b[0m - this is what you need (pick the \u001b[96m'IPv4'\u001b[0m address where possible)",
                "",
                "Important: Ensure you use the iPhone IP address that matches the Wi-Fi network of your PC!"
            },
            ["IphonePort"] = new[]
            {
                "To find your iPhone's port number:",
                "1. Open VTube Studio app on your iPhone",
                "2. Go to \u001b[96m'Settings'\u001b[0m (Gear Icon) → Scroll all the way down to \u001b[96m'3rd Party PC Clients'\u001b[0m",
                "3. Tap on \u001b[96m'Show IP List'\u001b[0m - this is what you need (look for the \u001b[96m'Port'\u001b[0m number)",
            },
            ["LocalPort"] = new[]
            {
                "This is the UDP port that Sharp Bridge will listen on to receive tracking data from your iPhone.",
                "",
                "Port selection guidelines:",
                "• Use ports \u001b[38;5;215m1024-65535\u001b[0m (avoid system ports < \u001b[38;5;215m1024\u001b[0m)",
                "• Recommended range: \u001b[38;5;215m20000-60000\u001b[0m",
                "",
                "!!! FIREWALL SETUP REQUIRED:",
                "Your iPhone won't be able to connect until you allow this port through Windows Firewall.",
                "",
                "Manual firewall rule (replace \u001b[38;5;215mXXXX\u001b[0m with your chosen port):",
                "\u001b[96mnetsh advfirewall firewall add rule name=\u001b[96m\"SharpBridge iPhone UDP Inbound\"\u001b[0m dir=in action=allow protocol=UDP localport=\u001b[38;5;215mXXXX\u001b[0m\u001b[0m",
                "",
                "Need help? Press F2 after the application has been started to access Network Status mode for detailed connectivity diagnostics."
            }
        };

        private static string[]? GetFieldNotes(string fieldName)
        {
            return FieldNotes.TryGetValue(fieldName, out var notes) ? notes : null;
        }

        private static readonly Dictionary<string, object> FieldDefaults = new()
        {
            ["LocalPort"] = 28964
            // No defaults for IphoneIpAddress or IphonePort - user must look them up
        };

        private static object? GetFieldDefault(string fieldName)
        {
            return FieldDefaults.TryGetValue(fieldName, out var defaultValue) ? defaultValue : null;
        }


        private static string[] BuildFieldFrame(
            ConfigFieldState activeField,
            string[]? notes,
            string? errorText)
        {
            var lines = new List<string>
            {
                "=== VTube Studio Phone Client Configuration - Remediation ==="
            };

            lines.Add("");
            lines.Add($"Now editing: {ConsoleColors.ColorizeBasicType(activeField.Description)}");
            if (activeField.Value != null)
            {
                lines.Add($"Current value: {ConsoleColors.ColorizeBasicType(activeField.Value)}");
            }
            if (notes != null && notes.Length > 0)
            {
                lines.AddRange(from noteLine in notes where !string.IsNullOrWhiteSpace(noteLine) select noteLine);
            }

            if (!string.IsNullOrWhiteSpace(errorText))
            {
                lines.Add($"Error: {ConsoleColors.Colorize(errorText, ConsoleColors.Error)}");
            }

            // Dynamic prompt with inline default
            var defaultValue = GetFieldDefault(activeField.FieldName);
            string promptText = defaultValue != null
                ? $"Enter new value (or press Enter for default value of {ConsoleColors.ColorizeBasicType(defaultValue)}):"
                : "Enter new value (cannot be empty):";
            lines.Add(promptText);

            return lines.ToArray();
        }

        private static bool TryParseInput(System.Type expectedType, string input, out object? parsed, out string? error)
        {
            if (expectedType == typeof(int))
            {
                if (int.TryParse(input, out var i))
                {
                    parsed = i;
                    error = null;
                    return true;
                }
                parsed = null;
                error = "Value must be an integer.";
                return false;
            }

            // Default to string for everything else
            parsed = input;
            error = null;
            return true;
        }

        /// <summary>
        /// Creates a VTubeStudioPhoneClientConfig from the field states.
        /// </summary>
        private static VTubeStudioPhoneClientConfig CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
        {
            var config = new VTubeStudioPhoneClientConfig();

            foreach (var field in fieldsState)
            {
                if (field.IsPresent && field.Value != null)
                {
                    switch (field.FieldName)
                    {
                        case "IphoneIpAddress" when field.Value is string ipAddress:
                            config.IphoneIpAddress = ipAddress;
                            break;
                        case "IphonePort" when field.Value is int port:
                            config.IphonePort = port;
                            break;
                        case "LocalPort" when field.Value is int localPort:
                            config.LocalPort = localPort;
                            break;
                    }
                }
            }

            return config;
        }
    }
}
