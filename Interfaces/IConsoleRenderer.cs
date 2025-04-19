using System.Collections.Generic;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for rendering service information to the console or other display
    /// </summary>
    public interface IConsoleRenderer
    {
        /// <summary>
        /// Registers a formatter for a specific entity type
        /// </summary>
        void RegisterFormatter<T>(IFormatter<T> formatter) where T : IFormattableObject;

        /// <summary>
        /// Gets a formatter for the specified type
        /// </summary>
        IFormatter<T> GetFormatter<T>() where T : IFormattableObject;

        /// <summary>
        /// Updates the display with service statistics
        /// </summary>
        void Update<T>(IEnumerable<IServiceStats<T>> stats) where T : IFormattableObject;

        /// <summary>
        /// Clears the console
        /// </summary>
        void ClearConsole();
    }
} 