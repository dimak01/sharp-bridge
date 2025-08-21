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
        /// Writes multiple lines to the console using flicker-free in-place updating.
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
                _lastRenderedLines = (string[])outputLines.Clone();
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

                int windowWidth = WindowWidth - 1;
                int maxLines = Math.Max(outputLines.Length, _lastRenderedLines.Length);

                // Only update lines that have changed
                for (int i = 0; i < maxLines; i++)
                {
                    // Ensure we don't exceed console boundaries
                    if (i >= WindowHeight - 1)
                        break;

                    string newLine = i < outputLines.Length ? outputLines[i] : "";
                    string oldLine = i < _lastRenderedLines.Length ? _lastRenderedLines[i] : "";

                    // Skip this line if it hasn't changed
                    if (newLine == oldLine)
                        continue;

                    SetCursorPosition(0, i);
                    Write(newLine);

                    // Clear the remainder of the line if the new line is shorter than the old one
                    int clearCount = Math.Max(0, oldLine.Length - newLine.Length);
                    if (clearCount > 0 && newLine.Length + clearCount <= windowWidth)
                    {
                        Write(new string(' ', clearCount));
                    }
                    // If new line is longer, ensure we don't exceed window width
                    else if (newLine.Length > windowWidth)
                    {
                        SetCursorPosition(0, i);
                        Write(newLine.Substring(0, windowWidth));
                    }
                }

                // Store the current output as our shadow buffer for next comparison
                _lastRenderedLines = (string[])outputLines.Clone();

                // Reset cursor position to the end of our content
                SetCursorPosition(0, Math.Min(outputLines.Length, WindowHeight - 1));

                // Restore cursor visibility
                if (cursorVisibilityChanged)
                {
                    Console.CursorVisible = originalCursorVisible;
                }
            }
            catch (Exception)
            {
                // Ensure cursor visibility is restored even on error
                if (cursorVisibilityChanged)
                {
                    try
                    {
                        Console.CursorVisible = originalCursorVisible;
                    }
                    catch
                    {
                        // If we can't restore cursor visibility, don't let that crash the app
                    }
                }

                // If flicker-free rendering fails, fall back to simple line-by-line output
                foreach (var line in outputLines)
                {
                    Console.WriteLine(line);
                }
                _lastRenderedLines = (string[])outputLines.Clone();
                throw; // Re-throw to let caller know something went wrong
            }
        }
    }
}