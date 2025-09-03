using System;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Validators
{
    /// <summary>
    /// Validator for VTubeStudioPhoneClientConfig configuration sections.
    /// </summary>
    public class VTubeStudioPhoneClientConfigValidator : IConfigSectionValidator
    {
        private readonly IConfigFieldValidator _fieldValidator;

        /// <summary>
        /// Initializes a new instance of the VTubeStudioPhoneClientConfigValidator class.
        /// </summary>
        /// <param name="fieldValidator">The field validator for common validation operations</param>
        public VTubeStudioPhoneClientConfigValidator(IConfigFieldValidator fieldValidator)
        {
            _fieldValidator = fieldValidator ?? throw new ArgumentNullException(nameof(fieldValidator));
        }
        /// <summary>
        /// Validates a VTubeStudioPhoneClientConfig section's fields and returns validation results.
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
        /// Validates a single configuration field and returns the validation result.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>Tuple indicating if the field is valid and any validation issue</returns>
        public (bool IsValid, FieldValidationIssue? Issue) ValidateSingleField(ConfigFieldState field)
        {
            // Skip internal settings (JsonIgnore fields) - they have defaults
            if (field.FieldName == "RequestIntervalSeconds" ||
                field.FieldName == "SendForSeconds" ||
                field.FieldName == "ReceiveTimeoutMs" ||
                field.FieldName == "ErrorDelayMs")
            {
                return (true, null);
            }

            // Check if required field is missing
            if (!field.IsPresent || field.Value == null)
            {
                var issue = new FieldValidationIssue(field.FieldName, field.ExpectedType, field.Description, providedValueText: null);
                return (false, issue);
            }

            // Validate field values based on field type
            var validationError = ValidateFieldValue(field);
            if (validationError != null)
            {
                return (false, validationError);
            }

            return (true, null);
        }

        /// <summary>
        /// Validates a specific field's value according to business rules.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        private FieldValidationIssue? ValidateFieldValue(ConfigFieldState field)
        {
            return field.FieldName switch
            {
                "IphoneIpAddress" => _fieldValidator.ValidateIpAddress(field),
                "IphonePort" => _fieldValidator.ValidatePort(field),
                "LocalPort" => _fieldValidator.ValidatePort(field),
                _ => CreateUnknownFieldError(field)
            };
        }

        /// <summary>
        /// Creates a validation error for unknown fields.
        /// </summary>
        /// <param name="field">The unknown field</param>
        /// <returns>FieldValidationIssue for the unknown field</returns>
        private static FieldValidationIssue CreateUnknownFieldError(ConfigFieldState field)
        {
            return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                $"Unknown field '{field.FieldName}' in VTubeStudioPhoneClientConfig",
                field.Value?.ToString());
        }
    }
}
