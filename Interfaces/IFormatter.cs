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
    public interface IFormatter<T> where T : IFormattableObject
    {
        /// <summary>
        /// Formats an entity into a display string
        /// </summary>
        string Format(T entity, VerbosityLevel verbosity);
    }
} 