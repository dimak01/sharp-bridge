using System.Collections.Generic;
using SharpBridge.Interfaces;

namespace SharpBridge.Models
{
    /// <summary>
    /// Contains current state information about the transformation engine for display purposes
    /// </summary>
    public class TransformationEngineInfo : IFormattableObject
    {
        /// <summary>
        /// The path to the configuration file being used
        /// </summary>
        public string ConfigFilePath { get; }
        
        /// <summary>
        /// The number of rules that are currently loaded and valid
        /// </summary>
        public int ValidRulesCount { get; }
        
        /// <summary>
        /// List of rules that failed validation during loading
        /// </summary>
        public IReadOnlyList<RuleInfo> InvalidRules { get; }
        
        /// <summary>
        /// List of rules that couldn't be evaluated in the last transformation due to missing dependencies or other evaluation errors
        /// </summary>
        public IReadOnlyList<RuleInfo> AbandonedRules { get; }
        
        /// <summary>
        /// Creates a new instance of TransformationEngineInfo
        /// </summary>
        /// <param name="configFilePath">The path to the configuration file</param>
        /// <param name="validRulesCount">The number of valid rules loaded</param>
        /// <param name="invalidRules">List of rules that failed validation</param>
        /// <param name="abandonedRules">List of rules that couldn't be evaluated</param>
        public TransformationEngineInfo(
            string configFilePath, 
            int validRulesCount,
            IReadOnlyList<RuleInfo> invalidRules = null,
            IReadOnlyList<RuleInfo> abandonedRules = null)
        {
            ConfigFilePath = configFilePath ?? string.Empty;
            ValidRulesCount = validRulesCount;
            InvalidRules = invalidRules ?? new List<RuleInfo>();
            AbandonedRules = abandonedRules ?? new List<RuleInfo>();
        }
    }
} 