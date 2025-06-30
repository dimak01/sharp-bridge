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

        /// <summary>
        /// Sets the console window size (in characters)
        /// </summary>
        /// <param name="width">Width in characters</param>
        /// <param name="height">Height in characters</param>
        /// <returns>True if the operation was successful</returns>
        public bool TrySetWindowSize(int width, int height)
        {
            try
            {
                // Don't attempt to resize if output is redirected
                if (Console.IsOutputRedirected)
                    return false;

                // Validate dimensions
                if (width <= 0 || height <= 0)
                    return false;

                Console.SetWindowSize(width, height);

                return true;
            }
            catch (Exception)
            {
                // Silently fail - console resizing can fail for various reasons
                // (terminal doesn't support it, insufficient permissions, etc.)
                return false;
            }
        }
    }
}