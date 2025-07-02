using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Utility class for managing console window settings with size change tracking
    /// </summary>
    public class ConsoleWindowManager : IConsoleWindowManager
    {
        private readonly IConsole _console;
        private readonly IAppLogger _logger;
        private bool _disposed = false;

        // Size change tracking
        private bool _sizeTrackingEnabled = false;
        private Action<int, int>? _updatePreferencesCallback;
        private (int width, int height) _lastKnownSize;

        /// <summary>
        /// Initializes a new instance of the ConsoleWindowManager class
        /// </summary>
        /// <param name="console">The console instance to manage</param>
        /// <param name="logger">Logger for debugging and error reporting</param>
        public ConsoleWindowManager(IConsole console, IAppLogger logger)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize last known size
            _lastKnownSize = GetCurrentSize();
        }

        /// <summary>
        /// Sets the console window to the specified size
        /// </summary>
        /// <param name="width">Preferred width in characters</param>
        /// <param name="height">Preferred height in characters</param>
        /// <returns>True if the window was resized successfully</returns>
        public bool SetConsoleSize(int width, int height)
        {
            var success = _console.TrySetWindowSize(width, height);

            // Update last known size if resize was successful
            if (success)
            {
                _lastKnownSize = (width, height);
            }

            return success;
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
        /// Starts tracking console size changes and automatically saves them to user preferences
        /// </summary>
        /// <param name="updatePreferencesCallback">Callback to update and save user preferences when size changes</param>
        public void StartSizeChangeTracking(Action<int, int> updatePreferencesCallback)
        {
            _updatePreferencesCallback = updatePreferencesCallback ?? throw new ArgumentNullException(nameof(updatePreferencesCallback));
            _sizeTrackingEnabled = true;
            _lastKnownSize = GetCurrentSize();

            _logger.Debug("Console size change tracking started. Current size: {0}x{1}", _lastKnownSize.width, _lastKnownSize.height);
        }

        /// <summary>
        /// Stops tracking console size changes
        /// </summary>
        public void StopSizeChangeTracking()
        {
            _sizeTrackingEnabled = false;
            _updatePreferencesCallback = null;

            _logger.Debug("Console size change tracking stopped");
        }

        /// <summary>
        /// Processes console size changes if tracking is enabled.
        /// Should be called regularly (e.g., in main loop) to detect changes.
        /// </summary>
        public void ProcessSizeChanges()
        {
            if (!_sizeTrackingEnabled || _updatePreferencesCallback == null)
            {
                return;
            }

            try
            {
                var currentSize = GetCurrentSize();

                // Check if console size has changed from last known size
                if (currentSize.width != _lastKnownSize.width ||
                    currentSize.height != _lastKnownSize.height)
                {
                    _logger.Debug("Console size changed from {0}x{1} to {2}x{3}",
                        _lastKnownSize.width, _lastKnownSize.height,
                        currentSize.width, currentSize.height);

                    // Update last known size
                    _lastKnownSize = currentSize;

                    // Trigger callback to update preferences
                    _updatePreferencesCallback(currentSize.width, currentSize.height);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash - console size tracking is not critical
                _logger.Error("Error processing console size changes: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Disposes the manager - console size is preserved between application runs
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                StopSizeChangeTracking();
                // No longer restore original size - let console size persist
                _disposed = true;
            }
        }
    }
}