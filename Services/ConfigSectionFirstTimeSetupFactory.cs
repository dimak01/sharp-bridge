using System;
using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.FirstTimeSetup;

namespace SharpBridge.Services
{
    /// <summary>
    /// Factory implementation for creating configuration section first-time setup services.
    /// Provides type-safe access to setup services for each configuration section type.
    /// </summary>
    public class ConfigSectionFirstTimeSetupFactory : IConfigSectionFirstTimeSetupFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the ConfigSectionFirstTimeSetupFactory class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving setup services</param>
        public ConfigSectionFirstTimeSetupFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets the first-time setup service for a specific configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to set up</param>
        /// <returns>The setup service for the specified section type</returns>
        public IConfigSectionFirstTimeSetupService GetFirstTimeSetupService(ConfigSectionTypes sectionType)
        {
            return sectionType switch
            {
                ConfigSectionTypes.VTubeStudioPCConfig =>
                    _serviceProvider.GetRequiredService<VTubeStudioPCConfigFirstTimeSetup>(),
                ConfigSectionTypes.VTubeStudioPhoneClientConfig =>
                    _serviceProvider.GetRequiredService<VTubeStudioPhoneClientConfigFirstTimeSetup>(),
                ConfigSectionTypes.GeneralSettingsConfig =>
                    _serviceProvider.GetRequiredService<GeneralSettingsConfigFirstTimeSetup>(),
                ConfigSectionTypes.TransformationEngineConfig =>
                    _serviceProvider.GetRequiredService<TransformationEngineConfigFirstTimeSetup>(),
                _ => throw new ArgumentException($"Unknown section type: {sectionType}", nameof(sectionType))
            };
        }
    }
}
