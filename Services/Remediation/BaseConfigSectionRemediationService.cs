

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;

namespace SharpBridge.Services.Remediation
{
    /// <summary>
    /// Base class for configuration section remediation services.
    /// </summary>
    public abstract class BaseConfigSectionRemediationService : IConfigSectionRemediationService
    {
        private readonly IConfigSectionValidator _validator;
        private readonly IConsole _console;

        /// <summary>
        /// Field notes for the configuration section.
        /// </summary>
        protected abstract Dictionary<string, string[]> FieldNotes { get; }

        /// <summary>
        /// Field defaults for the configuration section.
        /// </summary>
        protected abstract Dictionary<string, object> FieldDefaults { get; }


        /// <summary>
        /// Initializes a new instance of the BaseConfigSectionRemediationService class.
        /// </summary>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="sectionType">The type of configuration section to remediate</param>
        /// <param name="console">Console abstraction for I/O</param>
        protected BaseConfigSectionRemediationService(
            IConfigSectionValidatorsFactory validatorsFactory,
            ConfigSectionTypes sectionType,
            IConsole console)
        {
            _validator = validatorsFactory.GetValidator(sectionType);
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

        /// <summary>
        /// Sorts validation issues by field order to ensure dependencies are handled correctly.
        /// </summary>
        /// <param name="issues">The validation issues to sort</param>
        /// <returns>Issues sorted by field order</returns>
        protected virtual List<FieldValidationIssue> SortIssuesByFieldOrder(List<FieldValidationIssue> issues)
        {
            return issues;
        }

        /// <summary>
        /// Creates a configuration section from the field states.
        /// </summary>
        /// <param name="fieldsState">The field states to create the configuration section from</param>
        /// <returns>The created configuration section</returns>
        protected abstract IConfigSection CreateConfigFromFieldStates(List<ConfigFieldState> fieldsState);

        /// <summary>
        /// Determines if a field is eligible for validation pass-through (skipping validation).
        /// </summary>
        /// <param name="workingFields">The working fields</param>
        /// <param name="activeFieldName">The name of the active field</param>
        /// <returns>True if the field is eligible for pass-through, false otherwise</returns>
        protected abstract bool IsEligibleForPassThru(List<ConfigFieldState> workingFields, string activeFieldName);

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

        /// <summary>
        /// Builds the splash/header lines for the remediation process.
        /// </summary>
        /// <param name="issues">The issues to build the splash lines from</param>
        /// <returns>The built splash lines</returns>
        protected abstract string[] BuildSplashLines(List<FieldValidationIssue> issues);

        /// <summary>
        /// Checks if all fields are missing.
        /// </summary>
        /// <param name="fields">The fields to check</param>
        /// <returns>True if all fields are missing</returns>
        protected abstract bool IsAllFieldsMissing(List<ConfigFieldState> fields);

        /// <summary>
        /// Applies defaults to missing fields.
        /// </summary>
        /// <param name="fields">The fields to apply defaults to</param>
        /// <returns>The fields with defaults applied</returns>
        protected abstract List<ConfigFieldState> ApplyDefaultsToMissingFields(List<ConfigFieldState> fields);

        /// <summary>
        /// Gets the notes for a field.
        /// </summary>
        /// <param name="fieldName">The name of the field to get the notes for</param>
        /// <returns>The notes for the field</returns>
        protected string[]? GetFieldNotes(string fieldName)
        {
            return FieldNotes.TryGetValue(fieldName, out var notes) ? notes : null;
        }

        /// <summary>
        /// Builds the field frame for the remediation process.
        /// </summary>
        /// <param name="activeField">The active field</param>
        /// <param name="notes">The notes for the field</param>
        /// <param name="errorText">The error text for the field</param>
        /// <returns>The built field frame</returns>
        protected abstract string[] BuildFieldFrame(ConfigFieldState activeField, string[]? notes, string? errorText);

        /// <summary>
        /// Gets the default value for a field.
        /// </summary>
        /// <param name="fieldName">The name of the field to get the default value for</param>
        /// <returns>The default value for the field</returns>
        protected object? GetFieldDefault(string fieldName)
        {
            return FieldDefaults.TryGetValue(fieldName, out var defaultValue) ? defaultValue : null;
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
    }
}