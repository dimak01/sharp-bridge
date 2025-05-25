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
        private Mock<IVTubeStudioPCParameterManager> _parameterManagerMock;
        private Mock<IRecoveryPolicy> _recoveryPolicyMock;
        private ApplicationOrchestrator _orchestrator;
        private string _tempConfigPath;
        private VTubeStudioPhoneClientConfig _phoneConfig;
        private VTubeStudioPCConfig _pcConfig;
        private CancellationTokenSource _defaultCts;
        private TimeSpan _longTimeout; 
        private CancellationTokenSource _longTimeoutCts;
        
        public ApplicationOrchestratorTests()
        {
            // Set up mocks
            _vtubeStudioPCClientMock = new Mock<IVTubeStudioPCClient>();
            _vtubeStudioPhoneClientMock = new Mock<IVTubeStudioPhoneClient>();
            _transformationEngineMock = new Mock<ITransformationEngine>();
            _loggerMock = new Mock<IAppLogger>();
            _consoleRendererMock = new Mock<IConsoleRenderer>();
            _keyboardInputHandlerMock = new Mock<IKeyboardInputHandler>();
            _parameterManagerMock = new Mock<IVTubeStudioPCParameterManager>();
            _recoveryPolicyMock = new Mock<IRecoveryPolicy>();
            
            // Configure recovery policy to return 2 second delay
            _recoveryPolicyMock.Setup(x => x.GetNextDelay())
                .Returns(TimeSpan.FromMilliseconds(50));
            
            _defaultCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
            _longTimeout = TimeSpan.FromMilliseconds(500);
            _longTimeoutCts = new CancellationTokenSource(_longTimeout);
            
            // Create a simple phone config for testing
            _phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1",
                IphonePort = 1234,
                LocalPort = 5678,
                RequestIntervalSeconds = 1.0,
                ReceiveTimeoutMs = 100,
                SendForSeconds = 5,
                ErrorDelayMs = 10 // Fast error retry for tests
            };
            
            // Create a simple PC config for testing
            _pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                TokenFilePath = "test_token.txt"
            };
            
            // Create orchestrator with mocked dependencies
            _orchestrator = new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object
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
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object
            );
        }
        
        private void SetupBasicMocks()
        {
            const string testToken = "test-token";
            
            // Configure PC client behavior
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            // Configure TryInitializeAsync for both clients
            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            _vtubeStudioPhoneClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Setup PC client service stats
            var pcStats = new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: "Connected",
                currentEntity: new PCTrackingInfo(),
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );
            _vtubeStudioPCClientMock.Setup(x => x.GetServiceStats())
                .Returns(pcStats);
                
            // Configure parameter manager
            _parameterManagerMock.Setup(x => x.SynchronizeParametersAsync(
                    It.IsAny<IEnumerable<VTSParameter>>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Configure transformation engine to return default parameters
            _transformationEngineMock.Setup(x => x.GetParameterDefinitions())
                .Returns(new List<VTSParameter>
                {
                    new VTSParameter("TestParam", -1.0, 1.0, 0.0)
                });
                
            _transformationEngineMock
                .Setup(engine => engine.LoadRulesAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
                
            // Configure phone client basic behavior
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Setup Phone client service stats
            var phoneStats = new ServiceStats(
                serviceName: "PhoneClient",
                status: "Connected",
                currentEntity: new PhoneTrackingInfo(),
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );
            _vtubeStudioPhoneClientMock.Setup(x => x.GetServiceStats())
                .Returns(phoneStats);
        }

        // Helper method to set up basic orchestrator requirements for event-based tests
        private void SetupOrchestratorTest(WebSocketState pcClientState = WebSocketState.Open)
        {
            // Set up basic mocks first
            SetupBasicMocks();
            
            // Override WebSocket state if needed
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(pcClientState);
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
        
        // Helper methods for test execution
        private async Task RunWithDefaultTimeout(Func<Task> testAction)
        {
            try
            {
                await testAction();
            }
            catch (OperationCanceledException)
            {
                // Expected for normal test completion
            }
        }

        private async Task RunWithCustomTimeout(Func<Task> testAction, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await testAction();
            }
            catch (OperationCanceledException)
            {
                // Expected for normal test completion
            }
        }

        private async Task RunWithException<TException>(Func<Task> testAction) where TException : Exception
        {
            await Assert.ThrowsAsync<TException>(testAction);
        }
        
        public void Dispose()
        {
            // Clean up temp file
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }
            
            _orchestrator.Dispose();
            _defaultCts.Dispose();
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
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;
            
            // Act
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Assert
            _transformationEngineMock.Verify(x => x.LoadRulesAsync(_tempConfigPath), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _vtubeStudioPhoneClientMock.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
        }
        
        [Fact]
        public async Task InitializeAsync_WithNonExistentConfigFile_ThrowsFileNotFoundException()
        {
            // Arrange
            SetupBasicMocks();
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var cancellationToken = CancellationToken.None;
            
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _orchestrator.InitializeAsync(nonExistentPath, cancellationToken));
        }
        
        [Fact]
        public async Task InitializeAsync_WhenPortDiscoveryFails_CompletesGracefully()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;
            
            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Initialization fails
            
            // Act - should not throw
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Assert - verify that initialization was attempted
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task InitializeAsync_WhenAuthenticationFails_CompletesGracefully()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;
            
            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Initialization (including authentication) fails
            
            // Act - should not throw
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Assert - verify that initialization was attempted
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        // Connection and Lifecycle Tests
        
        [Fact]
        public async Task RunAsync_WithoutInitialization_RunsGracefully()
        {
            // Arrange
            SetupBasicMocks();
            
            // Act - should not throw exception
            await RunWithDefaultTimeout(async () => 
                await _orchestrator.RunAsync(_defaultCts.Token));
            
            // Assert - verify that the application attempted to run
            _vtubeStudioPhoneClientMock.Verify(x => x.SendTrackingRequestAsync(), Times.AtLeastOnce);
        }
        
        [Fact]
        public async Task RunAsync_WithInitialization_StartsPhoneClient()
        {
            var originalOut = Console.Out;  
            try{
                SetupBasicMocks();
                var consoleWriter = new StringWriter();
                Console.SetOut(consoleWriter);
                
                await _orchestrator.InitializeAsync(_tempConfigPath, _defaultCts.Token);
                
                // Act
                await RunWithDefaultTimeout(async () => 
                {
                    await _orchestrator.RunAsync(_defaultCts.Token);
                });
                
                // Assert
                _vtubeStudioPhoneClientMock.Verify(x => x.SendTrackingRequestAsync(), Times.AtLeastOnce);
            }
            finally{
                Console.SetOut(originalOut);
            }
        }
        
        [Fact]
        public async Task RunAsync_WhenCloseAsyncThrowsException_HandlesGracefullyWithRetry()
        {
            // Arrange
            SetupBasicMocks();
            
            // Additional setup for this specific test case
            _vtubeStudioPCClientMock
                .Setup(client => client.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));
                
            // Create and initialize orchestrator
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(_tempConfigPath, _defaultCts.Token);
            
            // Act & Assert
            await RunWithDefaultTimeout(async () => 
            {
                await orchestrator.RunAsync(_defaultCts.Token);
            });
            
            // Verify that close was attempted
            _vtubeStudioPCClientMock.Verify(
                client => client.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact]
        public async Task RunAsync_WhenCancelled_ClosesConnectionsGracefully()
        {
            // Arrange
            SetupBasicMocks();
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            await _orchestrator.InitializeAsync(_tempConfigPath, _defaultCts.Token);
            
            // Act & Assert - should not throw exception
            await RunWithDefaultTimeout(async () => 
            {
                await _orchestrator.RunAsync(_defaultCts.Token);
            });
            
            // Verify cleanup was performed
            _vtubeStudioPCClientMock.Verify(x => 
                x.CloseAsync(
                    It.IsAny<WebSocketCloseStatus>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
                
            // Verify the proper log message was written
            _loggerMock.Verify(x => x.Info("Application stopped"), Times.Once);
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
            
            var transformedParams = new PCTrackingInfo 
            { 
                FaceFound = true,
                Parameters = new List<TrackingParam> 
                { 
                    new TrackingParam 
                    { 
                        Id = "Test", 
                        Value = 0.5,
                        Weight = 1.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter(
                        "Test",
                        -0.75,
                        1.25,
                        0.33
                    )
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
                        pc.Parameters.First().Id == "Test" &&
                        pc.Parameters.First().Value == 0.5 &&
                        pc.ParameterDefinitions["Test"].Min == -0.75 &&
                        pc.ParameterDefinitions["Test"].Max == 1.25 &&
                        pc.ParameterDefinitions["Test"].DefaultValue == 0.33),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingResponse, 
                _longTimeout);
            
            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(
                    It.Is<PCTrackingInfo>(pc => 
                        pc.FaceFound == trackingResponse.FaceFound && 
                        pc.Parameters.First().Id == "Test" &&
                        pc.Parameters.First().Value == 0.5 &&
                        pc.ParameterDefinitions["Test"].Min == -0.75 &&
                        pc.ParameterDefinitions["Test"].Max == 1.25 &&
                        pc.ParameterDefinitions["Test"].DefaultValue == 0.33),
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
                _longTimeout);
            
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
            var transformedParams = new PCTrackingInfo 
            { 
                FaceFound = true,
                Parameters = new List<TrackingParam> 
                { 
                    new TrackingParam 
                    { 
                        Id = "Test", 
                        Value = 0.5,
                        Weight = 1.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter(
                        "Test",
                        -0.75,
                        1.25,
                        0.33
                    )
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
                _longTimeout);
            
            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingResponse), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        
        [Fact]
        public async Task OnTrackingDataReceived_WithMultipleEvents_ProcessesAllEvents()
        {
            // Arrange
            SetupBasicMocks();
            var trackingData1 = new PhoneTrackingInfo { FaceFound = true, BlendShapes = new List<BlendShape>() };
            var trackingData2 = new PhoneTrackingInfo { FaceFound = false, BlendShapes = new List<BlendShape>() };
            
            var transformedParams1 = new PCTrackingInfo 
            { 
                FaceFound = true,
                Parameters = new List<TrackingParam> { new TrackingParam { Id = "Test1", Value = 0.5 } },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test1"] = new VTSParameter("Test1", -1.0, 1.0, 0.0)
                }
            };
            
            var transformedParams2 = new PCTrackingInfo 
            { 
                FaceFound = false,
                Parameters = new List<TrackingParam> { new TrackingParam { Id = "Test2", Value = 0.7 } },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test2"] = new VTSParameter("Test2", -1.0, 1.0, 0.0)
                }
            };
            
            // Configure transformation for both events
            _transformationEngineMock.Setup(x => x.TransformData(It.Is<PhoneTrackingInfo>(p => p.FaceFound == true)))
                .Returns(transformedParams1);
                
            _transformationEngineMock.Setup(x => x.TransformData(It.Is<PhoneTrackingInfo>(p => p.FaceFound == false)))
                .Returns(transformedParams2);
            
            // Configure PC client to track sends
            var sentParams = new List<PCTrackingInfo>();
            _vtubeStudioPCClientMock.Setup(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()))
                .Callback<PCTrackingInfo, CancellationToken>((info, _) => sentParams.Add(info))
                .Returns(Task.CompletedTask);

            // Set the WebSocketState to Open
            _vtubeStudioPCClientMock.SetupGet(x => x.State)
                .Returns(WebSocketState.Open);
            
            // Initialize orchestrator to subscribe to events
            var cancellationToken = CancellationToken.None;
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Get the event handler method via reflection
            var onTrackingDataReceivedMethod = typeof(ApplicationOrchestrator)
                .GetMethod("OnTrackingDataReceived", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
            
            // Act
            // Directly invoke the event handler with each tracking data
            onTrackingDataReceivedMethod.Invoke(_orchestrator, 
                new object[] { _vtubeStudioPhoneClientMock.Object, trackingData1 });
                
            // Small delay between events
            await Task.Delay(50);
            
            onTrackingDataReceivedMethod.Invoke(_orchestrator, 
                new object[] { _vtubeStudioPhoneClientMock.Object, trackingData2 });
                
            // Allow time for processing
            await Task.Delay(100);
            
            // Assert
            _transformationEngineMock.Verify(
                x => x.TransformData(It.Is<PhoneTrackingInfo>(p => p.FaceFound == true)), 
                Times.Once);
                
            _transformationEngineMock.Verify(
                x => x.TransformData(It.Is<PhoneTrackingInfo>(p => p.FaceFound == false)), 
                Times.Once);
                
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
                
            Assert.Equal(2, sentParams.Count);
            Assert.True(sentParams[0].FaceFound);
            Assert.Equal("Test1", sentParams[0].Parameters.First().Id);
            Assert.Equal(0.5, sentParams[0].Parameters.First().Value);
            Assert.Equal(-1.0, sentParams[0].ParameterDefinitions["Test1"].Min);
            Assert.Equal(1.0, sentParams[0].ParameterDefinitions["Test1"].Max);
            Assert.Equal(0.0, sentParams[0].ParameterDefinitions["Test1"].DefaultValue);
            
            Assert.False(sentParams[1].FaceFound);
            Assert.Equal("Test2", sentParams[1].Parameters.First().Id);
            Assert.Equal(0.7, sentParams[1].Parameters.First().Value);
            Assert.Equal(-1.0, sentParams[1].ParameterDefinitions["Test2"].Min);
            Assert.Equal(1.0, sentParams[1].ParameterDefinitions["Test2"].Max);
            Assert.Equal(0.0, sentParams[1].ParameterDefinitions["Test2"].DefaultValue);
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

        [Fact]
        public void Dispose_WhenResourcesAreInErrorState_CleansUpGracefully()
        {
            // Arrange
            SetupBasicMocks();
            _vtubeStudioPCClientMock.Setup(x => x.CloseAsync(
                It.IsAny<WebSocketCloseStatus>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Test exception"));
            
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
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object));
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
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object));
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
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object));
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
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object));
        }

        [Fact]
        public void Constructor_WithNullPCConfig_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                null,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object));
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
                _pcConfig,
                null,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object));
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
                _pcConfig,
                _loggerMock.Object,
                null,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object));
        }

        [Fact]
        public void Constructor_WithNullKeyboardInputHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                null,
                _parameterManagerMock.Object,
                _recoveryPolicyMock.Object));
        }

        [Fact]
        public void Constructor_WithNullParameterManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                null,
                _recoveryPolicyMock.Object));
        }

        [Fact]
        public void Constructor_WithNullRecoveryPolicy_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ApplicationOrchestrator(
                _vtubeStudioPCClientMock.Object,
                _vtubeStudioPhoneClientMock.Object,
                _transformationEngineMock.Object,
                _phoneConfig,
                _pcConfig,
                _loggerMock.Object,
                _consoleRendererMock.Object,
                _keyboardInputHandlerMock.Object,
                _parameterManagerMock.Object,
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
        public async Task RunAsync_ChecksForKeyboardInput()
        {
            // Arrange
            SetupBasicMocks();
            
            // Set up the keyboardInputHandler to signal when CheckForKeyboardInput is called
            bool keyboardInputChecked = false;
            _keyboardInputHandlerMock
                .Setup(handler => handler.CheckForKeyboardInput())
                .Callback(() => keyboardInputChecked = true);
                
            // Create and initialize orchestrator
            var orchestrator = CreateOrchestrator();
            await orchestrator.InitializeAsync(_tempConfigPath, _defaultCts.Token);
            
            // Act
            await RunWithDefaultTimeout(async () => 
            {
                await orchestrator.RunAsync(_defaultCts.Token);
            });
            
            // Assert
            _keyboardInputHandlerMock.Verify(
                handler => handler.CheckForKeyboardInput(),
                Times.AtLeast(1));
            Assert.True(keyboardInputChecked, "Keyboard input should have been checked");
        }

        [Fact]
        public async Task RunAsync_WhenReceiveResponseAsyncThrowsException_ContinuesRunning()
        {
            // Arrange
            SetupBasicMocks();
            
            var exceptionThrown = false;
            
            // Setup to throw exception on first call, then succeed on second call, then cancel
            int receiveCallCount = 0;
            _vtubeStudioPhoneClientMock.Setup(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .Returns(() => {
                    receiveCallCount++;
                    if (receiveCallCount == 1)
                    {
                        exceptionThrown = true;
                        throw new InvalidOperationException("Test exception");
                    }
                    else if (receiveCallCount == 2)
                    {
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                });
            
            await _orchestrator.InitializeAsync(_tempConfigPath, _longTimeoutCts.Token);
            
            // Act - Use a longer timeout to ensure we get multiple calls
            await RunWithCustomTimeout(async () => 
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);
            
            // Assert
            Assert.True(exceptionThrown, "Exception should have been thrown");
            _vtubeStudioPhoneClientMock.Verify(x => x.ReceiveResponseAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
            _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error in application loop")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenCloseAsyncThrowsException_HandlesGracefully()
        {
            // Arrange
            SetupBasicMocks();
            
            // Setup CloseAsync to throw an exception
            _vtubeStudioPCClientMock.Setup(x => 
                x.CloseAsync(
                    It.IsAny<WebSocketCloseStatus>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Test exception"));
            
            await _orchestrator.InitializeAsync(_tempConfigPath, _defaultCts.Token);
            
            // Act
            await RunWithDefaultTimeout(async () => 
            {
                await _orchestrator.RunAsync(_defaultCts.Token);
            });
            
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
                _longTimeout);
            
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
            var transformedParams = new PCTrackingInfo 
            { 
                FaceFound = true,
                Parameters = new List<TrackingParam> 
                { 
                    new TrackingParam { 
                        Id = "Test", 
                        Value = 0.5,
                        Weight = 1.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter(
                        "Test",
                        -0.75,
                        1.25,
                        0.33
                    )
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
                _longTimeout);
            
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
            SetupBasicMocks();
            const string testToken = "test-token";
            
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
        public async Task ReloadTransformationConfig_LoadsNewConfiguration()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;
            
            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Setup transform engine to track reloads
            _transformationEngineMock.Invocations.Clear(); // Clear previous invocations
            
            // Get the reload action from registered shortcuts
            Action reloadAction = null;
            _keyboardInputHandlerMock.Verify(x => x.RegisterShortcut(
                ConsoleKey.K,
                ConsoleModifiers.Alt,
                It.IsAny<Action>(),
                It.IsAny<string>()),
                Times.Once);
                
            // Capture the reload action from the registration
            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.K,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => reloadAction = action);
                
            // Re-initialize to capture the action
            _orchestrator = CreateOrchestrator();
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Act - trigger the reload action
            reloadAction.Should().NotBeNull("Reload action should be registered");
            reloadAction();
            
            // Allow time for async operations to complete
            await Task.Delay(100);
            
            // Assert
            _transformationEngineMock.Verify(x => x.LoadRulesAsync(_tempConfigPath), Times.Exactly(2));
        }
        
        [Fact]
        public async Task ReloadTransformationConfig_WhenLoadingFails_LogsError()
        {
            // Arrange
            SetupBasicMocks();
            var cancellationToken = CancellationToken.None;
            var expectedException = new InvalidOperationException("Test exception");
            
            // Setup transform engine to throw on second load
            int loadCount = 0;
            _transformationEngineMock.Setup(x => x.LoadRulesAsync(It.IsAny<string>()))
                .Returns(() => {
                    loadCount++;
                    if (loadCount > 1)
                        throw expectedException;
                    return Task.CompletedTask;
                });
            
            // Get the reload action from registered shortcuts
            Action reloadAction = null;
            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.K,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => reloadAction = action);
                
            // Initialize the orchestrator
            await _orchestrator.InitializeAsync(_tempConfigPath, cancellationToken);
            
            // Act
            reloadAction();
            
            // Allow time for async operations to complete
            await Task.Delay(100);
            
            // Assert
            _loggerMock.Verify(x => x.ErrorWithException(
                It.Is<string>(s => s.Contains("Error reloading transformation config")),
                expectedException), Times.Once);
        }
        
        // Tests for UpdateConsoleStatus
        
        [Fact]
        public void UpdateConsoleStatus_WithValidStats_UpdatesConsoleRenderer()
        {
            // Arrange
            var phoneStats = new ServiceStats("PhoneClient", "Connected", new PhoneTrackingInfo());
            var pcStats = new ServiceStats("PCClient", "Connected", new PCTrackingInfo());
            
            _vtubeStudioPhoneClientMock.Setup(x => x.GetServiceStats())
                .Returns(phoneStats);
                
            _vtubeStudioPCClientMock.Setup(x => x.GetServiceStats())
                .Returns(pcStats);
            
            // Get the private method via reflection
            var updateConsoleStatusMethod = typeof(ApplicationOrchestrator)
                .GetMethod("UpdateConsoleStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            updateConsoleStatusMethod.Invoke(_orchestrator, null);
            
            // Assert
            _consoleRendererMock.Verify(x => x.Update(It.Is<List<IServiceStats>>(
                stats => stats.Count == 2 && stats.Contains(phoneStats) && stats.Contains(pcStats))), 
                Times.Once);
        }
        
        [Fact]
        public void UpdateConsoleStatus_WithNullStats_HandlesGracefully()
        {
            // Arrange
            _vtubeStudioPhoneClientMock.Setup(x => x.GetServiceStats())
                .Returns((ServiceStats)null);
                
            _vtubeStudioPCClientMock.Setup(x => x.GetServiceStats())
                .Returns(new ServiceStats(
                    serviceName: "PCClient",
                    status: "Disconnected",
                    currentEntity: new PCTrackingInfo(),
                    isHealthy: false,
                    lastSuccessfulOperation: DateTime.UtcNow.AddMinutes(-5),
                    lastError: "Connection lost",
                    counters: new Dictionary<string, long>()
                ));
            
            // Get the private method via reflection
            var updateConsoleStatusMethod = typeof(ApplicationOrchestrator)
                .GetMethod("UpdateConsoleStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            updateConsoleStatusMethod.Invoke(_orchestrator, null);
            
            // Assert
            _consoleRendererMock.Verify(x => x.Update(It.Is<List<IServiceStats>>(
                stats => stats.Count == 1 && stats[0] is ServiceStats)), 
                Times.Once);
        }
        
        [Fact]
        public void UpdateConsoleStatus_WhenRendererThrowsException_LogsError()
        {
            // Arrange
            var phoneStats = new ServiceStats("PhoneClient", "Connected", new PhoneTrackingInfo());
            var pcStats = new ServiceStats("PCClient", "Connected", new PCTrackingInfo());
            var expectedException = new InvalidOperationException("Test exception");
            
            _vtubeStudioPhoneClientMock.Setup(x => x.GetServiceStats())
                .Returns(phoneStats);
                
            _vtubeStudioPCClientMock.Setup(x => x.GetServiceStats())
                .Returns(new ServiceStats(
                    serviceName: "PCClient",
                    status: "Disconnected",
                    currentEntity: new PCTrackingInfo(),
                    isHealthy: false,
                    lastSuccessfulOperation: DateTime.UtcNow.AddMinutes(-5),
                    lastError: "Connection lost",
                    counters: new Dictionary<string, long>()
                ));
                
            _consoleRendererMock.Setup(x => x.Update(It.IsAny<List<IServiceStats>>()))
                .Throws(expectedException);
            
            // Get the private method via reflection
            var updateConsoleStatusMethod = typeof(ApplicationOrchestrator)
                .GetMethod("UpdateConsoleStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act - should not throw
            updateConsoleStatusMethod.Invoke(_orchestrator, null);
            
            // Assert
            _loggerMock.Verify(x => x.ErrorWithException(
                It.Is<string>(s => s.Contains("Error updating console status")),
                expectedException), Times.Once);
        }
        
        // Tests for Console Renderer Formatting
        
        [Fact]
        public void RegisterKeyboardShortcuts_RegistersCycleVerbosityForPCClient()
        {
            // Arrange
            var pcFormatter = new Mock<IFormatter>();
            pcFormatter.Setup(x => x.CycleVerbosity());
            
            _consoleRendererMock.Setup(x => x.GetFormatter<PCTrackingInfo>())
                .Returns(pcFormatter.Object);
                
            // Get the registered action for Alt+P
            Action cycleAction = null;
            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.P,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => cycleAction = action);
                
            // Initialize orchestrator
            _orchestrator = CreateOrchestrator();
            
            // Register shortcuts manually
            typeof(ApplicationOrchestrator)
                .GetMethod("RegisterKeyboardShortcuts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_orchestrator, null);
            
            // Act
            cycleAction.Should().NotBeNull("Cycle action should be registered");
            cycleAction();
            
            // Assert
            pcFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }
        
        [Fact]
        public void RegisterKeyboardShortcuts_RegistersCycleVerbosityForPhoneClient()
        {
            // Arrange
            var phoneFormatter = new Mock<IFormatter>();
            phoneFormatter.Setup(x => x.CycleVerbosity());
            
            _consoleRendererMock.Setup(x => x.GetFormatter<PhoneTrackingInfo>())
                .Returns(phoneFormatter.Object);
                
            // Get the registered action for Alt+O
            Action cycleAction = null;
            _keyboardInputHandlerMock
                .Setup(x => x.RegisterShortcut(
                    ConsoleKey.O,
                    ConsoleModifiers.Alt,
                    It.IsAny<Action>(),
                    It.IsAny<string>()))
                .Callback<ConsoleKey, ConsoleModifiers, Action, string>((_, __, action, ___) => cycleAction = action);
                
            // Initialize orchestrator
            _orchestrator = CreateOrchestrator();
            
            // Register shortcuts manually
            typeof(ApplicationOrchestrator)
                .GetMethod("RegisterKeyboardShortcuts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_orchestrator, null);
            
            // Act
            cycleAction.Should().NotBeNull("Cycle action should be registered");
            cycleAction();
            
            // Assert
            phoneFormatter.Verify(x => x.CycleVerbosity(), Times.Once);
        }
        
        // Test for RequestIntervalSeconds configuration
        
        [Fact]
        public async Task RunAsync_RespectsRequestIntervalSeconds()
        {
            // Arrange
            SetupBasicMocks();
            var requestTimes = new List<DateTime>();
            
            // Set up a short request interval for testing
            _phoneConfig.RequestIntervalSeconds = 0.2; // 200ms
            
            // Track request times
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .Callback(() => requestTimes.Add(DateTime.UtcNow))
                .Returns(Task.CompletedTask);
                
            await _orchestrator.InitializeAsync(_tempConfigPath, _longTimeoutCts.Token);
            
            // Act - Run with default timeout
            await RunWithDefaultTimeout(async () => 
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            });
            
            // Assert
            Assert.True(requestTimes.Count >= 2, $"Should have at least 2 requests, but got {requestTimes.Count}");
            
            // Verify we're respecting the request interval timing in general
            if (requestTimes.Count >= 2)
            {
                // Get a more accurate measure by averaging multiple intervals
                var totalInterval = (requestTimes[requestTimes.Count - 1] - requestTimes[0]).TotalSeconds;
                var avgInterval = totalInterval / (requestTimes.Count - 1);
                
                // Allow for some flexibility in timing - the interval should be approximately the configured value
                // but may vary due to thread scheduling, task delays, etc.
                var minExpectedInterval = _phoneConfig.RequestIntervalSeconds * 0.5; // Allow 50% margin
                Assert.True(avgInterval >= minExpectedInterval, 
                    $"Average interval {avgInterval:F4}s should be close to configured interval {_phoneConfig.RequestIntervalSeconds}s");
            }
        }
        
        // Test for validation of transformed parameters
        
        [Fact]
        public async Task OnTrackingDataReceived_PreservesParameterBoundaries()
        {
            // Arrange
            var trackingData = new PhoneTrackingInfo { FaceFound = true, BlendShapes = new List<BlendShape>() };
            var transformedParams = new PCTrackingInfo 
            { 
                FaceFound = true,
                Parameters = new List<TrackingParam> 
                { 
                    new TrackingParam { 
                        Id = "Test", 
                        Value = 0.5,
                        Weight = 1.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter(
                        "Test",
                        -0.75,
                        1.25,
                        0.33
                    )
                }
            };
            
            // Setup basic orchestrator requirements
            SetupOrchestratorTest();
            
            // Configure the transformation engine to return test parameters
            _transformationEngineMock.Setup(x => x.TransformData(trackingData))
                .Returns(transformedParams);
            
            // Track parameters sent to VTube Studio
            PCTrackingInfo sentInfo = null;
            _vtubeStudioPCClientMock.Setup(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()))
                .Callback<PCTrackingInfo, CancellationToken>((info, _) => sentInfo = info)
                .Returns(Task.CompletedTask);
            
            // Act - Run the test with a 2 second timeout
            bool eventWasRaised = await RunWithTimeoutAndEventTrigger(
                trackingData, 
                _longTimeout);
            
            // Assert
            eventWasRaised.Should().BeTrue("The event should have been raised by the test");
            _transformationEngineMock.Verify(x => x.TransformData(trackingData), Times.Once);
            _vtubeStudioPCClientMock.Verify(x => 
                x.SendTrackingAsync(It.IsAny<PCTrackingInfo>(), It.IsAny<CancellationToken>()),
                Times.Once);
                
            Assert.NotNull(sentInfo);
            Assert.True(sentInfo.FaceFound);
            Assert.NotNull(sentInfo.Parameters);
            
            var param = sentInfo.Parameters.First();
            Assert.Equal("Test", param.Id);
            Assert.Equal(0.5, param.Value);
            Assert.Equal(-0.75, sentInfo.ParameterDefinitions["Test"].Min);
            Assert.Equal(1.25, sentInfo.ParameterDefinitions["Test"].Max);
            Assert.Equal(0.33, sentInfo.ParameterDefinitions["Test"].DefaultValue);
        }

        // Test for specific OperationCanceledException handling
        
        [Fact]
        public async Task RunAsync_WhenOperationCanceledExceptionIsThrown_HandlesGracefully()
        {
            // Arrange
            SetupBasicMocks();
            
            _vtubeStudioPCClientMock.Setup(x => x.DiscoverPortAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(8001);
                
            _vtubeStudioPCClientMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            _vtubeStudioPCClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            await _orchestrator.InitializeAsync(_tempConfigPath, _defaultCts.Token);
            
            // Act & Assert - should not throw exception
            await RunWithDefaultTimeout(async () => 
            {
                await _orchestrator.RunAsync(_defaultCts.Token);
            });
            
            // Verify cleanup was performed
            _vtubeStudioPCClientMock.Verify(x => 
                x.CloseAsync(
                    It.IsAny<WebSocketCloseStatus>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
                
            // Verify the proper log message was written
            _loggerMock.Verify(x => x.Info("Application stopped"), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenOperationCanceledExceptionIsThrownDirectly_LogsGracefulShutdown()
        {
            // Arrange
            SetupBasicMocks();
            
            // Setup phone client to throw OperationCanceledException specifically
            _vtubeStudioPhoneClientMock.Setup(x => x.SendTrackingRequestAsync())
                .Throws(new OperationCanceledException("Test cancellation"));
            
            await _orchestrator.InitializeAsync(_tempConfigPath, CancellationToken.None);
                
            // Act
            await _orchestrator.RunAsync(CancellationToken.None);
            
            // Assert - verify the specific message for OperationCanceledException
            _loggerMock.Verify(x => x.Info("Operation was canceled, shutting down gracefully..."), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenServicesAreUnhealthy_AttemptsRecovery()
        {
            // Arrange
            SetupBasicMocks();
            
            // Setup services to be unhealthy and stay unhealthy
            var unhealthyStats = new ServiceStats(
                serviceName: "TestService",
                status: "Disconnected",
                currentEntity: new PCTrackingInfo(),
                isHealthy: false,
                lastSuccessfulOperation: DateTime.UtcNow.AddMinutes(-5),
                lastError: "Connection lost",
                counters: new Dictionary<string, long>()
            );
            
            _vtubeStudioPCClientMock.Setup(x => x.GetServiceStats())
                .Returns(unhealthyStats);
            _vtubeStudioPhoneClientMock.Setup(x => x.GetServiceStats())
                .Returns(unhealthyStats);
                
            // Setup TryInitializeAsync to always return false (recovery fails)
            _vtubeStudioPCClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _vtubeStudioPhoneClientMock.Setup(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            await _orchestrator.InitializeAsync(_tempConfigPath, _longTimeoutCts.Token);
                
            // Act - Use a longer timeout to allow for recovery attempts
            await RunWithCustomTimeout(async () => 
            {
                await _orchestrator.RunAsync(_longTimeoutCts.Token);
            }, _longTimeout);
            
            // Assert
            _vtubeStudioPCClientMock.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
            _vtubeStudioPhoneClientMock.Verify(x => x.TryInitializeAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }
    }
}