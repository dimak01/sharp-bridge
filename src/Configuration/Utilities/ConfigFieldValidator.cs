// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration.Services.Validators;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Configuration.Utilities
{
    /// <summary>
    /// Implementation of IConfigFieldValidator providing reusable validation methods for common field types.
    /// </summary>
    public partial class ConfigFieldValidator : IConfigFieldValidator
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
                return CreateValidationIssue(field, $"Port value must be an integer, got {field.Value?.GetType().Name ?? "null"}");
            }

            // Check if port is in valid range (1-65535)
            if (port < 1 || port > 65535)
            {
                return CreateValidationIssue(field, $"Port {port} is out of valid range (1-65535)");
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
                return CreateValidationIssue(field, "IP address cannot be null or empty");
            }

            // Check if it's a valid IP address
            if (!IPAddress.TryParse(ipAddress, out _))
            {
                return CreateValidationIssue(field, $"'{ipAddress}' is not a valid IP address");
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
                return CreateValidationIssue(field, "Host address cannot be null or empty");
            }

            // Check if it's a valid hostname or IP address
            if (!IsValidHost(host))
            {
                return CreateValidationIssue(field, $"'{host}' is not a valid host address (must be a valid hostname or IP address)");
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
                return CreateValidationIssue(field, $"Boolean value must be true or false, got {field.Value?.GetType().Name ?? "null"}");
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
            // Handle null values
            if (field.Value == null)
                return allowEmpty ? null : CreateValidationIssue(field, "String value cannot be null");

            // Handle non-string types
            if (field.ExpectedType != field.Value.GetType() ||
                field.Value is not string str)
                return CreateValidationIssue(field, $"{field.ExpectedType.Name} value expected, got {field.Value.GetType().Name}");

            // Handle empty/whitespace strings
            if (!allowEmpty && string.IsNullOrWhiteSpace(str))
                return CreateValidationIssue(field, "String value cannot be null or empty");

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
                return CreateValidationIssue(field, $"Value must be an integer, got {field.Value?.GetType().Name ?? "null"}");
            }

            if (value < minValue || value > maxValue)
            {
                return CreateValidationIssue(field, $"Value {value} is out of valid range ({minValue}-{maxValue})");
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
                return CreateValidationIssue(field, "File path cannot be null or empty");
            }

            try
            {
                ValidatePathFormat(path);
                var invalidChars = FindInvalidCharacters(path);
                
                if (invalidChars.Any())
                {
                    return CreateValidationIssue(field, $"File path contains invalid characters: {string.Join(", ", invalidChars)}");
                }

                return null; // Validation passed
            }
            catch (ArgumentException ex)
            {
                return CreateValidationIssue(field, $"Invalid file path format: {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                return CreateValidationIssue(field, $"Unsupported file path: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates the path format using Path.GetFullPath
        /// </summary>
        /// <param name="path">The path to validate</param>
        private static void ValidatePathFormat(string path)
        {
            // Use Path.GetFullPath to validate the path format
            // This will throw an exception if the path contains invalid characters
            _ = Path.GetFullPath(path);
        }

        /// <summary>
        /// Finds invalid characters in the path, allowing drive separators
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>List of invalid characters found</returns>
        private static List<char> FindInvalidCharacters(string path)
        {
            var invalidChars = GetInvalidPathCharacters();
            var foundInvalidChars = new List<char>();

            for (int i = 0; i < path.Length; i++)
            {
                if (invalidChars.Contains(path[i]))
                {
                    // Special case: allow colon if it's followed by backslash (drive separator)
                    if (IsDriveSeparator(path, i))
                    {
                        continue; // Skip this colon as it's a drive separator
                    }

                    if (!foundInvalidChars.Contains(path[i]))
                    {
                        foundInvalidChars.Add(path[i]);
                    }
                }
            }

            return foundInvalidChars;
        }

        /// <summary>
        /// Gets the set of invalid path characters
        /// </summary>
        /// <returns>Array of invalid path characters</returns>
        private static char[] GetInvalidPathCharacters()
        {
            return Path.GetInvalidPathChars()
                .Union(Path.GetInvalidFileNameChars())
                .Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar)
                .ToArray();
        }

        /// <summary>
        /// Checks if a character at the given position is a drive separator (colon followed by backslash)
        /// </summary>
        /// <param name="path">The path string</param>
        /// <param name="index">The character index</param>
        /// <returns>True if it's a drive separator</returns>
        private static bool IsDriveSeparator(string path, int index)
        {
            return path[index] == ':' && 
                   index + 1 < path.Length && 
                   path[index + 1] == '\\';
        }

        /// <summary>
        /// Checks if a string is a valid host address (hostname or IP address).
        /// </summary>
        /// <param name="host">The host string to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        private static bool IsValidHost(string host)
        {
            // Check if it's a valid IP address first
            if (IPAddress.TryParse(host, out _))
                return true;

            // Use Uri.CheckHostName for hostname validation
            var hostNameType = Uri.CheckHostName(host);
            return hostNameType == UriHostNameType.Dns;
        }


        [System.Text.RegularExpressions.GeneratedRegex(@"^[a-zA-Z0-9_]+$", System.Text.RegularExpressions.RegexOptions.None, 100)]
        private static partial System.Text.RegularExpressions.Regex ParameterPrefixRegex();

        /// <summary>
        /// Validates that a field contains a valid parameter prefix for VTube Studio PC.
        /// </summary>
        /// <param name="field">The field to validate</param>
        /// <returns>FieldValidationIssue if validation fails, null if validation passes</returns>
        public FieldValidationIssue? ValidateParameterPrefix(ConfigFieldState field)
        {
            if (field.Value is not string prefix)
            {
                return CreateValidationIssue(field, $"Parameter prefix must be a string, got {field.Value?.GetType().Name ?? "null"}");
            }

            // Check length (0-15 characters, empty is allowed)
            if (prefix.Length > 15)
            {
                return CreateValidationIssue(field, "Parameter prefix cannot exceed 15 characters");
            }

            // Check format (alphanumeric and underscores only, no spaces)
            if (!string.IsNullOrEmpty(prefix) && !ParameterPrefixRegex().IsMatch(prefix))
            {
                return CreateValidationIssue(field, "Parameter prefix must contain only alphanumeric characters and underscores, no spaces");
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Creates a validation issue with consistent formatting.
        /// </summary>
        /// <param name="field">The field being validated</param>
        /// <param name="message">The validation error message</param>
        /// <returns>A FieldValidationIssue with the provided message</returns>
        private static FieldValidationIssue CreateValidationIssue(ConfigFieldState field, string message)
        {
            return new FieldValidationIssue(field.FieldName, field.ExpectedType, message, FormatForDisplay(field.Value));
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
