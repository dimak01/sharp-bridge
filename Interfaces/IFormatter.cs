namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Defines verbosity levels for formatting
    /// </summary>
    public enum VerbosityLevel
    {
        /// <summary>
        /// Basic information only
        /// </summary>
        Basic,
        
        /// <summary>
        /// Standard level of detail
        /// </summary>
        Normal,
        
        /// <summary>
        /// Detailed information for debugging
        /// </summary>
        Detailed
    }
    
    /// <summary>
    /// Interface for formatters that convert entities to display strings
    /// </summary>
    public interface IFormatter
    {
        /// <summary>
        /// Gets or sets the current verbosity level used by this formatter
        /// </summary>
        VerbosityLevel CurrentVerbosity { get; }
        
        /// <summary>
        /// Cycles through the verbosity levels
        /// </summary>
        void CycleVerbosity();
        
        /// <summary>
        /// Formats an entity into a display string using the formatter's current verbosity level
        /// </summary>
        string Format(IFormattableObject entity);
    }
} 