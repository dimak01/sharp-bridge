using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;
using FluentAssertions;

namespace SharpBridge.Tests.Utilities
{
    public class VTubeStudioPCParameterManagerTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IWebSocketWrapper> _mockWebSocket;
        private readonly VTubeStudioPCParameterManager _parameterManager;

        public VTubeStudioPCParameterManagerTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockWebSocket = new Mock<IWebSocketWrapper>();
            _parameterManager = new VTubeStudioPCParameterManager(_mockWebSocket.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetParametersAsync_ReturnsEmptyCollection_WhenNoParametersExist()
        {
            // Arrange
            _mockWebSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), 
                It.IsAny<System.Net.WebSockets.WebSocketMessageType>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _parameterManager.GetParametersAsync(CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetParametersAsync_ReturnsAllParameters_WhenParametersExist()
        {
            // Arrange
            var expectedParameters = new List<VTSParameter>
            {
                new VTSParameter("Param1", -1.0, 1.0, 0.0),
                new VTSParameter("Param2", -1.0, 1.0, 0.0)
            };

            _mockWebSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), 
                It.IsAny<System.Net.WebSockets.WebSocketMessageType>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _parameterManager.GetParametersAsync(CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedParameters);
        }

        [Fact]
        public async Task CreateParameterAsync_Succeeds_WhenParameterIsValid()
        {
            // Arrange
            var parameter = new VTSParameter("TestParam", -1.0, 1.0, 0.0);
            _mockWebSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), 
                It.IsAny<System.Net.WebSockets.WebSocketMessageType>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _parameterManager.CreateParameterAsync(parameter, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CreateParameterAsync_Fails_WhenParameterAlreadyExists()
        {
            // Arrange
            var parameter = new VTSParameter("TestParam", -1.0, 1.0, 0.0);
            _mockWebSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), 
                It.IsAny<System.Net.WebSockets.WebSocketMessageType>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Parameter already exists"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _parameterManager.CreateParameterAsync(parameter, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateParameterAsync_Succeeds_WhenParameterExists()
        {
            // Arrange
            var parameter = new VTSParameter("TestParam", -1.0, 1.0, 0.0);
            _mockWebSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), 
                It.IsAny<System.Net.WebSockets.WebSocketMessageType>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _parameterManager.UpdateParameterAsync(parameter, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateParameterAsync_Fails_WhenParameterDoesNotExist()
        {
            // Arrange
            var parameter = new VTSParameter("TestParam", -1.0, 1.0, 0.0);
            _mockWebSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), 
                It.IsAny<System.Net.WebSockets.WebSocketMessageType>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Parameter not found"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _parameterManager.UpdateParameterAsync(parameter, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteParameterAsync_Succeeds_WhenParameterExists()
        {
            // Arrange
            var parameterName = "TestParam";
            _mockWebSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), 
                It.IsAny<System.Net.WebSockets.WebSocketMessageType>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _parameterManager.DeleteParameterAsync(parameterName, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteParameterAsync_Fails_WhenParameterDoesNotExist()
        {
            // Arrange
            var parameterName = "TestParam";
            _mockWebSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), 
                It.IsAny<System.Net.WebSockets.WebSocketMessageType>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Parameter not found"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _parameterManager.DeleteParameterAsync(parameterName, CancellationToken.None));
        }
    }
} 