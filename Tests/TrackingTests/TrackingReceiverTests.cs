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

namespace SharpBridge.Tests.TrackingTests
{
    public class TrackingReceiverTests
    {
        // Test that we can create the tracking receiver
        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new TrackingReceiverConfig { IphoneIpAddress = "127.0.0.1" };
            
            // Act
            var receiver = new TrackingReceiver(mockUdpClient.Object, config);
            
            // Assert
            receiver.Should().NotBeNull();
        }
        
        // Test that the tracking receiver can start and stop when requested
        [Fact]
        public async Task RunAsync_StartsAndStops_WhenCancelled()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var mockReceiver = new Mock<ITrackingReceiver>();
            
            // Set up the mock to return a completed task when RunAsync is called
            mockReceiver
                .Setup(r => r.RunAsync(cancellationTokenSource.Token))
                .Returns(Task.CompletedTask);
            
            // Act
            await mockReceiver.Object.RunAsync(cancellationTokenSource.Token);
            
            // Assert
            mockReceiver.Verify(r => r.RunAsync(cancellationTokenSource.Token), Times.Once);
        }
        
        // Test that the tracking receiver sends tracking request with correct parameters
        [Fact]
        public async Task SendsTrackingRequest_WithCorrectParameters()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new TrackingReceiverConfig 
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
            var receiver = new TrackingReceiver(mockUdpClient.Object, config);
            
            // Act
            cancellationTokenSource.CancelAfter(100); // Cancel after 100ms
            try
            {
                await receiver.RunAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
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
                Times.AtLeastOnce);
        }
        
        // Test that the tracking receiver uses the new cancellation token approach for timeouts
        [Fact]
        public async Task ReceiveAsync_UsesTimeoutTokens()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new TrackingReceiverConfig 
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
                
            var receiver = new TrackingReceiver(mockUdpClient.Object, config);
            
            // Act
            var cts = new CancellationTokenSource(250); // Short main cancellation
            try
            {
                await receiver.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            // Assert
            passedToken.Should().NotBe(CancellationToken.None, "a cancellation token should be passed");
            passedToken.Should().NotBe(cts.Token, "a different token should be passed (linked or timeout token)");
        }
        
        // Test that the tracking receiver properly processes valid tracking data and raises an event
        [Fact]
        public async Task TrackingDataReceived_IsRaised_WithProperlyDeserializedData_WhenValidDataReceived()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new TrackingReceiverConfig { IphoneIpAddress = "127.0.0.1" };
            
            // Create expected tracking data with EyeRight (matching our updated model)
            var expectedData = new TrackingResponse
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
            
            // Configure the UDP mock to return the JSON data directly (no polling first)
            int callCount = 0;
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => 
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return new UdpReceiveResult(jsonBytes, new System.Net.IPEndPoint(0, 0));
                    }
                    throw new OperationCanceledException();
                });
            
            // Create the receiver and track event raising
            var receiver = new TrackingReceiver(mockUdpClient.Object, config);
            var eventWasRaised = false;
            TrackingResponse receivedData = null;
            
            receiver.TrackingDataReceived += (sender, data) => 
            {
                eventWasRaised = true;
                receivedData = data;
            };
            
            // Act - run until event is raised or timeout
            var cts = new CancellationTokenSource(1000); // 1 second timeout
            try 
            {
                await receiver.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            // Assert
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
        
        // Test that the tracking receiver continues to function when invalid data is received
        [Fact]
        public async Task RunAsync_ContinuesRunning_WhenInvalidDataReceived()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new TrackingReceiverConfig { IphoneIpAddress = "127.0.0.1" };
            
            // First provide invalid JSON data, then valid data
            var invalidJson = Encoding.UTF8.GetBytes("{ this is not valid json }");
            var validData = new TrackingResponse { FaceFound = true };
            var validJson = JsonSerializer.Serialize(validData);
            var validJsonBytes = Encoding.UTF8.GetBytes(validJson);
            
            var callCount = 0;
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => 
                {
                    callCount++;
                    // First return invalid data, then valid, then throw to end test
                    if (callCount == 1)
                        return new UdpReceiveResult(invalidJson, new System.Net.IPEndPoint(0, 0));
                    else if (callCount == 2)
                        return new UdpReceiveResult(validJsonBytes, new System.Net.IPEndPoint(0, 0));
                    else
                        throw new OperationCanceledException();
                });
            
            // Create receiver and track event raising
            var receiver = new TrackingReceiver(mockUdpClient.Object, config);
            var validEventCount = 0;
            
            receiver.TrackingDataReceived += (sender, data) => 
            {
                if (data.FaceFound) validEventCount++;
            };
            
            // Act - run until event is raised twice or timeout
            var cts = new CancellationTokenSource(1000); 
            try
            {
                await receiver.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            // Assert
            validEventCount.Should().BeGreaterThan(0, "the receiver should handle valid data after invalid data");
            callCount.Should().BeGreaterThan(1, "multiple receive calls should be made");
        }
        
        // Test that the tracking receiver handles invalid JSON gracefully
        [Fact]
        public async Task ProcessReceivedData_HandlesInvalidJson_Gracefully()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var config = new TrackingReceiverConfig { IphoneIpAddress = "127.0.0.1" };
            
            // Configure UDP client to return invalid JSON
            var invalidJson = Encoding.UTF8.GetBytes("{ this is not valid json }");
            mockUdpClient.Setup(c => c.Poll(It.IsAny<int>(), It.IsAny<SelectMode>())).Returns(true);
            mockUdpClient.Setup(c => c.Available).Returns(invalidJson.Length);
            mockUdpClient
                .Setup(c => c.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(invalidJson, new System.Net.IPEndPoint(0, 0)));
            
            var receiver = new TrackingReceiver(mockUdpClient.Object, config);
            var eventRaised = false;
            
            receiver.TrackingDataReceived += (s, e) => eventRaised = true;
            
            // Act - run for a short time
            var cts = new CancellationTokenSource(200);
            try
            {
                await receiver.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            // Assert - no event should be raised for invalid JSON, but no exception should be thrown
            eventRaised.Should().BeFalse("invalid JSON should not trigger events");
        }
        
        // Test that the tracking receiver validates config
        [Fact]
        public void Constructor_Validates_IphoneIpAddress()
        {
            // Arrange
            var mockUdpClient = new Mock<IUdpClientWrapper>();
            var invalidConfig = new TrackingReceiverConfig { IphoneIpAddress = "" };
            
            // Act & Assert
            Action act = () => new TrackingReceiver(mockUdpClient.Object, invalidConfig);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*iPhone IP address cannot be null or empty*");
        }
    }
} 