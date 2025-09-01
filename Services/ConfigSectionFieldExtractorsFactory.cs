using System;
using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.FieldExtractors;

namespace SharpBridge.Services
{
    /// <summary>
    /// Factory implementation for creating configuration section field extractors.
    /// Provides type-safe access to field extractors for each configuration section type.
    /// </summary>
    public class ConfigSectionFieldExtractorsFactory : IConfigSectionFieldExtractorsFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the ConfigSectionFieldExtractorsFactory class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving field extractors</param>
        public ConfigSectionFieldExtractorsFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets the field extractor for the specified configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to extract fields from</param>
        /// <returns>The field extractor for the specified section type</returns>
        public IConfigSectionFieldExtractor GetExtractor(ConfigSectionTypes sectionType)
        {
            return sectionType switch
            {
                ConfigSectionTypes.VTubeStudioPCConfig =>
                    _serviceProvider.GetRequiredService<VTubeStudioPCConfigFieldExtractor>(),
                ConfigSectionTypes.VTubeStudioPhoneClientConfig =>
                    _serviceProvider.GetRequiredService<VTubeStudioPhoneClientConfigFieldExtractor>(),
                ConfigSectionTypes.GeneralSettingsConfig =>
                    _serviceProvider.GetRequiredService<GeneralSettingsConfigFieldExtractor>(),
                ConfigSectionTypes.TransformationEngineConfig =>
                    _serviceProvider.GetRequiredService<TransformationEngineConfigFieldExtractor>(),
                _ => throw new ArgumentException($"Unknown section type: {sectionType}", nameof(sectionType))
            };
        }
    }
}
