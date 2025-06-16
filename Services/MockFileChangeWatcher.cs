using System;

namespace SharpBridge.Services
{
    /// <summary>
    /// Mock implementation of IFileChangeWatcher for testing.
    /// </summary>
    public sealed class MockFileChangeWatcher : IFileChangeWatcher
    {
        /// <summary>
        /// Event raised when a file change is simulated.
        /// </summary>
        public event EventHandler<FileChangeEventArgs>? FileChanged;

        private string? _filePath;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MockFileChangeWatcher class.
        /// </summary>
        public MockFileChangeWatcher()
        {
        }

        /// <summary>
        /// Starts watching the specified file path (stored for simulation purposes).
        /// </summary>
        /// <param name="filePath">The path to the file to watch.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the watcher has been disposed.</exception>
        public void StartWatching(string filePath)
        {
            ThrowIfDisposed();
            _filePath = filePath;
        }

        /// <summary>
        /// Stops watching the current file.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the watcher has been disposed.</exception>
        public void StopWatching()
        {
            ThrowIfDisposed();
            _filePath = null;
        }

        /// <summary>
        /// Simulates a file change event for testing.
        /// </summary>
        /// <param name="filePath">Optional file path to use for the event. If not provided, uses the last watched file path or a default value.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the watcher has been disposed.</exception>
        public void SimulateFileChange(string? filePath = null)
        {
            ThrowIfDisposed();
            FileChanged?.Invoke(this, new FileChangeEventArgs(filePath ?? _filePath ?? "mock.txt"));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MockFileChangeWatcher));
            }
        }

        /// <summary>
        /// Releases all resources used by the MockFileChangeWatcher.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                StopWatching();
                _disposed = true;
            }
        }
    }
} 