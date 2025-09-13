using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Configuration.Services;
using SharpBridge.Interfaces.Configuration;
using SharpBridge.Interfaces.Configuration.Factories;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.Configuration.Services.Remediation;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Models.Configuration;
using Xunit;

namespace SharpBridge.Tests.Configuration.Services
{
    /// <summary>
    /// Unit tests for ConfigRemediationService
    /// </summary>
    public class ConfigRemediationServiceTests : IDisposable
    {
        private readonly Mock<IConfigManager> _mockConfigManager;
        private readonly Mock<IConfigSectionRemediationServiceFactory> _mockRemediationFactory;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IConfigSectionRemediationService> _mockRemediationService;

        public ConfigRemediationServiceTests()
        {
            _mockConfigManager = new Mock<IConfigManager>();
            _mockRemediationFactory = new Mock<IConfigSectionRemediationServiceFactory>();
            _mockLogger = new Mock<IAppLogger>();
            _mockRemediationService = new Mock<IConfigSectionRemediationService>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, _mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_InitializesSuccessfully()
        {
            // Act
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, null);

            // Assert
            service.Should().NotBeNull();
        }

        [Theory]
        [InlineData("configManager")]
        [InlineData("remediationFactory")]
        public void Constructor_WithNullParameter_ThrowsArgumentNullException(string nullParameter)
        {
            // Arrange
            var configManager = nullParameter == "configManager" ? null : _mockConfigManager.Object;
            var remediationFactory = nullParameter == "remediationFactory" ? null : _mockRemediationFactory.Object;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConfigRemediationService(configManager!, remediationFactory!, _mockLogger.Object));
            exception.ParamName.Should().Be(nullParameter);
        }

        #endregion

        #region RemediateConfigurationAsync Tests

        [Fact]
        public async Task RemediateConfigurationAsync_WithAllSectionsValid_ReturnsTrue()
        {
            // Arrange
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, _mockLogger.Object);
            var sectionTypes = Enum.GetValues<ConfigSectionTypes>();

            // Mock all sections to return NoRemediationNeeded
            foreach (var sectionType in sectionTypes)
            {
                var fields = new List<ConfigFieldState>();
                _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(sectionType))
                    .ReturnsAsync(fields);

                _mockRemediationFactory.Setup(x => x.GetRemediationService(sectionType))
                    .Returns(_mockRemediationService.Object);

                _mockRemediationService.Setup(x => x.Remediate(fields))
                    .ReturnsAsync((RemediationResult.NoRemediationNeeded, (IConfigSection?)null));
            }

            // Act
            var result = await service.RemediateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
            _mockLogger.Verify(x => x.Info("Starting configuration remediation process"), Times.Once);
            _mockLogger.Verify(x => x.Info("Configuration remediation completed successfully"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithSomeSectionsNeedingRemediation_ReturnsTrue()
        {
            // Arrange
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, _mockLogger.Object);
            var sectionTypes = Enum.GetValues<ConfigSectionTypes>();
            var updatedConfig = new Mock<IConfigSection>().Object;
            var fields = new List<ConfigFieldState>();

            // Mock first section to need remediation
            var firstSection = sectionTypes[0];
            var firstSectionRemediationService = new Mock<IConfigSectionRemediationService>();

            _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(firstSection))
                .ReturnsAsync(fields);
            _mockRemediationFactory.Setup(x => x.GetRemediationService(firstSection))
                .Returns(firstSectionRemediationService.Object);
            firstSectionRemediationService.Setup(x => x.Remediate(fields))
                .ReturnsAsync((RemediationResult.Succeeded, updatedConfig));

            // Mock other sections to be valid
            for (int i = 1; i < sectionTypes.Length; i++)
            {
                var sectionType = sectionTypes[i];
                var sectionRemediationService = new Mock<IConfigSectionRemediationService>();

                _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(sectionType))
                    .ReturnsAsync(fields);
                _mockRemediationFactory.Setup(x => x.GetRemediationService(sectionType))
                    .Returns(sectionRemediationService.Object);
                sectionRemediationService.Setup(x => x.Remediate(fields))
                    .ReturnsAsync((RemediationResult.NoRemediationNeeded, (IConfigSection?)null));
            }

            // Act
            var result = await service.RemediateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
            _mockConfigManager.Verify(x => x.SaveSectionAsync(firstSection, updatedConfig), Times.Once);
            _mockLogger.Verify(x => x.Debug($"Remediation succeeded for section {firstSection}"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithSectionRemediationFailure_ReturnsFalse()
        {
            // Arrange
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, _mockLogger.Object);
            var sectionTypes = Enum.GetValues<ConfigSectionTypes>();
            var failingSection = sectionTypes[0];
            var fields = new List<ConfigFieldState>();

            _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(failingSection))
                .ReturnsAsync(fields);
            _mockRemediationFactory.Setup(x => x.GetRemediationService(failingSection))
                .Returns(_mockRemediationService.Object);
            _mockRemediationService.Setup(x => x.Remediate(fields))
                .ReturnsAsync((RemediationResult.Failed, (IConfigSection?)null));

            // Act
            var result = await service.RemediateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(x => x.Error($"Remediation failed for section {failingSection}"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithException_ReturnsFalse()
        {
            // Arrange
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, _mockLogger.Object);
            var exception = new Exception("Test exception");

            _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(It.IsAny<ConfigSectionTypes>()))
                .ThrowsAsync(exception);

            // Act
            var result = await service.RemediateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(x => x.Error($"Configuration remediation failed: {exception.Message}"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithMultipleSectionsNeedingRemediation_SavesAll()
        {
            // Arrange
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, _mockLogger.Object);
            var sectionTypes = Enum.GetValues<ConfigSectionTypes>();
            var updatedConfig1 = new Mock<IConfigSection>().Object;
            var updatedConfig2 = new Mock<IConfigSection>().Object;
            var fields = new List<ConfigFieldState>();

            // Mock first two sections to need remediation
            var section1 = sectionTypes[0];
            var section2 = sectionTypes[1];
            var section1RemediationService = new Mock<IConfigSectionRemediationService>();
            var section2RemediationService = new Mock<IConfigSectionRemediationService>();

            _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(section1))
                .ReturnsAsync(fields);
            _mockRemediationFactory.Setup(x => x.GetRemediationService(section1))
                .Returns(section1RemediationService.Object);
            section1RemediationService.Setup(x => x.Remediate(fields))
                .ReturnsAsync((RemediationResult.Succeeded, updatedConfig1));

            _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(section2))
                .ReturnsAsync(fields);
            _mockRemediationFactory.Setup(x => x.GetRemediationService(section2))
                .Returns(section2RemediationService.Object);
            section2RemediationService.Setup(x => x.Remediate(fields))
                .ReturnsAsync((RemediationResult.Succeeded, updatedConfig2));

            // Mock other sections to be valid
            for (int i = 2; i < sectionTypes.Length; i++)
            {
                var sectionType = sectionTypes[i];
                var sectionRemediationService = new Mock<IConfigSectionRemediationService>();

                _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(sectionType))
                    .ReturnsAsync(fields);
                _mockRemediationFactory.Setup(x => x.GetRemediationService(sectionType))
                    .Returns(sectionRemediationService.Object);
                sectionRemediationService.Setup(x => x.Remediate(fields))
                    .ReturnsAsync((RemediationResult.NoRemediationNeeded, (IConfigSection?)null));
            }

            // Act
            var result = await service.RemediateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
            _mockConfigManager.Verify(x => x.SaveSectionAsync(section1, updatedConfig1), Times.Once);
            _mockConfigManager.Verify(x => x.SaveSectionAsync(section2, updatedConfig2), Times.Once);
            _mockLogger.Verify(x => x.Info($"Saved configuration for {section1}"), Times.Once);
            _mockLogger.Verify(x => x.Info($"Saved configuration for {section2}"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithNoLogger_DoesNotLog()
        {
            // Arrange
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, null);
            var sectionTypes = Enum.GetValues<ConfigSectionTypes>();
            var fields = new List<ConfigFieldState>();

            foreach (var sectionType in sectionTypes)
            {
                _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(sectionType))
                    .ReturnsAsync(fields);
                _mockRemediationFactory.Setup(x => x.GetRemediationService(sectionType))
                    .Returns(_mockRemediationService.Object);
                _mockRemediationService.Setup(x => x.Remediate(fields))
                    .ReturnsAsync((RemediationResult.NoRemediationNeeded, (IConfigSection?)null));
            }

            // Act
            var result = await service.RemediateConfigurationAsync();

            // Assert
            result.Should().BeTrue();
            // No logger calls should be made since logger is null
        }

        [Fact]
        public async Task RemediateConfigurationAsync_WithSaveException_ReturnsFalse()
        {
            // Arrange
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, _mockLogger.Object);
            var updatedConfig = new Mock<IConfigSection>().Object;
            var fields = new List<ConfigFieldState>();
            var saveException = new Exception("Save failed");

            // Mock first section to need remediation
            var firstSection = ConfigSectionTypes.VTubeStudioPCConfig;
            var firstSectionRemediationService = new Mock<IConfigSectionRemediationService>();

            _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(firstSection))
                .ReturnsAsync(fields);
            _mockRemediationFactory.Setup(x => x.GetRemediationService(firstSection))
                .Returns(firstSectionRemediationService.Object);
            firstSectionRemediationService.Setup(x => x.Remediate(fields))
                .ReturnsAsync((RemediationResult.Succeeded, updatedConfig));

            // Mock save to throw exception
            _mockConfigManager.Setup(x => x.SaveSectionAsync(firstSection, updatedConfig))
                .ThrowsAsync(saveException);

            // Mock other sections to be valid
            var otherSections = new[] { ConfigSectionTypes.VTubeStudioPhoneClientConfig, ConfigSectionTypes.GeneralSettingsConfig, ConfigSectionTypes.TransformationEngineConfig };
            foreach (var sectionType in otherSections)
            {
                var sectionRemediationService = new Mock<IConfigSectionRemediationService>();

                _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(sectionType))
                    .ReturnsAsync(fields);
                _mockRemediationFactory.Setup(x => x.GetRemediationService(sectionType))
                    .Returns(sectionRemediationService.Object);
                sectionRemediationService.Setup(x => x.Remediate(fields))
                    .ReturnsAsync((RemediationResult.NoRemediationNeeded, (IConfigSection?)null));
            }

            // Act
            var result = await service.RemediateConfigurationAsync();

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(x => x.Error($"Configuration remediation failed: {saveException.Message}"), Times.Once);
        }

        [Fact]
        public async Task RemediateConfigurationAsync_ProcessesAllSectionTypes()
        {
            // Arrange
            var service = new ConfigRemediationService(_mockConfigManager.Object, _mockRemediationFactory.Object, _mockLogger.Object);
            var sectionTypes = Enum.GetValues<ConfigSectionTypes>();
            var fields = new List<ConfigFieldState>();

            foreach (var sectionType in sectionTypes)
            {
                _mockConfigManager.Setup(x => x.GetSectionFieldsAsync(sectionType))
                    .ReturnsAsync(fields);
                _mockRemediationFactory.Setup(x => x.GetRemediationService(sectionType))
                    .Returns(_mockRemediationService.Object);
                _mockRemediationService.Setup(x => x.Remediate(fields))
                    .ReturnsAsync((RemediationResult.NoRemediationNeeded, (IConfigSection?)null));
            }

            // Act
            var result = await service.RemediateConfigurationAsync();

            // Assert
            result.Should().BeTrue();

            // Verify all section types were processed
            foreach (var sectionType in sectionTypes)
            {
                _mockConfigManager.Verify(x => x.GetSectionFieldsAsync(sectionType), Times.Once);
                _mockRemediationFactory.Verify(x => x.GetRemediationService(sectionType), Times.Once);
                _mockRemediationService.Verify(x => x.Remediate(fields), Times.Exactly(sectionTypes.Length));
            }
        }

        #endregion


        public void Dispose()
        {
            _mockConfigManager?.Reset();
            _mockRemediationFactory?.Reset();
            _mockLogger?.Reset();
            _mockRemediationService?.Reset();
        }
    }
}