using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class ApplicationOrchestratorTests
    {
        private Mock<IVTubeStudioPCClient> _vtubeStudioPCClientMock;
        private Mock<IVTubeStudioPhoneClient> _vtubeStudioPhoneClientMock;
        private Mock<ITransformationEngine> _transformationEngineMock;
        private ApplicationOrchestrator _orchestrator;
        private string _tempConfigPath;
        
        public ApplicationOrchestratorTests()
        {
            // Set up mocks
            _vtubeStudioPCClientMock = new Mock<IVTubeStudioPCClient>();
            _vtubeStudioPhoneClientMock = new Mock<IVTubeStudioPhoneClient>();
            _transformationEngineMock = new Mock<ITransformationEngine>();
            
            // Create orchestrator with mocked dependencies
            _orchestrator = new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object);
                
            // Create temp config file for tests
            _tempConfigPath = CreateTempConfigFile("[]");
        }
        
        public void Dispose()
        {
            // Clean up temp file
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }
            
            _orchestrator.Dispose();
        }
        
        private string CreateTempConfigFile(string content)
        {
            var tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, content);
            return tempPath;
        }
        
        // Configuration and Initialization Tests
        
        [Fact]
        public async Task InitializeAsync_WithValidConfig_InitializesComponents()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Assert
            _transformationEngineMock.Verify(x => x.LoadRulesAsync(_tempConfigPath), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => x.DiscoverPortAsync(cancellationToken), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => x.ConnectAsync(cancellationToken), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => x.AuthenticateAsync(cancellationToken), Times.Once);
        }
        
        [Fact]
        public async Task InitializeAsync_WithEmptyIphoneIp_ThrowsArgumentException()
        {
            // This test is no longer relevant since the iPhone IP is now handled through config
            // and validated in the VTubeStudioPhoneClient constructor
        }
        
        [Fact]
        public async Task InitializeAsync_WithNonExistentConfigFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var cancellationToken = CancellationToken.None;
            
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _orchestrator.InitializeAsync(nonExistentPath, cancellationToken));
        }
        
        [Fact]
        public async Task InitializeAsync_WhenPortDiscoveryFails_ThrowsInvalidOperationException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0); // Return invalid port
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken));
        }
        
        [Fact]
        public async Task InitializeAsync_WhenAuthenticationFails_ThrowsInvalidOperationException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Authentication fails
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken));
        }
        
        // Connection and Lifecycle Tests
        
        [Fact]
        public async Task RunAsync_WithoutInitialization_ThrowsInvalidOperationException()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _orchestrator.RunAsync(cancellationToken));
        }
        
        [Fact]
        public async Task RunAsync_WithInitialization_StartsPhoneClient()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPhoneClientMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            await _orchestrator.InitializeAsync(_tempConfigPath, cts.Token);
            
            // Immediately cancel so RunAsync doesn't hang
            cts.CancelAfter(50);
            
            // Act
            await _orchestrator.RunAsync(cts.Token);
            
            // Assert
            _vtubeStudioPhoneClientMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task RunAsync_WhenCancelled_ClosesConnectionsGracefully()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPhoneClientMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            await _orchestrator.InitializeAsync(_tempConfigPath, cts.Token);
            
            // Immediately cancel so RunAsync doesn't hang
            cts.CancelAfter(50);
            
            // Act
            await _orchestrator.RunAsync(cts.Token);
            
            // Assert
            _vtubeStudioPCClientMock.Verify(x => 
                x.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        // Data Flow Tests
        
        [Fact]
        public async Task OnTrackingDataReceived_TransformsDataAndSendsToVTubeStudio()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var trackingResponse = new TrackingResponse 
            { 
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var transformedParams = new List<TrackingParam> 
            { 
                new TrackingParam { Id = "Test", Value = 0.5 } 
            };
            
            // Make RunAsync return completed task instead of hanging
            _vtubeStudioPhoneClientMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(WebSocketState.Open);
                
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);
            
            _vtubeStudioPCClientMock.Setup(x => 
                x.SendTrackingAsync(
                    transformedParams, 
                    trackingResponse.FaceFound, 
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Call RunAsync to subscribe to events but don't await it
            var _ = _orchestrator.RunAsync(cancellationToken);
            
            // Act - Trigger the event handler
            _vtubeStudioPhoneClientMock.Raise(
                x => x.TrackingDataReceived += null,
                new object[] { _vtubeStudioPhoneClientMock.Object, trackingResponse });
            
            // Need a small delay for the async event handler to complete
            await Task.Delay(50);
            
            // Assert
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(
                    transformedParams,
                    trackingResponse.FaceFound,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact]
        public async Task OnTrackingDataReceived_WhenTrackingDataIsNull_DoesNotProcessOrSend()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Act - Trigger the event handler with null data
            _vtubeStudioPhoneClientMock.Raise(
                x => x.TrackingDataReceived += null,
                new object[] { _vtubeStudioPhoneClientMock.Object, null });
            
            // Need a small delay for the async event handler to complete
            await Task.Delay(50);
            
            // Assert
            _transformationEngineMock.Verify(x => x.TransformData(It.IsAny<TrackingResponse>()), Times.Never);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
        
        [Fact]
        public async Task OnTrackingDataReceived_WhenConnectionClosed_DoesNotSendTracking()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var trackingResponse = new TrackingResponse 
            { 
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var transformedParams = new List<TrackingParam> 
            { 
                new TrackingParam { Id = "Test", Value = 0.5 } 
            };
            
            // Make RunAsync return completed task instead of hanging
            _vtubeStudioPhoneClientMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(WebSocketState.Closed); // Connection is closed
                
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);
            
            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Call RunAsync to subscribe to events but don't await it
            var _ = _orchestrator.RunAsync(cancellationToken);
            
            // Act - Trigger the event handler
            _vtubeStudioPhoneClientMock.Raise(
                x => x.TrackingDataReceived += null,
                new object[] { _vtubeStudioPhoneClientMock.Object, trackingResponse });
            
            // Need a small delay for the async event handler to complete
            await Task.Delay(50);
            
            // Assert
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
        
        // Disposal Tests
        
        [Fact]
        public void Dispose_DisposesAllDisposableComponents()
        {
            // Arrange
            // The mocks are already set up in the constructor
            
            // Act
            _orchestrator.Dispose();
            
            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.Dispose(), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.Dispose(), Times.Once);
        }
        
        [Fact]
        public void Dispose_CalledMultipleTimes_DisposesComponentsOnlyOnce()
        {
            // Arrange
            // The mocks are already set up in the constructor
            
            // Act
            _orchestrator.Dispose();
            _orchestrator.Dispose(); // Second call should be a no-op
            
            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.Dispose(), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.Dispose(), Times.Once);
        }

        // Constructor Parameter Tests

        [Fact]
        public void Constructor_WithNullVTubeStudioPCClient_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                null,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object));
        }

        [Fact]
        public void Constructor_WithNullVTubeStudioPhoneClient_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                null,
                _transformationEngineMock.Object));
        }

        [Fact]
        public void Constructor_WithNullTransformationEngine_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                null));
        }

        // Additional Initialization Tests

        [Fact]
        public async Task InitializeAsync_WithEmptyTransformConfigPath_ThrowsArgumentException()
        {
            // Arrange
            string emptyPath = string.Empty;
            var cancellationToken = CancellationToken.None;
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _orchestrator.InitializeAsync(emptyPath, cancellationToken));
        }

        // Additional RunAsync Tests

        [Fact]
        public async Task RunAsync_WhenCloseAsyncThrowsException_HandlesGracefully()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPhoneClientMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            // Setup CloseAsync to throw an exception
            _vtubeStudioPCClientMock.Setup(x => 
                x.CloseAsync(
                    It.IsAny<WebSocketCloseStatus>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Test exception"));
            
            await _orchestrator.InitializeAsync(_tempConfigPath, cts.Token);
            
            // Immediately cancel so RunAsync doesn't hang
            cts.CancelAfter(50);
            
            // Act
            await _orchestrator.RunAsync(cts.Token);
            
            // Assert
            _vtubeStudioPCClientMock.Verify(x => 
                x.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        // Additional OnTrackingDataReceived Tests

        [Fact]
        public async Task OnTrackingDataReceived_WhenTransformDataThrowsException_HandlesGracefully()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var trackingResponse = new TrackingResponse 
            { 
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            
            // Make RunAsync return completed task instead of hanging
            _vtubeStudioPhoneClientMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(WebSocketState.Open);
                
            // Setup TransformData to throw an exception
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Throws(new InvalidOperationException("Test exception"));
            
            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Call RunAsync to subscribe to events but don't await it
            var _ = _orchestrator.RunAsync(cancellationToken);
            
            // Act - Trigger the event handler
            _vtubeStudioPhoneClientMock.Raise(
                x => x.TrackingDataReceived += null,
                new object[] { _vtubeStudioPhoneClientMock.Object, trackingResponse });
            
            // Need a small delay for the async event handler to complete
            await Task.Delay(50);
            
            // Assert - No exception should be thrown and no tracking data should be sent
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WhenSendTrackingAsyncThrowsException_HandlesGracefully()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var trackingResponse = new TrackingResponse 
            { 
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var transformedParams = new List<TrackingParam> 
            { 
                new TrackingParam { Id = "Test", Value = 0.5 } 
            };
            
            // Make RunAsync return completed task instead of hanging
            _vtubeStudioPhoneClientMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(WebSocketState.Open);
                
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);
            
            // Setup SendTrackingAsync to throw an exception
            _vtubeStudioPCClientMock.Setup(x => 
                x.SendTrackingAsync(
                    transformedParams, 
                    trackingResponse.FaceFound, 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Test exception"));
            
            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Call RunAsync to subscribe to events but don't await it
            var _ = _orchestrator.RunAsync(cancellationToken);
            
            // Act - Trigger the event handler
            _vtubeStudioPhoneClientMock.Raise(
                x => x.TrackingDataReceived += null,
                new object[] { _vtubeStudioPhoneClientMock.Object, trackingResponse });
            
            // Need a small delay for the async event handler to complete
            await Task.Delay(50);
            
            // Assert - Verify that the transform was called but exception was handled
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(
                    transformedParams,
                    trackingResponse.FaceFound,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}