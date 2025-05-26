using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of IConsole that wraps the System.Console class
    /// </summary>
    public class SystemConsole : IConsole
    {
        private int? _savedWindowWidth;
        private int? _savedWindowHeight;
        
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
        /// Temporarily sets the console window size (in characters)
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
        
        /// <summary>
        /// Saves the current console window size for later restoration
        /// </summary>
        public void SaveCurrentWindowSize()
        {
            try
            {
                if (!Console.IsOutputRedirected)
                {
                    _savedWindowWidth = Console.WindowWidth;
                    _savedWindowHeight = Console.WindowHeight;
                }
            }
            catch (Exception)
            {
                // If we can't read the current size, we can't save it
                _savedWindowWidth = null;
                _savedWindowHeight = null;
            }
        }
        
        /// <summary>
        /// Restores previously saved console window size
        /// </summary>
        /// <returns>True if settings were restored successfully</returns>
        public bool TryRestoreWindowSize()
        {
            if (_savedWindowWidth.HasValue && _savedWindowHeight.HasValue)
            {
                return TrySetWindowSize(_savedWindowWidth.Value, _savedWindowHeight.Value);
            }
            return false;
        }
    }
} 