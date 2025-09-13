using System;
using System.Threading.Tasks;
using Moq;
using SharpBridge.Core.Services;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Infrastructure;
using Xunit;

namespace SharpBridge.Tests.Core.Services
{
    /// <summary>
    /// Unit tests for PortStatusMonitorService.
    /// Demonstrates how clean and testable the service is with mocked dependencies.
    /// </summary>
    public class PortStatusMonitorServiceTests
    {
        private readonly Mock<IFirewallAnalyzer> _mockFirewallAnalyzer;
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly PortStatusMonitorService _service;

        public PortStatusMonitorServiceTests()
        {
            _mockFirewallAnalyzer = new Mock<IFirewallAnalyzer>();
            _mockConfigManager = new Mock<IConfigManager>();
            _service = new PortStatusMonitorService(_mockFirewallAnalyzer.Object, _mockConfigManager.Object);
        }

        [Fact]
        public void Constructor_WithNullFirewallAnalyzer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PortStatusMonitorService(null!, _mockConfigManager.Object));
        }

        [Fact]
        public void Constructor_WithNullConfigManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PortStatusMonitorService(_mockFirewallAnalyzer.Object, null!));
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithValidConfig_ReturnsCompleteStatus()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = true
            };

            var inboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var outboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var webSocketAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var discoveryAnalysis = new FirewallAnalysisResult { IsAllowed = false };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules("28964", "0.0.0.0", "0", "UDP"))
                .Returns(inboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP"))
                .Returns(outboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "8001", "TCP"))
                .Returns(webSocketAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"))
                .Returns(discoveryAnalysis);

            // Act
            var result = await _service.GetNetworkStatusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.IPhone);
            Assert.NotNull(result.PC);
            Assert.True(result.IPhone.LocalPortOpen);
            Assert.True(result.IPhone.OutboundAllowed);
            Assert.True(result.PC.WebSocketAllowed);
            Assert.False(result.PC.DiscoveryAllowed);
            Assert.Equal(inboundAnalysis, result.IPhone.InboundFirewallAnalysis);
            Assert.Equal(outboundAnalysis, result.IPhone.OutboundFirewallAnalysis);
            Assert.Equal(webSocketAnalysis, result.PC.WebSocketFirewallAnalysis);
            Assert.Equal(discoveryAnalysis, result.PC.DiscoveryFirewallAnalysis);
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithPortDiscoveryDisabled_SkipsDiscoveryCheck()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = false // Disabled
            };

            var inboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var outboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var webSocketAnalysis = new FirewallAnalysisResult { IsAllowed = true };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules("28964", "0.0.0.0", "0", "UDP"))
                .Returns(inboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP"))
                .Returns(outboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "8001", "TCP"))
                .Returns(webSocketAnalysis);

            // Act
            var result = await _service.GetNetworkStatusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PC);
            Assert.True(result.PC.WebSocketAllowed);
            Assert.False(result.PC.DiscoveryAllowed); // Should be false when disabled (bool default)
            Assert.Null(result.PC.DiscoveryFirewallAnalysis); // Should be null when disabled

            // Verify discovery port was not checked
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"), Times.Never);
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithBlockedConnections_ReturnsCorrectStatus()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = true
            };

            var inboundAnalysis = new FirewallAnalysisResult { IsAllowed = false }; // Blocked
            var outboundAnalysis = new FirewallAnalysisResult { IsAllowed = false }; // Blocked
            var webSocketAnalysis = new FirewallAnalysisResult { IsAllowed = false }; // Blocked
            var discoveryAnalysis = new FirewallAnalysisResult { IsAllowed = false }; // Blocked

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules("28964", "0.0.0.0", "0", "UDP"))
                .Returns(inboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP"))
                .Returns(outboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "8001", "TCP"))
                .Returns(webSocketAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"))
                .Returns(discoveryAnalysis);

            // Act
            var result = await _service.GetNetworkStatusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IPhone.LocalPortOpen);
            Assert.False(result.IPhone.OutboundAllowed);
            Assert.False(result.PC.WebSocketAllowed);
            Assert.False(result.PC.DiscoveryAllowed);
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithMixedConnections_ReturnsCorrectStatus()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = true
            };

            var inboundAnalysis = new FirewallAnalysisResult { IsAllowed = true }; // Allowed
            var outboundAnalysis = new FirewallAnalysisResult { IsAllowed = false }; // Blocked
            var webSocketAnalysis = new FirewallAnalysisResult { IsAllowed = true }; // Allowed
            var discoveryAnalysis = new FirewallAnalysisResult { IsAllowed = false }; // Blocked

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules("28964", "0.0.0.0", "0", "UDP"))
                .Returns(inboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP"))
                .Returns(outboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "8001", "TCP"))
                .Returns(webSocketAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"))
                .Returns(discoveryAnalysis);

            // Act
            var result = await _service.GetNetworkStatusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IPhone.LocalPortOpen); // Inbound allowed
            Assert.False(result.IPhone.OutboundAllowed); // Outbound blocked
            Assert.True(result.PC.WebSocketAllowed); // WebSocket allowed
            Assert.False(result.PC.DiscoveryAllowed); // Discovery blocked
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithDifferentPorts_UsesCorrectPorts()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "10.0.0.50",
                IphonePort = 12345,
                LocalPort = 54321
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "127.0.0.1",
                Port = 9000,
                UsePortDiscovery = true
            };

            var inboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var outboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var webSocketAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var discoveryAnalysis = new FirewallAnalysisResult { IsAllowed = true };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules("54321", "0.0.0.0", "0", "UDP"))
                .Returns(inboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "10.0.0.50", "12345", "UDP"))
                .Returns(outboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "127.0.0.1", "9000", "TCP"))
                .Returns(webSocketAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "127.0.0.1", "47779", "UDP"))
                .Returns(discoveryAnalysis);

            // Act
            var result = await _service.GetNetworkStatusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IPhone.LocalPortOpen);
            Assert.True(result.IPhone.OutboundAllowed);
            Assert.True(result.PC.WebSocketAllowed);
            Assert.True(result.PC.DiscoveryAllowed);

            // Verify correct ports were used
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules("54321", "0.0.0.0", "0", "UDP"), Times.Once);
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules(null, "10.0.0.50", "12345", "UDP"), Times.Once);
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules(null, "127.0.0.1", "9000", "TCP"), Times.Once);
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules(null, "127.0.0.1", "47779", "UDP"), Times.Once);
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithConfigManagerException_HandlesGracefully()
        {
            // Arrange
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>())
                .ThrowsAsync(new InvalidOperationException("Config error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetNetworkStatusAsync());
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithFirewallAnalyzerException_HandlesGracefully()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = true
            };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Firewall analysis error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetNetworkStatusAsync());
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithNullConfigs_HandlesGracefully()
        {
            // Arrange
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync((VTubeStudioPhoneClientConfig)null!);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync((VTubeStudioPCConfig)null!);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => _service.GetNetworkStatusAsync());
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithDiscoveryPort_UsesCorrectPort()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = true
            };

            var inboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var outboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var webSocketAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var discoveryAnalysis = new FirewallAnalysisResult { IsAllowed = true };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules("28964", "0.0.0.0", "0", "UDP"))
                .Returns(inboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP"))
                .Returns(outboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "8001", "TCP"))
                .Returns(webSocketAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"))
                .Returns(discoveryAnalysis);

            // Act
            var result = await _service.GetNetworkStatusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.PC.DiscoveryAllowed);

            // Verify discovery port 47779 was used (VTube Studio discovery port)
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"), Times.Once);
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithCorrectProtocols_UsesCorrectProtocols()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = true
            };

            var inboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var outboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var webSocketAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var discoveryAnalysis = new FirewallAnalysisResult { IsAllowed = true };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules("28964", "0.0.0.0", "0", "UDP"))
                .Returns(inboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP"))
                .Returns(outboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "8001", "TCP"))
                .Returns(webSocketAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"))
                .Returns(discoveryAnalysis);

            // Act
            var result = await _service.GetNetworkStatusAsync();

            // Assert
            Assert.NotNull(result);

            // Verify correct protocols were used
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules("28964", "0.0.0.0", "0", "UDP"), Times.Once); // iPhone inbound: UDP
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP"), Times.Once); // iPhone outbound: UDP
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules(null, "localhost", "8001", "TCP"), Times.Once); // PC WebSocket: TCP
            _mockFirewallAnalyzer.Verify(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"), Times.Once); // PC Discovery: UDP
        }

        [Fact]
        public async Task GetNetworkStatusAsync_WithCorrectTimestamps_SetsTimestamps()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = "192.168.1.100",
                IphonePort = 21412,
                LocalPort = 28964
            };

            var pcConfig = new VTubeStudioPCConfig
            {
                Host = "localhost",
                Port = 8001,
                UsePortDiscovery = true
            };

            var inboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var outboundAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var webSocketAnalysis = new FirewallAnalysisResult { IsAllowed = true };
            var discoveryAnalysis = new FirewallAnalysisResult { IsAllowed = true };

            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPhoneClientConfig>()).ReturnsAsync(phoneConfig);
            _mockConfigManager.Setup(x => x.LoadSectionAsync<VTubeStudioPCConfig>()).ReturnsAsync(pcConfig);

            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules("28964", "0.0.0.0", "0", "UDP"))
                .Returns(inboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "192.168.1.100", "21412", "UDP"))
                .Returns(outboundAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "8001", "TCP"))
                .Returns(webSocketAnalysis);
            _mockFirewallAnalyzer.Setup(x => x.AnalyzeFirewallRules(null, "localhost", "47779", "UDP"))
                .Returns(discoveryAnalysis);

            var beforeCall = DateTime.UtcNow;

            // Act
            var result = await _service.GetNetworkStatusAsync();

            var afterCall = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.IPhone);
            Assert.NotNull(result.PC);
            Assert.True(result.LastUpdated >= beforeCall);
            Assert.True(result.LastUpdated <= afterCall);
            Assert.True(result.IPhone.LastChecked >= beforeCall);
            Assert.True(result.IPhone.LastChecked <= afterCall);
            Assert.True(result.PC.LastChecked >= beforeCall);
            Assert.True(result.PC.LastChecked <= afterCall);
        }
    }
}
