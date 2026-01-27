// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using SharpBridge.Models.Configuration;

namespace SharpBridge.Interfaces.Configuration.Services.Validators
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

        /// <summary>
        /// Validates that a field contains a valid integer within the specified range.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <param name="minValue">The minimum allowed value (inclusive)</param>
        /// <param name="maxValue">The maximum allowed value (inclusive)</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        FieldValidationIssue? ValidateIntegerRange(ConfigFieldState field, int minValue, int maxValue);

        /// <summary>
        /// Validates that a field contains a valid file path.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        FieldValidationIssue? ValidateFilePath(ConfigFieldState field);
    }
}
