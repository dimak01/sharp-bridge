using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Event arguments for when transformation rules file changes
    /// </summary>
    public class RulesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The path to the file that changed
        /// </summary>
        public string FilePath { get; }
        
        /// <summary>
        /// The timestamp when the change was detected
        /// </summary>
        public DateTime ChangeTime { get; }
        
        /// <summary>
        /// Creates a new instance of RulesChangedEventArgs
        /// </summary>
        /// <param name="filePath">The path to the file that changed</param>
        public RulesChangedEventArgs(string filePath)
        {
            FilePath = filePath ?? string.Empty;
            ChangeTime = DateTime.UtcNow;
        }
    }
} 