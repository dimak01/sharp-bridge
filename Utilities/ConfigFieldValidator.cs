using System;
using System.IO;
using System.Linq;
using System.Net;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of IConfigFieldValidator providing reusable validation methods for common field types.
    /// </summary>
    public class ConfigFieldValidator : IConfigFieldValidator
    {
        /// <summary>
        /// Validates that a field contains a valid port number (1-65535).
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        public FieldValidationIssue? ValidatePort(ConfigFieldState field)
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

        /// <summary>
        /// Validates that a field contains a valid IP address.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        public FieldValidationIssue? ValidateIpAddress(ConfigFieldState field)
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
        /// Validates that a field contains a valid host address (hostname or IP address).
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        public FieldValidationIssue? ValidateHost(ConfigFieldState field)
        {
            if (field.Value is not string host || string.IsNullOrWhiteSpace(host))
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    "Host address cannot be null or empty", FormatForDisplay(field.Value));
            }

            // Check if it's a valid hostname or IP address
            if (!IsValidHost(host))
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"'{host}' is not a valid host address (must be a valid hostname or IP address)", host);
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Validates that a field contains a valid boolean value.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        public FieldValidationIssue? ValidateBoolean(ConfigFieldState field)
        {
            if (field.Value is not bool)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Boolean value must be true or false, got {field.Value?.GetType().Name ?? "null"}", FormatForDisplay(field.Value));
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Validates that a field contains a valid string value.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <param name="allowEmpty">Whether empty strings are allowed (default: false)</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        public FieldValidationIssue? ValidateString(ConfigFieldState field, bool allowEmpty = false)
        {
            if (field.Value is not string str)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"String value expected, got {field.Value?.GetType().Name ?? "null"}", FormatForDisplay(field.Value));
            }

            if (!allowEmpty && string.IsNullOrWhiteSpace(str))
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    "String value cannot be null or empty", FormatForDisplay(field.Value));
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Validates that a field contains a valid integer within the specified range.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <param name="minValue">The minimum allowed value (inclusive)</param>
        /// <param name="maxValue">The maximum allowed value (inclusive)</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        public FieldValidationIssue? ValidateIntegerRange(ConfigFieldState field, int minValue, int maxValue)
        {
            if (field.Value is not int value)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Value must be an integer, got {field.Value?.GetType().Name ?? "null"}", FormatForDisplay(field.Value));
            }

            if (value < minValue || value > maxValue)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Value {value} is out of valid range ({minValue}-{maxValue})", value.ToString());
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Validates that a field contains a valid file path.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        public FieldValidationIssue? ValidateFilePath(ConfigFieldState field)
        {
            if (field.Value is not string path || string.IsNullOrWhiteSpace(path))
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    "File path cannot be null or empty", FormatForDisplay(field.Value));
            }

            try
            {
                // Use Path.GetFullPath to validate the path format
                // This will throw an exception if the path contains invalid characters
                _ = Path.GetFullPath(path);

                // Check for invalid characters in the path
                var invalidChars = Path.GetInvalidPathChars();
                if (path.IndexOfAny(invalidChars) >= 0)
                {
                    return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                        $"File path contains invalid characters: {string.Join(", ", invalidChars.Where(c => path.Contains(c)))}", path);
                }

                return null; // Validation passed
            }
            catch (ArgumentException ex)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Invalid file path format: {ex.Message}", path);
            }
            catch (NotSupportedException ex)
            {
                return new FieldValidationIssue(field.FieldName, field.ExpectedType,
                    $"Unsupported file path: {ex.Message}", path);
            }
        }

        /// <summary>
        /// Checks if a string is a valid host address (hostname or IP address).
        /// </summary>
        /// <param name="host">The host string to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private static bool IsValidHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            // Check if it's a valid IP address first
            if (IPAddress.TryParse(host, out _))
                return true;

            // Use Uri.CheckHostName for hostname validation
            var hostNameType = Uri.CheckHostName(host);
            return hostNameType == UriHostNameType.Dns;
        }

        /// <summary>
        /// Formats a value for display in error messages, truncating long strings.
        /// </summary>
        /// <param name="value">The value to format</param>
        /// <returns>Formatted string for display</returns>
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
