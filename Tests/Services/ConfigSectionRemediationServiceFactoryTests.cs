using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;

namespace SharpBridge.Tests.Services
{
    /// <summary>
    /// Tests for ConfigSectionRemediationServiceFactory class.
    /// Focuses on core factory behavior rather than complex DI mocking scenarios.
    /// </summary>
    public class ConfigSectionRemediationServiceFactoryTests : IDisposable
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly ConfigSectionRemediationServiceFactory _factory;

        public ConfigSectionRemediationServiceFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _factory = new ConfigSectionRemediationServiceFactory(_mockServiceProvider.Object);
        }

        public void Dispose()
        {
            // No resources to dispose
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidServiceProvider_InitializesSuccessfully()
        {
            // Act & Assert - no exception should be thrown
            var factory = new ConfigSectionRemediationServiceFactory(_mockServiceProvider.Object);

            Assert.NotNull(factory);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConfigSectionRemediationServiceFactory(null!));

            Assert.Equal("serviceProvider", exception.ParamName);
        }

        #endregion

        #region GetRemediationService Tests

        [Theory]
        [InlineData((ConfigSectionTypes)999)]
        [InlineData((ConfigSectionTypes)(-1))]
        public void GetRemediationService_WithInvalidSectionType_ThrowsArgumentException(ConfigSectionTypes invalidSectionType)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _factory.GetRemediationService(invalidSectionType));

            Assert.Equal("sectionType", exception.ParamName);
            Assert.Contains($"Unknown section type: {invalidSectionType}", exception.Message);
        }

        [Fact]
        public void GetRemediationService_WhenServiceProviderThrowsException_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Service resolution failed");
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Throws(expectedException);

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetRemediationService(ConfigSectionTypes.VTubeStudioPCConfig));

            Assert.Same(expectedException, thrownException);
        }

        [Fact]
        public void GetRemediationService_WhenServiceProviderReturnsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Returns((object?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetRemediationService(ConfigSectionTypes.VTubeStudioPCConfig));

            // The exception message will be from GetRequiredService when it can't find the service
            Assert.Contains("VTubeStudioPCConfigRemediationService", exception.Message);
        }

        #endregion

        #region Integration-Style Tests (Testing the switch logic mapping)

        [Fact]
        public void GetRemediationService_WithVTubeStudioPCConfig_RequestsCorrectServiceType()
        {
            // This test verifies the switch logic without complex DI mocking
            // We expect it to fail with cast exception, but we can verify the correct type was requested

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetRemediationService(ConfigSectionTypes.VTubeStudioPCConfig));

            // Verify the correct service type was requested (evidenced by the error message)
            Assert.Contains("VTubeStudioPCConfigRemediationService", exception.Message);
        }

        [Fact]
        public void GetRemediationService_WithVTubeStudioPhoneClientConfig_RequestsCorrectServiceType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetRemediationService(ConfigSectionTypes.VTubeStudioPhoneClientConfig));

            // Verify the correct service type was requested
            Assert.Contains("VTubeStudioPhoneClientConfigRemediationService", exception.Message);
        }

        [Fact]
        public void GetRemediationService_WithGeneralSettingsConfig_RequestsCorrectServiceType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetRemediationService(ConfigSectionTypes.GeneralSettingsConfig));

            // Verify the correct service type was requested
            Assert.Contains("GeneralSettingsConfigRemediationService", exception.Message);
        }

        [Fact]
        public void GetRemediationService_WithTransformationEngineConfig_RequestsCorrectServiceType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetRemediationService(ConfigSectionTypes.TransformationEngineConfig));

            // Verify the correct service type was requested
            Assert.Contains("TransformationEngineConfigRemediationService", exception.Message);
        }

        [Fact]
        public void GetRemediationService_WithAllValidSectionTypes_RequestsCorrectServiceTypes()
        {
            // This test verifies that all valid enum values map to some service request
            // We expect all to fail with InvalidOperationException, but this proves the switch logic works

            foreach (ConfigSectionTypes sectionType in Enum.GetValues<ConfigSectionTypes>())
            {
                // Act & Assert
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    _factory.GetRemediationService(sectionType));

                // Each should fail with a service resolution error, proving the switch case was hit
                Assert.Contains("RemediationService", exception.Message);
            }
        }

        #endregion
    }
}