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
    /// Remediation service for VTubeStudioPCConfig configuration sections.
    /// </summary>
    public class VTubeStudioPCConfigRemediationService : IConfigSectionRemediationService
    {
        private readonly IConfigSectionValidator _validator;
        private readonly IConsole _console;

        /// <summary>
        /// Initializes a new instance of the VTubeStudioPCConfigRemediationService class.
        /// </summary>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="console">Console abstraction for I/O</param>
        public VTubeStudioPCConfigRemediationService(
            IConfigSectionValidatorsFactory validatorsFactory,
            IConsole console)
        {
            _validator = validatorsFactory.GetValidator(ConfigSectionTypes.VTubeStudioPCConfig);
            _console = console;
        }

        /// <summary>
        /// Remediates configuration issues for a VTubeStudioPCConfig section by fixing missing or invalid fields.
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

            // Sort issues by field order to ensure dependencies are handled correctly
            var orderedIssues = SortIssuesByFieldOrder(initialValidation.Issues);

            // Remediate each issue - each method will loop until the field is valid
            foreach (var issue in orderedIssues)
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

        private static bool IsEligibleForPassThru(List<ConfigFieldState> workingFields, string activeFieldName)
        {
            if (activeFieldName == "Port")
            {
                var usePortDiscovery = workingFields.FirstOrDefault(f => f.FieldName == "UsePortDiscovery");
                if (usePortDiscovery?.Value is bool discoveryEnabled && discoveryEnabled)
                {
                    // Skip Port field if discovery is enabled, set default port
                    var defaultPort = new ConfigFieldState("Port", 8001, true, typeof(int), "VTube Studio PC API Port");
                    var existing = workingFields.FirstOrDefault(f => f.FieldName == "Port");
                    if (existing != null)
                    {
                        var idx = workingFields.IndexOf(existing);
                        workingFields[idx] = defaultPort;
                    }
                    else
                    {
                        workingFields.Add(defaultPort);
                    }
                    return true;
                }
            }

            return false;
        }


        private static string[] BuildSplashLines(List<FieldValidationIssue> issues, bool isFirstTimeSetup)
        {
            var lines = new List<string>
            {
                isFirstTimeSetup
                    ? "=== Welcome! Let's complete the initial setup for VTube Studio PC connection ==="
                    : "=== VTube Studio PC Configuration - Remediation ===",
                isFirstTimeSetup
                    ? "We'll guide you through the required fields to connect to VTube Studio on PC:"
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

            var missingHost = Missing("Host");
            var missingPort = Missing("Port");
            var missingUsePortDiscovery = Missing("UsePortDiscovery");

            return missingHost && missingPort && missingUsePortDiscovery;
        }

        private static readonly Dictionary<string, string[]> FieldNotes = new()
        {
            ["Host"] = new[]
            {
                "This is the host address where VTube Studio is running on your PC.",
                "",
                "Common value is \u001b[96mlocalhost\u001b[0m or \u001b[96m127.0.0.1\u001b[0m - if VTube Studio is on the same PC. Leave it as is if you're not sure.",
                "",
            },
            ["Port"] = new[]
            {
                "This is the port number that VTube Studio's API is listening on.",
                "",
                "Default VTube Studio API port is \u001b[38;5;215m8001\u001b[0m.",
                "You can check/change this in VTube Studio:",
                "1. Open VTube Studio",
                "2. Go to Settings (Gear Icon) → Plugins",
                "3. Check the \u001b[96m'Port'\u001b[0m setting",
                "",
                "Common ports: \u001b[38;5;215m8001\u001b[0m (default), \u001b[38;5;215m8002\u001b[0m, \u001b[38;5;215m8003\u001b[0m, etc."
            },
            ["UsePortDiscovery"] = new[]
            {
                "When enabled, Sharp Bridge will automatically try to find VTube Studio using its discovery port (\u001b[38;5;215m47779\u001b[0m).",
                "It is recommended to keep this setting enabled.",
                "",
                "Allowed values: \u001b[96mtrue\u001b[0m to enable, \u001b[96mfalse\u001b[0m to disable."
            }
        };

        private static string[]? GetFieldNotes(string fieldName)
        {
            return FieldNotes.TryGetValue(fieldName, out var notes) ? notes : null;
        }

        private static readonly Dictionary<string, object> FieldDefaults = new()
        {
            ["Host"] = "localhost",
            ["Port"] = 8001,
            ["UsePortDiscovery"] = true
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
                "=== VTube Studio PC Configuration - Remediation ==="
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

            if (expectedType == typeof(bool))
            {
                if (bool.TryParse(input, out var b))
                {
                    parsed = b;
                    error = null;
                    return true;
                }
                parsed = null;
                error = "Value must be 'true' or 'false'.";
                return false;
            }

            // Default to string for everything else
            parsed = input;
            error = null;
            return true;
        }

        /// <summary>
        /// Creates a VTubeStudioPCConfig from the field states.
        /// </summary>
        private static VTubeStudioPCConfig CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
        {
            var config = new VTubeStudioPCConfig();

            foreach (var field in fieldsState)
            {
                if (field.IsPresent && field.Value != null)
                {
                    switch (field.FieldName)
                    {
                        case "Host" when field.Value is string host:
                            config.Host = host;
                            break;
                        case "Port" when field.Value is int port:
                            config.Port = port;
                            break;
                        case "UsePortDiscovery" when field.Value is bool usePortDiscovery:
                            config.UsePortDiscovery = usePortDiscovery;
                            break;
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Sorts validation issues by field order to ensure dependencies are handled correctly.
        /// </summary>
        /// <param name="issues">The validation issues to sort</param>
        /// <returns>Issues sorted by field order</returns>
        private static List<FieldValidationIssue> SortIssuesByFieldOrder(List<FieldValidationIssue> issues)
        {
            // Define field order - dependencies must come before dependent fields
            var fieldOrder = new[] { "Host", "UsePortDiscovery", "Port" };

            return issues
                .OrderBy(issue => GetFieldOrder(issue.FieldName, fieldOrder))
                .ToList();
        }

        /// <summary>
        /// Gets the order index for a field name.
        /// </summary>
        /// <param name="fieldName">The field name</param>
        /// <param name="fieldOrder">The ordered array of field names</param>
        /// <returns>The order index, or 999 for unknown fields</returns>
        private static int GetFieldOrder(string fieldName, string[] fieldOrder)
        {
            var index = Array.IndexOf(fieldOrder, fieldName);
            return index >= 0 ? index : 999; // Unknown fields go last
        }
    }
}
