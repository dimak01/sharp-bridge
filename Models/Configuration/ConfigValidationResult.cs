using System.Collections.Generic;
using System;

namespace SharpBridge.Models.Configuration
{
    /// <summary>
    /// Represents the result of validating a configuration section.
    /// </summary>
    public class ConfigValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the ConfigValidationResult class.
        /// </summary>
        /// <param name="issues">List of fields that are missing or invalid</param>
        public ConfigValidationResult(List<FieldValidationIssue> issues)
        {
            Issues = issues ?? new List<FieldValidationIssue>();
        }

        /// <summary>
        /// Gets whether the configuration section is valid.
        /// </summary>
        public bool IsValid => Issues.Count == 0;

        /// <summary>
        /// Gets the list of missing or invalid fields.
        /// </summary>
        public List<FieldValidationIssue> Issues { get; }
    }

    /// <summary>
    /// Represents a missing or invalid configuration field.
    /// </summary>
    public class FieldValidationIssue
    {
        /// <summary>
        /// Initializes a new instance of the FieldValidationIssue class.
        /// </summary>
        /// <param name="fieldName">The name of the field</param>
        /// <param name="expectedType">The expected type for this field</param>
        /// <param name="description">Human-readable description of the issue</param>
        /// <param name="providedValueText">Optional string representation of the provided value (for display)</param>
        public FieldValidationIssue(string fieldName, Type expectedType, string description, string? providedValueText = null)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            ExpectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            ProvidedValueText = providedValueText;
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the expected type for this field.
        /// </summary>
        public Type ExpectedType { get; }

        /// <summary>
        /// Gets the human-readable description of the issue.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the optional string representation of the provided value (for display purposes only).
        /// </summary>
        public string? ProvidedValueText { get; }
    }
}
