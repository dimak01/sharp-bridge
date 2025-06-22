using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using SharpBridge.Utilities;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace SharpBridge.Tests.Services
{
    public class VTubeStudioPhoneClientTests
    {
        // Test that we can create the phone client
        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new VTubeStudioPhoneClientConfig();
            var mockLogger = new Mock<IAppLogger>();

            // Act
            var receiver = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Assert
            receiver.Should().NotBeNull();
        }

        // Test that the phone client can start and stop when requested
        [Fact]
        public async Task SendAndReceive_WorksCorrectly()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var mockClient = new Mock<IVTubeStudioPhoneClient>();

            // Set up the mock to return a completed task for SendTrackingRequestAsync
            mockClient
                .Setup(r => r.SendTrackingRequestAsync())
                .Returns(Task.CompletedTask);

            // Set up the mock to return true for ReceiveResponseAsync
            mockClient
                .Setup(r => r.ReceiveResponseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await mockClient.Object.SendTrackingRequestAsync();
            var result = await mockClient.Object.ReceiveResponseAsync(cancellationTokenSource.Token);

            // Assert
            mockClient.Verify(r => r.SendTrackingRequestAsync(), Times.Once);
            mockClient.Verify(r => r.ReceiveResponseAsync(cancellationTokenSource.Token), Times.Once);
            result.Should().BeTrue("ReceiveResponseAsync should return true when data is received");
        }

        // Test that the phone client sends tracking request with correct parameters
        [Fact]
        public async Task SendsTrackingRequest_WithCorrectParameters()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 21413,
                SendForSeconds = 10, // The duration should be 10 seconds
                RequestIntervalSeconds = 1 // Requests should be sent every 1 second
            };

            byte[] sentData = null!;
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .Callback<byte[], int, string, int>((data, bytes, host, port) =>
                {
                    sentData = data;
                })
                .ReturnsAsync(100);

            // Create receiver
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act - directly call SendTrackingRequestAsync
            await client.SendTrackingRequestAsync();

            // Assert
            sentData.Should().NotBeNull("a tracking request should be sent");

            // Decode and verify the request JSON
            var requestJson = Encoding.UTF8.GetString(sentData);
            var requestObj = JsonSerializer.Deserialize<JsonElement>(requestJson);

            // Verify the request parameters
            requestObj.GetProperty("messageType").GetString().Should().Be("iOSTrackingDataRequest");
            requestObj.GetProperty("sentBy").GetString().Should().Be("SharpBridge");
            requestObj.GetProperty("sendForSeconds").GetInt32().Should().Be(10); // Should match SendForSeconds

            // Verify the ports array
            var ports = requestObj.GetProperty("ports").EnumerateArray().ToList();
            ports.Count.Should().Be(1);
            ports[0].GetInt32().Should().Be(21413); // Should use the LocalPort

            // Verify it was sent to the correct destination
            mockUdpClient.Verify(
                c => c.SendAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    "192.168.1.100",
                    21412),
                Times.Once);
        }

        // Test that the phone client uses the new cancellation token approach for timeouts
        [Fact]
        public async Task ReceiveResponseAsync_UsesTimeoutTokens()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1",
                ReceiveTimeoutMs = 2000 // The Rust 2-second timeout
            };

            CancellationToken passedToken = CancellationToken.None;

            // Configure the mock to capture the cancellation token and throw a timeout
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(token =>
                {
                    passedToken = token;
                    // Simulate a timeout by cancelling the token ourselves
                    if (token.CanBeCanceled)
                    {
                        var source = CancellationTokenSource.CreateLinkedTokenSource(token);
                        source.Cancel();
                    }
                })
                .ThrowsAsync(new OperationCanceledException());

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            var cts = new CancellationTokenSource();
            bool result = await client.ReceiveResponseAsync(cts.Token);

            // Assert
            passedToken.Should().NotBe(CancellationToken.None, "a cancellation token should be passed");
            passedToken.Should().NotBe(cts.Token, "a different token should be passed (linked or timeout token)");
            result.Should().BeFalse("the method should return false on timeout");
        }

        // Test that the phone client properly processes valid tracking data and raises an event
        [Fact]
        public async Task TrackingDataReceived_IsRaised_WithProperlyDeserializedData_WhenValidDataReceived()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1"
            };

            // Create expected tracking data with EyeRight (matching our updated model)
            var expectedData = new PhoneTrackingInfo
            {
                Timestamp = 12345,
                Hotkey = 0,
                FaceFound = true,
                Rotation = new Coordinates { X = 10, Y = 20, Z = 30 },
                Position = new Coordinates { X = 1, Y = 2, Z = 3 },
                EyeLeft = new Coordinates { X = 0.1, Y = 0.2, Z = 0.3 },
                EyeRight = new Coordinates { X = 0.15, Y = 0.25, Z = 0.35 },
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "JawOpen", Value = 0.5 },
                    new BlendShape { Key = "EyeBlinkLeft", Value = 0.2 }
                }
            };

            // Serialize the expected data to JSON
            var json = JsonSerializer.Serialize(expectedData);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            // Configure the UDP mock to return the JSON data
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(jsonBytes, new System.Net.IPEndPoint(0, 0)));

            // Create the receiver and track event raising
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);
            var eventWasRaised = false;
            PhoneTrackingInfo receivedData = null!;

            client.TrackingDataReceived += (sender, data) =>
            {
                eventWasRaised = true;
                receivedData = data;
            };

            // Act - directly call ReceiveResponseAsync
            var result = await client.ReceiveResponseAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue("the method should return true when data is received");
            eventWasRaised.Should().BeTrue("the event should be raised when data is received");
            receivedData.Should().NotBeNull("the event should include the tracking data");
            receivedData.FaceFound.Should().BeTrue("properties should match the expected data");
            receivedData.Timestamp.Should().Be(12345, "properties should match the expected data");
            receivedData.Rotation.Y.Should().Be(20, "properties should match the expected data");
            receivedData.EyeRight.Should().NotBeNull("EyeRight should be processed");
            receivedData.EyeRight.X.Should().Be(0.15, "EyeRight coordinates should be correct");
            receivedData.BlendShapes.Count.Should().Be(2, "all blend shapes should be included");
            receivedData.BlendShapes[0].Key.Should().Be("JawOpen", "blend shape keys should be correct");
            receivedData.BlendShapes[0].Value.Should().Be(0.5, "blend shape values should be correct");
        }

        // Test that the phone client continues to function when invalid data is received
        [Fact]
        public async Task ReceiveResponseAsync_ContinuesWorking_AfterInvalidDataReceived()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1"
            };

            // Prepare invalid and valid data
            var invalidJson = Encoding.UTF8.GetBytes("{ this is not valid json }");
            var validData = new PhoneTrackingInfo { FaceFound = true };
            var validJson = JsonSerializer.Serialize(validData);
            var validJsonBytes = Encoding.UTF8.GetBytes(validJson);

            // Configure mock to first return invalid data, then valid data
            int callCount = 0;
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return callCount == 1
                        ? new UdpReceiveResult(invalidJson, new System.Net.IPEndPoint(0, 0))
                        : new UdpReceiveResult(validJsonBytes, new System.Net.IPEndPoint(0, 0));
                });

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Set up tracking
            var eventRaisedCount = 0;
            client.TrackingDataReceived += (s, e) => eventRaisedCount++;

            // Act - call first with invalid data, then with valid data
            var firstResult = await client.ReceiveResponseAsync(CancellationToken.None);
            var secondResult = await client.ReceiveResponseAsync(CancellationToken.None);

            // Assert
            firstResult.Should().BeTrue("the method should return true when data is received but not processed");
            secondResult.Should().BeTrue("the method should continue working after invalid data");
            eventRaisedCount.Should().Be(1, "only the valid tracking data should raise an event");
        }

        [Fact]
        public async Task ReceiveResponseAsync_HandlesInvalidJson_Gracefully()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1"
            };

            // Invalid JSON data
            var invalidJson = Encoding.UTF8.GetBytes("{ this is not valid json }");

            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(invalidJson, new System.Net.IPEndPoint(0, 0)));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            bool eventRaised = false;
            client.TrackingDataReceived += (s, e) => eventRaised = true;

            // Act
            var result = await client.ReceiveResponseAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue("the method should return true when data is received, even if invalid");
            eventRaised.Should().BeFalse("invalid data should not raise the event");

            // Verify that error was logged
            mockLogger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void Constructor_Validates_IphoneIpAddress()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object))
                .ParamName.Should().Be("config");
        }

        [Fact]
        public void Dispose_CallsDisposeOnUdpClient()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            mockUdpClient.Setup(c => c.Dispose());

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            client.Dispose();

            // Assert
            mockUdpClient.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ReceiveResponseAsync_HandlesGeneralExceptions_AndLogsMessages()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            var result = await client.ReceiveResponseAsync(CancellationToken.None);

            // Assert
            result.Should().BeFalse("the method should return false when an exception occurs");

            // Verify that the error was logged
            mockLogger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task SendTrackingRequestAsync_HandlesSocketExceptions_And_Rethrows()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new SocketException(10054));  // Connection reset by peer

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<SocketException>(() => client.SendTrackingRequestAsync());

            // Verify that the error was logged
            mockLogger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenUdpClientIsNull()
        {
            // Arrange
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new VTubeStudioPhoneClient(null!, config, mockLogger.Object))
                .ParamName.Should().Be("udpClient");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenConfigIsNull()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new VTubeStudioPhoneClient(mockUdpClient.Object, null!, mockLogger.Object))
                .ParamName.Should().Be("config");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new VTubeStudioPhoneClient(mockUdpClient.Object, config, null!))
                .ParamName.Should().Be("logger");
        }

        [Fact]
        public async Task TrackingDataReceived_NoErrorWhenNoSubscribers()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            var validData = new PhoneTrackingInfo { FaceFound = true };
            var validJson = JsonSerializer.Serialize(validData);
            var validJsonBytes = Encoding.UTF8.GetBytes(validJson);

            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(validJsonBytes, new System.Net.IPEndPoint(0, 0)));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act - No subscribers to TrackingDataReceived event
            var result = await client.ReceiveResponseAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue("the method should return true when data is received");
            // No exception should be thrown
        }

        [Fact]
        public async Task ReceiveResponseAsync_ReturnsFalse_WhenCancelled()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Configure mock to throw OperationCanceledException when the token is cancelled
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(token =>
                {
                    if (token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(token);
                    }
                })
                .ThrowsAsync(new OperationCanceledException());

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Create a cancelled token
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var result = await client.ReceiveResponseAsync(cts.Token);

            // Assert
            result.Should().BeFalse("the method should return false when cancelled");
        }

        [Fact]
        public async Task SendTrackingRequestReturnsFalse_WhenSendAsyncThrowsSocketExceptionOtherThan10054()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Setup to throw a SocketException with error code 10053 (Software caused connection abort)
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new SocketException(10053));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act & Assert
            // Should rethrow the exception
            await Assert.ThrowsAsync<SocketException>(() => client.SendTrackingRequestAsync());
        }

        [Fact]
        public void GetServiceStats_ReturnsCorrectStats_WhenNoTrackingData()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            var stats = client.GetServiceStats();

            // Assert
            stats.Should().NotBeNull();
            stats.ServiceName.Should().Be("Phone Client");
            stats.Status.Should().Be("Initializing");
            stats.CurrentEntity.Should().BeNull();
            stats.Counters.Should().ContainKey("Total Frames").WhoseValue.Should().Be(0);
            stats.Counters.Should().ContainKey("Failed Frames").WhoseValue.Should().Be(0);
            stats.Counters.Should().ContainKey("Uptime (seconds)").WhoseValue.Should().BeGreaterOrEqualTo(0);
            stats.Counters.Should().NotContainKey("FPS");
        }

        [Fact]
        public async Task GetServiceStats_ReturnsCorrectStats_WithTrackingData()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Setup tracking data
            var trackingData = new PhoneTrackingInfo { FaceFound = true };
            var json = JsonSerializer.Serialize(trackingData);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(jsonBytes, new System.Net.IPEndPoint(0, 0)));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Receive some data to populate stats
            await client.ReceiveResponseAsync(CancellationToken.None);

            // Act
            var stats = client.GetServiceStats();

            // Assert
            stats.Should().NotBeNull();
            stats.ServiceName.Should().Be("Phone Client");
            stats.Status.Should().Be("ReceivingData");
            stats.CurrentEntity.Should().NotBeNull();
            (stats.CurrentEntity as PhoneTrackingInfo)!.FaceFound.Should().BeTrue();
            stats.Counters.Should().ContainKey("Total Frames").WhoseValue.Should().Be(1);
            stats.Counters.Should().ContainKey("Failed Frames").WhoseValue.Should().Be(0);
            stats.Counters.Should().ContainKey("Uptime (seconds)").WhoseValue.Should().BeGreaterOrEqualTo(0);
            stats.Counters.Should().ContainKey("FPS").WhoseValue.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task GetServiceStats_ReturnsCorrectStatus_AfterError()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Setup to throw an error
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test error"));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act - trigger an error
            await client.ReceiveResponseAsync(CancellationToken.None);

            // Assert
            var stats = client.GetServiceStats();
            stats.Status.Should().Be("ReceiveError");
            stats.Counters.Should().ContainKey("Failed Frames").WhoseValue.Should().Be(1);
        }

        [Fact]
        public async Task GetServiceStats_ReturnsCorrectStatus_AfterSendingRequest()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(100);

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act - send a request
            await client.SendTrackingRequestAsync();

            // Assert
            var stats = client.GetServiceStats();
            stats.Status.Should().Be("SendingRequests");
        }

        // ===== NEW TESTS TO CLOSE COVERAGE GAPS =====

        [Fact]
        public async Task TryInitializeAsync_ReturnsTrue_WhenSuccessful()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Setup successful send
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(100);

            // Setup successful receive with valid tracking data
            var trackingData = new PhoneTrackingInfo { FaceFound = true };
            var json = JsonSerializer.Serialize(trackingData);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(jsonBytes, new System.Net.IPEndPoint(0, 0)));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue("initialization should succeed when iPhone responds");

            var stats = client.GetServiceStats();
            stats.Status.Should().Be("Connected");
            client.LastInitializationError.Should().BeEmpty("no error should be set on success");
        }

        [Fact]
        public async Task TryInitializeAsync_ReturnsFalse_WhenNoResponseReceived()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Setup successful send
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(100);

            // Setup receive to timeout (return false)
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            result.Should().BeFalse("initialization should fail when no response is received");

            var stats = client.GetServiceStats();
            stats.Status.Should().Be("InitializationFailed");
            client.LastInitializationError.Should().Be("Failed to receive initial response from iPhone");
        }

        [Fact]
        public async Task TryInitializeAsync_ReturnsFalse_WhenExceptionOccurs()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Setup send to throw an exception
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Network error"));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            result.Should().BeFalse("initialization should fail when an exception occurs");

            var stats = client.GetServiceStats();
            stats.Status.Should().Be("InitializationFailed");
            client.LastInitializationError.Should().Be("Network error");

            // Verify error was logged (twice: once in SendTrackingRequestAsync, once in TryInitializeAsync)
            mockLogger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<object[]>()), Times.Exactly(2));
        }

        [Fact]
        public void LastInitializationError_ReturnsEmpty_WhenNoErrorOccurred()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act & Assert
            client.LastInitializationError.Should().BeEmpty("no error should be present initially");
        }

        [Fact]
        public async Task LastInitializationError_ReturnsError_AfterFailedInitialization()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            // Setup to fail during send
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Test initialization error"));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            client.LastInitializationError.Should().Be("Test initialization error");
        }

        [Fact]
        public void GetServiceStats_DoesNotIncludeFPS_WhenNoFramesReceived()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            // Act
            var stats = client.GetServiceStats();

            // Assert
            stats.Counters.Should().ContainKey("Total Frames").WhoseValue.Should().Be(0);
            stats.Counters.Should().NotContainKey("FPS", "FPS should not be included when no frames received");
            stats.CurrentEntity.Should().BeNull("current entity should be null when no data received");
        }

        [Fact]
        public async Task TrackingDataReceived_InvokesEvent_WhenSubscribersExist()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };

            var trackingData = new PhoneTrackingInfo { FaceFound = true };
            var json = JsonSerializer.Serialize(trackingData);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(jsonBytes, new System.Net.IPEndPoint(0, 0)));

            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config, mockLogger.Object);

            bool eventInvoked = false;
            PhoneTrackingInfo receivedData = null!;

            client.TrackingDataReceived += (sender, data) =>
            {
                eventInvoked = true;
                receivedData = data;
            };

            // Act
            await client.ReceiveResponseAsync(CancellationToken.None);

            // Assert
            eventInvoked.Should().BeTrue("event should be invoked when subscribers exist");
            receivedData.Should().NotBeNull();
            receivedData.FaceFound.Should().BeTrue();
        }
    }
}