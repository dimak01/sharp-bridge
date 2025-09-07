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
    /// Remediation service for TransformationEngineConfig configuration sections.
    /// </summary>
    public class TransformationEngineConfigRemediationService : BaseConfigSectionRemediationService
    {
        /// <summary>
        /// Field notes for the TransformationEngineConfig configuration section.
        /// </summary>
        protected override Dictionary<string, string[]> FieldNotes { get; } = new()
        {
            ["ConfigPath"] = new[]
            {
                "This is the path to the JSON file containing transformation rules.",
                "",
                "The transformation rules file defines how tracking parameters from your iPhone",
                "are mapped and transformed before being sent to VTube Studio on PC.",
                "",
                "Default location: \u001b[96m'Configs/vts_transforms.json'\u001b[0m",
                "You can place this file anywhere, but keep it in the \u001b[96m'Configs'\u001b[0m folder for organization."
            },
            ["MaxEvaluationIterations"] = new[]
            {
                "This controls how many times the transformation engine will evaluate",
                "parameter dependencies to ensure all transformations are applied correctly.",
                "",
                "Higher values allow for more complex dependency chains but may impact performance.",
                "Lower values may not fully resolve all parameter dependencies.",
                "",
                "Recommended range: \u001b[38;5;215m5-20\u001b[0m (default: \u001b[38;5;215m10\u001b[0m)"
            }
        };


        /// <summary>
        /// Field defaults for the TransformationEngineConfig configuration section.
        /// </summary>
        protected override Dictionary<string, object> FieldDefaults { get; } = new()
        {
            ["ConfigPath"] = "Configs/vts_transforms.json",
            ["MaxEvaluationIterations"] = 10
        };


        /// <summary>
        /// Initializes a new instance of the TransformationEngineConfigRemediationService class.
        /// </summary>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="console">Console abstraction for I/O</param>
        public TransformationEngineConfigRemediationService(
            IConfigSectionValidatorsFactory validatorsFactory,
            IConsole console) : base(validatorsFactory, ConfigSectionTypes.TransformationEngineConfig, console)
        {
        }

        /// <summary>
        /// Determines if a field is eligible for validation pass-through (skipping validation).
        /// </summary>
        /// <param name="workingFields">The working fields</param>
        /// <param name="activeFieldName">The name of the active field</param>
        /// <returns>True if the field is eligible for pass-through, false otherwise</returns>
        protected override bool IsEligibleForPassThru(List<ConfigFieldState> workingFields, string activeFieldName)
        {
            return false;
        }

        /// <summary>
        /// Builds the splash/header lines for the remediation process.
        /// </summary>
        /// <param name="issues">The issues to build the splash lines from</param>
        /// <returns>The built splash lines</returns>
        protected override string[] BuildSplashLines(List<FieldValidationIssue> issues)
        {
            var lines = new List<string>
            {
                "=== Transformation Engine Configuration - Remediation ===",
                "The following fields need attention:"
            };

            foreach (var issue in issues)
            {
                var detail = string.IsNullOrWhiteSpace(issue.ProvidedValueText)
                    ? issue.Description
                    : $"{issue.Description} (provided: '{ConsoleColors.Colorize(issue.ProvidedValueText, ConsoleColors.Error)}')";
                lines.Add($" - {ConsoleColors.ColorizeBasicType(detail)}");
            }

            lines.Add("");
            lines.Add("Press Enter to start remediation...");
            return lines.ToArray();
        }

        /// <summary>
        /// Checks if all fields are missing.
        /// </summary>
        /// <param name="fields">The fields to check</param>
        /// <returns>True if all fields are missing</returns>
        protected override bool IsAllFieldsMissing(List<ConfigFieldState> fields)
        {
            // Check if all required fields are missing (not present or null)
            bool Missing(string name)
            {
                var f = fields.FirstOrDefault(x => x.FieldName == name);
                return f == null || !f.IsPresent || f.Value == null;
            }

            var missingConfigPath = Missing("ConfigPath");
            var missingMaxIterations = Missing("MaxEvaluationIterations");

            return missingConfigPath && missingMaxIterations;
        }

        /// <summary>
        /// Applies defaults to missing fields.
        /// </summary>
        /// <param name="fields">The fields to apply defaults to</param>
        /// <returns>The fields with defaults applied</returns>
        protected override List<ConfigFieldState> ApplyDefaultsToMissingFields(List<ConfigFieldState> fields)
        {
            var result = new List<ConfigFieldState>(fields);

            // Apply defaults for missing fields
            var defaults = new Dictionary<string, object>
            {
                ["ConfigPath"] = "Configs/vts_transforms.json",
                ["MaxEvaluationIterations"] = 10
            };

            foreach (var (fieldName, defaultValue) in defaults)
            {
                var existing = result.FirstOrDefault(f => f.FieldName == fieldName);
                if (existing == null || !existing.IsPresent || existing.Value == null)
                {
                    var defaultField = new ConfigFieldState(
                        fieldName,
                        defaultValue,
                        true,
                        defaultValue.GetType(),
                        GetFieldDescription(fieldName));

                    if (existing != null)
                    {
                        var idx = result.IndexOf(existing);
                        result[idx] = defaultField;
                    }
                    else
                    {
                        result.Add(defaultField);
                    }
                }
            }

            return result;
        }

        private static string GetFieldDescription(string fieldName)
        {
            return fieldName switch
            {
                "ConfigPath" => "Path to Transformation Rules JSON File",
                "MaxEvaluationIterations" => "Maximum Evaluation Iterations for Parameter Dependencies",
                _ => fieldName
            };
        }

        /// <summary>
        /// Builds the field frame for the TransformationEngineConfig configuration section.
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
                "=== Transformation Engine Configuration - Remediation ==="
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
        /// Creates a TransformationEngineConfig from the field states.
        /// </summary>
        protected override IConfigSection CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
        {
            var config = new TransformationEngineConfig();

            foreach (var field in fieldsState)
            {
                if (field.IsPresent && field.Value != null)
                {
                    switch (field.FieldName)
                    {
                        case "ConfigPath" when field.Value is string configPath:
                            config.ConfigPath = configPath;
                            break;
                        case "MaxEvaluationIterations" when field.Value is int maxIterations:
                            config.MaxEvaluationIterations = maxIterations;
                            break;
                    }
                }
            }

            return config;
        }
    }
}
