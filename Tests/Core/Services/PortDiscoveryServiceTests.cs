using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;
using FluentAssertions;

namespace SharpBridge.Tests.Core.Services
{
    public class PortDiscoveryServiceTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IUdpClientWrapper> _mockUdpClient;
        private const int VTubeStudioDiscoveryPort = 47779;

        public PortDiscoveryServiceTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockUdpClient = new Mock<IUdpClientWrapper>();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PortDiscoveryService(null!, _mockUdpClient.Object));
        }

        [Fact]
        public void Constructor_WithNullUdpClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PortDiscoveryService(_mockLogger.Object, null!));
        }

        [Fact]
        public async Task DiscoverAsync_WhenVTubeStudioFound_ReturnsDiscoveryResponse()
        {
            // Arrange
            var response = new VTSApiResponse<DiscoveryResponse>
            {
                Data = new DiscoveryResponse
                {
                    Active = true,
                    InstanceId = "test-instance",
                    Port = 8001,
                    WindowTitle = "VTube Studio - Test Instance"
                }
            };
            var responseJson = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            _mockUdpClient.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(responseBytes, new IPEndPoint(IPAddress.Any, VTubeStudioDiscoveryPort)));

            var service = new PortDiscoveryService(_mockLogger.Object, _mockUdpClient.Object);

            // Act
            var result = await service.DiscoverAsync(2000, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Active.Should().BeTrue();
            result.Port.Should().Be(8001);
            result.InstanceId.Should().Be("test-instance");
            result.WindowTitle.Should().Be("VTube Studio - Test Instance");

            _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Listening for VTube Studio broadcast")), It.IsAny<object[]>()), Times.Once);
            _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Found VTube Studio")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task DiscoverAsync_WhenTimeout_ReturnsNull()
        {
            // Arrange
            _mockUdpClient.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(3000).ContinueWith(_ => new UdpReceiveResult(new byte[0], new IPEndPoint(IPAddress.Any, 0))));

            var service = new PortDiscoveryService(_mockLogger.Object, _mockUdpClient.Object);

            // Act
            var result = await service.DiscoverAsync(2000, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("timed out")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task DiscoverAsync_WhenInvalidResponse_ReturnsNull()
        {
            // Arrange
            var invalidJson = "invalid json";
            var responseBytes = Encoding.UTF8.GetBytes(invalidJson);

            _mockUdpClient.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(responseBytes, new IPEndPoint(IPAddress.Any, VTubeStudioDiscoveryPort)));

            var service = new PortDiscoveryService(_mockLogger.Object, _mockUdpClient.Object);

            // Act
            var result = await service.DiscoverAsync(2000, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error during port discovery")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task DiscoverAsync_WhenNotVTubeStudio_ReturnsNull()
        {
            // Arrange
            var response = new VTSApiResponse<DiscoveryResponse>
            {
                Data = new DiscoveryResponse
                {
                    Active = true,
                    InstanceId = "test-instance",
                    Port = 8001,
                    WindowTitle = "Not VTube Studio" // Different window title
                }
            };
            var responseJson = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            _mockUdpClient.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(responseBytes, new IPEndPoint(IPAddress.Any, VTubeStudioDiscoveryPort)));

            var service = new PortDiscoveryService(_mockLogger.Object, _mockUdpClient.Object);

            // Act
            var result = await service.DiscoverAsync(2000, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("doesn't appear to be VTube Studio")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task DiscoverAsync_WhenReceiveFails_ReturnsNull()
        {
            // Arrange
            _mockUdpClient.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SocketException());

            var service = new PortDiscoveryService(_mockLogger.Object, _mockUdpClient.Object);

            // Act
            var result = await service.DiscoverAsync(2000, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(x => x.Error(It.Is<string>(s => s.Contains("Error during port discovery")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task DiscoverAsync_WhenInactiveVTubeStudio_ReturnsNull()
        {
            // Arrange
            var response = new VTSApiResponse<DiscoveryResponse>
            {
                Data = new DiscoveryResponse
                {
                    Active = false, // Inactive instance
                    InstanceId = "test-instance",
                    Port = 8001,
                    WindowTitle = "VTube Studio - Test Instance"
                }
            };
            var responseJson = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            _mockUdpClient.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(responseBytes, new IPEndPoint(IPAddress.Any, VTubeStudioDiscoveryPort)));

            var service = new PortDiscoveryService(_mockLogger.Object, _mockUdpClient.Object);

            // Act
            var result = await service.DiscoverAsync(2000, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("No active VTube Studio instance found")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task DiscoverAsync_WhenMissingInstanceId_ReturnsNull()
        {
            // Arrange
            var response = new VTSApiResponse<DiscoveryResponse>
            {
                Data = new DiscoveryResponse
                {
                    Active = true,
                    InstanceId = "", // Missing instance ID
                    Port = 8001,
                    WindowTitle = "VTube Studio - Test Instance"
                }
            };
            var responseJson = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            _mockUdpClient.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(responseBytes, new IPEndPoint(IPAddress.Any, VTubeStudioDiscoveryPort)));

            var service = new PortDiscoveryService(_mockLogger.Object, _mockUdpClient.Object);

            // Act
            var result = await service.DiscoverAsync(2000, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("doesn't appear to be VTube Studio")), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task DiscoverAsync_WhenMissingWindowTitle_ReturnsNull()
        {
            // Arrange
            var response = new VTSApiResponse<DiscoveryResponse>
            {
                Data = new DiscoveryResponse
                {
                    Active = true,
                    InstanceId = "test-instance",
                    Port = 8001,
                    WindowTitle = "" // Missing window title
                }
            };
            var responseJson = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            _mockUdpClient.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UdpReceiveResult(responseBytes, new IPEndPoint(IPAddress.Any, VTubeStudioDiscoveryPort)));

            var service = new PortDiscoveryService(_mockLogger.Object, _mockUdpClient.Object);

            // Act
            var result = await service.DiscoverAsync(2000, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(x => x.Warning(It.Is<string>(s => s.Contains("doesn't appear to be VTube Studio")), It.IsAny<object[]>()), Times.Once);
        }
    }
}