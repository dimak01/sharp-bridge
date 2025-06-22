using System;
using System.IO;
using System.Threading;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Watches a file for changes using FileSystemWatcher and raises events.
    /// </summary>
    public sealed class FileSystemChangeWatcher : IFileChangeWatcher
    {
        private readonly IAppLogger _logger;
        private readonly IFileSystemWatcherFactory _watcherFactory;
        private IFileSystemWatcherWrapper? _watcher;
        private string? _currentFilePath;
        private DateTime _lastEventTime;
        private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(200);
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// Event raised when the watched file changes.
        /// </summary>
        public event EventHandler<FileChangeEventArgs>? FileChanged;

        /// <summary>
        /// Initializes a new instance of the FileSystemChangeWatcher class.
        /// </summary>
        /// <param name="logger">The logger to use for recording file system events.</param>
        /// <param name="watcherFactory">The factory to create file system watcher instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or watcherFactory is null.</exception>
        public FileSystemChangeWatcher(IAppLogger logger, IFileSystemWatcherFactory watcherFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _watcherFactory = watcherFactory ?? throw new ArgumentNullException(nameof(watcherFactory));
        }

        /// <summary>
        /// Starts watching the specified file for changes.
        /// </summary>
        /// <param name="filePath">The path to the file to watch.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the watcher has been disposed.</exception>
        public void StartWatching(string filePath)
        {
            ThrowIfDisposed();
            StopWatching();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                _logger.Warning($"FileSystemChangeWatcher: File '{filePath}' does not exist.");
                return;
            }
            _currentFilePath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(_currentFilePath);
            var fileName = Path.GetFileName(_currentFilePath);
            _watcher = _watcherFactory.Create(directory ?? ".", fileName);
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
            _watcher.EnableRaisingEvents = true;
            _watcher.IncludeSubdirectories = false;
            _watcher.Changed += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Error += OnWatcherError;
            _logger.Info($"FileSystemChangeWatcher: Started watching '{_currentFilePath}'.");
        }

        /// <summary>
        /// Stops watching the current file.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the watcher has been disposed.</exception>
        public void StopWatching()
        {
            ThrowIfDisposed();
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnFileChanged;
                _watcher.Renamed -= OnFileChanged;
                _watcher.Deleted -= OnFileChanged;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
                _watcher = null;
                _logger.Info($"FileSystemChangeWatcher: Stopped watching '{_currentFilePath}'.");
            }
            _currentFilePath = null;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_disposed) return;
            
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastEventTime) < _debounceInterval)
                    return;
                _lastEventTime = now;
            }
            _logger.Info($"FileSystemChangeWatcher: Detected change in '{e.FullPath}'.");
            FileChanged?.Invoke(this, new FileChangeEventArgs(e.FullPath));
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.Error($"FileSystemChangeWatcher: Watcher error: {e.GetException()?.Message}");
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        /// <summary>
        /// Releases all resources used by the FileSystemChangeWatcher.
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