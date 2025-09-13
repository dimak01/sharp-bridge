// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using SharpBridge.Infrastructure.Wrappers;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Factories;
using SharpBridge.Interfaces.Infrastructure.Wrappers;

namespace SharpBridge.Infrastructure.Factories
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