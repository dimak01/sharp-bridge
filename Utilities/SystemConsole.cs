using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of IConsole that wraps the System.Console class
    /// </summary>
    public class SystemConsole : IConsole
    {
        /// <summary>
        /// Gets the width of the console window
        /// </summary>
        public int WindowWidth => Console.WindowWidth;
        
        /// <summary>
        /// Gets the height of the console window
        /// </summary>
        public int WindowHeight => Console.WindowHeight;
        
        /// <summary>
        /// Sets the position of the console cursor
        /// </summary>
        public void SetCursorPosition(int left, int top)
        {
            Console.SetCursorPosition(left, top);
        }
        
        /// <summary>
        /// Writes text to the console without a line break
        /// </summary>
        public void Write(string text)
        {
            Console.Write(text);
        }
        
        /// <summary>
        /// Writes text to the console followed by a line break
        /// </summary>
        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }
        
        /// <summary>
        /// Clears the console screen
        /// </summary>
        public void Clear()
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
        }
    }
} 