using System;
using System.ComponentModel;

namespace SharpBridge.Models
{
    /// <summary>
    /// Represents the raw state of a configuration field for validation and remediation purposes.
    /// This record captures all the information needed to determine if a field is valid and how to fix it.
    /// </summary>
    /// <param name="FieldName">The name of the field (e.g., "Host", "Port", "EditorCommand")</param>
    /// <param name="Value">The actual value from the config file (null if missing)</param>
    /// <param name="IsPresent">Whether this field was present in the JSON</param>
    /// <param name="ExpectedType">The expected .NET type for this field</param>
    /// <param name="Description">Human-readable description from [Description] attribute</param>
    public record ConfigFieldState(
        string FieldName,
        object? Value,
        bool IsPresent,
        Type ExpectedType,
        string Description
    );
}
