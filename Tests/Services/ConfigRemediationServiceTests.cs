using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;

namespace SharpBridge.Tests.Services
{
    /// <summary>
    /// Tests for ConfigRemediationService class.
    /// </summary>
    public class ConfigRemediationServiceTests : IDisposable
    {
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly Mock<IConfigSectionRemediationServiceFactory> _mockRemediationFactory;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly ConfigRemediationService _service;

        public ConfigRemediationServiceTests()
        {
            _mockConfigManager = new Mock<IConfigManager>();
            _mockRemediationFactory = new Mock<IConfigSectionRemediationServiceFactory>();
            _mockLogger = new Mock<IAppLogger>();

            _service = new ConfigRemediationService(
                _mockConfigManager.Object,
                _mockRemediationFactory.Object,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            // No resources to dispose
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act & Assert - no exception should be thrown
            var service = new ConfigRemediationService(
                _mockConfigManager.Object,
                _mockRemediationFactory.Object,
                _mockLogger.Object);

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullConfigManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConfigRemediationService(null!, _mockRemediationFactory.Object, _mockLogger.Object));

            Assert.Equal("configManager", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullRemediationFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConfigRemediationService(_mockConfigManager.Object, null!, _mockLogger.Object));

            Assert.Equal("remediationFactory", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLogger_InitializesSuccessfully()
        {
            // Act & Assert - null logger should be acceptable
            var service = new ConfigRemediationService(
                _mockConfigManager.Object,
                _mockRemediationFactory.Object,
                null);

            Assert.NotNull(service);
        }

        #endregion

        #region RemediateConfigurationAsync Tests

        [Fact]
        public async Task RemediateConfigurationAsync_WithAllSectionsValid_ReturnsTrue()
        {
            // Arrange
            SetupMocksForAllSections(RemediationResult.NoRemediationNeeded, null);

            // Act
            var result = await _service.RemediateConfigurationAsync();

            // Assert
            Assert.True(result);
            VerifyAllSectionsProcessed();
            VerifyNoSectionsSaved();
            _mockLogger.Verify(l => l.Info("Starting configuration remediation process"), Times.Once);
            _mockLogger.Verify(l => l.Info("Configuration remediation completed successfully"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithAllSectionsNeedingRemediation_RemediatesAndReturnsTrue()
        {
            // Arrange
            var mockConfigs = CreateMockConfigs();
            SetupMocksForAllSections(RemediationResult.Succeeded, mockConfigs);

            // Act
            var result = await _service.RemediateConfigurationAsync();

            // Assert
            Assert.True(result);
            VerifyAllSectionsProcessed();
            VerifyAllSectionsSaved(mockConfigs);
            _mockLogger.Verify(l => l.Info("Configuration remediation completed successfully"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithMixedResults_ProcessesCorrectly()
        {
            // Arrange
            var phoneConfig = new VTubeStudioPhoneClientConfig();
            var pcConfig = new VTubeStudioPCConfig();

            SetupMockForSection(ConfigSectionTypes.VTubeStudioPCConfig, RemediationResult.NoRemediationNeeded, null);
            SetupMockForSection(ConfigSectionTypes.VTubeStudioPhoneClientConfig, RemediationResult.Succeeded, phoneConfig);
            SetupMockForSection(ConfigSectionTypes.GeneralSettingsConfig, RemediationResult.NoRemediationNeeded, null);
            SetupMockForSection(ConfigSectionTypes.TransformationEngineConfig, RemediationResult.Succeeded, pcConfig);

            // Act
            var result = await _service.RemediateConfigurationAsync();

            // Assert
            Assert.True(result);
            VerifyAllSectionsProcessed();

            // Only sections that succeeded should be saved
            _mockConfigManager.Verify(m => m.SaveSectionAsync<IConfigSection>(ConfigSectionTypes.VTubeStudioPhoneClientConfig, phoneConfig), Times.Once);
            _mockConfigManager.Verify(m => m.SaveSectionAsync<IConfigSection>(ConfigSectionTypes.TransformationEngineConfig, pcConfig), Times.Once);

            // Sections that didn't need remediation should not be saved
            _mockConfigManager.Verify(m => m.SaveSectionAsync<IConfigSection>(ConfigSectionTypes.VTubeStudioPCConfig, It.IsAny<IConfigSection>()), Times.Never);
            _mockConfigManager.Verify(m => m.SaveSectionAsync<IConfigSection>(ConfigSectionTypes.GeneralSettingsConfig, It.IsAny<IConfigSection>()), Times.Never);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithOneFailure_ReturnsFalse()
        {
            // Arrange
            SetupMockForSection(ConfigSectionTypes.VTubeStudioPCConfig, RemediationResult.NoRemediationNeeded, null);
            SetupMockForSection(ConfigSectionTypes.VTubeStudioPhoneClientConfig, RemediationResult.Failed, null);
            // Other sections should not be processed after failure

            // Act
            var result = await _service.RemediateConfigurationAsync();

            // Assert
            Assert.False(result);

            // Verify first two sections were processed
            _mockConfigManager.Verify(m => m.GetSectionFieldsAsync(ConfigSectionTypes.VTubeStudioPCConfig), Times.Once);
            _mockConfigManager.Verify(m => m.GetSectionFieldsAsync(ConfigSectionTypes.VTubeStudioPhoneClientConfig), Times.Once);

            // Verify remaining sections were not processed (early exit on failure)
            _mockConfigManager.Verify(m => m.GetSectionFieldsAsync(ConfigSectionTypes.GeneralSettingsConfig), Times.Never);
            _mockConfigManager.Verify(m => m.GetSectionFieldsAsync(ConfigSectionTypes.TransformationEngineConfig), Times.Never);

            // No sections should be saved when there's a failure
            VerifyNoSectionsSaved();

            _mockLogger.Verify(l => l.Error("Remediation failed for section VTubeStudioPhoneClientConfig"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithConfigManagerException_ReturnsFalseAndLogsError()
        {
            // Arrange
            var exception = new InvalidOperationException("Config load failed");
            _mockConfigManager.Setup(m => m.GetSectionFieldsAsync(It.IsAny<ConfigSectionTypes>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _service.RemediateConfigurationAsync();

            // Assert
            Assert.False(result);
            _mockLogger.Verify(l => l.Error("Configuration remediation failed: Config load failed"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithRemediationServiceException_ReturnsFalseAndLogsError()
        {
            // Arrange
            var mockFields = new List<ConfigFieldState>();
            _mockConfigManager.Setup(m => m.GetSectionFieldsAsync(It.IsAny<ConfigSectionTypes>()))
                .ReturnsAsync(mockFields);

            var exception = new InvalidOperationException("Remediation failed");
            var mockRemediationService = new Mock<IConfigSectionRemediationService>();
            mockRemediationService.Setup(s => s.Remediate(It.IsAny<List<ConfigFieldState>>()))
                .ThrowsAsync(exception);

            _mockRemediationFactory.Setup(f => f.GetRemediationService(It.IsAny<ConfigSectionTypes>()))
                .Returns(mockRemediationService.Object);

            // Act
            var result = await _service.RemediateConfigurationAsync();

            // Assert
            Assert.False(result);
            _mockLogger.Verify(l => l.Error("Configuration remediation failed: Remediation failed"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithSaveException_ReturnsFalseAndLogsError()
        {
            // Arrange
            var mockConfig = new VTubeStudioPhoneClientConfig();

            // Setup all sections to avoid null reference issues
            SetupMockForSection(ConfigSectionTypes.VTubeStudioPCConfig, RemediationResult.NoRemediationNeeded, null);
            SetupMockForSection(ConfigSectionTypes.VTubeStudioPhoneClientConfig, RemediationResult.Succeeded, mockConfig);
            SetupMockForSection(ConfigSectionTypes.GeneralSettingsConfig, RemediationResult.NoRemediationNeeded, null);
            SetupMockForSection(ConfigSectionTypes.TransformationEngineConfig, RemediationResult.NoRemediationNeeded, null);

            var exception = new InvalidOperationException("Save failed");
            _mockConfigManager.Setup(m => m.SaveSectionAsync<IConfigSection>(It.IsAny<ConfigSectionTypes>(), It.IsAny<IConfigSection>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _service.RemediateConfigurationAsync();

            // Assert
            Assert.False(result);
            _mockLogger.Verify(l => l.Error("Configuration remediation failed: Save failed"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_LogsProgressForEachSection()
        {
            // Arrange
            var mockConfigs = CreateMockConfigs();
            SetupMocksForAllSections(RemediationResult.Succeeded, mockConfigs);

            // Act
            await _service.RemediateConfigurationAsync();

            // Assert
            _mockLogger.Verify(l => l.Info("Starting configuration remediation process"), Times.Once);
            _mockLogger.Verify(l => l.Debug("Remediation succeeded for section VTubeStudioPCConfig"), Times.Once);
            _mockLogger.Verify(l => l.Debug("Remediation succeeded for section VTubeStudioPhoneClientConfig"), Times.Once);
            _mockLogger.Verify(l => l.Debug("Remediation succeeded for section GeneralSettingsConfig"), Times.Once);
            _mockLogger.Verify(l => l.Debug("Remediation succeeded for section TransformationEngineConfig"), Times.Once);
            _mockLogger.Verify(l => l.Info("Saved configuration for VTubeStudioPCConfig"), Times.Once);
            _mockLogger.Verify(l => l.Info("Saved configuration for VTubeStudioPhoneClientConfig"), Times.Once);
            _mockLogger.Verify(l => l.Info("Saved configuration for GeneralSettingsConfig"), Times.Once);
            _mockLogger.Verify(l => l.Info("Saved configuration for TransformationEngineConfig"), Times.Once);
            _mockLogger.Verify(l => l.Info("Configuration remediation completed successfully"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_ProcessesAllSectionTypesInEnumOrder()
        {
            // Arrange
            var processedSections = new List<ConfigSectionTypes>();
            _mockConfigManager.Setup(m => m.GetSectionFieldsAsync(It.IsAny<ConfigSectionTypes>()))
                .Callback<ConfigSectionTypes>(section => processedSections.Add(section))
                .ReturnsAsync(new List<ConfigFieldState>());

            var mockRemediationService = new Mock<IConfigSectionRemediationService>();
            mockRemediationService.Setup(s => s.Remediate(It.IsAny<List<ConfigFieldState>>()))
                .ReturnsAsync((RemediationResult.NoRemediationNeeded, null));

            _mockRemediationFactory.Setup(f => f.GetRemediationService(It.IsAny<ConfigSectionTypes>()))
                .Returns(mockRemediationService.Object);

            // Act
            await _service.RemediateConfigurationAsync();

            // Assert
            var expectedSections = Enum.GetValues<ConfigSectionTypes>();
            Assert.Equal(expectedSections.Length, processedSections.Count);

            for (int i = 0; i < expectedSections.Length; i++)
            {
                Assert.Equal(expectedSections[i], processedSections[i]);
            }
        }

        #endregion

        #region Helper Methods

        private void SetupMocksForAllSections(RemediationResult result, Dictionary<ConfigSectionTypes, IConfigSection>? configs)
        {
            foreach (ConfigSectionTypes sectionType in Enum.GetValues<ConfigSectionTypes>())
            {
                var config = configs?.GetValueOrDefault(sectionType);
                SetupMockForSection(sectionType, result, config);
            }
        }

        private void SetupMockForSection(ConfigSectionTypes sectionType, RemediationResult result, IConfigSection? config)
        {
            var mockFields = new List<ConfigFieldState>();
            _mockConfigManager.Setup(m => m.GetSectionFieldsAsync(sectionType))
                .ReturnsAsync(mockFields);

            var mockRemediationService = new Mock<IConfigSectionRemediationService>();
            mockRemediationService.Setup(s => s.Remediate(mockFields))
                .ReturnsAsync((result, config));

            _mockRemediationFactory.Setup(f => f.GetRemediationService(sectionType))
                .Returns(mockRemediationService.Object);
        }

        private static Dictionary<ConfigSectionTypes, IConfigSection> CreateMockConfigs()
        {
            return new Dictionary<ConfigSectionTypes, IConfigSection>
            {
                { ConfigSectionTypes.VTubeStudioPCConfig, new VTubeStudioPCConfig() },
                { ConfigSectionTypes.VTubeStudioPhoneClientConfig, new VTubeStudioPhoneClientConfig() },
                { ConfigSectionTypes.GeneralSettingsConfig, new GeneralSettingsConfig() },
                { ConfigSectionTypes.TransformationEngineConfig, new TransformationEngineConfig() }
            };
        }

        private void VerifyAllSectionsProcessed()
        {
            foreach (ConfigSectionTypes sectionType in Enum.GetValues<ConfigSectionTypes>())
            {
                _mockConfigManager.Verify(m => m.GetSectionFieldsAsync(sectionType), Times.Once);
                _mockRemediationFactory.Verify(f => f.GetRemediationService(sectionType), Times.Once);
            }
        }

        private void VerifyNoSectionsSaved()
        {
            _mockConfigManager.Verify(m => m.SaveSectionAsync<IConfigSection>(It.IsAny<ConfigSectionTypes>(), It.IsAny<IConfigSection>()), Times.Never);
        }

        private void VerifyAllSectionsSaved(Dictionary<ConfigSectionTypes, IConfigSection> configs)
        {
            foreach (var kvp in configs)
            {
                _mockConfigManager.Verify(m => m.SaveSectionAsync<IConfigSection>(kvp.Key, kvp.Value), Times.Once);
            }
        }

        #endregion
    }
}
