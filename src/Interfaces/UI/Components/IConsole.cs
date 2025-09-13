// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;

namespace SharpBridge.Interfaces.UI.Components
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
        /// Clears the console screen
        /// </summary>
        void Clear();

        /// <summary>
        /// Sets the console window size (in characters)
        /// </summary>
        /// <param name="width">Width in characters</param>
        /// <param name="height">Height in characters</param>
        /// <returns>True if the operation was successful</returns>
        bool TrySetWindowSize(int width, int height);

        /// <summary>
        /// Writes multiple lines to the console using flicker-free in-place updating.
        /// Overwrites existing content line-by-line and clears any remaining lines.
        /// </summary>
        /// <param name="outputLines">Array of lines to write</param>
        void WriteLines(string[] outputLines);

        /// <summary>
        /// Gets or sets whether the cursor is visible
        /// </summary>
        bool CursorVisible { get; set; }

        /// <summary>
        /// Reads a line of input from the console
        /// </summary>
        /// <returns>The line read from the console</returns>
        string? ReadLine();
    }
}