using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

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
        /// <returns>A tuple indicating success and the updated configuration section</returns>
        public async Task<(bool Success, IConfigSection? UpdatedConfig)> Remediate(List<ConfigFieldState> fieldsState)
        {
            var workingFields = new List<ConfigFieldState>(fieldsState);

            while (true)
            {
                var validation = _validator.ValidateSection(workingFields);
                if (validation.IsValid)
                {
                    var config = CreateConfigFromFieldStates(workingFields);
                    return (true, config);
                }

                // Splash: show full list and wait for Enter
                var isFirstTimeSetup = IsFirstTimeSetup(workingFields);
                var splash = BuildSplashLines(validation.Issues, isFirstTimeSetup);
                _console.WriteLines(splash);
                _console.ReadLine();

                // Remediate each issue with a tight per-field loop
                foreach (var issue in validation.Issues)
                {
                    await RemediateFieldUntilValidAsync(issue, workingFields);
                }
            }
        }

        private async Task RemediateFieldUntilValidAsync(
            FieldValidationIssue initialIssue,
            List<ConfigFieldState> workingFields)
        {
            var activeFieldName = initialIssue.FieldName;
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
                    : $"{issue.Description} (provided: '{issue.ProvidedValueText}')";
                lines.Add($" - {detail}");
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
                "2. Go to Settings (Gear Icon) → Scroll all the way down to '3rd Party PC Clients'",
                "3. Tap on 'Show IP List' - this is what you need (pick the 'IPv4' address where possible)",
                "",
                "Important: Ensure you use the iPhone IP address that matches the Wi-Fi network of your PC!"
            },
            ["IphonePort"] = new[]
            {
                "To find your iPhone's port number:",
                "1. Open VTube Studio app on your iPhone",
                "2. Go to Settings (Gear Icon) → Scroll all the way down to '3rd Party PC Clients'",
                "3. Tap on 'Show IP List' - this is what you need (look for the 'Port' number)",
            },
            ["LocalPort"] = new[]
            {
                "This is the UDP port that Sharp Bridge will listen on to receive tracking data from your iPhone.",
                "",
                "Port selection guidelines:",
                "• Use ports 1024-65535 (avoid system ports < 1024)",
                "• Recommended range: 20000-60000",
                "",
                "!!! FIREWALL SETUP REQUIRED:",
                "Your iPhone won't be able to connect until you allow this port through Windows Firewall.",
                "",
                "Manual firewall rule (replace XXXX with your chosen port):",
                "netsh advfirewall firewall add rule name=\"SharpBridge iPhone UDP Inbound\" dir=in action=allow protocol=UDP localport=XXXX",
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
            lines.Add($"Now editing: {activeField.Description}");
            if (activeField.Value != null)
            {
                lines.Add($"Current value: {activeField.Value}");
            }
            if (notes != null && notes.Length > 0)
            {
                lines.AddRange(from noteLine in notes where !string.IsNullOrWhiteSpace(noteLine) select noteLine);
            }

            if (!string.IsNullOrWhiteSpace(errorText))
            {
                lines.Add($"Error: {errorText}");
            }

            // Dynamic prompt with inline default
            var defaultValue = GetFieldDefault(activeField.FieldName);
            string promptText = defaultValue != null
                ? $"Enter new value (or press Enter for default value of {defaultValue}):"
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
