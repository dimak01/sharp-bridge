using System;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Validators
{
    /// <summary>
    /// Validator for GeneralSettingsConfig configuration sections.
    /// </summary>
    public class GeneralSettingsConfigValidator : BaseConfigSectionValidator
    {
        /// <summary>
        /// Initializes a new instance of the GeneralSettingsConfigValidator class.
        /// </summary>
        /// <param name="fieldValidator">The field validator for common validation operations</param>
        public GeneralSettingsConfigValidator(IConfigFieldValidator fieldValidator)
            : base(fieldValidator)
        {
        }

        /// <summary>
        /// Gets the list of field names that should be ignored during validation.
        /// </summary>
        /// <returns>Array of field names to ignore (none for this config)</returns>
        protected override string[] GetIgnoredFields()
        {
            return Array.Empty<string>();
        }

        /// <summary>
        /// Validates a specific field's value according to business rules.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        protected override FieldValidationIssue? ValidateFieldValue(ConfigFieldState field)
        {
            return field.FieldName switch
            {
                "EditorCommand" => ValidateEditorCommand(field),
                "Shortcuts" => ValidateShortcuts(field),
                _ => CreateUnknownFieldError(field)
            };
        }

        /// <summary>
        /// Gets the configuration type name for error messages.
        /// </summary>
        /// <returns>The configuration type name</returns>
        protected override string GetConfigTypeName()
        {
            return "GeneralSettingsConfig";
        }

        /// <summary>
        /// Creates a validation error for unknown fields with custom formatting.
        /// </summary>
        /// <param name="field">The unknown field</param>
        /// <returns>FieldValidationIssue for the unknown field</returns>
        protected override FieldValidationIssue CreateUnknownFieldError(ConfigFieldState field)
        {
            return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                $"Unknown field '{field.FieldName}' in {GetConfigTypeName()}", FormatForDisplay(field.Value));
        }

        /// <summary>
        /// Validates the EditorCommand field.
        /// </summary>
        /// <param name="field">The EditorCommand field to validate</param>
        /// <returns>Validation result</returns>
        private static FieldValidationIssue? ValidateEditorCommand(ConfigFieldState field)
        {
            if (field.Value is not string editorCommand || string.IsNullOrWhiteSpace(editorCommand))
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    "External editor command cannot be null or empty", FormatForDisplay(field.Value));
            }

            return null;
        }

        /// <summary>
        /// Validates the Shortcuts field.
        /// </summary>
        /// <param name="field">The Shortcuts field to validate</param>
        /// <returns>Validation result</returns>
        private static FieldValidationIssue? ValidateShortcuts(ConfigFieldState field)
        {
            if (field.Value is not Dictionary<string, string> shortcuts || shortcuts.Count == 0)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    "Keyboard shortcuts dictionary cannot be null or empty", FormatForDisplay(field.Value));
            }

            return null;
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

