using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Validators
{
    /// <summary>
    /// Validator for GeneralSettingsConfig configuration sections.
    /// </summary>
    public class GeneralSettingsConfigValidator : IConfigSectionValidator
    {
        /// <summary>
        /// Validates a GeneralSettingsConfig section's fields and returns validation results.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>Validation result indicating if the section is valid and what fields need attention</returns>
        public ConfigValidationResult ValidateSection(List<ConfigFieldState> fieldsState)
        {
            var issues = new List<FieldValidationIssue>();

            foreach (var field in fieldsState)
            {
                var (isValid, issue) = ValidateSingleField(field);
                if (!isValid && issue != null)
                {
                    issues.Add(issue);
                }
            }

            return new ConfigValidationResult(issues);
        }

        /// <summary>
        /// Validates a single field from the GeneralSettingsConfig section.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>A tuple indicating if the field is valid and any validation issue</returns>
        public (bool IsValid, FieldValidationIssue? Issue) ValidateSingleField(ConfigFieldState field)
        {
            // Check if field is present and has a value
            if (!field.IsPresent || field.Value == null)
            {
                return (false, new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Required field '{field.Description}' is missing", null));
            }

            // Validate field based on its name - simple null/empty checks only
            return field.FieldName switch
            {
                "EditorCommand" => ValidateEditorCommand(field),
                "Shortcuts" => ValidateShortcuts(field),
                _ => (false, CreateUnknownFieldError(field))
            };
        }

        /// <summary>
        /// Validates the EditorCommand field.
        /// </summary>
        /// <param name="field">The EditorCommand field to validate</param>
        /// <returns>Validation result</returns>
        private static (bool IsValid, FieldValidationIssue? Issue) ValidateEditorCommand(ConfigFieldState field)
        {
            if (field.Value is not string editorCommand || string.IsNullOrWhiteSpace(editorCommand))
            {
                return (false, new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    "External editor command cannot be null or empty", FormatForDisplay(field.Value)));
            }

            return (true, null);
        }

        /// <summary>
        /// Validates the Shortcuts field.
        /// </summary>
        /// <param name="field">The Shortcuts field to validate</param>
        /// <returns>Validation result</returns>
        private static (bool IsValid, FieldValidationIssue? Issue) ValidateShortcuts(ConfigFieldState field)
        {
            if (field.Value is not Dictionary<string, string> shortcuts || shortcuts.Count == 0)
            {
                return (false, new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    "Keyboard shortcuts dictionary cannot be null or empty", FormatForDisplay(field.Value)));
            }

            return (true, null);
        }

        /// <summary>
        /// Creates an error for unknown fields.
        /// </summary>
        /// <param name="field">The unknown field</param>
        /// <returns>Validation issue for unknown field</returns>
        private static FieldValidationIssue CreateUnknownFieldError(ConfigFieldState field)
        {
            return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                $"Unknown field '{field.FieldName}' in GeneralSettingsConfig", FormatForDisplay(field.Value));
        }

        /// <summary>
        /// Formats a value for display in error messages, truncating long strings.
        /// </summary>
        /// <param name="value">The value to format</param>
        /// <returns>Formatted string for display</returns>
        private static string? FormatForDisplay(object? value)
        {
            if (value is string s)
            {
                const int max = 128;
                return s.Length > max ? s.Substring(0, max - 3) + "..." : s;
            }

            return value?.ToString();
        }
    }
}

