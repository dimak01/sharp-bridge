using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SharpBridge.Configuration.Factories;
using SharpBridge.Models.Configuration;
using Xunit;

namespace SharpBridge.Tests.Configuration.Factories
{
    /// <summary>
    /// Tests for ConfigSectionValidatorsFactory class.
    /// Focuses on core factory behavior rather than complex DI mocking scenarios.
    /// </summary>
    public class ConfigSectionValidatorsFactoryTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly ConfigSectionValidatorsFactory _factory;

        public ConfigSectionValidatorsFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _factory = new ConfigSectionValidatorsFactory(_mockServiceProvider.Object);
        }


        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidServiceProvider_InitializesSuccessfully()
        {
            // Act & Assert - no exception should be thrown
            var factory = new ConfigSectionValidatorsFactory(_mockServiceProvider.Object);

            Assert.NotNull(factory);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ConfigSectionValidatorsFactory(null!));

            Assert.Equal("serviceProvider", exception.ParamName);
        }

        #endregion

        #region GetValidator Tests

        [Theory]
        [InlineData((ConfigSectionTypes)999)]
        [InlineData((ConfigSectionTypes)(-1))]
        public void GetValidator_WithInvalidSectionType_ThrowsArgumentException(ConfigSectionTypes invalidSectionType)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _factory.GetValidator(invalidSectionType));

            Assert.Equal("sectionType", exception.ParamName);
            Assert.Contains($"Unknown section type: {invalidSectionType}", exception.Message);
        }

        [Fact]
        public void GetValidator_WhenServiceProviderThrowsException_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Service resolution failed");
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Throws(expectedException);

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetValidator(ConfigSectionTypes.VTubeStudioPCConfig));

            Assert.Same(expectedException, thrownException);
        }

        [Fact]
        public void GetValidator_WhenServiceProviderReturnsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Returns((object?)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetValidator(ConfigSectionTypes.VTubeStudioPCConfig));

            // The exception message will be from GetRequiredService when it can't find the service
            Assert.Contains("VTubeStudioPCConfigValidator", exception.Message);
        }

        #endregion

        #region Integration-Style Tests (Testing the switch logic mapping)

        [Fact]
        public void GetValidator_WithVTubeStudioPCConfig_RequestsCorrectValidatorType()
        {
            // This test verifies the switch logic without complex DI mocking
            // We expect it to fail with cast exception, but we can verify the correct type was requested

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetValidator(ConfigSectionTypes.VTubeStudioPCConfig));

            // Verify the correct validator type was requested (evidenced by the error message)
            Assert.Contains("VTubeStudioPCConfigValidator", exception.Message);
        }

        [Fact]
        public void GetValidator_WithVTubeStudioPhoneClientConfig_RequestsCorrectValidatorType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetValidator(ConfigSectionTypes.VTubeStudioPhoneClientConfig));

            // Verify the correct validator type was requested
            Assert.Contains("VTubeStudioPhoneClientConfigValidator", exception.Message);
        }

        [Fact]
        public void GetValidator_WithGeneralSettingsConfig_RequestsCorrectValidatorType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetValidator(ConfigSectionTypes.GeneralSettingsConfig));

            // Verify the correct validator type was requested
            Assert.Contains("GeneralSettingsConfigValidator", exception.Message);
        }

        [Fact]
        public void GetValidator_WithTransformationEngineConfig_RequestsCorrectValidatorType()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _factory.GetValidator(ConfigSectionTypes.TransformationEngineConfig));

            // Verify the correct validator type was requested
            Assert.Contains("TransformationEngineConfigValidator", exception.Message);
        }

        [Fact]
        public void GetValidator_WithAllValidSectionTypes_RequestsCorrectValidatorTypes()
        {
            // This test verifies that all valid enum values map to some validator request
            // We expect all to fail with InvalidOperationException, but this proves the switch logic works

            foreach (ConfigSectionTypes sectionType in Enum.GetValues<ConfigSectionTypes>())
            {
                // Act & Assert
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    _factory.GetValidator(sectionType));

                // Each should fail with a service resolution error, proving the switch case was hit
                Assert.Contains("Validator", exception.Message);
            }
        }

        #endregion
    }
}


