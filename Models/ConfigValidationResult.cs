using System.Collections.Generic;
using System.Linq;

namespace SharpBridge.Models
{
    /// <summary>
    /// Result of configuration validation indicating what fields are missing or require setup
    /// </summary>
    public class ConfigValidationResult
    {
        /// <summary>
        /// Initializes a new instance of ConfigValidationResult
        /// </summary>
        /// <param name="missingFields">Collection of fields that are missing or require setup</param>
        public ConfigValidationResult(IEnumerable<MissingField> missingFields)
        {
            MissingFields = missingFields?.ToList() ?? new List<MissingField>();
        }

        /// <summary>
        /// Fields that are missing or require user configuration
        /// </summary>
        public IReadOnlyList<MissingField> MissingFields { get; }

        /// <summary>
        /// Whether the configuration is valid (no missing fields)
        /// </summary>
        public bool IsValid => !MissingFields.Any();

        /// <summary>
        /// Whether first-time setup is required
        /// </summary>
        public bool RequiresSetup => MissingFields.Any();

        /// <summary>
        /// Creates a valid configuration result
        /// </summary>
        public static ConfigValidationResult Valid() => new(new List<MissingField>());

        /// <summary>
        /// Creates an invalid configuration result with the specified missing fields
        /// </summary>
        public static ConfigValidationResult Invalid(params MissingField[] missingFields) => new(missingFields);
    }
}
