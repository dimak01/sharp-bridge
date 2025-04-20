using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private Mock<IAppLogger> _loggerMock;
        private Mock<IConsoleRenderer> _consoleRendererMock;
        private Mock<IKeyboardInputHandler> _keyboardInputHandlerMock;
        private ApplicationOrchestrator _orchestrator;
        private string _tempConfigPath;
        private VTubeStudioPhoneClientConfig _phoneConfig;
        
        public ApplicationOrchestratorTests()
        {
            // Set up mocks
            _vtubeStudioPCClientMock = new Mock<IVTubeStudioPCClient>();
            _vtubeStudioPhoneClientMock = new Mock<IVTubeStudioPhoneClient>();
            _transformationEngineMock = new Mock<ITransformationEngine>();
            _loggerMock = new Mock<IAppLogger>();
            _consoleRendererMock = new Mock<IConsoleRenderer>();
            _keyboardInputHandlerMock = new Mock<IKeyboardInputHandler>();
            
            // Create a simple phone config for testing
            _phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1",
                IphonePort = 1234,
                LocalPort = 5678,
                RequestIntervalSeconds = 1.0,
                ReceiveTimeoutMs = 100,
                SendForSeconds = 5
            };
            
            // Create orchestrator with mocked dependencies
            _orchestrator = new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object
            );
                
            // Create temp config file for tests
            _tempConfigPath = CreateTempConfigFile("[]");
        }
        
        private ApplicationOrchestrator CreateOrchestrator()
        {
            return new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object
            );
        }
        
        // Helper method to set up basic orchestrator requirements for event-based tests
        private void SetupOrchestratorTest(WebSocketState pcClientState = WebSocketState.Open)
        {
            // Configure PC client behavior
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(pcClientState);
                
            // Configure phone client basic behavior
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .Returns(Task.CompletedTask);
        }

        // Helper method to run an event-triggered test with timeout protection
        private async Task<bool> RunWithTimeoutAndEventTrigger(
            PhoneTrackingInfo trackingData, 
            TimeSpan timeout,
            Func<Task> additionalSetup = null)
        {
            // Create cancellation tokens with timeout
            using var cts = new CancellationTokenSource();
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, timeoutCts.Token);
            
            bool eventWasRaised = false;
            
            // Configure ReceiveResponseAsync to raise the event
            int receiveCallCount = 0;
            _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .Callback(() => {
                    receiveCallCount++;
                    if (receiveCallCount == 1) {
                        // Raise the event on first call
                        _vtubeStudioPhoneClientMock.Raise(
                            x => x.TrackingDataReceived += null,
                            new object[] { _vtubeStudioPhoneClientMock.Object, trackingData });
                        
                        eventWasRaised = true;
                        
                        // Cancel after a short delay to allow event processing
                        Task.Delay(100).ContinueWith(_ => cts.Cancel());
                    }
                })
                .ReturnsAsync(true);
            
            // Allow caller to perform additional setup if needed
            if (additionalSetup != null)
            {
                await additionalSetup();
            }
            
            // Initialize and run the orchestrator
            await _orchestrator.InitializeAsync(_tempConfigPath, linkedCts.Token);
            await _orchestrator.RunAsync(linkedCts.Token);
            
            return eventWasRaised;
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
            var originalOut = Console.Out;  
            try{
                var consoleWriter = new StringWriter();
                Console.SetOut(consoleWriter);
                // Arrange
                var cts = new CancellationTokenSource();
                
                _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(8001);
                    
                _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                    
                _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
                    
                _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                    .Returns(Task.CompletedTask);
                    
                _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
                
                await _orchestrator.InitializeAsync(_tempConfigPath, cts.Token);
                
                // Immediately cancel so RunAsync doesn't hang
                cts.CancelAfter(50);
                
                // Act
                await _orchestrator.RunAsync(cts.Token);
                
                // Assert
                _vtubeStudioPhoneClientMock.Verify(x => x.SendTrackingRequestAsync(), Times.AtLeastOnce);
            }
            finally{
                Console.SetOut(originalOut);
            }
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
                
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
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
            var trackingResponse = new PhoneTrackingInfo 
            { 
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var transformedParams = new List<TrackingParam> 
            { 
                new TrackingParam { 
                    Id = "Test", 
                    Value = 0.5,
                    Min = -0.75,
                    Max = 1.25,
                    DefaultValue = 0.33
                } 
            };
            
            // Setup basic orchestrator requirements
            SetupOrchestratorTest();
            
            // Configure the transformation engine to return test parameters
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);
            
            // Configure the PC client to verify the test parameters
            _vtubeStudioPCClientMock.Setup(x => 
                x.SendTrackingAsync(
                    It.Is<PCTrackingInfo>(pc => 
                        pc.FaceFound == trackingResponse.FaceFound && 
                        pc.Parameters == transformedParams &&
                        pc.Parameters.First().Min == -0.75 &&
                        pc.Parameters.First().Max == 1.25 &&
                        pc.Parameters.First().DefaultValue == 0.33),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse, 
                TimeSpan.FromSeconds(2));
            
            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(
                    It.Is<PCTrackingInfo>(pc => 
                        pc.FaceFound == trackingResponse.FaceFound && 
                        pc.Parameters == transformedParams &&
                        pc.Parameters.First().Min == -0.75 &&
                        pc.Parameters.First().Max == 1.25 &&
                        pc.Parameters.First().DefaultValue == 0.33),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact]
        public async Task OnTrackingDataReceived_WhenTrackingDataIsNull_DoesNotProcessOrSend()
        {
            // Arrange
            PhoneTrackingInfo nullTrackingData = null;
            
            // Setup basic orchestrator requirements
            SetupOrchestratorTest();
            
            // Act - Run the test with a 2 second timeout, passing null tracking data
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                nullTrackingData, 
                TimeSpan.FromSeconds(2));
            
            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(It.IsAny<PhoneTrackingInfo>()), Times.Never);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        
        [Fact]
        public async Task OnTrackingDataReceived_WhenConnectionClosed_DoesNotSendTracking()
        {
            // Arrange
            var trackingResponse = new PhoneTrackingInfo 
            { 
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var transformedParams = new List<TrackingParam> 
            { 
                new TrackingParam { 
                    Id = "Test", 
                    Value = 0.5,
                    Min = -0.75,
                    Max = 1.25,
                    DefaultValue = 0.33
                } 
            };
            
            // Setup basic orchestrator requirements with closed connection state
            SetupOrchestratorTest(WebSocketState.Closed);
            
            // Configure the transformation engine to return test parameters
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);
            
            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse, 
                TimeSpan.FromSeconds(2));
            
            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
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
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullVTubeStudioPhoneClient_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                null,
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullTransformationEngine_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                null,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPhoneConfig_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                null,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                null,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullConsoleRenderer_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                null,
                _keyboardInputHandlerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullKeyboardInputHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
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
                
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
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
            var trackingResponse = new PhoneTrackingInfo 
            { 
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            
            // Setup basic orchestrator requirements
            SetupOrchestratorTest();
            
            // Setup TransformData to throw an exception
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Throws(new InvalidOperationException("Test exception"));
            
            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse, 
                TimeSpan.FromSeconds(2));
            
            // Assert - No exception should be thrown and no tracking data should be sent
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task OnTrackingDataReceived_WhenSendTrackingAsyncThrowsException_HandlesGracefully()
        {
            // Arrange
            var trackingResponse = new PhoneTrackingInfo 
            { 
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var transformedParams = new List<TrackingParam> 
            { 
                new TrackingParam { 
                    Id = "Test", 
                    Value = 0.5,
                    Min = -0.75,
                    Max = 1.25,
                    DefaultValue = 0.33
                } 
            };
            
            // Setup basic orchestrator requirements
            SetupOrchestratorTest();
            
            // Configure the transformation engine to return test parameters
            _transformationEngineMock.Setup(x => x.TransformData(trackingResponse))
                .Returns(transformedParams);
            
            // Setup SendTrackingAsync to throw an exception with PCTrackingInfo parameter
            _vtubeStudioPCClientMock.Setup(x => 
                x.SendTrackingAsync(
                    It.IsAny<PCTrackingInfo>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Test exception"));
            
            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse, 
                TimeSpan.FromSeconds(2));
            
            // Assert - Verify that the transform was called but exception was handled
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_RegistersKeyboardShortcuts()
        {
            // Arrange
            _vtubeStudioPCClientMock
                .Setup(client => client.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock
                .Setup(client => client.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock
                .Setup(client => client.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _transformationEngineMock
                .Setup(engine => engine.LoadRulesAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
                
            // Setup keyboard handler mock to verify registration calls
            _keyboardInputHandlerMock
                .Setup(handler => handler.RegisterShortcut(
                    It.IsAny<ConsoleKey>(), 
                    It.IsAny<ConsoleModifiers>(), 
                    It.IsAny<Action>(), 
                    It.IsAny<string>()))
                .Verifiable();
                
            var orchestrator = CreateOrchestrator();
            
            // Act
            await orchestrator.InitializeAsync(_tempConfigPath, CancellationToken.None);
            
            // Assert
            _keyboardInputHandlerMock.Verify(
                handler => handler.RegisterShortcut(
                    It.IsAny<ConsoleKey>(), 
                    It.IsAny<ConsoleModifiers>(), 
                    It.IsAny<Action>(), 
                    It.IsAny<string>()),
                Times.AtLeast(3));
        }

        [Fact]
        public async Task RunAsync_ChecksForKeyboardInput()
        {
            // Arrange
            // Setup basic mocks for initialization
            _vtubeStudioPCClientMock
                .Setup(client => client.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock
                .Setup(client => client.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock
                .Setup(client => client.AuthenticateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _transformationEngineMock
                .Setup(engine => engine.LoadRulesAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            // Set up the keyboardInputHandler to signal when CheckForKeyboardInput is called
            bool keyboardInputChecked = false;
            _keyboardInputHandlerMock
                .Setup(handler => handler.CheckForKeyboardInput())
                .Callback(() => keyboardInputChecked = true);
                
            // Set up phoneClient to return no data after N tries
            int attempts = 0;
            _vtubeStudioPhoneClientMock
                .Setup(client => client.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => {
                    attempts++;
                    return attempts <= 3; // Only return true for first 3 attempts
                });
                
            _vtubeStudioPhoneClientMock
                .Setup(client => client.SendTrackingRequestAsync())
                .Returns(Task.CompletedTask);
                
            // Create and initialize orchestrator
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(_tempConfigPath, CancellationToken.None);
            
            // Use a cancellation token that will cancel after a delay
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            
            // Act
            await orchestrator.RunAsync(cts.Token);
            
            // Assert
            _keyboardInputHandlerMock.Verify(
                handler => handler.CheckForKeyboardInput(),
                Times.AtLeast(1));
            Assert.True(keyboardInputChecked, "Keyboard input should have been checked");
        }
    }
}