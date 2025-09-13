// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using Xunit;
using SharpBridge.Configuration.Factories;
using SharpBridge.Models.Configuration;
using SharpBridge.Interfaces.Configuration.Extractors;

namespace SharpBridge.Tests.Configuration.Factories
{
    /// <summary>
    /// Tests for ConfigSectionFieldExtractorsFactory class.
    /// Focuses on core factory behavior and field extraction.
    /// </summary>
    public class ConfigSectionFieldExtractorsFactoryTests : IDisposable
    {
        private readonly ConfigSectionFieldExtractorsFactory _factory;

        public ConfigSectionFieldExtractorsFactoryTests()
        {
            _factory = new ConfigSectionFieldExtractorsFactory();
        }

        public void Dispose()
        {
            // No resources to dispose
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_InitializesSuccessfully()
        {
            // Act
            var factory = new ConfigSectionFieldExtractorsFactory();

            // Assert
            Assert.NotNull(factory);
        }

        #endregion

        #region GetExtractor Tests

        [Fact]
        public void GetExtractor_WithVTubeStudioPCConfig_ReturnsConfigSectionFieldExtractor()
        {
            // Act
            var result = _factory.GetExtractor(ConfigSectionTypes.VTubeStudioPCConfig);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IConfigSectionFieldExtractor>(result);
        }

        [Fact]
        public void GetExtractor_WithVTubeStudioPhoneClientConfig_ReturnsConfigSectionFieldExtractor()
        {
            // Act
            var result = _factory.GetExtractor(ConfigSectionTypes.VTubeStudioPhoneClientConfig);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IConfigSectionFieldExtractor>(result);
        }

        [Fact]
        public void GetExtractor_WithGeneralSettingsConfig_ReturnsConfigSectionFieldExtractor()
        {
            // Act
            var result = _factory.GetExtractor(ConfigSectionTypes.GeneralSettingsConfig);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IConfigSectionFieldExtractor>(result);
        }

        [Fact]
        public void GetExtractor_WithTransformationEngineConfig_ReturnsConfigSectionFieldExtractor()
        {
            // Act
            var result = _factory.GetExtractor(ConfigSectionTypes.TransformationEngineConfig);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IConfigSectionFieldExtractor>(result);
        }

        [Fact]
        public void GetExtractor_WithAllValidSectionTypes_ReturnsConfigSectionFieldExtractor()
        {
            // Act & Assert
            foreach (ConfigSectionTypes sectionType in Enum.GetValues<ConfigSectionTypes>())
            {
                var result = _factory.GetExtractor(sectionType);
                Assert.NotNull(result);
                Assert.IsAssignableFrom<IConfigSectionFieldExtractor>(result);
            }
        }

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

        #endregion
    }
}