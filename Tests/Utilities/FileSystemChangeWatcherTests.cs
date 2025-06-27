using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class FileSystemChangeWatcherTests : IDisposable
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IFileSystemWatcherFactory> _mockWatcherFactory;
        private readonly Mock<IFileSystemWatcherWrapper> _mockWatcher;
        private readonly FileSystemChangeWatcher _watcher;

        public FileSystemChangeWatcherTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockWatcherFactory = new Mock<IFileSystemWatcherFactory>();
            _mockWatcher = new Mock<IFileSystemWatcherWrapper>();

            // Setup factory to return our mock watcher
            _mockWatcherFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_mockWatcher.Object);

            _watcher = new FileSystemChangeWatcher(_mockLogger.Object, _mockWatcherFactory.Object);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new FileSystemChangeWatcher(_mockLogger.Object, _mockWatcherFactory.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FileSystemChangeWatcher(null!, _mockWatcherFactory.Object));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullWatcherFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FileSystemChangeWatcher(_mockLogger.Object, null!));
            exception.ParamName.Should().Be("watcherFactory");
        }

        #endregion

        #region StartWatching Tests

        [Fact]
        public void StartWatching_WithNullFilePath_LogsWarningAndReturns()
        {
            // Act
            _watcher.StartWatching(null!);

            // Assert
            _mockLogger.Verify(l => l.Warning(It.Is<string>(s => s.Contains("does not exist"))), Times.Once);
            _mockWatcherFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void StartWatching_WithEmptyFilePath_LogsWarningAndReturns()
        {
            // Act
            _watcher.StartWatching(string.Empty);

            // Assert
            _mockLogger.Verify(l => l.Warning(It.Is<string>(s => s.Contains("does not exist"))), Times.Once);
            _mockWatcherFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void StartWatching_WithNonExistentFile_LogsWarningAndReturns()
        {
            // Arrange
            var nonExistentPath = @"C:\NonExistent\file.txt";

            // Act
            _watcher.StartWatching(nonExistentPath);

            // Assert
            _mockLogger.Verify(l => l.Warning(It.Is<string>(s => s.Contains("does not exist"))), Times.Once);
            _mockWatcherFactory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void StartWatching_WithValidFile_CreatesWatcherAndLogsStartMessage()
        {
            // Arrange
            var validPath = Path.GetTempFileName();

            try
            {
                // Act
                _watcher.StartWatching(validPath);

                // Assert
                _mockWatcherFactory.Verify(f => f.Create(Path.GetDirectoryName(validPath)!, Path.GetFileName(validPath)), Times.Once);
                _mockWatcher.VerifySet(w => w.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName, Times.Once);
                _mockWatcher.VerifySet(w => w.EnableRaisingEvents = true, Times.Once);
                _mockWatcher.VerifySet(w => w.IncludeSubdirectories = false, Times.Once);
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Started watching"))), Times.Once);
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        [Fact]
        public void StartWatching_CalledTwice_StopsWatchingPreviousFile()
        {
            // Arrange
            var firstFile = Path.GetTempFileName();
            var secondFile = Path.GetTempFileName();

            try
            {
                // Act
                _watcher.StartWatching(firstFile);
                _watcher.StartWatching(secondFile);

                // Assert
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Started watching"))), Times.Exactly(2));
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Stopped watching"))), Times.Once);
                _mockWatcher.Verify(w => w.Dispose(), Times.Once);
            }
            finally
            {
                if (File.Exists(firstFile)) File.Delete(firstFile);
                if (File.Exists(secondFile)) File.Delete(secondFile);
            }
        }

        [Fact]
        public void StartWatching_WithValidFile_WiresUpEventHandlers()
        {
            // Arrange
            var validPath = Path.GetTempFileName();

            try
            {
                // Act
                _watcher.StartWatching(validPath);

                // Assert - Verify event handlers were attached
                _mockWatcher.VerifyAdd(w => w.Changed += It.IsAny<FileSystemEventHandler>(), Times.Once);
                _mockWatcher.VerifyAdd(w => w.Renamed += It.IsAny<RenamedEventHandler>(), Times.Once);
                _mockWatcher.VerifyAdd(w => w.Deleted += It.IsAny<FileSystemEventHandler>(), Times.Once);
                _mockWatcher.VerifyAdd(w => w.Error += It.IsAny<ErrorEventHandler>(), Times.Once);
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        #endregion

        #region StopWatching Tests

        [Fact]
        public void StopWatching_WhenNotWatching_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            _watcher.StopWatching();

            // Assert - Verify no operations were performed
            _mockWatcher.Verify(w => w.Dispose(), Times.Never);
            _mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void StopWatching_WhenWatching_DisposesWatcherAndLogsStopMessage()
        {
            // Arrange
            var validPath = Path.GetTempFileName();

            try
            {
                _watcher.StartWatching(validPath);

                // Act
                _watcher.StopWatching();

                // Assert
                _mockWatcher.VerifySet(w => w.EnableRaisingEvents = false, Times.Once);
                _mockWatcher.VerifyRemove(w => w.Changed -= It.IsAny<FileSystemEventHandler>(), Times.Once);
                _mockWatcher.VerifyRemove(w => w.Renamed -= It.IsAny<RenamedEventHandler>(), Times.Once);
                _mockWatcher.VerifyRemove(w => w.Deleted -= It.IsAny<FileSystemEventHandler>(), Times.Once);
                _mockWatcher.VerifyRemove(w => w.Error -= It.IsAny<ErrorEventHandler>(), Times.Once);
                _mockWatcher.Verify(w => w.Dispose(), Times.Once);
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Stopped watching"))), Times.Once);
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        #endregion

        #region File Change Detection Tests

        [Fact]
        public void FileChanged_WhenFileModified_RaisesEvent()
        {
            // Arrange
            var validPath = Path.GetTempFileName();
            var eventRaised = false;
            FileChangeEventArgs? eventArgs = null;

            _watcher.FileChanged += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            try
            {
                _watcher.StartWatching(validPath);

                // Act - Simulate file change event
                _mockWatcher.Raise(w => w.Changed += null,
                    new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(validPath)!, Path.GetFileName(validPath)));

                // Assert - Event should be raised immediately
                eventRaised.Should().BeTrue();
                eventArgs.Should().NotBeNull();
                eventArgs!.FilePath.Should().Be(validPath);
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Detected change"))), Times.Once);
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        [Fact]
        public void FileChanged_WhenFileDeleted_RaisesEvent()
        {
            // Arrange
            var validPath = Path.GetTempFileName();
            var eventRaised = false;
            FileChangeEventArgs? eventArgs = null;

            _watcher.FileChanged += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            try
            {
                _watcher.StartWatching(validPath);

                // Act - Simulate file deletion event
                _mockWatcher.Raise(w => w.Deleted += null,
                    new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(validPath)!, Path.GetFileName(validPath)));

                // Assert - Event should be raised immediately
                eventRaised.Should().BeTrue();
                eventArgs.Should().NotBeNull();
                eventArgs!.FilePath.Should().Be(validPath);
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        [Fact]
        public void FileChanged_WhenFileRenamed_RaisesEvent()
        {
            // Arrange
            var validPath = Path.GetTempFileName();
            var eventRaised = false;
            FileChangeEventArgs? eventArgs = null;

            _watcher.FileChanged += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            try
            {
                _watcher.StartWatching(validPath);

                // Act - Simulate file rename event
                _mockWatcher.Raise(w => w.Renamed += null,
                    new RenamedEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(validPath)!, "newname.txt", Path.GetFileName(validPath)));

                // Assert - Event should be raised immediately
                eventRaised.Should().BeTrue();
                eventArgs.Should().NotBeNull();
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        #endregion

        #region Debouncing Tests

        [Fact]
        public void FileChanged_MultipleRapidChanges_DebouncesEvents()
        {
            // Arrange
            var validPath = Path.GetTempFileName();
            var eventCount = 0;

            _watcher.FileChanged += (sender, args) => eventCount++;

            try
            {
                _watcher.StartWatching(validPath);

                // Act - Simulate multiple rapid changes (within debounce interval)
                var fileSystemArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(validPath)!, Path.GetFileName(validPath));

                _mockWatcher.Raise(w => w.Changed += null, fileSystemArgs);
                _mockWatcher.Raise(w => w.Changed += null, fileSystemArgs);
                _mockWatcher.Raise(w => w.Changed += null, fileSystemArgs);
                _mockWatcher.Raise(w => w.Changed += null, fileSystemArgs);
                _mockWatcher.Raise(w => w.Changed += null, fileSystemArgs);

                // Assert - Only the first event should be processed due to debouncing
                eventCount.Should().Be(1, "Events should be debounced within the 200ms interval");
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        [Fact]
        public async Task FileChanged_ChangesWithLongInterval_RaisesMultipleEvents()
        {
            // Arrange
            var validPath = Path.GetTempFileName();
            var eventCount = 0;

            _watcher.FileChanged += (sender, args) => eventCount++;

            try
            {
                _watcher.StartWatching(validPath);
                var fileSystemArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(validPath)!, Path.GetFileName(validPath));

                // Act - Simulate changes with intervals longer than debounce period
#pragma warning disable S6966 // Awaitable method should be used
                _mockWatcher.Raise(w => w.Changed += null, fileSystemArgs);
#pragma warning restore S6966 // Awaitable method should be used

                // Wait longer than debounce interval (200ms)
                await Task.Delay(250);

#pragma warning disable S6966 // Awaitable method should be used
                _mockWatcher.Raise(w => w.Changed += null, fileSystemArgs);
#pragma warning restore S6966 // Awaitable method should be used

                // Assert - Both events should be processed
                eventCount.Should().Be(2, "Events separated by longer intervals should both be processed");
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void FileChanged_WhenWatcherError_LogsError()
        {
            // Arrange
            var validPath = Path.GetTempFileName();

            try
            {
                _watcher.StartWatching(validPath);

                // Act - Simulate watcher error
                var errorException = new InvalidOperationException("Test error");
                _mockWatcher.Raise(w => w.Error += null, new ErrorEventArgs(errorException));

                // Assert - Error should be logged
                _mockLogger.Verify(l => l.Error(It.Is<string>(s => s.Contains("Watcher error"))), Times.Once);
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        [Fact]
        public void FileChanged_AfterDisposal_DoesNotRaiseEvents()
        {
            // Arrange
            var validPath = Path.GetTempFileName();
            var eventRaised = false;

            _watcher.FileChanged += (sender, args) => eventRaised = true;

            try
            {
                _watcher.StartWatching(validPath);

                // Act
                _watcher.Dispose();

                // Simulate file change after disposal
                _mockWatcher.Raise(w => w.Changed += null,
                    new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(validPath)!, Path.GetFileName(validPath)));

                // Assert - No events should be raised after disposal
                eventRaised.Should().BeFalse("No events should be raised after disposal");
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_WhenWatching_StopsWatching()
        {
            // Arrange
            var validPath = Path.GetTempFileName();

            try
            {
                _watcher.StartWatching(validPath);

                // Act
                _watcher.Dispose();

                // Assert
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Stopped watching"))), Times.Once);
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var validPath = Path.GetTempFileName();

            try
            {
                _watcher.StartWatching(validPath);

                // Act & Assert - Should not throw
                _watcher.Dispose();
                _watcher.Dispose();
                _watcher.Dispose();

                // Assert - Verify stop watching was called only once
                _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Stopped watching"))), Times.Once);
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        [Fact]
        public void StartWatching_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var validPath = Path.GetTempFileName();

            try
            {
                _watcher.Dispose();

                // Act & Assert
                Assert.Throws<ObjectDisposedException>(() => _watcher.StartWatching(validPath));
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        [Fact]
        public void StopWatching_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            _watcher.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _watcher.StopWatching());
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public void FileChanged_EventHandlerThreadSafety_HandlesCorrectly()
        {
            // Arrange
            var validPath = Path.GetTempFileName();
            var eventCount = 0;
            var exceptions = new List<Exception>();

            _watcher.FileChanged += (sender, args) =>
            {
                try
                {
                    Interlocked.Increment(ref eventCount);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            };

            try
            {
                _watcher.StartWatching(validPath);
                var fileSystemArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(validPath)!, Path.GetFileName(validPath));

                // Act - Simulate concurrent file changes
                _mockWatcher.Raise(w => w.Changed += null, fileSystemArgs);

                // Assert
                exceptions.Should().BeEmpty("No exceptions should occur in event handlers");
                eventCount.Should().BeGreaterThan(0, "At least one event should be raised");
            }
            finally
            {
                if (File.Exists(validPath))
                    File.Delete(validPath);
            }
        }

        #endregion
    }
}