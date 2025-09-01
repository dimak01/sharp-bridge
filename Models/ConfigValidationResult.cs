using System.Collections.Generic;
using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Represents the result of validating a configuration section.
    /// </summary>
    public class ConfigValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the ConfigValidationResult class.
        /// </summary>
        /// <param name="missingFields">List of fields that are missing or invalid</param>
        public ConfigValidationResult(List<MissingField> missingFields)
        {
            MissingFields = missingFields ?? new List<MissingField>();
        }

        /// <summary>
        /// Gets whether the configuration section is valid.
        /// </summary>
        public bool IsValid => MissingFields.Count == 0;

        /// <summary>
        /// Gets the list of missing or invalid fields.
        /// </summary>
        public List<MissingField> MissingFields { get; }
    }

    /// <summary>
    /// Represents a missing or invalid configuration field.
    /// </summary>
    public class MissingField
    {
        /// <summary>
        /// Initializes a new instance of the MissingField class.
        /// </summary>
        /// <param name="fieldName">The name of the missing field</param>
        /// <param name="expectedType">The expected type for this field</param>
        /// <param name="description">Human-readable description of the field</param>
        public MissingField(string fieldName, Type expectedType, string description)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            ExpectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        /// <summary>
        /// Gets the name of the missing field.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the expected type for this field.
        /// </summary>
        public Type ExpectedType { get; }

        /// <summary>
        /// Gets the human-readable description of the field.
        /// </summary>
        public string Description { get; }
    }
}
