using System;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration.Services.Validators;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Configuration.Services.Validators
{
    /// <summary>
    /// Base class for configuration section validators that provides common validation logic.
    /// </summary>
    public abstract class BaseConfigSectionValidator : IConfigSectionValidator
    {
        private readonly IConfigFieldValidator _fieldValidator;

        /// <summary>
        /// Initializes a new instance of the BaseConfigSectionValidator class.
        /// </summary>
        /// <param name="fieldValidator">The field validator for common validation operations</param>
        protected BaseConfigSectionValidator(IConfigFieldValidator fieldValidator)
        {
            _fieldValidator = fieldValidator ?? throw new ArgumentNullException(nameof(fieldValidator));
        }

        /// <summary>
        /// Validates a configuration section's fields and returns validation results.
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
            if (IsIgnoredField(field.FieldName))
            {
                return (true, null);
            }

            // Check if required field is missing
            if (!field.IsPresent || field.Value == null)
            {
                var issue = new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Required field '{field.Description}' is missing", providedValueText: null);
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
        /// Determines if a field should be ignored during validation (typically internal settings with defaults).
        /// </summary>
        /// <param name="fieldName">The name of the field to check</param>
        /// <returns>True if the field should be ignored, false otherwise</returns>
        protected virtual bool IsIgnoredField(string fieldName)
        {
            var ignoredFields = GetIgnoredFields();
            return Array.Exists(ignoredFields, field => field == fieldName);
        }

        /// <summary>
        /// Gets the list of field names that should be ignored during validation.
        /// </summary>
        /// <returns>Array of field names to ignore</returns>
        protected abstract string[] GetIgnoredFields();

        /// <summary>
        /// Validates a specific field's value according to business rules.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        protected abstract FieldValidationIssue? ValidateFieldValue(ConfigFieldState field);

        /// <summary>
        /// Gets the configuration type name for error messages.
        /// </summary>
        /// <returns>The configuration type name</returns>
        protected abstract string GetConfigTypeName();

        /// <summary>
        /// Creates a validation error for unknown fields.
        /// </summary>
        /// <param name="field">The unknown field</param>
        /// <returns>FieldValidationIssue for the unknown field</returns>
        protected virtual FieldValidationIssue CreateUnknownFieldError(ConfigFieldState field)
        {
            return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                $"Unknown field '{field.FieldName}' in {GetConfigTypeName()}",
                field.Value?.ToString());
        }

        /// <summary>
        /// Gets the field validator for use in derived classes.
        /// </summary>
        protected IConfigFieldValidator FieldValidator => _fieldValidator;
    }
}
