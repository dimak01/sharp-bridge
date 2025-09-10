using System;
using System.IO;

namespace SharpBridge.Interfaces.Infrastructure.Wrappers
{
    /// <summary>
    /// Interface wrapper around FileSystemWatcher to enable mocking and testing
    /// </summary>
    public interface IFileSystemWatcherWrapper : IDisposable
    {
        /// <summary>
        /// Gets or sets the path of the directory to watch
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Gets or sets the filter string used to determine what files are monitored
        /// </summary>
        string Filter { get; set; }

        /// <summary>
        /// Gets or sets the type of changes to watch for
        /// </summary>
        NotifyFilters NotifyFilter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component is enabled
        /// </summary>
        bool EnableRaisingEvents { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether subdirectories should be monitored
        /// </summary>
        bool IncludeSubdirectories { get; set; }

        /// <summary>
        /// Occurs when a file or directory in the specified path is changed
        /// </summary>
        event FileSystemEventHandler? Changed;

        /// <summary>
        /// Occurs when a file or directory in the specified path is renamed
        /// </summary>
        event RenamedEventHandler? Renamed;

        /// <summary>
        /// Occurs when a file or directory in the specified path is deleted
        /// </summary>
        event FileSystemEventHandler? Deleted;

        /// <summary>
        /// Occurs when the internal buffer overflows
        /// </summary>
        event ErrorEventHandler? Error;
    }
} 