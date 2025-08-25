using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of IConsole that wraps the System.Console class
    /// </summary>
    public class SystemConsole : IConsole
    {
        private string[] _lastRenderedLines = Array.Empty<string>();
        /// <summary>
        /// Gets the width of the console window
        /// </summary>
        public int WindowWidth => Console.WindowWidth;

        /// <summary>
        /// Gets the height of the console window
        /// </summary>
        public int WindowHeight => Console.WindowHeight;

        /// <summary>
        /// Gets or sets whether the cursor is visible
        /// </summary>
        public bool CursorVisible
        {
            get => Console.CursorVisible;
            set => Console.CursorVisible = value;
        }

        /// <summary>
        /// Sets the position of the console cursor (private - used internally by WriteLines)
        /// </summary>
        private static void SetCursorPosition(int left, int top)
        {
            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Writes text to the console without a line break (private - all output should go through WriteLines)
        /// </summary>
        private static void Write(string text)
        {
            Console.Write(text);
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

        /// <summary>
        /// Writes multiple lines to the console using flicker-free rectangular buffer approach.
        /// Normalizes all content to a rectangular buffer of WindowWidth × WindowHeight.
        /// Only updates lines that have changed since the last call (shadow buffer optimization).
        /// </summary>
        /// <param name="outputLines">Array of lines to write</param>
        public void WriteLines(string[] outputLines)
        {
            // Skip flicker-free rendering if output is redirected
            if (Console.IsOutputRedirected)
            {
                foreach (var line in outputLines)
                {
                    Console.WriteLine(line);
                }
                _lastRenderedLines = NormalizeToRectangularBuffer(outputLines);
                return;
            }

            // Store original cursor visibility to restore later
            bool originalCursorVisible = false;
            bool cursorVisibilityChanged = false;

            try
            {
                // Hide cursor during rendering to prevent flicker
                originalCursorVisible = Console.CursorVisible;
                if (originalCursorVisible)
                {
                    Console.CursorVisible = false;
                    cursorVisibilityChanged = true;
                }

                // Normalize input to rectangular buffer
                var normalizedLines = NormalizeToRectangularBuffer(outputLines);

                // Compare with shadow buffer and update only changed lines
                for (int row = 0; row < normalizedLines.Length; row++)
                {
                    string newLine = normalizedLines[row];
                    string oldLine = row < _lastRenderedLines.Length ? _lastRenderedLines[row] : "";

                    // Skip this line if it hasn't changed
                    if (newLine == oldLine)
                        continue;

                    // Update the entire line (it's already normalized to WindowWidth)
                    SetCursorPosition(0, row);
                    Write(newLine);
                }

                // Store normalized buffer as shadow buffer for next comparison
                _lastRenderedLines = normalizedLines;

                // Reset cursor position to bottom of content
                SetCursorPosition(0, Math.Min(outputLines.Length, WindowHeight - 1));
            }
            finally
            {
                // Always restore cursor visibility
                if (cursorVisibilityChanged)
                {
                    try
                    {
                        Console.CursorVisible = originalCursorVisible;
                    }
                    catch
                    {
                        // Ignore cursor visibility restoration errors
                    }
                }
            }
        }

        /// <summary>
        /// Normalizes input lines to a rectangular buffer of WindowWidth × WindowHeight
        /// Uses visual length calculation to properly handle ANSI color codes
        /// </summary>
        /// <param name="inputLines">Input lines to normalize</param>
        /// <returns>Rectangular buffer with all lines exactly WindowWidth visual characters</returns>
        private string[] NormalizeToRectangularBuffer(string[] inputLines)
        {
            int width = WindowWidth;
            int height = WindowHeight;

            var buffer = new string[height];

            for (int row = 0; row < height; row++)
            {
                if (row < inputLines.Length && !string.IsNullOrEmpty(inputLines[row]))
                {
                    string line = inputLines[row];
                    int visualLength = ConsoleColors.GetVisualLength(line);

                    // Handle visual length vs target width
                    if (visualLength > width)
                    {
                        // Truncate while preserving color codes
                        buffer[row] = TruncateVisually(line, width);
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

        /// <summary>
        /// Truncates a string to a specific visual width while preserving ANSI color codes
        /// </summary>
        /// <param name="text">Text that may contain ANSI escape sequences</param>
        /// <param name="maxVisualWidth">Maximum visual width (excluding ANSI codes)</param>
        /// <returns>Truncated string with preserved color codes</returns>
        private static string TruncateVisually(string text, int maxVisualWidth)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var result = new System.Text.StringBuilder(text.Length);
            int visualPosition = 0;

            for (int i = 0; i < text.Length; i++)
            {
                // Check for ANSI escape sequence
                if (text[i] == '\u001b' && i + 1 < text.Length && text[i + 1] == '[')
                {
                    // Find the end of the ANSI sequence (ends with 'm')
                    int sequenceEnd = text.IndexOf('m', i + 2);
                    if (sequenceEnd != -1)
                    {
                        // Copy the entire ANSI sequence (doesn't count toward visual width)
                        result.Append(text.Substring(i, sequenceEnd - i + 1));
                        i = sequenceEnd; // Skip to end of sequence
                        continue;
                    }
                }

                // Regular visible character
                if (visualPosition >= maxVisualWidth)
                    break; // Stop at visual width limit

                result.Append(text[i]);
                visualPosition++;
            }

            return result.ToString();
        }
    }
}