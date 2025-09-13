using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration;
using SharpBridge.Interfaces.Configuration.Factories;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.UI.Utilities;
using SharpBridge.Utilities;

namespace SharpBridge.Configuration.Services.Remediation
{
    /// <summary>
    /// Remediation service for VTubeStudioPhoneClientConfig configuration sections.
    /// </summary>
    public class VTubeStudioPhoneClientConfigRemediationService : BaseConfigSectionRemediationService
    {
        /// <summary>
        /// Field notes for the configuration section.
        /// </summary>
        protected override Dictionary<string, string[]> FieldNotes { get; } = new()
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

        /// <summary>
        /// Field defaults for the configuration section.
        /// </summary>
        protected override Dictionary<string, object> FieldDefaults { get; } = new()
        {
            ["LocalPort"] = 28964
            // No defaults for IphoneIpAddress or IphonePort - user must look them up
        };


        /// <summary>
        /// Initializes a new instance of the VTubeStudioPhoneClientConfigRemediationService class.
        /// </summary>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="console">Console abstraction for I/O</param>
        public VTubeStudioPhoneClientConfigRemediationService(
            IConfigSectionValidatorsFactory validatorsFactory,
            IConsole console)
            : base(validatorsFactory, ConfigSectionTypes.VTubeStudioPhoneClientConfig, console)
        {
        }

        /// <summary>
        /// Checks if the configuration section is eligible for pass-through.
        /// </summary>
        /// <param name="workingFields">The fields to check</param>
        /// <param name="activeFieldName">The name of the active field</param>
        /// <returns>True if the configuration section is eligible for pass-through</returns>
        protected override bool IsEligibleForPassThru(List<ConfigFieldState> workingFields, string activeFieldName)
        {
            return false;
        }

        /// <summary>
        /// Builds the splash lines for the remediation process.
        /// </summary>
        /// <param name="issues">The issues to build the splash lines from</param>
        /// <param name="isFirstTimeSetup">True if the configuration section is in first-time setup mode</param>
        /// <returns>The built splash lines</returns>
        protected override string[] BuildSplashLines(List<FieldValidationIssue> issues, bool isFirstTimeSetup)
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

        /// <summary>
        /// Checks if the configuration section should fall back to defaults.
        /// </summary>
        /// <param name="fields">The fields to check</param>
        protected override bool ShouldFallBackToDefaults(List<ConfigFieldState> fields)
        {
            return false;
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

            var missingIp = Missing("IphoneIpAddress");
            var missingIphonePort = Missing("IphonePort");
            var missingLocalPort = Missing("LocalPort");

            return missingIp && missingIphonePort && missingLocalPort;
        }


        /// <summary>
        /// Builds the field frame for the remediation process.
        /// </summary>
        /// <param name="activeField"></param>
        /// <param name="notes"></param>
        /// <param name="errorText"></param>
        /// <returns></returns>

        protected override string[] BuildFieldFrame(
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

        /// <summary>
        /// Creates a VTubeStudioPhoneClientConfig from the field states.
        /// </summary>
        protected override IConfigSection CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
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
