using System;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Abstraction over console operations to enable better testability and flexibility
    /// </summary>
    public interface IConsole
    {
        /// <summary>
        /// Gets the width of the console window
        /// </summary>
        int WindowWidth { get; }
        
        /// <summary>
        /// Gets the height of the console window
        /// </summary>
        int WindowHeight { get; }
        
        /// <summary>
        /// Sets the position of the console cursor
        /// </summary>
        /// <param name="left">The column position</param>
        /// <param name="top">The row position</param>
        void SetCursorPosition(int left, int top);
        
        /// <summary>
        /// Writes text to the console without a line break
        /// </summary>
        /// <param name="text">The text to write</param>
        void Write(string text);
        
        /// <summary>
        /// Writes text to the console followed by a line break
        /// </summary>
        /// <param name="text">The text to write</param>
        void WriteLine(string text);
        
        /// <summary>
        /// Clears the console screen
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Temporarily sets the console window size (in characters)
        /// </summary>
        /// <param name="width">Width in characters</param>
        /// <param name="height">Height in characters</param>
        /// <returns>True if the operation was successful</returns>
        bool TrySetWindowSize(int width, int height);
        
        /// <summary>
        /// Saves the current console window size for later restoration
        /// </summary>
        void SaveCurrentWindowSize();
        
        /// <summary>
        /// Restores previously saved console window size
        /// </summary>
        /// <returns>True if settings were restored successfully</returns>
        bool TryRestoreWindowSize();
    }
} 