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

        /// <summary>
        /// Writes multiple lines to the console using flicker-free in-place updating.
        /// Overwrites existing content line-by-line and clears any remaining lines.
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
                return;
            }

            try
            {
                SetCursorPosition(0, 0);

                int currentLine = 0;
                int windowWidth = WindowWidth - 1;

                // Write each line and clear the remainder of each line
                foreach (var line in outputLines)
                {
                    SetCursorPosition(0, currentLine);
                    Write(line);

                    // Clear the rest of this line (in case previous content was longer)
                    int remainingSpace = windowWidth - line.Length;
                    if (remainingSpace > 0)
                    {
                        Write(new string(' ', remainingSpace));
                    }

                    currentLine++;

                    // Ensure we don't exceed console boundaries
                    if (currentLine >= WindowHeight - 1)
                        break;
                }

                // Clear any remaining lines that might have had content before
                int windowHeight = WindowHeight;
                for (int i = currentLine; i < windowHeight - 1; i++)
                {
                    SetCursorPosition(0, i);
                    Write(new string(' ', windowWidth));
                }

                // Reset cursor position to the end of our content
                SetCursorPosition(0, currentLine);
            }
            catch (Exception)
            {
                // If flicker-free rendering fails, fall back to simple line-by-line output
                foreach (var line in outputLines)
                {
                    Console.WriteLine(line);
                }
                throw; // Re-throw to let caller know something went wrong
            }
        }
    }
}