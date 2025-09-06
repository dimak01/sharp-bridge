using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Validators
{
    /// <summary>
    /// Validator for TransformationEngineConfig configuration sections.
    /// </summary>
    public class TransformationEngineConfigValidator : IConfigSectionValidator
    {
        private readonly IConfigFieldValidator _fieldValidator;

        /// <summary>
        /// Initializes a new instance of the TransformationEngineConfigValidator class.
        /// </summary>
        /// <param name="fieldValidator">The field validator for common validation operations</param>
        public TransformationEngineConfigValidator(IConfigFieldValidator fieldValidator)
        {
            _fieldValidator = fieldValidator ?? throw new ArgumentNullException(nameof(fieldValidator));
        }

        /// <summary>
        /// Validates a TransformationEngineConfig section's fields and returns validation results.
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
        /// Validates a single field from the TransformationEngineConfig section.
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

            // Validate field based on its name
            return field.FieldName switch
            {
                "ConfigPath" => ValidateConfigPath(field),
                "MaxEvaluationIterations" => ValidateMaxEvaluationIterations(field),
                _ => (false, CreateUnknownFieldError(field))
            };
        }

        /// <summary>
        /// Validates the ConfigPath field.
        /// </summary>
        /// <param name="field">The ConfigPath field to validate</param>
        /// <returns>Validation result</returns>
        private (bool IsValid, FieldValidationIssue? Issue) ValidateConfigPath(ConfigFieldState field)
        {
            var issue = _fieldValidator.ValidateFilePath(field);
            return (issue == null, issue);
        }

        /// <summary>
        /// Validates the MaxEvaluationIterations field.
        /// </summary>
        /// <param name="field">The MaxEvaluationIterations field to validate</param>
        /// <returns>Validation result</returns>
        private (bool IsValid, FieldValidationIssue? Issue) ValidateMaxEvaluationIterations(ConfigFieldState field)
        {
            var issue = _fieldValidator.ValidateIntegerRange(field, 1, 50);
            return (issue == null, issue);
        }

        /// <summary>
        /// Creates an error for unknown fields.
        /// </summary>
        /// <param name="field">The unknown field</param>
        /// <returns>Validation issue for unknown field</returns>
        private static FieldValidationIssue CreateUnknownFieldError(ConfigFieldState field)
        {
            return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                $"Unknown field '{field.FieldName}' in TransformationEngineConfig", FormatForDisplay(field.Value));
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
