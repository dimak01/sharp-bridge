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
            var hint = GetFieldHint(activeFieldName);
            string? lastError = null;

            while (true)
            {
                // Get current field state (or create a placeholder state for prompting)
                var current = workingFields.FirstOrDefault(f => f.FieldName == activeFieldName)
                              ?? new ConfigFieldState(activeFieldName, null, false, initialIssue.ExpectedType, initialIssue.Description);

                // Render frame focused on the active field with optional hint and persistent error
                var frame = BuildFieldFrame(current, hint, lastError);
                _console.WriteLines(frame);
                var input = _console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    // Set error and continue prompting same field
                    lastError = "Input cannot be empty. Please provide a value.";
                    continue;
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

        private static readonly Dictionary<string, string> FieldHints = new()
        {
            ["IphoneIpAddress"] = "Find your iPhone's IP in Wi‑Fi settings. Ensure phone and PC are on the same network.",
            ["IphonePort"] = "Enter the UDP port configured in your iPhone tracking app.",
            ["LocalPort"] = "Local UDP port to bind on this PC. Avoid ports < 1024; 20000–60000 is typical."
        };

        private static string? GetFieldHint(string fieldName)
        {
            return FieldHints.TryGetValue(fieldName, out var hint) ? hint : null;
        }

        private static string[] BuildFieldFrame(
            ConfigFieldState activeField,
            string? hint,
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
            if (!string.IsNullOrWhiteSpace(hint))
            {
                lines.Add($"Hint: {hint}");
            }
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                lines.Add($"Error: {errorText}");
            }
            lines.Add("Enter new value (cannot be empty):");

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
