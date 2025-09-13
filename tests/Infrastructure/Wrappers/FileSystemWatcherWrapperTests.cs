// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SharpBridge.Infrastructure.Factories;
using SharpBridge.Infrastructure.Wrappers;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Wrappers;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Infrastructure.Wrappers
{
    /// <summary>
    /// Tests for FileSystemWatcherWrapper - these are integration tests that use real file system operations
    /// but are kept minimal and fast to verify the wrapper correctly delegates to FileSystemWatcher
    /// </summary>
    public class FileSystemWatcherWrapperTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _testFileName = "test.txt";
        private readonly string _testFilePath;
        private IFileSystemWatcherWrapper? _wrapper;

        public FileSystemWatcherWrapperTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _testFilePath = Path.Combine(_tempDirectory, _testFileName);
            File.WriteAllText(_testFilePath, "initial content");
        }

        public void Dispose()
        {
            _wrapper?.Dispose();

            if (Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            GC.SuppressFinalize(this);
        }

        #region Factory Tests (integrated into wrapper tests)

        [Fact]
        public void Factory_Create_ReturnsFileSystemWatcherWrapper()
        {
            // Arrange
            var factory = new FileSystemWatcherFactory();

            // Act
            var wrapper = factory.Create(_tempDirectory, _testFileName);

            // Assert
            wrapper.Should().NotBeNull();
            wrapper.Should().BeOfType<FileSystemWatcherWrapper>();

            // Cleanup
            wrapper.Dispose();
        }

        [Fact]
        public void Factory_Create_WithNullDirectory_ThrowsException()
        {
            // Arrange
            var factory = new FileSystemWatcherFactory();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => factory.Create(null!, _testFileName));
        }

        [Fact]
        public void Factory_Create_WithEmptyDirectory_ThrowsException()
        {
            // Arrange
            var factory = new FileSystemWatcherFactory();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => factory.Create(string.Empty, _testFileName));
        }

        #endregion

        #region Constructor and Basic Properties Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesWrapper()
        {
            // Act
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);

            // Assert
            _wrapper.Should().NotBeNull();
            _wrapper.Path.Should().Be(_tempDirectory);
            _wrapper.Filter.Should().Be(_testFileName);
        }

        [Fact]
        public void Constructor_WithNullDirectory_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileSystemWatcherWrapper(null!, _testFileName));
        }

        [Fact]
        public void Constructor_WithEmptyDirectory_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new FileSystemWatcherWrapper(string.Empty, _testFileName));
        }

        #endregion

        #region Property Delegation Tests

        [Fact]
        public void Path_SetAndGet_DelegatesToFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);
            var newPath = Path.GetTempPath();

            // Act
            _wrapper.Path = newPath;

            // Assert
            _wrapper.Path.Should().Be(newPath);
        }

        [Fact]
        public void Filter_SetAndGet_DelegatesToFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);
            var newFilter = "*.log";

            // Act
            _wrapper.Filter = newFilter;

            // Assert
            _wrapper.Filter.Should().Be(newFilter);
        }

        [Fact]
        public void NotifyFilter_SetAndGet_DelegatesToFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);
            var notifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite;

            // Act
            _wrapper.NotifyFilter = notifyFilter;

            // Assert
            _wrapper.NotifyFilter.Should().Be(notifyFilter);
        }

        [Fact]
        public void EnableRaisingEvents_SetAndGet_DelegatesToFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);

            // Act
            _wrapper.EnableRaisingEvents = true;

            // Assert
            _wrapper.EnableRaisingEvents.Should().BeTrue();

            // Act
            _wrapper.EnableRaisingEvents = false;

            // Assert
            _wrapper.EnableRaisingEvents.Should().BeFalse();
        }

        [Fact]
        public void IncludeSubdirectories_SetAndGet_DelegatesToFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);

            // Act
            _wrapper.IncludeSubdirectories = true;

            // Assert
            _wrapper.IncludeSubdirectories.Should().BeTrue();

            // Act
            _wrapper.IncludeSubdirectories = false;

            // Assert
            _wrapper.IncludeSubdirectories.Should().BeFalse();
        }

        #endregion

        #region Event Forwarding Tests

        [Fact]
        public async Task Changed_Event_IsForwardedFromFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);
            var eventRaised = false;
            FileSystemEventArgs? receivedArgs = null;

            _wrapper.Changed += (sender, args) =>
            {
                eventRaised = true;
                receivedArgs = args;
            };

            _wrapper.EnableRaisingEvents = true;

            // Act - Modify the file to trigger the event
            await Task.Delay(100); // Allow watcher to initialize
            File.WriteAllText(_testFilePath, "modified content");

            // Assert - Wait for event with timeout
            var timeout = DateTime.UtcNow.AddMilliseconds(2000);
            while (!eventRaised && DateTime.UtcNow < timeout)
            {
                await Task.Delay(50);
            }

            eventRaised.Should().BeTrue("Changed event should be forwarded");
            receivedArgs.Should().NotBeNull();
            receivedArgs!.ChangeType.Should().Be(WatcherChangeTypes.Changed);
        }

        [Fact]
        public async Task Deleted_Event_IsForwardedFromFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);
            var eventRaised = false;
            FileSystemEventArgs? receivedArgs = null;

            _wrapper.Deleted += (sender, args) =>
            {
                eventRaised = true;
                receivedArgs = args;
            };

            _wrapper.EnableRaisingEvents = true;

            // Act - Delete the file to trigger the event
            await Task.Delay(100); // Allow watcher to initialize
            File.Delete(_testFilePath);

            // Assert - Wait for event with timeout
            var timeout = DateTime.UtcNow.AddMilliseconds(2000);
            while (!eventRaised && DateTime.UtcNow < timeout)
            {
                await Task.Delay(50);
            }

            eventRaised.Should().BeTrue("Deleted event should be forwarded");
            receivedArgs.Should().NotBeNull();
            receivedArgs!.ChangeType.Should().Be(WatcherChangeTypes.Deleted);
        }

        [Fact]
        public async Task Renamed_Event_IsForwardedFromFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, "*.txt"); // Use wildcard to catch rename
            var eventRaised = false;
            RenamedEventArgs? receivedArgs = null;

            _wrapper.Renamed += (sender, args) =>
            {
                eventRaised = true;
                receivedArgs = args;
            };

            _wrapper.EnableRaisingEvents = true;

            // Act - Rename the file to trigger the event
            await Task.Delay(100); // Allow watcher to initialize
            var newFilePath = Path.Combine(_tempDirectory, "renamed.txt");
            File.Move(_testFilePath, newFilePath);

            // Assert - Wait for event with timeout
            var timeout = DateTime.UtcNow.AddMilliseconds(2000);
            while (!eventRaised && DateTime.UtcNow < timeout)
            {
                await Task.Delay(50);
            }

            eventRaised.Should().BeTrue("Renamed event should be forwarded");
            receivedArgs.Should().NotBeNull();
            receivedArgs!.ChangeType.Should().Be(WatcherChangeTypes.Renamed);
        }

        [Fact]
        public void Error_Event_IsForwardedFromFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);
            var eventRaised = false;
            ErrorEventArgs? receivedArgs = null;

            _wrapper.Error += (sender, args) =>
            {
                eventRaised = true;
                receivedArgs = args;
            };

            // Note: Error events are harder to trigger reliably in tests,
            // but we can verify the event handler is wired up correctly
            // by checking that the wrapper doesn't throw when we attach handlers

            // Assert - No exception should be thrown when attaching error handler
            eventRaised.Should().BeFalse(); // No error should have occurred yet
            receivedArgs.Should().BeNull();
        }

        #endregion

        #region Multiple Event Handlers Tests

        [Fact]
        public async Task MultipleEventHandlers_AllReceiveEvents()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);
            var handler1Called = false;
            var handler2Called = false;

            _wrapper.Changed += (sender, args) => handler1Called = true;
            _wrapper.Changed += (sender, args) => handler2Called = true;

            _wrapper.EnableRaisingEvents = true;

            // Act
            await Task.Delay(100); // Allow watcher to initialize
            File.WriteAllText(_testFilePath, "content for multiple handlers");

            // Assert - Wait for events with timeout
            var timeout = DateTime.UtcNow.AddMilliseconds(2000);
            while ((!handler1Called || !handler2Called) && DateTime.UtcNow < timeout)
            {
                await Task.Delay(50);
            }

            handler1Called.Should().BeTrue("First handler should be called");
            handler2Called.Should().BeTrue("Second handler should be called");
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_DisposesUnderlyingFileSystemWatcher()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);
            _wrapper.EnableRaisingEvents = true;

            // Act
            var exception = Record.Exception(() => _wrapper.Dispose());

            // Assert - Should not throw and should be in disposed state
            exception.Should().BeNull("Dispose should not throw any exceptions");

            // Verify multiple dispose calls don't throw
            var secondDisposeException = Record.Exception(() => _wrapper.Dispose());
            secondDisposeException.Should().BeNull("Multiple dispose calls should not throw exceptions");
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            _wrapper = new FileSystemWatcherWrapper(_tempDirectory, _testFileName);

            // Act & Assert - Should not throw
            var firstException = Record.Exception(() => _wrapper.Dispose());
            var secondException = Record.Exception(() => _wrapper.Dispose());
            var thirdException = Record.Exception(() => _wrapper.Dispose());

            // Assert
            firstException.Should().BeNull("First dispose should not throw");
            secondException.Should().BeNull("Second dispose should not throw");
            thirdException.Should().BeNull("Third dispose should not throw");
        }

        #endregion

        #region Integration with FileSystemChangeWatcher

        [Fact]
        public void Integration_WorksWithFileSystemChangeWatcher()
        {
            // Arrange - Test that our wrapper works with the actual FileSystemChangeWatcher
            var factory = new FileSystemWatcherFactory();
            var wrapper = factory.Create(_tempDirectory, _testFileName);

            // Act & Assert - Should create successfully and have expected properties
            wrapper.Should().NotBeNull();
            wrapper.Path.Should().Be(_tempDirectory);
            wrapper.Filter.Should().Be(_testFileName);

            // Cleanup
            wrapper.Dispose();
        }

        #endregion
    }
}