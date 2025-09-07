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
    public class VTubeStudioPCConfigRemediationService : BaseConfigSectionRemediationService
    {
        /// <summary>
        /// Field notes for the configuration section.
        /// </summary>
        protected override Dictionary<string, string[]> FieldNotes { get; } = new()
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
            },
            ["ParameterPrefix"] = new[]
            {
                "This prefix will be added to all parameter names when sending to VTube Studio PC.",
                "Helps avoid naming conflicts with other plugins.",
                "",
                "Requirements:",
                "- 0-15 characters long",
                "- Alphanumeric characters only (no spaces)",
                "- Empty prefix is allowed (no prefix will be added)",
                "",
                "Default: \u001b[96m_SB_\u001b[0m (puts parameters at top of VTS parameter list)"
            }
        };

        /// <summary>
        /// Field defaults for the configuration section.
        /// </summary>
        protected override Dictionary<string, object> FieldDefaults { get; } = new()
        {
            ["Host"] = "localhost",
            ["Port"] = 8001,
            ["UsePortDiscovery"] = true,
            ["ParameterPrefix"] = "_SB_"
        };


        /// <summary>
        /// Initializes a new instance of the VTubeStudioPCConfigRemediationService class.
        /// </summary>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="console">Console abstraction for I/O</param>
        public VTubeStudioPCConfigRemediationService(
            IConfigSectionValidatorsFactory validatorsFactory,
            IConsole console)
            : base(validatorsFactory, ConfigSectionTypes.VTubeStudioPCConfig, console)
        {
        }

        /// <summary>
        /// Checks if the configuration section should fall back to defaults.
        /// </summary>
        /// <param name="fields">The fields to check</param>
        /// <returns>True if the configuration section should fall back to defaults</returns>
        protected override bool ShouldFallBackToDefaults(List<ConfigFieldState> fields)
        {
            // Check if only ParameterPrefix is missing (it's optional with a default)
            var missingFields = fields.Where(f => f.Value == null).Select(f => f.FieldName).ToList();
            return missingFields.Count == 1 && missingFields.Contains("ParameterPrefix");
        }

        /// <summary>
        /// Applies defaults to missing fields.
        /// </summary>
        /// <param name="fields">The fields to apply defaults to</param>
        /// <returns>The fields with defaults applied</returns>
        protected override List<ConfigFieldState> ApplyDefaultsToMissingFields(List<ConfigFieldState> fields)
        {
            return fields;
        }

        /// <summary>
        /// Checks if a field is eligible for validation pass-through (skipping validation).
        /// </summary>
        /// <param name="workingFields">The working fields</param>
        /// <param name="activeFieldName">The name of the active field</param>
        /// <returns>True if the field is eligible for validation pass-through, false otherwise</returns>
        protected override bool IsEligibleForPassThru(List<ConfigFieldState> workingFields, string activeFieldName)
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

        /// <summary>
        /// Builds the splash/header lines for the remediation process.
        /// </summary>
        /// <param name="issues">The issues to build the splash lines from</param>
        /// <param name="isFirstTimeSetup">Whether this is first-time setup</param>
        /// <returns>The built splash lines</returns>
        protected override string[] BuildSplashLines(List<FieldValidationIssue> issues, bool isFirstTimeSetup)
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

        /// <summary>
        /// Checks if the configuration section is in first-time setup mode.
        /// </summary>
        /// <param name="fields">The fields to check</param>
        /// <returns>True if the configuration section is in first-time setup mode</returns>
        protected override bool IsFirstTimeSetup(List<ConfigFieldState> fields)
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

        /// <summary>
        /// Builds the field frame for the remediation process.
        /// </summary>
        /// <param name="activeField">The active field</param>
        /// <param name="notes">The notes for the field</param>
        /// <param name="errorText">The error text for the field</param>
        /// <returns>The built field frame</returns>
        protected override string[] BuildFieldFrame(
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


        /// <summary>
        /// Creates a VTubeStudioPCConfig from the field states.
        /// </summary>
        protected override IConfigSection CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
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
                        case "ParameterPrefix" when field.Value is string parameterPrefix:
                            config.ParameterPrefix = parameterPrefix;
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
        protected override List<FieldValidationIssue> SortIssuesByFieldOrder(List<FieldValidationIssue> issues)
        {
            // Define field order - dependencies must come before dependent fields
            var fieldOrder = new[] { "Host", "UsePortDiscovery", "Port", "ParameterPrefix" };

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
