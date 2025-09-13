using System.Collections.Generic;
using SharpBridge.Models.Domain;

namespace SharpBridge.Models.Events
{
    /// <summary>
    /// Contains the result of loading transformation rules from a source
    /// </summary>
    public class RulesLoadResult
    {
        /// <summary>
        /// List of successfully validated transformation rules
        /// </summary>
        public List<ParameterTransformation> ValidRules { get; }

        /// <summary>
        /// List of rules that failed validation
        /// </summary>
        public List<RuleInfo> InvalidRules { get; }

        /// <summary>
        /// List of validation error messages
        /// </summary>
        public List<string> ValidationErrors { get; }

        /// <summary>
        /// Indicates whether this result was loaded from cache due to a loading error
        /// </summary>
        public bool LoadedFromCache { get; }

        /// <summary>
        /// Error message if loading failed and cache was used
        /// </summary>
        public string? LoadError { get; }

        /// <summary>
        /// Creates a new instance of RulesLoadResult
        /// </summary>
        /// <param name="validRules">Successfully validated transformation rules</param>
        /// <param name="invalidRules">Rules that failed validation</param>
        /// <param name="validationErrors">Validation error messages</param>
        /// <param name="loadedFromCache">Whether this result was loaded from cache</param>
        /// <param name="loadError">Error message if loading failed</param>
        public RulesLoadResult(
            List<ParameterTransformation> validRules,
            List<RuleInfo> invalidRules,
            List<string> validationErrors,
            bool loadedFromCache = false,
            string? loadError = null)
        {
            ValidRules = validRules ?? new List<ParameterTransformation>();
            InvalidRules = invalidRules ?? new List<RuleInfo>();
            ValidationErrors = validationErrors ?? new List<string>();
            LoadedFromCache = loadedFromCache;
            LoadError = loadError;
        }
    }
}