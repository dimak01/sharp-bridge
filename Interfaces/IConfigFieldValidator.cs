using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for validating individual configuration fields.
    /// Provides reusable validation methods for common field types across different configuration sections.
    /// </summary>
    public interface IConfigFieldValidator
    {
        /// <summary>
        /// Validates that a field contains a valid port number (1-65535).
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        FieldValidationIssue? ValidatePort(ConfigFieldState field);

        /// <summary>
        /// Validates that a field contains a valid IP address.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        FieldValidationIssue? ValidateIpAddress(ConfigFieldState field);

        /// <summary>
        /// Validates that a field contains a valid host address (hostname or IP address).
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        FieldValidationIssue? ValidateHost(ConfigFieldState field);

        /// <summary>
        /// Validates that a field contains a valid boolean value.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        FieldValidationIssue? ValidateBoolean(ConfigFieldState field);

        /// <summary>
        /// Validates that a field contains a valid string value.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <param name="allowEmpty">Whether empty strings are allowed (default: false)</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        FieldValidationIssue? ValidateString(ConfigFieldState field, bool allowEmpty = false);
    }
}
