namespace SharpBridge.Models
{
    /// <summary>
    /// Represents the current operational status of the transformation engine
    /// </summary>
    public enum TransformationEngineStatus
    {
        /// <summary>
        /// All loaded rules are valid and actively processing data
        /// </summary>
        AllRulesValid,
        
        /// <summary>
        /// Some rules are active and working, others have issues
        /// </summary>
        RulesPartiallyValid,
        
        /// <summary>
        /// Hot-reload failed, continuing with previously loaded rules
        /// </summary>
        ConfigErrorCached,
        
        /// <summary>
        /// Configuration loaded but no rules passed validation
        /// </summary>
        NoValidRules,
        
        /// <summary>
        /// Initial state, no configuration load attempted
        /// </summary>
        NeverLoaded,
        
        /// <summary>
        /// Configuration file not found, no fallback rules available
        /// </summary>
        ConfigMissing
    }
} 