using System;
using System.IO;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Wrappers;

namespace SharpBridge.Infrastructure.Wrappers
{
    /// <summary>
    /// Wrapper around FileSystemWatcher that implements IFileSystemWatcherWrapper for testability
    /// </summary>
    public class FileSystemWatcherWrapper : IFileSystemWatcherWrapper
    {
        private readonly FileSystemWatcher _watcher;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the FileSystemWatcherWrapper
        /// </summary>
        /// <param name="directory">The directory to monitor</param>
        /// <param name="fileName">The file name pattern to monitor</param>
        public FileSystemWatcherWrapper(string directory, string fileName)
        {
            _watcher = new FileSystemWatcher(directory, fileName);

            // Wire up event forwarding
            _watcher.Changed += (sender, e) => Changed?.Invoke(sender, e);
            _watcher.Renamed += (sender, e) => Renamed?.Invoke(sender, e);
            _watcher.Deleted += (sender, e) => Deleted?.Invoke(sender, e);
            _watcher.Error += (sender, e) => Error?.Invoke(sender, e);
        }

        /// <inheritdoc />
        public string Path
        {
            get => _watcher.Path;
            set => _watcher.Path = value;
        }

        /// <inheritdoc />
        public string Filter
        {
            get => _watcher.Filter;
            set => _watcher.Filter = value;
        }

        /// <inheritdoc />
        public NotifyFilters NotifyFilter
        {
            get => _watcher.NotifyFilter;
            set => _watcher.NotifyFilter = value;
        }

        /// <inheritdoc />
        public bool EnableRaisingEvents
        {
            get => _watcher.EnableRaisingEvents;
            set => _watcher.EnableRaisingEvents = value;
        }

        /// <inheritdoc />
        public bool IncludeSubdirectories
        {
            get => _watcher.IncludeSubdirectories;
            set => _watcher.IncludeSubdirectories = value;
        }

        /// <inheritdoc />
        public event FileSystemEventHandler? Changed;

        /// <inheritdoc />
        public event RenamedEventHandler? Renamed;

        /// <inheritdoc />
        public event FileSystemEventHandler? Deleted;

        /// <inheritdoc />
        public event ErrorEventHandler? Error;

        /// <summary>
        /// Releases all resources used by the FileSystemWatcherWrapper
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _watcher?.Dispose();
                _disposed = true;
            }
        }
    }
}