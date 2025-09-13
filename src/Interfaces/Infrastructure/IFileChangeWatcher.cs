// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;

namespace SharpBridge.Interfaces.Infrastructure
{
    /// <summary>
    /// Interface for watching file changes in a testable way.
    /// </summary>
    public interface IFileChangeWatcher : IDisposable
    {
        /// <summary>
        /// Event raised when a watched file changes on disk.
        /// </summary>
        event EventHandler<FileChangeEventArgs> FileChanged;
        
        /// <summary>
        /// Starts watching the specified file for changes.
        /// </summary>
        /// <param name="filePath">The path to the file to watch for changes</param>
        void StartWatching(string filePath);
        
        /// <summary>
        /// Stops watching the currently watched file.
        /// </summary>
        void StopWatching();
    }

    /// <summary>
    /// Event arguments for file change events.
    /// </summary>
    public class FileChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the path of the file that changed.
        /// </summary>
        public string FilePath { get; }
        
        /// <summary>
        /// Gets the UTC timestamp when the file change was detected.
        /// </summary>
        public DateTime ChangeTime { get; }

        /// <summary>
        /// Initializes a new instance of the FileChangeEventArgs class.
        /// </summary>
        /// <param name="filePath">The path of the file that changed</param>
        public FileChangeEventArgs(string filePath)
        {
            FilePath = filePath;
            ChangeTime = DateTime.UtcNow;
        }
    }
} 