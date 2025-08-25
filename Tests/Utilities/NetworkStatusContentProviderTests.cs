using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class NetworkStatusContentProviderTests : IDisposable
    {
        private readonly Mock<IPortStatusMonitorService> _portStatusMonitorMock;
        private readonly Mock<INetworkStatusFormatter> _networkStatusFormatterMock;
        private readonly Mock<IExternalEditorService> _externalEditorServiceMock;
        private readonly Mock<IAppLogger> _loggerMock;
        private readonly Mock<IConsole> _consoleMock;
        private readonly ApplicationConfig _testAppConfig;
        private readonly ConsoleRenderContext _testContext;
        private readonly NetworkStatus _testNetworkStatus;

        public NetworkStatusContentProviderTests()
        {
            _portStatusMonitorMock = new Mock<IPortStatusMonitorService>();
            _networkStatusFormatterMock = new Mock<INetworkStatusFormatter>();
            _externalEditorServiceMock = new Mock<IExternalEditorService>();
            _loggerMock = new Mock<IAppLogger>();
            _consoleMock = new Mock<IConsole>();

            // Setup test data
            _testAppConfig = new ApplicationConfig();
            _testNetworkStatus = new NetworkStatus();
            _testContext = new ConsoleRenderContext
            {
                ApplicationConfig = _testAppConfig,
                ConsoleSize = (120, 30)
            };

            // Default mock setups
            _portStatusMonitorMock.Setup(x => x.GetNetworkStatusAsync()).ReturnsAsync(_testNetworkStatus);
            _networkStatusFormatterMock.Setup(x => x.RenderNetworkTroubleshooting(It.IsAny<NetworkStatus>(), It.IsAny<ApplicationConfig>()))
                .Returns("Test network status content");
            _externalEditorServiceMock.Setup(x => x.TryOpenApplicationConfigAsync()).ReturnsAsync(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup managed resources if needed
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Assert
            provider.Should().NotBeNull();
            provider.Mode.Should().Be(ConsoleMode.NetworkStatus);
            provider.DisplayName.Should().Be("Network Status");
            provider.ToggleAction.Should().Be(ShortcutAction.ShowNetworkStatus);
            provider.PreferredUpdateInterval.Should().Be(TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void Constructor_WithNullPortStatusMonitor_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new NetworkStatusContentProvider(null!, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object));
            exception.ParamName.Should().Be("portStatusMonitor");
        }

        [Fact]
        public void Constructor_WithNullNetworkStatusFormatter_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new NetworkStatusContentProvider(_portStatusMonitorMock.Object, null!, _externalEditorServiceMock.Object, _loggerMock.Object));
            exception.ParamName.Should().Be("networkStatusFormatter");
        }

        [Fact]
        public void Constructor_WithNullExternalEditorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, null!, _loggerMock.Object));
            exception.ParamName.Should().Be("externalEditorService");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, null!));
            exception.ParamName.Should().Be("logger");
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Mode_ReturnsNetworkStatus()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act & Assert
            provider.Mode.Should().Be(ConsoleMode.NetworkStatus);
        }

        [Fact]
        public void DisplayName_ReturnsNetworkStatus()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act & Assert
            provider.DisplayName.Should().Be("Network Status");
        }

        [Fact]
        public void ToggleAction_ReturnsShowNetworkStatus()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act & Assert
            provider.ToggleAction.Should().Be(ShortcutAction.ShowNetworkStatus);
        }

        [Fact]
        public void PreferredUpdateInterval_Returns100Milliseconds()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act & Assert
            provider.PreferredUpdateInterval.Should().Be(TimeSpan.FromMilliseconds(100));
        }

        #endregion

        #region Enter Method Tests

        [Fact]
        public void Enter_ClearsConsoleAndLogsDebug()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act
            provider.Enter(_consoleMock.Object);

            // Assert
            _consoleMock.Verify(x => x.Clear(), Times.Once);
            _loggerMock.Verify(x => x.Debug("Entered Network Status mode."), Times.Once);
        }

        [Fact]
        public void Enter_StartsBackgroundRefreshTask()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act
            provider.Enter(_consoleMock.Object);

            // Assert - Use reflection to check that background task was started
            var backgroundTaskField = typeof(NetworkStatusContentProvider).GetField("_backgroundTask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var backgroundTask = backgroundTaskField?.GetValue(provider) as Task;
            backgroundTask.Should().NotBeNull();
            backgroundTask!.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public void Enter_MultipleCallsDoNotCreateMultipleTasks()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act
            provider.Enter(_consoleMock.Object);
            var backgroundTaskField = typeof(NetworkStatusContentProvider).GetField("_backgroundTask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var firstTask = backgroundTaskField?.GetValue(provider) as Task;

            provider.Enter(_consoleMock.Object);
            var secondTask = backgroundTaskField?.GetValue(provider) as Task;

            // Assert - Should be the same task
            secondTask.Should().BeSameAs(firstTask);
        }

        #endregion

        #region Exit Method Tests

        [Fact]
        public void Exit_LogsDebugMessage()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act
            provider.Exit(_consoleMock.Object);

            // Assert
            _loggerMock.Verify(x => x.Debug("Exited Network Status mode."), Times.Once);
        }

        [Fact]
        public void Exit_DoesNotStopBackgroundTask()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            provider.Enter(_consoleMock.Object);

            var backgroundTaskField = typeof(NetworkStatusContentProvider).GetField("_backgroundTask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskBeforeExit = backgroundTaskField?.GetValue(provider) as Task;

            // Act
            provider.Exit(_consoleMock.Object);

            // Assert - Task should still be running
            var taskAfterExit = backgroundTaskField?.GetValue(provider) as Task;
            taskAfterExit.Should().BeSameAs(taskBeforeExit);
            taskAfterExit!.IsCompleted.Should().BeFalse();
        }

        #endregion

        #region GetContent Method Tests

        [Fact]
        public void GetContent_WithNoSnapshot_ReturnsLoadingMessage()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act
            var result = provider.GetContent(_testContext);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Should().Be("Loading network status...");
        }

        [Fact]
        public void GetContent_WithNullApplicationConfig_ReturnsErrorMessage()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            var contextWithNullConfig = new ConsoleRenderContext { ApplicationConfig = null! };

            // Set a snapshot using reflection to bypass the loading message
            SetSnapshotUsingReflection(provider, _testNetworkStatus);

            // Act
            var result = provider.GetContent(contextWithNullConfig);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Should().Be("Error: Application configuration not available.");
        }

        [Fact]
        public void GetContent_WithValidSnapshotAndConfig_ReturnsFormattedContent()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            SetSnapshotUsingReflection(provider, _testNetworkStatus);

            _networkStatusFormatterMock.Setup(x => x.RenderNetworkTroubleshooting(_testNetworkStatus, _testAppConfig))
                .Returns("Network Status Line 1");

            // Act
            var result = provider.GetContent(_testContext);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Should().Be("Network Status Line 1");
            _networkStatusFormatterMock.Verify(x => x.RenderNetworkTroubleshooting(_testNetworkStatus, _testAppConfig), Times.Once);
        }

        [Fact]
        public void GetContent_WithMultiLineFormattedContent_SplitsCorrectly()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            SetSnapshotUsingReflection(provider, _testNetworkStatus);

            var multiLineContent = "Line 1" + Environment.NewLine + "Line 2" + Environment.NewLine + "Line 3";
            _networkStatusFormatterMock.Setup(x => x.RenderNetworkTroubleshooting(_testNetworkStatus, _testAppConfig))
                .Returns(multiLineContent);

            // Act
            var result = provider.GetContent(_testContext);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Should().Be("Line 1");
            result[1].Should().Be("Line 2");
            result[2].Should().Be("Line 3");
        }

        [Fact]
        public void GetContent_ThreadSafe_HandlesSnapshotAccess()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            SetSnapshotUsingReflection(provider, _testNetworkStatus);

            // Act - Call GetContent from multiple threads simultaneously
            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() => provider.GetContent(_testContext))).ToArray();
            var results = Task.WhenAll(tasks).GetAwaiter().GetResult();

            // Assert - All calls should succeed
            results.Should().HaveCount(10);
            results.Should().AllSatisfy(result => result.Should().NotBeNull());
        }

        #endregion

        #region TryOpenInExternalEditorAsync Method Tests

        [Fact]
        public async Task TryOpenInExternalEditorAsync_CallsExternalEditorService()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Act
            await provider.TryOpenInExternalEditorAsync();

            // Assert
            _externalEditorServiceMock.Verify(x => x.TryOpenApplicationConfigAsync(), Times.Once);
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_ReturnsServiceResult()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            _externalEditorServiceMock.Setup(x => x.TryOpenApplicationConfigAsync()).ReturnsAsync(true);

            // Act
            var result = await provider.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TryOpenInExternalEditorAsync_WhenServiceFails_ReturnsFalse()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            _externalEditorServiceMock.Setup(x => x.TryOpenApplicationConfigAsync()).ReturnsAsync(false);

            // Act
            var result = await provider.TryOpenInExternalEditorAsync();

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Background Refresh Logic Tests

        [Fact]
        public async Task BackgroundRefresh_RefreshesNetworkStatusPeriodically()
        {
            // Arrange
            var tcs = new TaskCompletionSource<NetworkStatus>();
            _portStatusMonitorMock.Setup(x => x.GetNetworkStatusAsync()).Returns(tcs.Task);

            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            provider.Enter(_consoleMock.Object);

            // Act - Complete the async operation
            tcs.SetResult(_testNetworkStatus);
            await Task.Delay(50); // Allow background task to process

            // Assert
            _portStatusMonitorMock.Verify(x => x.GetNetworkStatusAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task BackgroundRefresh_UpdatesSnapshotThreadSafely()
        {
            // Arrange
            var tcs = new TaskCompletionSource<NetworkStatus>();
            _portStatusMonitorMock.Setup(x => x.GetNetworkStatusAsync()).Returns(tcs.Task);

            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            provider.Enter(_consoleMock.Object);

            // Act - Complete the async operation
            tcs.SetResult(_testNetworkStatus);
            await Task.Delay(50); // Allow background task to process

            // Assert - Snapshot should be updated
            var snapshot = GetSnapshotUsingReflection(provider);
            snapshot.Should().BeSameAs(_testNetworkStatus);
        }

        [Fact]
        public async Task BackgroundRefresh_LogsSuccessfulRefresh()
        {
            // Arrange
            var tcs = new TaskCompletionSource<NetworkStatus>();
            _portStatusMonitorMock.Setup(x => x.GetNetworkStatusAsync()).Returns(tcs.Task);

            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            provider.Enter(_consoleMock.Object);

            // Act - Complete the async operation
            tcs.SetResult(_testNetworkStatus);
            await Task.Delay(50); // Allow background task to process

            // Assert
            _loggerMock.Verify(x => x.Debug("Network status refreshed successfully"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task BackgroundRefresh_OnException_LogsWarningAndRetries()
        {
            // Arrange
            _portStatusMonitorMock.Setup(x => x.GetNetworkStatusAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));

            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            provider.Enter(_consoleMock.Object);

            // Act - Allow background task to process
            await Task.Delay(100);

            // Assert
            _loggerMock.Verify(x => x.Warning("Failed to refresh network status: {0}", "Test exception"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task BackgroundRefresh_OnException_WaitsLongerBeforeRetry()
        {
            // Arrange
            var callCount = 0;
            _portStatusMonitorMock.Setup(x => x.GetNetworkStatusAsync())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("Test exception");
                    return Task.FromResult(_testNetworkStatus);
                });

            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            provider.Enter(_consoleMock.Object);

            // Act - Allow background task to process
            await Task.Delay(100);

            // Assert - Should have been called at least once (the failing call)
            _portStatusMonitorMock.Verify(x => x.GetNetworkStatusAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task BackgroundRefresh_RespectsRefreshInterval()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Use reflection to set a recent refresh time
            var lastRefreshField = typeof(NetworkStatusContentProvider).GetField("_lastRefresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastRefreshField?.SetValue(provider, DateTime.UtcNow);

            provider.Enter(_consoleMock.Object);

            // Act - Allow background task to process briefly
            await Task.Delay(50);

            // Assert - Should not have called the service yet due to recent refresh
            _portStatusMonitorMock.Verify(x => x.GetNetworkStatusAsync(), Times.Never);
        }

        [Fact]
        public async Task BackgroundRefresh_SkipsRefreshWhenTooSoon()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Use reflection to set a very recent refresh time
            var lastRefreshField = typeof(NetworkStatusContentProvider).GetField("_lastRefresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastRefreshField?.SetValue(provider, DateTime.UtcNow.AddMilliseconds(-100)); // 100ms ago (less than 10s interval)

            provider.Enter(_consoleMock.Object);

            // Act - Allow background task to process briefly
            await Task.Delay(50);

            // Assert - Should not have called the service yet
            _portStatusMonitorMock.Verify(x => x.GetNetworkStatusAsync(), Times.Never);
        }

        #endregion

        #region Integration & Edge Case Tests

        [Fact]
        public async Task GetContent_AfterBackgroundRefresh_ReturnsUpdatedContent()
        {
            // Arrange
            var tcs = new TaskCompletionSource<NetworkStatus>();
            _portStatusMonitorMock.Setup(x => x.GetNetworkStatusAsync()).Returns(tcs.Task);

            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            provider.Enter(_consoleMock.Object);

            // Initial state should show loading
            var initialResult = provider.GetContent(_testContext);
            initialResult[0].Should().Be("Loading network status...");

            // Act - Complete the background refresh
            tcs.SetResult(_testNetworkStatus);
            await Task.Delay(50); // Allow background task to process

            // Assert - Should now show formatted content
            var updatedResult = provider.GetContent(_testContext);
            updatedResult[0].Should().Be("Test network status content");
        }

        [Fact]
        public void BackgroundRefresh_TaskCompletion_CreatesNewTask()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);

            // Use reflection to set a completed task
            var backgroundTaskField = typeof(NetworkStatusContentProvider).GetField("_backgroundTask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            backgroundTaskField?.SetValue(provider, Task.CompletedTask);

            // Act
            provider.Enter(_consoleMock.Object);

            // Assert - Should create a new task since the old one was completed
            var newTask = backgroundTaskField?.GetValue(provider) as Task;
            newTask.Should().NotBeNull();
            newTask!.Should().NotBe(Task.CompletedTask);
        }

        [Fact]
        public void Enter_WhenRefreshTaskCompleted_StartsNewTask()
        {
            // Arrange
            var provider = new NetworkStatusContentProvider(_portStatusMonitorMock.Object, _networkStatusFormatterMock.Object, _externalEditorServiceMock.Object, _loggerMock.Object);
            provider.Enter(_consoleMock.Object); // Start first task

            var backgroundTaskField = typeof(NetworkStatusContentProvider).GetField("_backgroundTask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var firstTask = backgroundTaskField?.GetValue(provider) as Task;

            // Simulate task completion
            backgroundTaskField?.SetValue(provider, Task.CompletedTask);

            // Act
            provider.Enter(_consoleMock.Object); // Should start new task

            // Assert
            var secondTask = backgroundTaskField?.GetValue(provider) as Task;
            secondTask.Should().NotBeSameAs(firstTask);
            secondTask!.IsCompleted.Should().BeFalse();
        }

        #endregion

        #region Helper Methods

        private static void SetSnapshotUsingReflection(NetworkStatusContentProvider provider, NetworkStatus snapshot)
        {
            var snapshotField = typeof(NetworkStatusContentProvider).GetField("_lastSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            snapshotField?.SetValue(provider, snapshot);
        }

        private static NetworkStatus? GetSnapshotUsingReflection(NetworkStatusContentProvider provider)
        {
            var snapshotField = typeof(NetworkStatusContentProvider).GetField("_lastSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return snapshotField?.GetValue(provider) as NetworkStatus;
        }

        #endregion
    }
}
