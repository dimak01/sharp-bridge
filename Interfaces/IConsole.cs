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
    }
} 