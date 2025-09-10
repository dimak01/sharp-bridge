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
// Removed unused directive

namespace SharpBridge.Tests.Core.Clients
{
    public class VTubeStudioPCClientTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private VTubeStudioPCConfig _config;
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly Mock<IFileChangeWatcher> _mockAppConfigWatcher;
        private readonly Mock<IWebSocketWrapper> _mockWebSocket;
        private readonly Mock<IPortDiscoveryService> _mockPortDiscoveryService;
        private readonly Mock<IVTSParameterAdapter> _mockParameterAdapter;

        public VTubeStudioPCClientTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockConfigManager = new Mock<IConfigManager>();
            _mockAppConfigWatcher = new Mock<IFileChangeWatcher>();
            _mockWebSocket = new Mock<IWebSocketWrapper>();
            _mockPortDiscoveryService = new Mock<IPortDiscoveryService>();
            _mockParameterAdapter = new Mock<IVTSParameterAdapter>();

            // Setup default WebSocket behavior
            _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.None);
            _mockWebSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.Open))
                .Returns(Task.CompletedTask);
            _mockWebSocket.Setup(x => x.CloseAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() => _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.Closed))
                .Returns(Task.CompletedTask);
            _mockWebSocket.Setup(x => x.Dispose())
                .Callback(() => _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.Closed));

            // Setup default parameter adapter behavior
            _mockParameterAdapter.Setup(x => x.AdaptParameters(It.IsAny<IEnumerable<VTSParameter>>()))
                .Returns<IEnumerable<VTSParameter>>(parameters => parameters);
            _mockParameterAdapter.Setup(x => x.AdaptTrackingParameters(It.IsAny<IEnumerable<TrackingParam>>()))
                .Returns<IEnumerable<TrackingParam>>(parameters => parameters);
        }

        private VTubeStudioPCClient CreateClient(IAppLogger? logger = null, VTubeStudioPCConfig? config = null, IWebSocketWrapper? webSocket = null, IPortDiscoveryService? portDiscoveryService = null, IVTSParameterAdapter? parameterAdapter = null, bool setupMock = true)
        {
            _config = config ??= new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };
            logger ??= _mockLogger.Object;
            webSocket ??= _mockWebSocket.Object;
            portDiscoveryService ??= _mockPortDiscoveryService.Object;
            parameterAdapter ??= _mockParameterAdapter.Object;

            // Setup for constructor call (initial load) - only if not already set up
            if (setupMock)
            {
                _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>())
                    .ReturnsAsync(_config);
            }

            return new VTubeStudioPCClient(logger, _mockConfigManager.Object, webSocket, portDiscoveryService, parameterAdapter, _mockAppConfigWatcher.Object);
        }



        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCClient(null!, _mockConfigManager.Object, _mockWebSocket.Object, _mockPortDiscoveryService.Object, _mockParameterAdapter.Object, _mockAppConfigWatcher.Object));
        }

        [Fact]
        public void Constructor_WithNullConfigManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCClient(_mockLogger.Object, null!, _mockWebSocket.Object, _mockPortDiscoveryService.Object, _mockParameterAdapter.Object, _mockAppConfigWatcher.Object));
        }

        [Fact]
        public void Constructor_WithNullWebSocket_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCClient(_mockLogger.Object, _mockConfigManager.Object, null!, _mockPortDiscoveryService.Object, _mockParameterAdapter.Object, _mockAppConfigWatcher.Object));
        }

        [Fact]
        public void Constructor_WithNullPortDiscoveryService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCClient(_mockLogger.Object, _mockConfigManager.Object, _mockWebSocket.Object, null!, _mockParameterAdapter.Object, _mockAppConfigWatcher.Object));
        }

        [Fact]
        public void Constructor_WithNullParameterAdapter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCClient(_mockLogger.Object, _mockConfigManager.Object, _mockWebSocket.Object, _mockPortDiscoveryService.Object, null!, _mockAppConfigWatcher.Object));
        }

        [Fact]
        public void GetServiceStats_ReturnsCorrectStats()
        {
            var client = CreateClient();
            var stats = client.GetServiceStats();

            Assert.Equal("VTubeStudioPCClient", stats.ServiceName);
            Assert.Equal("Initializing", stats.Status);
            Assert.NotNull(stats.CurrentEntity);
            Assert.NotNull(stats.Counters);
        }

        [Fact]
        public void GetServiceStats_ReturnsValidStats()
        {
            // Arrange
            var client = CreateClient();

            // Act
            var stats = client.GetServiceStats();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal("VTubeStudioPCClient", stats.ServiceName);
            Assert.Equal("Initializing", stats.Status);
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
                Assert.NotNull(pcTrackingInfo); // Ensure the cast was successful
                Assert.Equal(false, pcTrackingInfo.FaceFound);
                Assert.NotNull(pcTrackingInfo.Parameters);
            }
        }

        [Fact]
        public async Task ConnectAsync_WhenNotConnected_ConnectsSuccessfully()
        {
            // Arrange
            var client = CreateClient();

            // Act
            await client.ConnectAsync(CancellationToken.None);

            // Assert
            Assert.Equal(WebSocketState.Open, _mockWebSocket.Object.State);
        }

        [Fact]
        public async Task State_TransitionsCorrectly_ThroughConnectionLifecycle()
        {
            // Arrange
            var client = CreateClient();

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
            var client = CreateClient();

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
                        Weight = 1.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["Test"] = new VTSParameter("Test", -0.75, 1.25, 0.33)
                }
            };

            // Queue a successful response
            _mockWebSocket.Setup(x => x.SendRequestAsync<InjectParamsRequest, object>(
                It.IsAny<string>(), It.IsAny<InjectParamsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new { success = true });

            await client.SendTrackingAsync(trackingData, CancellationToken.None);

            // Get the stats
            var stats = client.GetServiceStats();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal("VTubeStudioPCClient", stats.ServiceName);
            Assert.Equal("Connected", stats.Status);

            // Check the counters
            Assert.Equal(1, stats.Counters["MessagesSent"]);
            Assert.Equal(1, stats.Counters["ConnectionAttempts"]);
            Assert.Equal(0, stats.Counters["FailedConnections"]);
            Assert.True(stats.Counters["UptimeSeconds"] >= 0);

            // Check the PCTrackingInfo entity
            Assert.NotNull(stats.CurrentEntity);
            Assert.IsType<PCTrackingInfo>(stats.CurrentEntity);
            var pcTrackingInfo = stats.CurrentEntity as PCTrackingInfo;
            Assert.True(pcTrackingInfo!.FaceFound);
            Assert.NotNull(pcTrackingInfo.Parameters);
            Assert.Single(pcTrackingInfo.Parameters);

            var param = pcTrackingInfo.Parameters.First();
            Assert.Equal("Test", param.Id);
            Assert.Equal(0.5, param.Value);
            Assert.Equal(1.0, param.Weight);

            var paramDef = pcTrackingInfo.ParameterDefinitions["Test"];
            Assert.Equal(-0.75, paramDef.Min);
            Assert.Equal(1.25, paramDef.Max);
            Assert.Equal(0.33, paramDef.DefaultValue);

            // Cleanup
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsTrue_AndLogsExpectedMessages()
        {
            // Arrange
            var client = CreateClient();
            await client.ConnectAsync(CancellationToken.None);

            // Queue token acquisition and authentication responses
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "test-token" });
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true, Reason = "Success" });

            // Act
            var result = await client.AuthenticateAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            // Verify logging
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Authenticating")), It.IsAny<object[]>()), Times.Once);
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Authentication result")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task DiscoverPortAsync_WhenPortDiscoveryEnabled_ReturnsDiscoveredPort()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = true,
                ConnectionTimeoutMs = 1000
            };

            _mockPortDiscoveryService.Setup(x => x.DiscoverAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DiscoveryResponse { Port = 8001 });

            var client = CreateClient(config: config);

            // Act
            var port = await client.DiscoverPortAsync(CancellationToken.None);

            // Assert
            port.Should().Be(8001);

            // Verify logging
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Discovering VTube Studio port"))), Times.Once);
        }

        [Fact]
        public async Task DiscoverPortAsync_WhenPortDiscoveryDisabled_ReturnsConfiguredPort()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = false,
                Port = 1234
            };

            var client = CreateClient(config: config);

            // Act
            var port = await client.DiscoverPortAsync(CancellationToken.None);

            // Assert
            port.Should().Be(1234);

            // Verify logging
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Discovering"))), Times.Never);
        }

        [Fact]
        public async Task ConnectAsync_WhenAlreadyConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = CreateClient();
            await client.ConnectAsync(CancellationToken.None);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.ConnectAsync(CancellationToken.None));
        }

        [Fact]
        public async Task CloseAsync_WhenConnected_ClosesConnection()
        {
            // Arrange
            var client = CreateClient();
            await client.ConnectAsync(CancellationToken.None);

            // Act
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);

            // Assert
            Assert.Equal(WebSocketState.Closed, _mockWebSocket.Object.State);
        }

        [Fact]
        public async Task CloseAsync_WhenNotConnected_DoesNothing()
        {
            // Arrange
            var client = CreateClient();

            // Act
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);

            // Assert
            Assert.Equal(WebSocketState.None, _mockWebSocket.Object.State);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.AuthenticateAsync(CancellationToken.None));
        }

        [Fact]
        public async Task SendTrackingAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = CreateClient();

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

            var client = CreateClient(config: config);
            await client.ConnectAsync(CancellationToken.None);

            // Queue token acquisition and authentication responses
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "new-test-token" });
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true, Reason = "Success" });

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

            var client = CreateClient(config: config);
            await client.ConnectAsync(CancellationToken.None);

            // Queue authentication response (no token acquisition needed)
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true, Reason = "Success" });

            // Act
            client.LoadAuthToken(); // Load the token before authentication
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
            var config = new VTubeStudioPCConfig();
            var client = CreateClient(config: config);
            var testToken = "test_token_123";

            // Queue authentication response
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true, Reason = "Success" });

            // Act
            await client.ConnectAsync(CancellationToken.None);
            var result = await client.AuthenticateAsync(testToken, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Authentication result:")), It.Is<object[]>(args => args[0].Equals(true))), Times.Once);
        }

        [Fact]
        public async Task GetTokenAsync_WhenNoTokenExists_RequestsNewToken()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDev",
                TokenFilePath = "test_token.txt"
            };
            var client = CreateClient(config: config);

            // Queue token response
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "new_test_token" });

            // Act
            await client.ConnectAsync(CancellationToken.None);
            var token = await client.GetTokenAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(token);
            Assert.Equal("new_test_token", token);
            _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Requesting new authentication token"))), Times.Once);

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
            var config = new VTubeStudioPCConfig
            {
                TokenFilePath = "test_token.txt"
            };
            var client = CreateClient(config: config);
            var testToken = "test_token_123";

            // Act
            await client.SaveTokenAsync(testToken);

            // Assert
            Assert.True(File.Exists(config.TokenFilePath));
            Assert.Equal(testToken, File.ReadAllText(config.TokenFilePath));
            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Saved authentication token")), It.IsAny<object[]>()), Times.Once);

            // Cleanup
            File.Delete(config.TokenFilePath);
        }

        [Fact]
        public async Task ClearTokenAsync_RemovesTokenFile()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                TokenFilePath = "test_token.txt"
            };
            var client = CreateClient(config: config);
            File.WriteAllText(config.TokenFilePath, "test_token");

            // Act
            await client.ClearTokenAsync();

            // Assert
            Assert.False(File.Exists(config.TokenFilePath));
            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Cleared authentication token")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDev",
                TokenFilePath = "test_token.txt"
            };

            // Create token file with invalid token
            await File.WriteAllTextAsync(config.TokenFilePath, "invalid-token");

            var client = CreateClient(config: config);

            // Queue authentication response indicating failure
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = false, Reason = "Invalid token" });

            // Act
            await client.ConnectAsync(CancellationToken.None);
            var result = await client.AuthenticateAsync("invalid-token", CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Authentication result:")), It.Is<object[]>(args => args[0].Equals(false))), Times.Once);

            // Cleanup
            File.Delete(config.TokenFilePath);
        }

        [Fact]
        public void Properties_ReturnExpectedValues()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };
            var mockLogger = new Mock<IAppLogger>();
            var client = CreateClient(mockLogger.Object, config);

            // Act & Assert
            Assert.Equal(config, client.Config);
            Assert.Equal(string.Empty, client.Token); // Token should be empty string initially
        }

        [Fact]
        public async Task ConnectAsync_WhenConnectionFails_ThrowsExceptionAndLogsError()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };
            var client = CreateClient(config: config);

            // Setup WebSocket to throw on connect
            _mockWebSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => client.ConnectAsync(CancellationToken.None));
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Failed to connect")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Dispose_WhenCalled_ClosesWebSocket()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };
            var client = CreateClient(config: config);

            // Act
            client.Dispose();

            // Assert
            Assert.Equal(WebSocketState.Closed, _mockWebSocket.Object.State);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenWebSocketNotOpen_ThrowsInvalidOperationException()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };
            var client = CreateClient(config: config);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.AuthenticateAsync(CancellationToken.None));
        }

        [Fact]
        public async Task DiscoverPortAsync_WhenDiscoveryFails_ReturnsConfiguredPort()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = true,
                Port = 1234,
                Host = "localhost",
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };
            var client = CreateClient(config: config);

            // Setup port discovery to return null (indicating failure)
            _mockPortDiscoveryService.Setup(x => x.DiscoverAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DiscoveryResponse)null!);

            // Act
            var port = await client.DiscoverPortAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1234, port);
        }

        [Fact]
        public async Task CloseAsync_WhenWebSocketThrowsException_LogsErrorAndThrows()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
            _mockWebSocket.Setup(ws => ws.CloseAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ThrowsAsync(new Exception("Close failed"));

            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test", CancellationToken.None));
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error closing connection")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenWebSocketThrowsException_LogsErrorAndReturnsFalse()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
            _mockWebSocket.Setup(ws => ws.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Authentication failed"));

            var client = CreateClient();

            // Act
            var result = await client.AuthenticateAsync(CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Authentication failed")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task SendTrackingAsync_WhenWebSocketThrowsException_LogsErrorAndThrows()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
            _mockWebSocket.Setup(ws => ws.SendRequestAsync<InjectParamsRequest, object>(
                It.IsAny<string>(), It.IsAny<InjectParamsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Send failed"));

            var client = CreateClient();
            var trackingData = new PCTrackingInfo();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => client.SendTrackingAsync(trackingData, CancellationToken.None));
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Failed to send tracking data")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void LoadAuthToken_WhenFileReadFails_LogsWarning()
        {
            // Arrange
            var config = new VTubeStudioPCConfig { TokenFilePath = "test-token-locked.txt" };
            var client = CreateClient(config: config);

            FileStream? fs = null;
            try
            {
                // Create and lock the file
                fs = new FileStream(config.TokenFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                // Act
                client.LoadAuthToken();
                // Assert
                _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("Failed to load authentication token")), It.IsAny<object[]>()), Times.Once);
            }
            finally
            {
                fs?.Dispose();
                if (File.Exists(config.TokenFilePath))
                    File.Delete(config.TokenFilePath);
            }
        }

        [Fact]
        public async Task SaveTokenAsync_WhenFileWriteFails_LogsWarning()
        {
            // Arrange
            var config = new VTubeStudioPCConfig { TokenFilePath = "invalid-path/token.txt" };
            var client = CreateClient(config: config);

            // Act
            await client.SaveTokenAsync("test-token");

            // Assert
            _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("Failed to save authentication token")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task ClearTokenAsync_WhenFileDeleteFails_LogsWarning()
        {
            // Deleting a read-only file only fails on Windows. Skip on other platforms
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            // Arrange
            var config = new VTubeStudioPCConfig { TokenFilePath = "test-token.txt" };
            var client = CreateClient(config: config);

            // Create a file and make it read-only to simulate deletion issues
            File.WriteAllText(config.TokenFilePath, "test-token");
            File.SetAttributes(config.TokenFilePath, FileAttributes.ReadOnly);

            try
            {
                // Act
                await client.ClearTokenAsync();

                // Assert
                _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("Failed to clear authentication token")), It.IsAny<object[]>()), Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(config.TokenFilePath))
                {
                    File.SetAttributes(config.TokenFilePath, FileAttributes.Normal);
                    File.Delete(config.TokenFilePath);
                }
            }
        }

        [Fact]
        public void Dispose_WhenWebSocketThrowsException_LogsError()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.Dispose()).Throws(new Exception("Dispose failed"));

            var client = CreateClient();

            // Act
            client.Dispose();

            // Assert
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error disposing")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task GetTokenAsync_WhenTokenFileEmpty_RequestsNewToken()
        {
            // Arrange
            var client = CreateClient();
            await client.ConnectAsync(CancellationToken.None);

            // Create empty token file
            File.WriteAllText(_config.TokenFilePath, string.Empty);

            // Queue token acquisition response
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "new-token" });

            // Act
            var result = await client.GetTokenAsync(CancellationToken.None);

            // Assert
            result.Should().Be("new-token");
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Requesting new authentication token")), It.IsAny<object[]>()), Times.Once);

            // Cleanup
            File.Delete(_config.TokenFilePath);
        }

        [Fact]
        public async Task GetTokenAsync_WhenTokenFileInvalid_RequestsNewToken()
        {
            // Arrange
            var client = CreateClient();
            await client.ConnectAsync(CancellationToken.None);

            // Create invalid token file
            File.WriteAllText(_config.TokenFilePath, "invalid-token-data");

            // Queue token acquisition response
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "new-token" });

            // Act
            var result = await client.GetTokenAsync(CancellationToken.None);

            // Assert
            result.Should().Be("new-token");
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Requesting new authentication token")), It.IsAny<object[]>()), Times.Once);

            // Cleanup
            File.Delete(_config.TokenFilePath);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenTokenFileCorrupted_RequestsNewToken()
        {
            // Arrange
            var client = CreateClient();
            await client.ConnectAsync(CancellationToken.None);

            // Create corrupted token file
            File.WriteAllText(_config.TokenFilePath, "corrupted-token-data");

            // Queue token acquisition and authentication responses
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "new-token" });
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true });

            // Act
            var result = await client.AuthenticateAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Requesting new authentication token")), It.IsAny<object[]>()), Times.Once);

            // Cleanup
            File.Delete(_config.TokenFilePath);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenTokenExpired_RequestsNewToken()
        {
            // Arrange
            var client = CreateClient();
            await client.ConnectAsync(CancellationToken.None);

            // Create token file with expired token
            File.WriteAllText(_config.TokenFilePath, "expired-token");

            // Load the expired token
            client.LoadAuthToken();

            // Queue authentication failure, then token acquisition and successful authentication
            var sequence = _mockWebSocket.SetupSequence(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()));
            sequence.ReturnsAsync(new AuthenticationResponse { Authenticated = false, Reason = "Token expired" });
            sequence.ReturnsAsync(new AuthenticationResponse { Authenticated = true });

            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "new-token" });

            // Act
            var result = await client.AuthenticateAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Requesting new authentication token")), It.IsAny<object[]>()), Times.Once);

            // Cleanup
            File.Delete(_config.TokenFilePath);
        }

        [Fact]
        public async Task GetTokenAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = CreateClient();
            // Do not connect

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetTokenAsync(CancellationToken.None));
        }

        [Fact]
        public async Task AuthenticateAsync_WithToken_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var client = CreateClient();
            // Do not connect

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.AuthenticateAsync("token", CancellationToken.None));
        }

        [Fact]
        public async Task AuthenticateAsync_WithToken_WhenWebSocketThrows_ReturnsFalseAndLogsError()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
            _mockWebSocket.Setup(ws => ws.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("WebSocket error"));
            var client = CreateClient();

            // Act
            var result = await client.AuthenticateAsync("token", CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Authentication failed")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task TryInitializeAsync_ReturnsTrue_WhenSuccessful()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = true,
                ConnectionTimeoutMs = 1000,
                TokenFilePath = Path.GetTempFileName()
            };

            var mockLogger = new Mock<IAppLogger>();
            var mockWebSocket = new Mock<IWebSocketWrapper>();
            var mockPortDiscovery = new Mock<IPortDiscoveryService>();

            // Setup port discovery to return a valid port
            mockPortDiscovery.Setup(x => x.DiscoverAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DiscoveryResponse { Port = 8001 });

            // Setup WebSocket state transitions
            mockWebSocket.Setup(x => x.State).Returns(WebSocketState.None);
            mockWebSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback(() => mockWebSocket.Setup(x => x.State).Returns(WebSocketState.Open))
                .Returns(Task.CompletedTask);

            // Queue authentication responses for successful flow
            mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "test-token" });
            mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true, Reason = "Success" });

            var client = CreateClient(mockLogger.Object, config, mockWebSocket.Object, mockPortDiscovery.Object);

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            // Verify the initialization steps were logged
            mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Attempting to initialize")), It.IsAny<object[]>()), Times.Once);
            mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("initialized successfully")), It.IsAny<object[]>()), Times.Once);

            // Cleanup
            if (File.Exists(config.TokenFilePath))
                File.Delete(config.TokenFilePath);
        }

        [Fact]
        public async Task TryInitializeAsync_ReturnsFalse_WhenPortDiscoveryFails()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = true,
                ConnectionTimeoutMs = 1000
            };

            // Setup port discovery to throw exception (simulates failure)
            _mockPortDiscoveryService.Setup(x => x.DiscoverAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Port discovery failed"));

            var client = CreateClient(config: config);

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            result.Should().BeFalse();

            // Verify error was logged (the actual error is "Failed to initialize VTube Studio PC Client" when exception occurs)
            _mockLogger.Verify(l => l.Error(It.Is<string>(s => s.Contains("Failed to initialize VTube Studio PC Client")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task TryInitializeAsync_ReturnsFalse_WhenAuthenticationFails()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = false,
                Port = 8001,
                TokenFilePath = Path.GetTempFileName()
            };

            // Queue authentication responses for failed authentication
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "test-token" });
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = false, Reason = "Invalid token" });

            var client = CreateClient(config: config);

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            result.Should().BeFalse();

            // Verify error was logged
            _mockLogger.Verify(l => l.Error(It.Is<string>(s => s.Contains("Failed to authenticate")), It.IsAny<object[]>()), Times.Once);

            // Cleanup
            if (File.Exists(config.TokenFilePath))
                File.Delete(config.TokenFilePath);
        }

        [Fact]
        public async Task TryInitializeAsync_ReturnsFalse_WhenExceptionOccurs()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = false,
                Port = 8001
            };

            // Setup WebSocket to throw exception during connect
            _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.None);
            _mockWebSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection failed"));

            var client = CreateClient(config: config);

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            result.Should().BeFalse();

            // Verify error was logged
            _mockLogger.Verify(l => l.Error(It.Is<string>(s => s.Contains("Failed to initialize VTube Studio PC Client")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task TryInitializeAsync_LogsRecreation_WhenInClosedState()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = false,
                Port = 8001,
                TokenFilePath = Path.GetTempFileName()
            };

            // Setup WebSocket to be in Closed state initially, then None after recreation, then Open after connect
            var stateSequence = _mockWebSocket.SetupSequence(x => x.State);
            stateSequence.Returns(WebSocketState.Closed);  // Initial check
            stateSequence.Returns(WebSocketState.None);    // After recreation
            stateSequence.Returns(WebSocketState.Open);    // After connect
            stateSequence.Returns(WebSocketState.Open);    // During authentication
            stateSequence.Returns(WebSocketState.Open);    // During authentication

            _mockWebSocket.Setup(x => x.RecreateWebSocket());
            _mockWebSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Mock authentication responses
            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "test-token" });

            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true, Reason = "Success" });

            var client = CreateClient(config: config);

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            // Verify WebSocket recreation was called and logged
            _mockWebSocket.Verify(x => x.RecreateWebSocket(), Times.Once);
            _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("WebSocket is in an invalid state, recreating")), It.IsAny<object[]>()), Times.Once);

            // Cleanup
            if (File.Exists(config.TokenFilePath))
                File.Delete(config.TokenFilePath);
        }

        [Fact]
        public void LastInitializationError_ReturnsEmpty_WhenNoErrorOccurred()
        {
            // Arrange
            var client = CreateClient();

            // Act
            var error = client.LastInitializationError;

            // Assert
            error.Should().BeEmpty();
        }

        [Fact]
        public async Task LastInitializationError_ReturnsError_AfterFailedInitialization()
        {
            // Arrange
            var config = new VTubeStudioPCConfig
            {
                UsePortDiscovery = false,
                Port = 8001,
                ConnectionTimeoutMs = 1000
            };

            // Don't queue any authentication responses - this will cause authentication to fail
            // (MockWebSocketWrapper will throw "No response queued" when trying to authenticate)

            var client = CreateClient(config: config);

            // Act
            await client.TryInitializeAsync(CancellationToken.None);
            var error = client.LastInitializationError;

            // Assert
            error.Should().Be("Failed to authenticate with VTube Studio");
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert - First disposal should work
            client.Dispose();

            // Act & Assert - Second disposal should not throw
            client.Dispose();

            // Verify the client was disposed properly
            Assert.True(true); // Test passes if no exception is thrown
        }

        [Fact]
        public async Task GetServiceStats_ReturnsHealthyFalse_WhenNotConnected()
        {
            // Arrange
            var client = CreateClient();

            // Act
            var stats = client.GetServiceStats();

            // Assert
            Assert.False(stats.IsHealthy);
        }

        [Fact]
        public void ConfigChanged_InitiallyReturnsFalse()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert
            Assert.False(client.ConfigChanged);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenConfigChanges_UpdatesInternalConfig()
        {
            // Arrange
            var originalConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "OriginalPlugin",
                PluginDeveloper = "OriginalDeveloper"
            };

            var newPCConfig = new VTubeStudioPCConfig
            {
                Host = "newhost",
                Port = 8002,
                PluginName = "NewPlugin",
                PluginDeveloper = "NewDeveloper"
            };

            // Setup for constructor call (initial load) and config change event
            _mockConfigManager.SetupSequence(x => x.LoadSectionAsync<VTubeStudioPCConfig>())
                .ReturnsAsync(originalConfig) // Constructor call
                .ReturnsAsync(newPCConfig); // Config change event

            var client = CreateClient(config: originalConfig, setupMock: false);

            // Act - Simulate config change event
            _mockAppConfigWatcher.Raise(x => x.FileChanged += null,
                new FileChangeEventArgs("test.json"));


            // Assert
            Assert.True(client.ConfigChanged);
            Assert.Equal("newhost", client.Config.Host);
            Assert.Equal(8002, client.Config.Port);
            Assert.Equal("NewPlugin", client.Config.PluginName);
            Assert.Equal("NewDeveloper", client.Config.PluginDeveloper);

            _mockLogger.Verify(x => x.Debug("Application config changed, checking if PC client config was affected"), Times.Once);
            _mockLogger.Verify(x => x.Info("PC client configuration changed, updating internal config"), Times.Once);
            _mockLogger.Verify(x => x.Debug("PC client config updated - Host: {0}:{1}, Plugin: {2}",
                "newhost", 8002, "NewPlugin"), Times.Once);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenConfigDoesNotChange_DoesNotUpdateInternalConfig()
        {
            // Arrange
            var originalConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };

            var newConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };

            // Setup for constructor call (initial load)
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>())
                .ReturnsAsync(originalConfig);

            var client = CreateClient(config: originalConfig);

            // Setup for config change event (should return same config)
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>())
                .ReturnsAsync(newConfig);

            // Act - Simulate config change event
            _mockAppConfigWatcher.Raise(x => x.FileChanged += null,
                new FileChangeEventArgs("test.json"));

            // Assert
            Assert.False(client.ConfigChanged);
            Assert.Equal("localhost", client.Config.Host);
            Assert.Equal(8001, client.Config.Port);
            Assert.Equal("TestPlugin", client.Config.PluginName);
            Assert.Equal("TestDeveloper", client.Config.PluginDeveloper);

            _mockLogger.Verify(x => x.Debug("Application config changed, checking if PC client config was affected"), Times.Once);
            _mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            var originalConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };

            // Setup for constructor call (initial load) and config change event (should throw exception)
            _mockConfigManager.SetupSequence(x => x.LoadSectionAsync<VTubeStudioPCConfig>())
                .ReturnsAsync(originalConfig) // Constructor call
                .ThrowsAsync(new InvalidOperationException("Test exception")); // Config change event

            var client = CreateClient(config: originalConfig, setupMock: false);

            // Act - Simulate config change event
            _mockAppConfigWatcher.Raise(x => x.FileChanged += null,
                new FileChangeEventArgs("test.json"));

            // Assert
            Assert.False(client.ConfigChanged);
            _mockLogger.Verify(x => x.Debug("Application config changed, checking if PC client config was affected"), Times.Once);
            _mockLogger.Verify(x => x.Error("Error handling application config change: {0}", "Test exception"), Times.Once);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenConfigManagerReturnsNull_HandlesGracefully()
        {
            // Arrange
            var originalConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };

            // Setup for constructor call (initial load) and config change event (should throw exception)
            _mockConfigManager.SetupSequence(x => x.LoadSectionAsync<VTubeStudioPCConfig>())
                .ReturnsAsync(originalConfig) // Constructor call
                .ThrowsAsync(new InvalidOperationException("Config not found")); // Config change event

            var client = CreateClient(config: originalConfig, setupMock: false);

            // Act - Simulate config change event
            _mockAppConfigWatcher.Raise(x => x.FileChanged += null,
                new FileChangeEventArgs("test.json"));

            // Assert
            Assert.False(client.ConfigChanged);
            _mockLogger.Verify(x => x.Debug("Application config changed, checking if PC client config was affected"), Times.Once);
            _mockLogger.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void OnApplicationConfigChanged_WhenNewConfigHasNullPCClient_HandlesGracefully()
        {
            // Arrange
            var originalConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                PluginName = "TestPlugin",
                PluginDeveloper = "TestDeveloper"
            };

            // Setup for constructor call (initial load) and config change event (should throw exception)
            _mockConfigManager.SetupSequence(x => x.LoadSectionAsync<VTubeStudioPCConfig>())
                .ReturnsAsync(originalConfig) // Constructor call
                .ThrowsAsync(new InvalidOperationException("PC client config not found")); // Config change event

            var client = CreateClient(config: originalConfig, setupMock: false);

            // Act - Simulate config change event
            _mockAppConfigWatcher.Raise(x => x.FileChanged += null,
                new FileChangeEventArgs("test.json"));

            // Assert
            Assert.False(client.ConfigChanged); // ConfigChanged should be false when exception occurs
            _mockLogger.Verify(x => x.Debug("Application config changed, checking if PC client config was affected"), Times.Once);
            _mockLogger.Verify(x => x.Error("Error handling application config change: {0}", "PC client config not found"), Times.Once);
        }

        [Fact]
        public void Dispose_WhenAppConfigWatcherIsNull_DoesNotThrow()
        {
            // Arrange
            var client = new VTubeStudioPCClient(
                _mockLogger.Object,
                _mockConfigManager.Object,
                _mockWebSocket.Object,
                _mockPortDiscoveryService.Object,
                _mockParameterAdapter.Object,
                null); // No app config watcher

            // Act & Assert
            client.Dispose();

            // Verify the client was disposed properly
            Assert.True(true); // Test passes if no exception is thrown
        }

        [Fact]
        public void Dispose_WhenWebSocketIsNull_DoesNotThrow()
        {
            // Arrange
            var mockWebSocket = new Mock<IWebSocketWrapper>();
            mockWebSocket.Setup(x => x.State).Returns(WebSocketState.Closed);
            mockWebSocket.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));

            var client = new VTubeStudioPCClient(
                _mockLogger.Object,
                _mockConfigManager.Object,
                mockWebSocket.Object,
                _mockPortDiscoveryService.Object,
                _mockParameterAdapter.Object,
                _mockAppConfigWatcher.Object);

            // Act & Assert
            client.Dispose();
            _mockLogger.Verify(x => x.Error("Error disposing WebSocket: {0}", "Test exception"), Times.Once);
        }

        [Fact]
        public async Task TryInitializeAsync_WhenWebSocketIsInOpenState_RecreatesWebSocket()
        {
            // Arrange
            _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.Open);
            _mockWebSocket.Setup(x => x.RecreateWebSocket()).Callback(() =>
            {
                _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.None);
            });

            var client = CreateClient();

            // Setup successful initialization
            _mockPortDiscoveryService.Setup(x => x.DiscoverAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DiscoveryResponse { Port = 8001 });

            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "test-token" });

            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true });

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockWebSocket.Verify(x => x.RecreateWebSocket(), Times.Once);
        }

        [Fact]
        public async Task TryInitializeAsync_WhenWebSocketIsInAbortedState_RecreatesWebSocket()
        {
            // Arrange
            _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.Aborted);
            _mockWebSocket.Setup(x => x.RecreateWebSocket()).Callback(() =>
            {
                _mockWebSocket.Setup(x => x.State).Returns(WebSocketState.None);
            });

            var client = CreateClient();

            // Setup successful initialization
            _mockPortDiscoveryService.Setup(x => x.DiscoverAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DiscoveryResponse { Port = 8001 });

            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "test-token" });

            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true });

            // Act
            var result = await client.TryInitializeAsync(CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockWebSocket.Verify(x => x.RecreateWebSocket(), Times.Once);
        }

        [Fact]
        public async Task GetServiceStats_WhenConfigChanged_ReturnsUnhealthy()
        {
            // Arrange
            var client = CreateClient();

            // Simulate config change
            var newConfig = new ApplicationConfig
            {
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "newhost",
                    Port = 8002,
                    PluginName = "NewPlugin",
                    PluginDeveloper = "NewDeveloper"
                }
            };

            _mockConfigManager.Setup(x => x.LoadApplicationConfigAsync())
                .ReturnsAsync(newConfig);

            _mockAppConfigWatcher.Raise(x => x.FileChanged += null,
                new FileChangeEventArgs("test.json"));

            // Act
            var stats = client.GetServiceStats();

            // Assert
            Assert.False(stats.IsHealthy);
        }

        [Fact]
        public async Task GetServiceStats_WhenConnectedAndConfigNotChanged_ReturnsHealthy()
        {
            // Arrange
            var client = CreateClient();

            // Setup successful connection and authentication
            _mockPortDiscoveryService.Setup(x => x.DiscoverAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DiscoveryResponse { Port = 8001 });

            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                It.IsAny<string>(), It.IsAny<AuthTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationTokenResponse { AuthenticationToken = "test-token" });

            _mockWebSocket.Setup(x => x.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                It.IsAny<string>(), It.IsAny<AuthRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationResponse { Authenticated = true });

            await client.TryInitializeAsync(CancellationToken.None);

            // Act
            var stats = client.GetServiceStats();

            // Assert
            Assert.True(stats.IsHealthy);
        }
    }
}