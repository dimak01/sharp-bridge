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
        private readonly FileSystemChangeWatcher _watcher;
        private readonly List<string> _tempFiles = new();
        private readonly List<string> _tempDirectories = new();

        public FileSystemChangeWatcherTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _watcher = new FileSystemChangeWatcher(_mockLogger.Object);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            
            // Clean up temp files
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                {
                    try { File.Delete(file); } catch { /* ignore */ }
                }
            }
            
            // Clean up temp directories
            foreach (var dir in _tempDirectories)
            {
                if (Directory.Exists(dir))
                {
                    try { Directory.Delete(dir, true); } catch { /* ignore */ }
                }
            }
            
            GC.SuppressFinalize(this);
        }

        #region Helper Methods

        private string CreateTempFile(string content = "test content")
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, content);
            _tempFiles.Add(filePath);
            return filePath;
        }

        private string CreateTempDirectory()
        {
            var dirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dirPath);
            _tempDirectories.Add(dirPath);
            return dirPath;
        }

        private string CreateTempFileInDirectory(string directory, string fileName, string content = "test content")
        {
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content);
            _tempFiles.Add(filePath);
            return filePath;
        }

        private static async Task<bool> WaitForEventAsync(Func<bool> condition, int timeoutMs = 2000)
        {
            var endTime = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < endTime)
            {
                if (condition())
                    return true;
                await Task.Delay(50);
            }
            return false;
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new FileSystemChangeWatcher(null!));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Act & Assert - Should not throw
            using var watcher = new FileSystemChangeWatcher(_mockLogger.Object);
            watcher.Should().NotBeNull();
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
        }

        [Fact]
        public void StartWatching_WithEmptyFilePath_LogsWarningAndReturns()
        {
            // Act
            _watcher.StartWatching(string.Empty);

            // Assert
            _mockLogger.Verify(l => l.Warning(It.Is<string>(s => s.Contains("does not exist"))), Times.Once);
        }

        [Fact]
        public void StartWatching_WithNonExistentFile_LogsWarningAndReturns()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            _watcher.StartWatching(nonExistentPath);

            // Assert
            _mockLogger.Verify(l => l.Warning(It.Is<string>(s => s.Contains("does not exist"))), Times.Once);
        }

        [Fact]
        public void StartWatching_WithValidFile_LogsStartMessage()
        {
            // Arrange
            var filePath = CreateTempFile();

            // Act
            _watcher.StartWatching(filePath);

            // Assert
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Started watching"))), Times.Once);
        }

        [Fact]
        public void StartWatching_CalledTwice_StopsWatchingPreviousFile()
        {
            // Arrange
            var firstFile = CreateTempFile();
            var secondFile = CreateTempFile();

            // Act
            _watcher.StartWatching(firstFile);
            _watcher.StartWatching(secondFile);

            // Assert
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Started watching"))), Times.Exactly(2));
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Stopped watching"))), Times.Once);
        }

        #endregion

        #region StopWatching Tests

        [Fact]
        public void StopWatching_WhenNotWatching_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            _watcher.StopWatching();
            
            // Assert - Verify no exception was thrown and no log messages were generated
            _mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void StopWatching_WhenWatching_LogsStopMessage()
        {
            // Arrange
            var filePath = CreateTempFile();
            _watcher.StartWatching(filePath);

            // Act
            _watcher.StopWatching();

            // Assert
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Stopped watching"))), Times.Once);
        }

        #endregion

        #region File Change Detection Tests

        [Fact]
        public async Task FileChanged_WhenFileModified_RaisesEvent()
        {
            // Arrange
            var filePath = CreateTempFile("initial content");
            var eventRaised = false;
            FileChangeEventArgs? eventArgs = null;

            _watcher.FileChanged += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            _watcher.StartWatching(filePath);
            await Task.Delay(100); // Allow watcher to initialize

            // Act
            File.WriteAllText(filePath, "modified content");

            // Assert
            var success = await WaitForEventAsync(() => eventRaised);
            success.Should().BeTrue("File change event should be raised");
            eventArgs.Should().NotBeNull();
            eventArgs!.FilePath.Should().Be(Path.GetFullPath(filePath));
        }

        [Fact]
        public async Task FileChanged_WhenFileDeleted_RaisesEvent()
        {
            // Arrange
            var filePath = CreateTempFile();
            var eventRaised = false;
            FileChangeEventArgs? eventArgs = null;

            _watcher.FileChanged += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            _watcher.StartWatching(filePath);
            await Task.Delay(100); // Allow watcher to initialize

            // Act
            File.Delete(filePath);

            // Assert
            var success = await WaitForEventAsync(() => eventRaised);
            success.Should().BeTrue("File deletion event should be raised");
            eventArgs.Should().NotBeNull();
        }

        [Fact]
        public async Task FileChanged_WhenFileRenamed_RaisesEvent()
        {
            // Arrange
            var directory = CreateTempDirectory();
            var originalPath = CreateTempFileInDirectory(directory, "original.txt");
            var newPath = Path.Combine(directory, "renamed.txt");
            
            var eventRaised = false;
            FileChangeEventArgs? eventArgs = null;

            _watcher.FileChanged += (sender, args) =>
            {
                eventRaised = true;
                eventArgs = args;
            };

            _watcher.StartWatching(originalPath);
            await Task.Delay(100); // Allow watcher to initialize

            // Act
            File.Move(originalPath, newPath);
            _tempFiles.Add(newPath); // Track for cleanup

            // Assert
            var success = await WaitForEventAsync(() => eventRaised);
            success.Should().BeTrue("File rename event should be raised");
            eventArgs.Should().NotBeNull();
        }

        #endregion

        #region Debouncing Tests

        [Fact]
        public async Task FileChanged_MultipleRapidChanges_DebouncesEvents()
        {
            // Arrange
            var filePath = CreateTempFile("initial");
            var eventCount = 0;
            var eventArgs = new List<FileChangeEventArgs>();

            _watcher.FileChanged += (sender, args) =>
            {
                eventCount++;
                eventArgs.Add(args);
            };

            _watcher.StartWatching(filePath);
            await Task.Delay(100); // Allow watcher to initialize

            // Act - Make multiple rapid changes
            for (int i = 0; i < 5; i++)
            {
                File.WriteAllText(filePath, $"content {i}");
                await Task.Delay(50); // Less than debounce interval (200ms)
            }

            // Wait for debounce period to complete
            await Task.Delay(300);

            // Assert
            eventCount.Should().BeLessThan(5, "Events should be debounced");
            eventCount.Should().BeGreaterThan(0, "At least one event should be raised");
        }

        [Fact]
        public async Task FileChanged_ChangesWithLongInterval_RaisesMultipleEvents()
        {
            // Arrange
            var filePath = CreateTempFile("initial");
            var eventCount = 0;

            _watcher.FileChanged += (sender, args) => eventCount++;

            _watcher.StartWatching(filePath);
            await Task.Delay(100); // Allow watcher to initialize

            // Act - Make changes with long intervals
            File.WriteAllText(filePath, "content 1");
            await Task.Delay(300); // Longer than debounce interval

            File.WriteAllText(filePath, "content 2");
            await Task.Delay(300); // Longer than debounce interval

            // Assert
            var success = await WaitForEventAsync(() => eventCount >= 2, 1000);
            success.Should().BeTrue("Multiple events should be raised for well-spaced changes");
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void StartWatching_WithInvalidDirectory_HandlesGracefully()
        {
            // Arrange
            var invalidPath = Path.Combine("Z:\\NonExistentDrive", "file.txt");

            // Act & Assert - Should not throw
            _watcher.StartWatching(invalidPath);

            // The watcher should log a warning about the file not existing
            _mockLogger.Verify(l => l.Warning(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task FileChanged_AfterDisposal_DoesNotRaiseEvents()
        {
            // Arrange
            var filePath = CreateTempFile();
            var eventRaised = false;

            _watcher.FileChanged += (sender, args) => eventRaised = true;
            _watcher.StartWatching(filePath);
            await Task.Delay(100); // Allow watcher to initialize

            // Act
            _watcher.Dispose();
            File.WriteAllText(filePath, "modified after disposal");
            await Task.Delay(300); // Wait for potential event

            // Assert
            eventRaised.Should().BeFalse("No events should be raised after disposal");
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_WhenWatching_StopsWatching()
        {
            // Arrange
            var filePath = CreateTempFile();
            _watcher.StartWatching(filePath);

            // Act
            _watcher.Dispose();

            // Assert
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Stopped watching"))), Times.Once);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var filePath = CreateTempFile();
            _watcher.StartWatching(filePath);

            // Act & Assert - Should not throw
            _watcher.Dispose();
            _watcher.Dispose();
            _watcher.Dispose();
            
            // Assert - Verify no exception was thrown and stop watching was called only once
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Stopped watching"))), Times.Once);
        }

        [Fact]
        public void StartWatching_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var filePath = CreateTempFile();
            _watcher.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _watcher.StartWatching(filePath));
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

        #region Event Logging Tests

        [Fact]
        public async Task FileChanged_LogsDetectedChange()
        {
            // Arrange
            var filePath = CreateTempFile();
            _watcher.StartWatching(filePath);
            await Task.Delay(100); // Allow watcher to initialize

            // Act
            File.WriteAllText(filePath, "modified content");

            // Assert
            var success = await WaitForEventAsync(() => 
                _mockLogger.Invocations.Any(i => 
                    i.Method.Name == "Info" && 
                    i.Arguments[0].ToString()!.Contains("Detected change")), 1000);
            
            success.Should().BeTrue("Should log detected change");
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task FileChanged_EventHandlerThreadSafety_HandlesCorrectly()
        {
            // Arrange
            var filePath = CreateTempFile();
            var eventCount = 0;
            var exceptions = new List<Exception>();

            _watcher.FileChanged += (sender, args) =>
            {
                try
                {
                    // Simulate some work in the event handler without Thread.Sleep
                    Task.Delay(10).Wait(); // Use Task.Delay instead of Thread.Sleep
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

            _watcher.StartWatching(filePath);
            await Task.Delay(100); // Allow watcher to initialize

            // Act - Make a few well-spaced file changes
            File.WriteAllText(filePath, "content 1");
            await Task.Delay(300); // Wait longer than debounce interval

            File.WriteAllText(filePath, "content 2");
            await Task.Delay(300); // Wait longer than debounce interval

            File.WriteAllText(filePath, "content 3");
            await Task.Delay(300); // Wait for final event

            // Assert
            exceptions.Should().BeEmpty("No exceptions should occur in event handlers");
            eventCount.Should().BeGreaterThan(0, "At least some events should be raised");
            eventCount.Should().BeLessOrEqualTo(3, "Should not exceed the number of changes made");
        }

        #endregion
    }
} 