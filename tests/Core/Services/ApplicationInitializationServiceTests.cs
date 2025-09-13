// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Core.Services;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.Core.Clients;
using SharpBridge.Interfaces.Core.Engines;
using SharpBridge.Interfaces.Core.Managers;
using SharpBridge.Interfaces.Core.Services;
using SharpBridge.Interfaces.Infrastructure;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Interfaces.UI.Managers;
using SharpBridge.Models.Domain;
using SharpBridge.Models.UI;
using SharpBridge.UI.Providers;
using Xunit;

namespace SharpBridge.Tests.Core.Services
{
    public class ApplicationInitializationServiceTests
    {
        private readonly Mock<IVTubeStudioPCClient> _mockPCClient;
        private readonly Mock<IVTubeStudioPhoneClient> _mockPhoneClient;
        private readonly Mock<ITransformationEngine> _mockTransformationEngine;
        private readonly Mock<IVTubeStudioPCParameterManager> _mockParameterManager;
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly Mock<IFileChangeWatcher> _mockAppConfigWatcher;
        private readonly Mock<IConsoleModeManager> _mockModeManager;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<InitializationContentProvider> _mockInitializationContentProvider;

        public ApplicationInitializationServiceTests()
        {
            _mockPCClient = new Mock<IVTubeStudioPCClient>();
            _mockPhoneClient = new Mock<IVTubeStudioPhoneClient>();
            _mockTransformationEngine = new Mock<ITransformationEngine>();
            _mockParameterManager = new Mock<IVTubeStudioPCParameterManager>();
            _mockConfigManager = new Mock<IConfigManager>();
            _mockAppConfigWatcher = new Mock<IFileChangeWatcher>();
            _mockModeManager = new Mock<IConsoleModeManager>();
            _mockLogger = new Mock<IAppLogger>();
            _mockInitializationContentProvider = new Mock<InitializationContentProvider>(_mockLogger.Object, new Mock<IExternalEditorService>().Object);


            // Setup common mocks
            _mockConfigManager.Setup(x => x.ApplicationConfigPath).Returns("test-config.json");
            _mockTransformationEngine.Setup(x => x.GetParameterDefinitions()).Returns(new List<VTSParameter>());
        }

        private ApplicationInitializationService CreateService()
        {
            return new ApplicationInitializationService(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                _mockParameterManager.Object,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object,
                _mockModeManager.Object,
                _mockLogger.Object,
                _mockInitializationContentProvider.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var service = CreateService();

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullPCClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                null!,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                _mockParameterManager.Object,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object,
                _mockModeManager.Object,
                _mockLogger.Object,
                _mockInitializationContentProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullPhoneClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                _mockPCClient.Object,
                null!,
                _mockTransformationEngine.Object,
                _mockParameterManager.Object,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object,
                _mockModeManager.Object,
                _mockLogger.Object,
                _mockInitializationContentProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullTransformationEngine_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                null!,
                _mockParameterManager.Object,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object,
                _mockModeManager.Object,
                _mockLogger.Object,
                _mockInitializationContentProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullParameterManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                null!,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object,
                _mockModeManager.Object,
                _mockLogger.Object,
                _mockInitializationContentProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullConfigManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                _mockParameterManager.Object,
                null!,
                _mockAppConfigWatcher.Object,
                _mockModeManager.Object,
                _mockLogger.Object,
                _mockInitializationContentProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullAppConfigWatcher_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                _mockParameterManager.Object,
                _mockConfigManager.Object,
                null!,
                _mockModeManager.Object,
                _mockLogger.Object,
                _mockInitializationContentProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullModeManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                _mockParameterManager.Object,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object,
                null!,
                _mockLogger.Object,
                _mockInitializationContentProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                _mockParameterManager.Object,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object,
                _mockModeManager.Object,
                null!,
                _mockInitializationContentProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullInitializationContentProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationInitializationService(
                _mockPCClient.Object,
                _mockPhoneClient.Object,
                _mockTransformationEngine.Object,
                _mockParameterManager.Object,
                _mockConfigManager.Object,
                _mockAppConfigWatcher.Object,
                _mockModeManager.Object,
                _mockLogger.Object,
                null!));
        }

        #endregion

        #region InitializeAsync Tests

        [Fact]
        public async Task InitializeAsync_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await service.InitializeAsync(cancellationToken);

            // Assert
            _mockModeManager.Verify(x => x.SetMode(ConsoleMode.Initialization), Times.Once);
            _mockModeManager.Verify(x => x.SetMode(ConsoleMode.Main), Times.Once);
            _mockInitializationContentProvider.Verify(x => x.SetProgress(It.IsAny<InitializationProgress>()), Times.Once);
            _mockTransformationEngine.Verify(x => x.LoadRulesAsync(), Times.Once);
            _mockAppConfigWatcher.Verify(x => x.StartWatching("test-config.json"), Times.Once);
            _mockPCClient.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _mockPhoneClient.Verify(x => x.TryInitializeAsync(cancellationToken), Times.Once);
            _mockParameterManager.Verify(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken), Times.Once);
            _mockLogger.Verify(x => x.Info("Application initialized successfully"), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WithFinalSetupActions_ExecutesActions()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;
            var action1Executed = false;
            var action2Executed = false;
            var finalSetupActions = new List<Action>
            {
                () => action1Executed = true,
                () => action2Executed = true
            };

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await service.InitializeAsync(cancellationToken, finalSetupActions);

            // Assert
            action1Executed.Should().BeTrue();
            action2Executed.Should().BeTrue();
        }

        [Fact]
        public async Task InitializeAsync_WithNullFinalSetupActions_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await service.InitializeAsync(cancellationToken, null);

            // Assert
            _mockModeManager.Verify(x => x.SetMode(ConsoleMode.Main), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenFinalSetupActionThrows_LogsErrorAndContinues()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;
            var finalSetupActions = new List<Action>
            {
                () => throw new Exception("Test exception")
            };

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await service.InitializeAsync(cancellationToken, null, finalSetupActions);

            // Assert
            _mockLogger.Verify(x => x.ErrorWithException("Error executing final setup action", It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenPCClientFails_LogsFailure()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(false));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await service.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Info("Attempting initial PC client connection..."), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenPhoneClientFails_LogsFailure()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(false));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await service.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Info("Attempting initial Phone client connection..."), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenParameterSyncFails_LogsWarning()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(false));

            // Act
            await service.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Warning("Parameter synchronization failed during initialization, will retry during recovery"), Times.Once);
        }



        [Fact]
        public async Task InitializeAsync_WhenExceptionOccurs_SwitchesToMainMode()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Throws(new Exception("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.InitializeAsync(cancellationToken));

            // Verify that it switches to main mode even when exception occurs
            _mockModeManager.Verify(x => x.SetMode(ConsoleMode.Initialization), Times.Once);
            _mockModeManager.Verify(x => x.SetMode(ConsoleMode.Main), Times.Once);
        }


        #endregion

        #region Private Method Tests (via reflection)


        [Fact]
        public async Task TrySynchronizeParametersAsync_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await service.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Info("VTube Studio parameters synchronized successfully"), Times.Once);
        }

        [Fact]
        public async Task TrySynchronizeParametersAsync_WithInvalidParameters_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(false));

            // Act
            await service.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Warning("Failed to synchronize VTube Studio parameters"), Times.Once);
        }

        [Fact]
        public async Task RenderInitializationProgress_WhenRenderingFails_LogsWarning()
        {
            // Arrange
            var service = CreateService();
            var cancellationToken = CancellationToken.None;

            _mockModeManager.Setup(x => x.Update(It.IsAny<IEnumerable<IServiceStats>>())).Throws(new Exception("Rendering error"));
            _mockTransformationEngine.Setup(x => x.LoadRulesAsync()).Returns(Task.CompletedTask);
            _mockPCClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockPhoneClient.Setup(x => x.TryInitializeAsync(cancellationToken)).Returns(Task.FromResult(true));
            _mockParameterManager.Setup(x => x.TrySynchronizeParametersAsync(It.IsAny<IEnumerable<VTSParameter>>(), cancellationToken))
                .Returns(Task.FromResult(true));

            // Act
            await service.InitializeAsync(cancellationToken);

            // Assert
            _mockLogger.Verify(x => x.Warning("Failed to render initialization progress: {0}", "Rendering error"), Times.AtLeastOnce);
        }


        #endregion

    }
}
