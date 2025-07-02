using System;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ConsoleWindowManagerTests
    {
        private readonly Mock<IConsole> _consoleMock;
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly ConsoleWindowManager _manager;

        public ConsoleWindowManagerTests()
        {
            _consoleMock = new Mock<IConsole>();
            _loggerMock = new Mock<IAppLogger>();

            // Setup default console size
            _consoleMock.Setup(x => x.WindowWidth).Returns(120);
            _consoleMock.Setup(x => x.WindowHeight).Returns(30);

            _manager = new ConsoleWindowManager(_consoleMock.Object, _loggerMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var manager = new ConsoleWindowManager(_consoleMock.Object, _loggerMock.Object);

            // Assert
            var currentSize = manager.GetCurrentSize();
            Assert.Equal(120, currentSize.width);
            Assert.Equal(30, currentSize.height);
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConsoleWindowManager(null!, _loggerMock.Object));
            Assert.Equal("console", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConsoleWindowManager(_consoleMock.Object, null!));
            Assert.Equal("logger", exception.ParamName);
        }

        #endregion

        #region SetConsoleSize Tests

        [Fact]
        public void SetConsoleSize_WhenSuccessful_ReturnsTrue()
        {
            // Arrange
            _consoleMock.Setup(x => x.TrySetWindowSize(100, 40)).Returns(true);

            // Act
            var result = _manager.SetConsoleSize(100, 40);

            // Assert
            Assert.True(result);
            _consoleMock.Verify(x => x.TrySetWindowSize(100, 40), Times.Once);
        }

        [Fact]
        public void SetConsoleSize_WhenSuccessful_UpdatesLastKnownSize()
        {
            // Arrange
            _consoleMock.Setup(x => x.TrySetWindowSize(100, 40)).Returns(true);
            _consoleMock.Setup(x => x.WindowWidth).Returns(100);
            _consoleMock.Setup(x => x.WindowHeight).Returns(40);

            // Act
            _manager.SetConsoleSize(100, 40);

            // Assert - Verify by getting current size
            var currentSize = _manager.GetCurrentSize();
            Assert.Equal(100, currentSize.width);
            Assert.Equal(40, currentSize.height);
        }

        [Fact]
        public void SetConsoleSize_WhenFailed_ReturnsFalse()
        {
            // Arrange
            _consoleMock.Setup(x => x.TrySetWindowSize(100, 40)).Returns(false);

            // Act
            var result = _manager.SetConsoleSize(100, 40);

            // Assert
            Assert.False(result);
            _consoleMock.Verify(x => x.TrySetWindowSize(100, 40), Times.Once);
        }

        [Fact]
        public void SetConsoleSize_WhenFailed_DoesNotUpdateLastKnownSize()
        {
            // Arrange
            _consoleMock.Setup(x => x.TrySetWindowSize(100, 40)).Returns(false);
            var originalSize = _manager.GetCurrentSize();

            // Act
            _manager.SetConsoleSize(100, 40);

            // Assert - Size should remain unchanged
            var currentSize = _manager.GetCurrentSize();
            Assert.Equal(originalSize.width, currentSize.width);
            Assert.Equal(originalSize.height, currentSize.height);
        }

        #endregion

        #region GetCurrentSize Tests

        [Fact]
        public void GetCurrentSize_ReturnsCurrentConsoleSize()
        {
            // Arrange
            _consoleMock.Setup(x => x.WindowWidth).Returns(150);
            _consoleMock.Setup(x => x.WindowHeight).Returns(50);

            // Act
            var result = _manager.GetCurrentSize();

            // Assert
            Assert.Equal(150, result.width);
            Assert.Equal(50, result.height);
        }

        #endregion

        #region StartSizeChangeTracking Tests

        [Fact]
        public void StartSizeChangeTracking_WithValidCallback_EnablesTracking()
        {
            // Arrange
            Action<int, int> callback = (w, h) => { };

            // Act
            _manager.StartSizeChangeTracking(callback);

            // Assert - Verify logging was called
            _loggerMock.Verify(x => x.Debug(
                "Console size change tracking started. Current size: {0}x{1}",
                It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void StartSizeChangeTracking_WithNullCallback_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                _manager.StartSizeChangeTracking(null!));
            Assert.Equal("updatePreferencesCallback", exception.ParamName);
        }

        #endregion

        #region StopSizeChangeTracking Tests

        [Fact]
        public void StopSizeChangeTracking_DisablesTracking()
        {
            // Arrange
            Action<int, int> callback = (w, h) => { };
            _manager.StartSizeChangeTracking(callback);

            // Act
            _manager.StopSizeChangeTracking();

            // Assert - Verify logging was called
            _loggerMock.Verify(x => x.Debug("Console size change tracking stopped"), Times.Once);
        }

        #endregion

        #region ProcessSizeChanges Tests

        [Fact]
        public void ProcessSizeChanges_WhenTrackingDisabled_DoesNothing()
        {
            // Arrange - Tracking is disabled by default

            // Act
            _manager.ProcessSizeChanges();

            // Assert - No calls should be made to console beyond constructor
            _consoleMock.Verify(x => x.WindowWidth, Times.AtMost(1)); // Only from constructor
            _consoleMock.Verify(x => x.WindowHeight, Times.AtMost(1)); // Only from constructor
        }

        [Fact]
        public void ProcessSizeChanges_WhenSizeUnchanged_DoesNotInvokeCallback()
        {
            // Arrange
            var callbackInvoked = false;
            Action<int, int> callback = (w, h) => callbackInvoked = true;
            _manager.StartSizeChangeTracking(callback);

            // Act
            _manager.ProcessSizeChanges();

            // Assert
            Assert.False(callbackInvoked);
        }

        [Fact]
        public void ProcessSizeChanges_WhenSizeChanged_InvokesCallback()
        {
            // Arrange
            var callbackWidth = 0;
            var callbackHeight = 0;
            Action<int, int> callback = (w, h) => { callbackWidth = w; callbackHeight = h; };

            _manager.StartSizeChangeTracking(callback);

            // Change the console size
            _consoleMock.Setup(x => x.WindowWidth).Returns(140);
            _consoleMock.Setup(x => x.WindowHeight).Returns(35);

            // Act
            _manager.ProcessSizeChanges();

            // Assert
            Assert.Equal(140, callbackWidth);
            Assert.Equal(35, callbackHeight);
        }

        [Fact]
        public void ProcessSizeChanges_WhenSizeChanged_LogsChange()
        {
            // Arrange
            Action<int, int> callback = (w, h) => { };
            _manager.StartSizeChangeTracking(callback);

            // Change the console size
            _consoleMock.Setup(x => x.WindowWidth).Returns(140);
            _consoleMock.Setup(x => x.WindowHeight).Returns(35);

            // Act
            _manager.ProcessSizeChanges();

            // Assert
            _loggerMock.Verify(x => x.Debug(
                "Console size changed from {0}x{1} to {2}x{3}",
                It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void ProcessSizeChanges_WhenWidthChangedOnly_InvokesCallback()
        {
            // Arrange
            var callbackInvoked = false;
            Action<int, int> callback = (w, h) => callbackInvoked = true;
            _manager.StartSizeChangeTracking(callback);

            // Change only the width
            _consoleMock.Setup(x => x.WindowWidth).Returns(140);
            // Height remains 30

            // Act
            _manager.ProcessSizeChanges();

            // Assert
            Assert.True(callbackInvoked);
        }

        [Fact]
        public void ProcessSizeChanges_WhenHeightChangedOnly_InvokesCallback()
        {
            // Arrange
            var callbackInvoked = false;
            Action<int, int> callback = (w, h) => callbackInvoked = true;
            _manager.StartSizeChangeTracking(callback);

            // Change only the height
            _consoleMock.Setup(x => x.WindowHeight).Returns(35);
            // Width remains 120

            // Act
            _manager.ProcessSizeChanges();

            // Assert
            Assert.True(callbackInvoked);
        }

        [Fact]
        public void ProcessSizeChanges_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            Action<int, int> callback = (w, h) => { };
            _manager.StartSizeChangeTracking(callback);

            // Setup console to throw exception
            _consoleMock.Setup(x => x.WindowWidth).Throws(new InvalidOperationException("Test exception"));

            // Act
            _manager.ProcessSizeChanges();

            // Assert
            _loggerMock.Verify(x => x.Error(
                "Error processing console size changes: {0}",
                It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void ProcessSizeChanges_AfterSizeChange_UpdatesLastKnownSize()
        {
            // Arrange
            var callbackInvoked = false;
            Action<int, int> callback = (w, h) => callbackInvoked = true;
            _manager.StartSizeChangeTracking(callback);

            // Change the console size
            _consoleMock.Setup(x => x.WindowWidth).Returns(140);
            _consoleMock.Setup(x => x.WindowHeight).Returns(35);

            // Act - First size change
            _manager.ProcessSizeChanges();

            // Verify first callback was invoked
            Assert.True(callbackInvoked);

            // Reset callback flag
            callbackInvoked = false;

            // Change size again to a different value
            _consoleMock.Setup(x => x.WindowWidth).Returns(160);
            _consoleMock.Setup(x => x.WindowHeight).Returns(40);

            // Act - Second size change
            _manager.ProcessSizeChanges();

            // Assert - Callback should be invoked again because size changed from 140x35 to 160x40
            Assert.True(callbackInvoked);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_StopsTracking()
        {
            // Arrange
            Action<int, int> callback = (w, h) => { };
            _manager.StartSizeChangeTracking(callback);

            // Act
            _manager.Dispose();

            // Assert - Verify stop tracking was called
            _loggerMock.Verify(x => x.Debug("Console size change tracking stopped"), Times.Once);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            Action<int, int> callback = (w, h) => { };
            _manager.StartSizeChangeTracking(callback);

            // Act
            _manager.Dispose();
            _manager.Dispose(); // Second call should not throw

            // Assert - Stop tracking should only be called once
            _loggerMock.Verify(x => x.Debug("Console size change tracking stopped"), Times.Once);
        }

        [Fact]
        public void Dispose_WhenTrackingNotStarted_DoesNotThrow()
        {
            // Act
            _manager.Dispose();

            // Assert - Should complete without throwing and call stop tracking once
            _loggerMock.Verify(x => x.Debug("Console size change tracking stopped"), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void FullWorkflow_StartTrackingProcessChangesStop_WorksCorrectly()
        {
            // Arrange
            var callbackInvoked = false;
            var callbackWidth = 0;
            var callbackHeight = 0;
            Action<int, int> callback = (w, h) =>
            {
                callbackInvoked = true;
                callbackWidth = w;
                callbackHeight = h;
            };

            // Act - Start tracking
            _manager.StartSizeChangeTracking(callback);

            // Change console size
            _consoleMock.Setup(x => x.WindowWidth).Returns(200);
            _consoleMock.Setup(x => x.WindowHeight).Returns(60);

            // Process changes
            _manager.ProcessSizeChanges();

            // Stop tracking
            _manager.StopSizeChangeTracking();

            // Assert
            Assert.True(callbackInvoked);
            Assert.Equal(200, callbackWidth);
            Assert.Equal(60, callbackHeight);

            // Verify all expected log calls
            _loggerMock.Verify(x => x.Debug(
                "Console size change tracking started. Current size: {0}x{1}",
                It.IsAny<object[]>()), Times.Once);
            _loggerMock.Verify(x => x.Debug(
                "Console size changed from {0}x{1} to {2}x{3}",
                It.IsAny<object[]>()), Times.Once);
            _loggerMock.Verify(x => x.Debug("Console size change tracking stopped"), Times.Once);
        }

        #endregion
    }
}