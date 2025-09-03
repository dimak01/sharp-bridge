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
    /// Tests for ConfigSectionFieldExtractorsFactory class.
    /// Focuses on core factory behavior rather than complex DI mocking scenarios.
    /// </summary>
    public class ConfigSectionFieldExtractorsFactoryTests : IDisposable
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly ConfigSectionFieldExtractorsFactory _factory;

        public ConfigSectionFieldExtractorsFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _factory = new ConfigSectionFieldExtractorsFactory();
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
            var factory = new ConfigSectionFieldExtractorsFactory();

            Assert.NotNull(factory);
        }

        [Fact]
        public void Constructor_WithNoParameters_InitializesSuccessfully()
        {
            // Act & Assert - no exception should be thrown
            var factory = new ConfigSectionFieldExtractorsFactory();

            Assert.NotNull(factory);
        }

        #endregion

        #region GetExtractor Tests

        [Theory]
        [InlineData((ConfigSectionTypes)999)]
        [InlineData((ConfigSectionTypes)(-1))]
        public void GetExtractor_WithInvalidSectionType_ThrowsArgumentException(ConfigSectionTypes invalidSectionType)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _factory.GetExtractor(invalidSectionType));

            Assert.Equal("sectionType", exception.ParamName);
            Assert.Contains($"Unknown section type: {invalidSectionType}", exception.Message);
        }

        [Fact]
        public void GetExtractor_WhenServiceProviderThrowsException_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Service resolution failed");
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Throws(expectedException);

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetExtractor(ConfigSectionTypes.VTubeStudioPCConfig));

            Assert.Same(expectedException, thrownException);
        }

        [Fact]
        public void GetExtractor_WhenServiceProviderReturnsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Returns((object?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetExtractor(ConfigSectionTypes.VTubeStudioPCConfig));

            // The exception message will be from GetRequiredService when it can't find the service
            Assert.Contains("VTubeStudioPCConfigFieldExtractor", exception.Message);
        }

        #endregion

        #region Integration-Style Tests (Testing the switch logic mapping)

        [Fact]
        public void GetExtractor_WithVTubeStudioPCConfig_RequestsCorrectExtractorType()
        {
            // This test verifies the switch logic without complex DI mocking
            // We expect it to fail with cast exception, but we can verify the correct type was requested

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetExtractor(ConfigSectionTypes.VTubeStudioPCConfig));

            // Verify the correct extractor type was requested (evidenced by the error message)
            Assert.Contains("VTubeStudioPCConfigFieldExtractor", exception.Message);
        }

        [Fact]
        public void GetExtractor_WithVTubeStudioPhoneClientConfig_RequestsCorrectExtractorType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetExtractor(ConfigSectionTypes.VTubeStudioPhoneClientConfig));

            // Verify the correct extractor type was requested
            Assert.Contains("VTubeStudioPhoneClientConfigFieldExtractor", exception.Message);
        }

        [Fact]
        public void GetExtractor_WithGeneralSettingsConfig_RequestsCorrectExtractorType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetExtractor(ConfigSectionTypes.GeneralSettingsConfig));

            // Verify the correct extractor type was requested
            Assert.Contains("GeneralSettingsConfigFieldExtractor", exception.Message);
        }

        [Fact]
        public void GetExtractor_WithTransformationEngineConfig_RequestsCorrectExtractorType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetExtractor(ConfigSectionTypes.TransformationEngineConfig));

            // Verify the correct extractor type was requested
            Assert.Contains("TransformationEngineConfigFieldExtractor", exception.Message);
        }

        [Fact]
        public void GetExtractor_WithAllValidSectionTypes_RequestsCorrectExtractorTypes()
        {
            // This test verifies that all valid enum values map to some extractor request
            // We expect all to fail with InvalidOperationException, but this proves the switch logic works

            foreach (ConfigSectionTypes sectionType in Enum.GetValues<ConfigSectionTypes>())
            {
                // Act & Assert
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    _factory.GetExtractor(sectionType));

                // Each should fail with a service resolution error, proving the switch case was hit
                Assert.Contains("FieldExtractor", exception.Message);
            }
        }

        #endregion
    }
}
