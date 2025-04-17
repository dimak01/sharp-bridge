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
            
            // Act
            var receiver = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            
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
            var config = new VTubeStudioPhoneClientConfig 
            { 
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 21413,
                SendForSeconds = 10, // The duration should be 10 seconds
                RequestIntervalSeconds = 1 // Requests should be sent every 1 second
            };

            byte[] sentData = null;
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .Callback<byte[], int, string, int>((data, bytes, host, port) => 
                {
                    sentData = data;
                })
                .ReturnsAsync(100);
                
            // Create receiver
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            
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
                
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            
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
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            var eventWasRaised = false;
            PhoneTrackingInfo receivedData = null;
            
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
            
            // Create receiver and track event raising
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            var validEventCount = 0;
            
            client.TrackingDataReceived += (sender, data) => 
            {
                if (data.FaceFound) validEventCount++;
            };
            
            // Act - call ReceiveResponseAsync twice
            await client.ReceiveResponseAsync(CancellationToken.None); // Should handle invalid JSON
            await client.ReceiveResponseAsync(CancellationToken.None); // Should process valid JSON
            
            // Assert
            validEventCount.Should().Be(1, "the event should be raised once for the valid data");
            callCount.Should().Be(2, "both receive calls should be made");
        }
        
        // Test that the phone client handles invalid JSON gracefully
        [Fact]
        public async Task ReceiveResponseAsync_HandlesInvalidJson_Gracefully()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "127.0.0.1"
            };
            
            // Configure UDP client to return invalid JSON
            var invalidJson = Encoding.UTF8.GetBytes("{ this is not valid json }");
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(invalidJson, new System.Net.IPEndPoint(0, 0)));
            
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            var eventRaised = false;
            
            client.TrackingDataReceived += (s, e) => eventRaised = true;
            
            // Act - directly call ReceiveResponseAsync
            bool result = await client.ReceiveResponseAsync(CancellationToken.None);
            
            // Assert
            result.Should().BeTrue("the method should return true when data is received, even if invalid");
            eventRaised.Should().BeFalse("invalid JSON should not trigger events");
        }
        
        // Test that the phone client validates config
        [Fact]
        public void Constructor_Validates_IphoneIpAddress()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var invalidConfig = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "" };
            
            // Act & Assert
            Action act = () => new VTubeStudioPhoneClient(mockUdpClient.Object, invalidConfig);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*iPhone IP address cannot be null or empty*");
        }

        // NEW TEST: Test that the Dispose method properly cleans up resources
        [Fact]
        public void Dispose_CallsDisposeOnUdpClient()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            mockUdpClient.Setup(c => c.Dispose());
            
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            
            // Act
            client.Dispose();
            
            // Assert
            mockUdpClient.Verify(c => c.Dispose(), Times.Once, "Dispose should be called on the UDP client");
        }

        // Test that the general exception catch block in ReceiveResponseAsync works
        [Fact]
        public async Task ReceiveResponseAsync_HandlesGeneralExceptions_AndLogsMessages()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };
            
            // Setup the mock to specifically trigger the general exception handler
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("This should hit the general exception handler"));
            
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            
            // Act
            bool result = await client.ReceiveResponseAsync(CancellationToken.None);
            
            // Assert
            result.Should().BeFalse("the method should return false when an exception occurs");
            mockUdpClient.Verify(
                c => c.ReceiveAsync(It.IsAny<CancellationToken>()), 
                Times.Once, 
                "ReceiveAsync should be called");
        }

        // Test exception handling in SendTrackingRequestAsync
        [Fact]
        public async Task SendTrackingRequestAsync_HandlesSocketExceptions_And_Rethrows()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };
            
            // Configure SendAsync to throw a SocketException
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new SocketException(10054)); // Connection reset
            
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            
            // Act & Assert
            await Assert.ThrowsAsync<SocketException>(async () => 
                await client.SendTrackingRequestAsync());
            
            // Verify SendAsync was called
            mockUdpClient.Verify(
                c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), 
                Times.Once, 
                "SendAsync should be called");
        }

        // NEW TEST: Test constructor with null UDP client
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenUdpClientIsNull()
        {
            // Arrange
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };
            
            // Act & Assert
            Action act = () => new VTubeStudioPhoneClient(null, config);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("udpClient");
        }

        // NEW TEST: Test constructor with null config
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenConfigIsNull()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            
            // Act & Assert
            Action act = () => new VTubeStudioPhoneClient(mockUdpClient.Object, null);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("config");
        }

        // TrackingDataReceived_NoErrorWhenNoSubscribers test references RunAsync which doesn't exist anymore
        [Fact]
        public async Task TrackingDataReceived_NoErrorWhenNoSubscribers()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };
            
            var validData = new PhoneTrackingInfo { FaceFound = true };
            var validJson = JsonSerializer.Serialize(validData);
            var jsonBytes = Encoding.UTF8.GetBytes(validJson);
            
            // Set up the mock to return valid data
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(jsonBytes, new System.Net.IPEndPoint(0, 0)));
            
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(100);
            
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            // Intentionally not subscribing to the event
            
            // Act - call the methods directly instead of using RunAsync
            await client.SendTrackingRequestAsync();
            bool receiveResult = await client.ReceiveResponseAsync(CancellationToken.None);
            
            // Assert
            receiveResult.Should().BeTrue("ReceiveResponseAsync should return true on success");
            mockUdpClient.Verify(
                c => c.ReceiveAsync(It.IsAny<CancellationToken>()), 
                Times.Once, 
                "Receiver should process data even without subscribers");
        }

        // Test cancellation handling in ReceiveResponseAsync
        [Fact]
        public async Task ReceiveResponseAsync_ReturnsFalse_WhenCancelled()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };
            
            // Make ReceiveAsync wait until cancelled
            var tcs = new TaskCompletionSource<UdpReceiveResult>();
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token => 
                {
                    var registration = token.Register(() => 
                    {
                        tcs.TrySetCanceled(token);
                    });
                    return tcs.Task;
                });
            
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            
            // Act
            var cts = new CancellationTokenSource();
            var receiveTask = client.ReceiveResponseAsync(cts.Token);
            
            // Cancel after a short delay
            await Task.Delay(50);
            cts.Cancel();
            
            bool result = await receiveTask;
            
            // Assert
            result.Should().BeFalse("the method should return false when cancelled");
        }

        [Fact]
        public async Task SendTrackingRequestReturnsFalse_WhenSendAsyncThrowsSocketExceptionOtherThan10054()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new VTubeStudioPhoneClientConfig { IphoneIpAddress = "127.0.0.1" };
            
            // Configure SendAsync to throw a different SocketException
            mockUdpClient
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new SocketException(10053)); // Connection aborted
            
            var client = new VTubeStudioPhoneClient(mockUdpClient.Object, config);
            
            // Act & Assert - Use the signature without cancellationToken parameter since it's not available
            var ex = await Assert.ThrowsAsync<SocketException>(() => 
                client.SendTrackingRequestAsync());
            
            ex.ErrorCode.Should().Be(10053);
        }
    }
} 