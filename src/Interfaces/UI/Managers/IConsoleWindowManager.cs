// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;

namespace SharpBridge.Interfaces.UI.Managers
{
    /// <summary>
    /// Interface for managing console window settings and size change tracking
    /// </summary>
    public interface IConsoleWindowManager : IDisposable
    {
        /// <summary>
        /// Sets the console window to the specified size
        /// </summary>
        /// <param name="width">Preferred width in characters</param>
        /// <param name="height">Preferred height in characters</param>
        /// <returns>True if the window was resized successfully</returns>
        bool SetConsoleSize(int width, int height);

        /// <summary>
        /// Gets the current console window dimensions
        /// </summary>
        /// <returns>Tuple containing width and height</returns>
        (int width, int height) GetCurrentSize();

        /// <summary>
        /// Starts tracking console size changes and automatically saves them to user preferences
        /// </summary>
        /// <param name="updatePreferencesCallback">Callback to update and save user preferences when size changes</param>
        void StartSizeChangeTracking(Action<int, int> updatePreferencesCallback);

        /// <summary>
        /// Stops tracking console size changes
        /// </summary>
        void StopSizeChangeTracking();

        /// <summary>
        /// Processes console size changes if tracking is enabled.
        /// Should be called regularly (e.g., in main loop) to detect changes.
        /// </summary>
        void ProcessSizeChanges();
    }
}