// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using SharpBridge.Interfaces.Infrastructure.Wrappers;

namespace SharpBridge.Interfaces.Infrastructure.Factories
{
    /// <summary>
    /// Factory interface for creating IFileSystemWatcherWrapper instances
    /// </summary>
    public interface IFileSystemWatcherFactory
    {
        /// <summary>
        /// Creates a new file system watcher wrapper for the specified directory and file name
        /// </summary>
        /// <param name="directory">The directory to watch</param>
        /// <param name="fileName">The file name pattern to watch</param>
        /// <returns>A new IFileSystemWatcherWrapper instance</returns>
        IFileSystemWatcherWrapper Create(string directory, string fileName);
    }
}