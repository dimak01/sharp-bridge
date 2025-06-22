using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Factory for creating FileSystemWatcherWrapper instances
    /// </summary>
    public class FileSystemWatcherFactory : IFileSystemWatcherFactory
    {
        /// <inheritdoc />
        public IFileSystemWatcherWrapper Create(string directory, string fileName)
        {
            return new FileSystemWatcherWrapper(directory, fileName);
        }
    }
} 