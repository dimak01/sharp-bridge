using System;
using System.Text;
using SharpBridge.Interfaces;

namespace SharpBridge.Tests.Utilities
{
    /// <summary>
    /// Implementation of IConsole for unit testing that captures output
    /// and provides safe no-op implementations of console operations
    /// </summary>
    public class TestConsole : IConsole
    {
        private readonly StringBuilder _outputBuilder = new StringBuilder();
        private int _cursorLeft = 0;
        private int _cursorTop = 0;
        private int? _savedWindowWidth;
        private int? _savedWindowHeight;
        
        /// <summary>
        /// Gets the captured console output as a string
        /// </summary>
        public string Output => _outputBuilder.ToString();
        
        /// <summary>
        /// Clears the captured output
        /// </summary>
        public void ClearOutput()
        {
            _outputBuilder.Clear();
        }
        
        /// <summary>
        /// Gets a simulated console width (default 80)
        /// </summary>
        public int WindowWidth { get; set; } = 80;
        
        /// <summary>
        /// Gets a simulated console height (default 25)
        /// </summary>
        public int WindowHeight { get; set; } = 25;
        
        /// <summary>
        /// Sets the cursor position (simulated)
        /// </summary>
        public void SetCursorPosition(int left, int top)
        {
            _cursorLeft = left;
            _cursorTop = top;
        }
        
        /// <summary>
        /// Writes text to the captured output
        /// </summary>
        public void Write(string text)
        {
            _outputBuilder.Append(text);
        }
        
        /// <summary>
        /// Writes text followed by a newline to the captured output
        /// </summary>
        public void WriteLine(string text)
        {
            _outputBuilder.AppendLine(text);
        }
        
        /// <summary>
        /// Clears the console (no-op in test environment)
        /// </summary>
        public void Clear()
        {
            // No-op in test environment
            ClearOutput();
        }
        
        /// <summary>
        /// Simulates setting the console window size
        /// </summary>
        /// <param name="width">Width in characters</param>
        /// <param name="height">Height in characters</param>
        /// <returns>True if the operation was successful</returns>
        public bool TrySetWindowSize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return false;
                
            WindowWidth = width;
            WindowHeight = height;
            return true;
        }
        
        /// <summary>
        /// Saves the current simulated console window size
        /// </summary>
        public void SaveCurrentWindowSize()
        {
            _savedWindowWidth = WindowWidth;
            _savedWindowHeight = WindowHeight;
        }
        
        /// <summary>
        /// Restores previously saved simulated console window size
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