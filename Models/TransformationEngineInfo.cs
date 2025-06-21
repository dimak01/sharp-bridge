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
        /// List of rules that failed validation during loading or couldn't be evaluated during transformation
        /// </summary>
        public IReadOnlyList<RuleInfo> InvalidRules { get; }
        
        /// <summary>
        /// Gets whether the currently loaded configuration is up to date with the file on disk
        /// </summary>
        public bool IsConfigUpToDate { get; }
        
        /// <summary>
        /// Creates a new instance of TransformationEngineInfo
        /// </summary>
        /// <param name="configFilePath">The path to the configuration file</param>
        /// <param name="validRulesCount">The number of valid rules loaded</param>
        /// <param name="invalidRules">List of rules that failed validation or couldn't be evaluated</param>
        /// <param name="isConfigUpToDate">Whether the configuration is up to date</param>
        public TransformationEngineInfo(
            string configFilePath, 
            int validRulesCount,
            IReadOnlyList<RuleInfo>? invalidRules = null,
            bool isConfigUpToDate = true)
        {
            ConfigFilePath = configFilePath ?? string.Empty;
            ValidRulesCount = validRulesCount;
            InvalidRules = invalidRules ?? new List<RuleInfo>();
            IsConfigUpToDate = isConfigUpToDate;
        }
    }
} 