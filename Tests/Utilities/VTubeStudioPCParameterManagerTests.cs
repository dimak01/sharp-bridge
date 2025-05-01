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
            _mockWebSocket.Setup(x => x.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                "InputParameterListRequest", It.IsAny<InputParameterListRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InputParameterListResponse 
                    { 
                        ModelLoaded = true,
                        ModelName = "TestModel",
                        ModelId = "TestId",
                        CustomParameters = new List<VTSParameter>(),
                        DefaultParameters = new List<VTSParameter>()
                    });

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

            _mockWebSocket.Setup(x => x.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                "InputParameterListRequest", It.IsAny<InputParameterListRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InputParameterListResponse 
                    { 
                        ModelLoaded = true,
                        ModelName = "TestModel",
                        ModelId = "TestId",
                        CustomParameters = expectedParameters,
                        DefaultParameters = new List<VTSParameter>()
                    });

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
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParameterCreationResponse { ParameterName = parameter.Name });

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
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
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
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParameterCreationResponse { ParameterName = parameter.Name });

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
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
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
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterDeletionRequest, object>(
                "ParameterDeletionRequest", It.IsAny<ParameterDeletionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new object());

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
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterDeletionRequest, object>(
                "ParameterDeletionRequest", It.IsAny<ParameterDeletionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Parameter not found"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _parameterManager.DeleteParameterAsync(parameterName, CancellationToken.None));
        }

        [Fact]
        public async Task SynchronizeParametersAsync_SuccessfullySynchronizesParameters()
        {
            // Arrange
            var existingParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.0)
            };

            var desiredParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.5), // Update existing
                new VTSParameter("NewParam", -1.0, 1.0, 0.0)      // Create new
            };

            // Setup GetParametersAsync to return existing parameters
            _mockWebSocket.Setup(x => x.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                "InputParameterListRequest", It.IsAny<InputParameterListRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InputParameterListResponse 
                    { 
                        ModelLoaded = true,
                        ModelName = "TestModel",
                        ModelId = "TestId",
                        CustomParameters = existingParameters,
                        DefaultParameters = new List<VTSParameter>()
                    });

            // Setup UpdateParameterAsync to succeed
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.Is<ParameterCreationRequest>(r => r.ParameterName == "ExistingParam"), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParameterCreationResponse { ParameterName = "ExistingParam" });

            // Setup CreateParameterAsync to succeed
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.Is<ParameterCreationRequest>(r => r.ParameterName == "NewParam"), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParameterCreationResponse { ParameterName = "NewParam" });

            // Act
            var result = await _parameterManager.SynchronizeParametersAsync(desiredParameters, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _mockWebSocket.Verify(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.Is<ParameterCreationRequest>(r => r.ParameterName == "ExistingParam"), 
                It.IsAny<CancellationToken>()), Times.Once);
            _mockWebSocket.Verify(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.Is<ParameterCreationRequest>(r => r.ParameterName == "NewParam"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SynchronizeParametersAsync_HandlesErrorsDuringSynchronization()
        {
            // Arrange
            var existingParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.0)
            };

            var desiredParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.5)
            };

            // Setup GetParametersAsync to return existing parameters
            _mockWebSocket.Setup(x => x.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                "InputParameterListRequest", It.IsAny<InputParameterListRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InputParameterListResponse 
                    { 
                        ModelLoaded = true,
                        ModelName = "TestModel",
                        ModelId = "TestId",
                        CustomParameters = existingParameters,
                        DefaultParameters = new List<VTSParameter>()
                    });

            // Setup UpdateParameterAsync to fail
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _parameterManager.SynchronizeParametersAsync(desiredParameters, CancellationToken.None));
            
            // Verify error was logged
            _mockLogger.Verify(x => x.Error("Failed to synchronize parameters: {0}", "Update failed"), Times.Once);
        }

        [Fact]
        public async Task SynchronizeParametersAsync_LogsErrorsAppropriately()
        {
            // Arrange
            var existingParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.0)
            };

            var desiredParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.5)
            };

            // Setup GetParametersAsync to fail
            _mockWebSocket.Setup(x => x.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                "InputParameterListRequest", It.IsAny<InputParameterListRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Get parameters failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _parameterManager.SynchronizeParametersAsync(desiredParameters, CancellationToken.None));
            
            // Verify error was logged
            _mockLogger.Verify(x => x.Error("Failed to synchronize parameters: {0}", "Get parameters failed"), Times.Once);
        }
    }
} 