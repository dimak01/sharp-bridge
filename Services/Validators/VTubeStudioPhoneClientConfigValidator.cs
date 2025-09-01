using System.Collections.Generic;
using System.Net;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.Validators
{
    /// <summary>
    /// Validator for VTubeStudioPhoneClientConfig configuration sections.
    /// </summary>
    public class VTubeStudioPhoneClientConfigValidator : IConfigSectionValidator
    {
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
        /// <returns>MissingField if validation fails, null if validation passes</returns>
        private static FieldValidationIssue? ValidateFieldValue(ConfigFieldState field)
        {
            switch (field.FieldName)
            {
                case "IphoneIpAddress":
                    return ValidateIpAddress(field);

                case "IphonePort":
                case "LocalPort":
                    return ValidatePort(field);

                default:
                    // Unknown field - this shouldn't happen but let's be defensive
                    return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                        $"Unknown field '{field.FieldName}' in VTubeStudioPhoneClientConfig", FormatForDisplay(field.Value));
            }
        }

        /// <summary>
        /// Validates that an IP address field contains a valid IP address.
        /// </summary>
        /// <param name="field">The IP address field to validate</param>
        /// <returns>MissingField if validation fails, null if validation passes</returns>
        private static FieldValidationIssue? ValidateIpAddress(ConfigFieldState field)
        {
            if (field.Value is not string ipAddress || string.IsNullOrWhiteSpace(ipAddress))
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    "IP address cannot be null or empty", FormatForDisplay(field.Value));
            }

            // Check if it's a valid IP address
            if (!IPAddress.TryParse(ipAddress, out _))
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"'{ipAddress}' is not a valid IP address", ipAddress);
            }

            // Optional: Check if it's not localhost (127.0.0.1) for production use
            // This could be configurable based on environment
            if (ipAddress == "127.0.0.1" || ipAddress == "localhost")
            {
                // Warning: This might be localhost - ensure this is intended for development
                // For now, we'll allow it but could add a warning or make this configurable
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Validates that a port field contains a valid port number.
        /// </summary>
        /// <param name="field">The port field to validate</param>
        /// <returns>MissingField if validation fails, null if validation passes</returns>
        private static FieldValidationIssue? ValidatePort(ConfigFieldState field)
        {
            if (field.Value is not int port)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Port value must be an integer, got {field.Value?.GetType().Name ?? "null"}", FormatForDisplay(field.Value));
            }

            // Check if port is in valid range (1-65535)
            if (port < 1 || port > 65535)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Port {port} is out of valid range (1-65535)", port.ToString());
            }

            // Check for common reserved ports (optional, could be configurable)
            if (port <= 1024)
            {
                // Warning: Ports 1-1024 are privileged ports on Unix systems
                // This might require elevated privileges to bind to
                // For now, we'll allow it but could add a warning
            }

            return null; // Validation passed
        }

        private static string? FormatForDisplay(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is string s)
            {
                const int max = 128;
                return s.Length > max ? s.Substring(0, max - 3) + "..." : s;
            }

            return value.ToString();
        }
    }
}
