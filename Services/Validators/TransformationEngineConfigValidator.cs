using System;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Validators
{
    /// <summary>
    /// Validator for TransformationEngineConfig configuration sections.
    /// </summary>
    public class TransformationEngineConfigValidator : BaseConfigSectionValidator
    {
        /// <summary>
        /// Initializes a new instance of the TransformationEngineConfigValidator class.
        /// </summary>
        /// <param name="fieldValidator">The field validator for common validation operations</param>
        public TransformationEngineConfigValidator(IConfigFieldValidator fieldValidator)
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
                "ConfigPath" => FieldValidator.ValidateFilePath(field),
                "MaxEvaluationIterations" => FieldValidator.ValidateIntegerRange(field, 1, 50),
                _ => CreateUnknownFieldError(field)
            };
        }

        /// <summary>
        /// Gets the configuration type name for error messages.
        /// </summary>
        /// <returns>The configuration type name</returns>
        protected override string GetConfigTypeName()
        {
            return "TransformationEngineConfig";
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
