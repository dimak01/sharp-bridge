using System;
using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Configuration.Services.Validators;
using SharpBridge.Interfaces.Configuration.Factories;
using SharpBridge.Interfaces.Configuration.Services.Validators;
using SharpBridge.Models.Configuration;


namespace SharpBridge.Configuration.Factories
{
    /// <summary>
    /// Factory implementation for creating configuration section validators.
    /// Provides type-safe access to validators for each configuration section type.
    /// </summary>
    public class ConfigSectionValidatorsFactory : IConfigSectionValidatorsFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the ConfigSectionValidatorsFactory class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving validators</param>
        public ConfigSectionValidatorsFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets the validator for a specific configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to validate</param>
        /// <returns>The validator for the specified section type</returns>
        public IConfigSectionValidator GetValidator(ConfigSectionTypes sectionType)
        {
            return sectionType switch
            {
                ConfigSectionTypes.VTubeStudioPCConfig =>
                    _serviceProvider.GetRequiredService<VTubeStudioPCConfigValidator>(),
                ConfigSectionTypes.VTubeStudioPhoneClientConfig =>
                    _serviceProvider.GetRequiredService<VTubeStudioPhoneClientConfigValidator>(),
                ConfigSectionTypes.GeneralSettingsConfig =>
                    _serviceProvider.GetRequiredService<GeneralSettingsConfigValidator>(),
                ConfigSectionTypes.TransformationEngineConfig =>
                    _serviceProvider.GetRequiredService<TransformationEngineConfigValidator>(),
                _ => throw new ArgumentException($"Unknown section type: {sectionType}", nameof(sectionType))
            };
        }
    }
}
