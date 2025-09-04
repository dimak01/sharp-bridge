using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Remediation
{
    /// <summary>
    /// Remediation service for TransformationEngineConfig configuration sections.
    /// </summary>
    public class TransformationEngineConfigRemediationService : IConfigSectionRemediationService
    {
        private readonly IConfigSectionValidator _validator;
        private readonly IConsole _console;

        /// <summary>
        /// Initializes a new instance of the TransformationEngineConfigRemediationService class.
        /// </summary>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="console">Console abstraction for I/O</param>
        public TransformationEngineConfigRemediationService(
            IConfigSectionValidatorsFactory validatorsFactory,
            IConsole console)
        {
            _validator = validatorsFactory.GetValidator(ConfigSectionTypes.TransformationEngineConfig);
            _console = console;
        }

        /// <summary>
        /// Remediates configuration issues for a TransformationEngineConfig section by fixing missing or invalid fields.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>A tuple indicating the remediation result and the updated configuration section (if changes were made)</returns>
        public async Task<(RemediationResult Result, IConfigSection? UpdatedConfig)> Remediate(List<ConfigFieldState> fieldsState)
        {
            var workingFields = new List<ConfigFieldState>(fieldsState);

            // Check if all fields are missing - if so, silently fill with defaults and return
            if (IsAllFieldsMissing(workingFields))
            {
                var defaultConfig = CreateConfigFromFieldStates(ApplyDefaultsToMissingFields(workingFields));
                return (RemediationResult.Succeeded, defaultConfig);
            }

            // First validation: check if section is already valid
            var initialValidation = _validator.ValidateSection(workingFields);
            if (initialValidation.IsValid)
            {
                // Section was already valid, no remediation needed
                return (RemediationResult.NoRemediationNeeded, null);
            }

            // Show splash screen and wait for user to start
            var splash = BuildSplashLines(initialValidation.Issues);
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

        private static string[] BuildSplashLines(List<FieldValidationIssue> issues)
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
                    : $"{issue.Description} (provided: '{issue.ProvidedValueText}')";
                lines.Add($" - {detail}");
            }

            lines.Add("");
            lines.Add("Press Enter to start remediation...");
            return lines.ToArray();
        }

        private static bool IsAllFieldsMissing(List<ConfigFieldState> fields)
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

        private static List<ConfigFieldState> ApplyDefaultsToMissingFields(List<ConfigFieldState> fields)
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

        private static readonly Dictionary<string, string[]> FieldNotes = new()
        {
            ["ConfigPath"] = new[]
            {
                "This is the path to the JSON file containing transformation rules.",
                "",
                "The transformation rules file defines how tracking parameters from your iPhone",
                "are mapped and transformed before being sent to VTube Studio on PC.",
                "",
                "Default location: Configs/vts_transforms.json",
                "You can place this file anywhere, but keep it in the Configs folder for organization."
            },
            ["MaxEvaluationIterations"] = new[]
            {
                "This controls how many times the transformation engine will evaluate",
                "parameter dependencies to ensure all transformations are applied correctly.",
                "",
                "Higher values allow for more complex dependency chains but may impact performance.",
                "Lower values may not fully resolve all parameter dependencies.",
                "",
                "Recommended range: 5-20 (default: 10)"
            }
        };

        private static string[]? GetFieldNotes(string fieldName)
        {
            return FieldNotes.TryGetValue(fieldName, out var notes) ? notes : null;
        }

        private static readonly Dictionary<string, object> FieldDefaults = new()
        {
            ["ConfigPath"] = "Configs/vts_transforms.json",
            ["MaxEvaluationIterations"] = 10
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
                "=== Transformation Engine Configuration - Remediation ==="
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
        /// Creates a TransformationEngineConfig from the field states.
        /// </summary>
        private static TransformationEngineConfig CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState)
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

        private static string GetFieldDescription(string fieldName)
        {
            return fieldName switch
            {
                "ConfigPath" => "Path to Transformation Rules JSON File",
                "MaxEvaluationIterations" => "Maximum Evaluation Iterations for Parameter Dependencies",
                _ => fieldName
            };
        }
    }
}
