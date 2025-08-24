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
            // For testing, we'll simulate the rectangular buffer behavior
            _outputBuilder.Clear();

            var normalizedLines = NormalizeToRectangularBuffer(outputLines);
            foreach (var line in normalizedLines)
            {
                _outputBuilder.AppendLine(line);
            }
        }

        /// <summary>
        /// Normalizes input lines to a rectangular buffer for testing consistency
        /// Uses visual length calculation to properly handle ANSI color codes
        /// </summary>
        /// <param name="inputLines">Input lines to normalize</param>
        /// <returns>Rectangular buffer with all lines exactly WindowWidth visual characters</returns>
        private string[] NormalizeToRectangularBuffer(string[] inputLines)
        {
            int width = WindowWidth;
            int height = WindowHeight - 1; // Reserve last line for cursor positioning

            var buffer = new string[height];

            for (int row = 0; row < height; row++)
            {
                if (row < inputLines.Length && !string.IsNullOrEmpty(inputLines[row]))
                {
                    string line = inputLines[row];
                    int visualLength = SharpBridge.Utilities.ConsoleColors.GetVisualLength(line);

                    // Handle visual length vs target width
                    if (visualLength > width)
                    {
                        // For testing, simple truncation (could implement TruncateVisually if needed)
                        buffer[row] = line.Substring(0, Math.Min(line.Length, width));
                    }
                    else if (visualLength < width)
                    {
                        // Pad to exact visual width (ANSI codes don't count toward padding)
                        buffer[row] = line + new string(' ', width - visualLength);
                    }
                    else
                    {
                        // Perfect visual fit
                        buffer[row] = line;
                    }
                }
                else
                {
                    // Empty line - fill with spaces
                    buffer[row] = new string(' ', width);
                }
            }

            return buffer;
        }
    }
}