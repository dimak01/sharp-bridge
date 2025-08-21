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
        private bool _cursorVisible = true;

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
        /// Gets or sets whether the cursor is visible (simulated)
        /// </summary>
        public bool CursorVisible
        {
            get => _cursorVisible;
            set => _cursorVisible = value;
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
        /// Writes multiple lines to the captured output (simulated flicker-free rendering)
        /// </summary>
        /// <param name="outputLines">Array of lines to write</param>
        public void WriteLines(string[] outputLines)
        {
            // For testing, we'll simulate the behavior by clearing and writing all lines
            _outputBuilder.Clear();

            foreach (var line in outputLines)
            {
                _outputBuilder.AppendLine(line);
            }
        }
    }
}