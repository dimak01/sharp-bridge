using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Utility class for managing console window settings temporarily
    /// </summary>
    public class ConsoleWindowManager : IDisposable
    {
        private readonly IConsole _console;
        private bool _settingsSaved = false;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the ConsoleWindowManager class
        /// </summary>
        /// <param name="console">The console instance to manage</param>
        public ConsoleWindowManager(IConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        /// <summary>
        /// Sets the console window to the specified size, saving current settings for restoration
        /// </summary>
        /// <param name="width">Preferred width in characters</param>
        /// <param name="height">Preferred height in characters</param>
        /// <returns>True if the window was resized successfully</returns>
        public bool SetTemporarySize(int width, int height)
        {
            if (!_settingsSaved)
            {
                _console.SaveCurrentWindowSize();
                _settingsSaved = true;
            }

            return _console.TrySetWindowSize(width, height);
        }

        /// <summary>
        /// Restores the console window to its original size
        /// </summary>
        /// <returns>True if the window was restored successfully</returns>
        public bool RestoreOriginalSize()
        {
            if (_settingsSaved)
            {
                return _console.TryRestoreWindowSize();
            }
            return false;
        }

        /// <summary>
        /// Gets the current console window dimensions
        /// </summary>
        /// <returns>Tuple containing width and height</returns>
        public (int width, int height) GetCurrentSize()
        {
            return (_console.WindowWidth, _console.WindowHeight);
        }

        /// <summary>
        /// Disposes the manager and restores original console settings
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                RestoreOriginalSize();
                _disposed = true;
            }
        }
    }
} 