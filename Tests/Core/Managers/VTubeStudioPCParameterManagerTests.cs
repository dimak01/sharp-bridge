using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;
using FluentAssertions;

namespace SharpBridge.Tests.Core.Managers
{
    public class VTubeStudioPCParameterManagerTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IWebSocketWrapper> _mockWebSocket;
        private readonly Mock<IVTSParameterAdapter> _mockParameterAdapter;
        private readonly VTubeStudioPCParameterManager _parameterManager;

        public VTubeStudioPCParameterManagerTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _mockWebSocket = new Mock<IWebSocketWrapper>();
            _mockParameterAdapter = new Mock<IVTSParameterAdapter>();

            // Set up default adapter behavior - return prefixed parameter names
            _mockParameterAdapter.Setup(x => x.AdaptParameterName(It.IsAny<string>()))
                .Returns<string>(name => $"SB_{name}");
            _mockParameterAdapter.Setup(x => x.AdaptParameters(It.IsAny<IEnumerable<VTSParameter>>()))
                .Returns<IEnumerable<VTSParameter>>(parameters => parameters.Select(p => new VTSParameter($"SB_{p.Name}", p.Min, p.Max, p.DefaultValue)));

            _parameterManager = new VTubeStudioPCParameterManager(_mockWebSocket.Object, _mockLogger.Object, _mockParameterAdapter.Object);
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

        // [Fact]
        // public async Task CreateParameterAsync_Succeeds_WhenParameterIsValid()
        // {
        //     // Arrange
        //     var parameter = new VTSParameter("TestParam", -1.0, 1.0, 0.0);
        //     _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
        //         "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
        //         .ReturnsAsync(new ParameterCreationResponse { ParameterName = parameter.Name });

        //     // Act
        //     var result = await _parameterManager.CreateParameterAsync(parameter, CancellationToken.None);

        //     // Assert
        //     result.Should().BeTrue();
        // }

        // [Fact]
        // public async Task CreateParameterAsync_ReturnsFalse_WhenParameterAlreadyExists()
        // {
        //     // Arrange
        //     var parameter = new VTSParameter("TestParam", -1.0, 1.0, 0.0);
        //     _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
        //         "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
        //         .ThrowsAsync(new InvalidOperationException("Parameter already exists"));

        //     // Act
        //     var result = await _parameterManager.CreateParameterAsync(parameter, CancellationToken.None);

        //     // Assert
        //     result.Should().BeFalse();
        //     _mockLogger.Verify(x => x.Error("Failed to {0} parameter {1}: {2}", "create", "TestParam", "Parameter already exists"), Times.Once);
        // }

        // [Fact]
        // public async Task UpdateParameterAsync_Succeeds_WhenParameterExists()
        // {
        //     // Arrange
        //     var parameter = new VTSParameter("TestParam", -1.0, 1.0, 0.0);
        //     _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
        //         "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
        //         .ReturnsAsync(new ParameterCreationResponse { ParameterName = parameter.Name });

        //     // Act
        //     var result = await _parameterManager.UpdateParameterAsync(parameter, CancellationToken.None);

        //     // Assert
        //     result.Should().BeTrue();
        // }

        // [Fact]
        // public async Task UpdateParameterAsync_ReturnsFalse_WhenParameterDoesNotExist()
        // {
        //     // Arrange
        //     var parameter = new VTSParameter("TestParam", -1.0, 1.0, 0.0);
        //     _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
        //         "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
        //         .ThrowsAsync(new InvalidOperationException("Parameter not found"));

        //     // Act
        //     var result = await _parameterManager.UpdateParameterAsync(parameter, CancellationToken.None);

        //     // Assert
        //     result.Should().BeFalse();
        //     _mockLogger.Verify(x => x.Error("Failed to {0} parameter {1}: {2}", "update", "TestParam", "Parameter not found"), Times.Once);
        // }

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
        public async Task DeleteParameterAsync_ReturnsFalse_WhenParameterDoesNotExist()
        {
            // Arrange
            var parameterName = "TestParam";
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterDeletionRequest, object>(
                "ParameterDeletionRequest", It.IsAny<ParameterDeletionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Parameter not found"));

            // Act
            var result = await _parameterManager.DeleteParameterAsync(parameterName, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(x => x.Error("Failed to delete parameter {0}: {1}", "SB_TestParam", "Parameter not found"), Times.Once);
        }

        [Fact]
        public async Task TrySynchronizeParametersAsync_SuccessfullySynchronizesParameters()
        {
            // Arrange
            var desiredParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.5), // Update existing
                new VTSParameter("NewParam", -1.0, 1.0, 0.0)      // Create new
            };

            // Adapter will use default behavior (add SB_ prefix)

            // Setup GetParametersAsync to return existing parameters
            _mockWebSocket.Setup(x => x.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                "InputParameterListRequest", It.IsAny<InputParameterListRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InputParameterListResponse
                {
                    ModelLoaded = true,
                    ModelName = "TestModel",
                    ModelId = "TestId",
                    CustomParameters = new List<VTSParameter> { new VTSParameter("SB_ExistingParam", -1.0, 1.0, 0.0) },
                    DefaultParameters = new List<VTSParameter>()
                });

            // Setup UpdateParameterAsync to succeed
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.Is<ParameterCreationRequest>(r => r.ParameterName == "SB_ExistingParam"),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParameterCreationResponse { ParameterName = "SB_ExistingParam" });

            // Setup CreateParameterAsync to succeed
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.Is<ParameterCreationRequest>(r => r.ParameterName == "SB_NewParam"),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParameterCreationResponse { ParameterName = "SB_NewParam" });

            // Act
            var result = await _parameterManager.TrySynchronizeParametersAsync(desiredParameters, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _mockWebSocket.Verify(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.Is<ParameterCreationRequest>(r => r.ParameterName == "SB_ExistingParam"),
                It.IsAny<CancellationToken>()), Times.Once);
            _mockWebSocket.Verify(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.Is<ParameterCreationRequest>(r => r.ParameterName == "SB_NewParam"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TrySynchronizeParametersAsync_ReturnsFalse_WhenSynchronizationFails()
        {
            // Arrange
            var desiredParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.5)
            };

            // Adapter will use default behavior (add SB_ prefix)

            // Setup GetParametersAsync to return existing parameters
            _mockWebSocket.Setup(x => x.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                "InputParameterListRequest", It.IsAny<InputParameterListRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InputParameterListResponse
                {
                    ModelLoaded = true,
                    ModelName = "TestModel",
                    ModelId = "TestId",
                    CustomParameters = new List<VTSParameter> { new VTSParameter("SB_ExistingParam", -1.0, 1.0, 0.0) },
                    DefaultParameters = new List<VTSParameter>()
                });

            // Setup UpdateParameterAsync to fail
            _mockWebSocket.Setup(x => x.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                "ParameterCreationRequest", It.IsAny<ParameterCreationRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act
            var result = await _parameterManager.TrySynchronizeParametersAsync(desiredParameters, CancellationToken.None);

            // Assert
            result.Should().BeFalse();

            // Verify error was logged at parameter level
            _mockLogger.Verify(x => x.Error("Failed to {0} parameter {1}: {2}", "update", "SB_ExistingParam", "Update failed"), Times.Once);
            _mockLogger.Verify(x => x.Error("Failed to update parameter: {0}", "SB_ExistingParam"), Times.Once);
        }

        [Fact]
        public async Task TrySynchronizeParametersAsync_LogsErrorsAppropriately()
        {
            // Arrange
            var desiredParameters = new List<VTSParameter>
            {
                new VTSParameter("ExistingParam", -1.0, 1.0, 0.5)
            };

            // Adapter will use default behavior (add SB_ prefix)

            // Setup GetParametersAsync to fail
            _mockWebSocket.Setup(x => x.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                "InputParameterListRequest", It.IsAny<InputParameterListRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Get parameters failed"));

            // Act
            var result = await _parameterManager.TrySynchronizeParametersAsync(desiredParameters, CancellationToken.None);

            // Assert
            result.Should().BeFalse();

            // Verify error was logged
            _mockLogger.Verify(x => x.Error("Failed to synchronize parameters: {0}", "Get parameters failed"), Times.Once);
        }
    }
}