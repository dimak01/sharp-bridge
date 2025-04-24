using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;

namespace SharpBridge.Tests.Services
{
    public class VTubeStudioPCClientTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly VTubeStudioPCConfig _config;
        private readonly MockWebSocketWrapper _mockWebSocket;
        
        public VTubeStudioPCClientTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockWebSocket = new MockWebSocketWrapper();
            _config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCClient(null, _config, _mockWebSocket));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCClient(_mockLogger.Object, null, _mockWebSocket));
        }

        [Fact]
        public void Constructor_WithNullWebSocket_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCClient(_mockLogger.Object, _config, null));
        }

        [Fact]
        public void GetServiceStats_ReturnsCorrectStats()
        {
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            var stats = client.GetServiceStats();

            Assert.Equal("VTubeStudioPCClient", stats.ServiceName);
            Assert.Equal("None", stats.Status);
            Assert.Null(stats.CurrentEntity);
            Assert.NotNull(stats.Counters);
        }

        [Fact]
        public void GetServiceStats_ReturnsValidStats()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            
            // Act
            var stats = client.GetServiceStats();
            
            // Assert
            Assert.NotNull(stats);
            Assert.Equal("VTubeStudioPCClient", stats.ServiceName);
            Assert.Equal(WebSocketState.None.ToString(), stats.Status);
            Assert.NotNull(stats.Counters);
            
            // Check the counters
            Assert.True(stats.Counters.ContainsKey("MessagesSent"));
            Assert.True(stats.Counters.ContainsKey("ConnectionAttempts"));
            Assert.True(stats.Counters.ContainsKey("FailedConnections"));
            Assert.True(stats.Counters.ContainsKey("UptimeSeconds"));
            
            // The entity might be null initially since no tracking data has been sent yet
            if (stats.CurrentEntity != null)
            {
                var pcTrackingInfo = stats.CurrentEntity as PCTrackingInfo; 
                Assert.Equal(false, pcTrackingInfo.FaceFound);
                Assert.NotNull(pcTrackingInfo.Parameters);
            }
        }
        
        [Fact]
        public async Task ConnectAsync_WhenNotConnected_ConnectsSuccessfully()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            
            // Act
            await client.ConnectAsync(CancellationToken.None);
            
            // Assert
            Assert.Equal(WebSocketState.Open, _mockWebSocket.State);
        }

        [Fact]
        public async Task State_TransitionsCorrectly_ThroughConnectionLifecycle()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            
            // Act - Connect
            await client.ConnectAsync(CancellationToken.None);
            
            // Assert - Should be Open
            client.State.Should().Be(WebSocketState.Open);
            
            // Act - Close
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            
            // Assert - Should be Closed
            client.State.Should().Be(WebSocketState.Closed);
        }
        
        [Fact]
        public async Task GetServiceStats_AfterConnectAndSend_UpdatesStatistics()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            
            // Act - Connect and send tracking data
            await client.ConnectAsync(CancellationToken.None);
            
            var trackingData = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new[]
                {
                    new TrackingParam { 
                        Id = "Test", 
                        Value = 0.5, 
                        Min = -0.75, 
                        Max = 1.25, 
                        DefaultValue = 0.33 
                    }
                }
            };
            
            // Queue a successful response
            _mockWebSocket.EnqueueResponse(new { success = true });
            
            await client.SendTrackingAsync(trackingData, CancellationToken.None);
            
            // Get the stats
            var stats = client.GetServiceStats();
            
            // Assert
            Assert.NotNull(stats);
            Assert.Equal("VTubeStudioPCClient", stats.ServiceName);
            Assert.Equal(WebSocketState.Open.ToString(), stats.Status);
            
            // Check the counters
            Assert.Equal(1, stats.Counters["MessagesSent"]);
            Assert.Equal(1, stats.Counters["ConnectionAttempts"]);
            Assert.Equal(0, stats.Counters["FailedConnections"]);
            Assert.True(stats.Counters["UptimeSeconds"] >= 0);
            
            // Check the PCTrackingInfo entity
            Assert.NotNull(stats.CurrentEntity);
            Assert.IsType<PCTrackingInfo>(stats.CurrentEntity);
            var pcTrackingInfo = stats.CurrentEntity as PCTrackingInfo;
            Assert.True(pcTrackingInfo.FaceFound);
            Assert.NotNull(pcTrackingInfo.Parameters);
            Assert.Single(pcTrackingInfo.Parameters);
            Assert.Equal("Test", pcTrackingInfo.Parameters.First().Id);
            Assert.Equal(0.5, pcTrackingInfo.Parameters.First().Value);
            Assert.Equal(-0.75, pcTrackingInfo.Parameters.First().Min);
            Assert.Equal(1.25, pcTrackingInfo.Parameters.First().Max);
            Assert.Equal(0.33, pcTrackingInfo.Parameters.First().DefaultValue);
            
            // Cleanup
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsTrue_AndLogsExpectedMessages()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            await client.ConnectAsync(CancellationToken.None);
            
            // Queue authentication responses
            //_mockWebSocket.EnqueueResponse(new AuthenticationTokenResponse { AuthenticationToken = "test-token" });
            _mockWebSocket.EnqueueResponse(new AuthenticationResponse { Authenticated = true, Reason = "Success" });
            
            // Act
            var result = await client.AuthenticateAsync(CancellationToken.None);
            
            // Assert
            result.Should().BeTrue();
            
            // Verify logging
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Authenticating")), It.IsAny<object[]>()), Times.Once);
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Authentication result")), It.IsAny<object[]>()), Times.Once);
        }
        
        [Fact]
        public async Task DiscoverPortAsync_ReturnsExpectedPort_AndLogsExpectedMessages()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            
            // Act
            var port = await client.DiscoverPortAsync(CancellationToken.None);
            
            // Assert
            port.Should().Be(8001);
            
            // Verify logging
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Discovering")), It.IsAny<object[]>()), Times.Once);
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Found port")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task ConnectAsync_WhenAlreadyConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            await client.ConnectAsync(CancellationToken.None);
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                client.ConnectAsync(CancellationToken.None));
        }

        [Fact]
        public async Task CloseAsync_WhenConnected_ClosesConnection()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            await client.ConnectAsync(CancellationToken.None);
            
            // Act
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            
            // Assert
            Assert.Equal(WebSocketState.Closed, _mockWebSocket.State);
        }

        [Fact]
        public async Task CloseAsync_WhenNotConnected_DoesNothing()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            
            // Act
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            
            // Assert
            Assert.Equal(WebSocketState.None, _mockWebSocket.State);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                client.AuthenticateAsync(CancellationToken.None));
        }

        [Fact]
        public async Task SendTrackingAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = new VTubeStudioPCClient(_mockLogger.Object, _config, _mockWebSocket);
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                client.SendTrackingAsync(new PCTrackingInfo(), CancellationToken.None));
        }

        [Fact]
        public async Task AuthenticateAsync_WhenNoToken_AcquiresAndSavesToken()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper",
                TokenFilePath = "test-token.txt"
            };
            
            var client = new VTubeStudioPCClient(_mockLogger.Object, config, _mockWebSocket);
            await client.ConnectAsync(CancellationToken.None);
            
            // Queue token acquisition and authentication responses
            _mockWebSocket.EnqueueResponse(new AuthenticationTokenResponse { AuthenticationToken = "new-test-token" });
            _mockWebSocket.EnqueueResponse(new AuthenticationResponse { Authenticated = true, Reason = "Success" });
            
            // Act
            var result = await client.AuthenticateAsync(CancellationToken.None);
            
            // Assert
            Assert.True(result);
            _mockLogger.Verify(x => x.Info("Authentication result: {0}", true), Times.Once);
            
            // Verify token was saved
            Assert.True(File.Exists(config.TokenFilePath));
            Assert.Equal("new-test-token", File.ReadAllText(config.TokenFilePath));
            
            // Cleanup
            File.Delete(config.TokenFilePath);
        }
        
        [Fact]
        public async Task AuthenticateAsync_WhenTokenExists_LoadsAndUsesToken()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper",
                TokenFilePath = "test-token.txt"
            };
            
            // Create token file
            File.WriteAllText(config.TokenFilePath, "existing-test-token");
            
            var client = new VTubeStudioPCClient(_mockLogger.Object, config, _mockWebSocket);
            await client.ConnectAsync(CancellationToken.None);
            
            // Queue authentication response (no token acquisition needed)
            _mockWebSocket.EnqueueResponse(new AuthenticationResponse { Authenticated = true, Reason = "Success" });
            
            // Act
            var result = await client.AuthenticateAsync(CancellationToken.None);
            
            // Assert
            Assert.True(result);
            _mockLogger.Verify(x => x.Info("Authentication result: {0}", true), Times.Once);
            
            // Cleanup
            File.Delete(config.TokenFilePath);
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPCConfig();
            var mockWebSocket = new MockWebSocketWrapper();
            var client = new VTubeStudioPCClient(mockLogger.Object, config, mockWebSocket);
            var testToken = "test_token_123";
            
            // Queue authentication response
            mockWebSocket.EnqueueResponse(new AuthenticationResponse { Authenticated = true, Reason = "Success" });
            
            // Act
            await client.ConnectAsync(CancellationToken.None);
            var result = await client.AuthenticateAsync(testToken, CancellationToken.None);
            
            // Assert
            Assert.True(result);
            mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Authentication result:")), It.Is<object[]>(args => args[0].Equals(true))), Times.Once);
        }

        [Fact]
        public async Task GetTokenAsync_WhenNoTokenExists_RequestsNewToken()
        {
            // Arrange
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPCConfig
            {
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDev",
                TokenFilePath = "test_token.txt"
            };
            var mockWebSocket = new MockWebSocketWrapper();
            var client = new VTubeStudioPCClient(mockLogger.Object, config, mockWebSocket);
            
            // Queue token response
            mockWebSocket.EnqueueResponse(new AuthenticationTokenResponse { AuthenticationToken = "new_test_token" });
            
            // Act
            await client.ConnectAsync(CancellationToken.None);
            var token = await client.GetTokenAsync(CancellationToken.None);
            
            // Assert
            Assert.NotNull(token);
            Assert.Equal("new_test_token", token);
            mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Requesting new authentication token"))), Times.Once);
            
            // Cleanup
            if (File.Exists(config.TokenFilePath))
            {
                File.Delete(config.TokenFilePath);
            }
        }
        
        [Fact]
        public async Task SaveTokenAsync_SavesTokenToFile()
        {
            // Arrange
            var mockWebSocket = new MockWebSocketWrapper();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPCConfig
            {
                TokenFilePath = "test_token.txt"
            };
            var client = new VTubeStudioPCClient(mockLogger.Object, config, mockWebSocket);
            var testToken = "test_token_123";
            
            // Act
            await client.SaveTokenAsync(testToken);
            
            // Assert
            Assert.True(File.Exists(config.TokenFilePath));
            Assert.Equal(testToken, File.ReadAllText(config.TokenFilePath));
            mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Saved authentication token")), It.IsAny<object[]>()), Times.Once);
            
            // Cleanup
            File.Delete(config.TokenFilePath);
        }
        
        [Fact]
        public async Task ClearTokenAsync_RemovesTokenFile()
        {
            // Arrange
            var mockWebSocket = new MockWebSocketWrapper();
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPCConfig
            {
                TokenFilePath = "test_token.txt"
            };
            var client = new VTubeStudioPCClient(mockLogger.Object, config, mockWebSocket);
            File.WriteAllText(config.TokenFilePath, "test_token");
            
            // Act
            await client.ClearTokenAsync();
            
            // Assert
            Assert.False(File.Exists(config.TokenFilePath));
            mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Cleared authentication token")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var mockLogger = new Mock<IAppLogger>();
            var config = new VTubeStudioPCConfig
            {
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDev",
                TokenFilePath = "test_token.txt"
            };
            
            // Create token file with invalid token
            File.WriteAllText(config.TokenFilePath, "invalid-token");
            
            var mockWebSocket = new MockWebSocketWrapper();
            var client = new VTubeStudioPCClient(mockLogger.Object, config, mockWebSocket);
            
            // Queue authentication response indicating failure
            mockWebSocket.EnqueueResponse(new AuthenticationResponse { Authenticated = false, Reason = "Invalid token" });
            
            // Act
            await client.ConnectAsync(CancellationToken.None);
            var result = await client.AuthenticateAsync("invalid-token", CancellationToken.None);
            
            // Assert
            Assert.False(result);
            mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Authentication result:")), It.Is<object[]>(args => args[0].Equals(false))), Times.Once);
            
            // Cleanup
            File.Delete(config.TokenFilePath);
        }
    }
} 