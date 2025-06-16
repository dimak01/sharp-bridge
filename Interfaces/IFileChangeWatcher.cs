using System;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for watching file changes in a testable way.
    /// </summary>
    public interface IFileChangeWatcher : IDisposable
    {
        event EventHandler<FileChangeEventArgs> FileChanged;
        void StartWatching(string filePath);
        void StopWatching();
    }

    /// <summary>
    /// Event arguments for file change events.
    /// </summary>
    public class FileChangeEventArgs : EventArgs
    {
        public string FilePath { get; }
        public DateTime ChangeTime { get; }

        public FileChangeEventArgs(string filePath)
        {
            FilePath = filePath;
            ChangeTime = DateTime.UtcNow;
        }
    }
} 