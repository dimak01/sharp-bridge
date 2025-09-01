using System;
using Microsoft.Extensions.DependencyInjection;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.Remediation;

namespace SharpBridge.Services
{
    /// <summary>
    /// Factory implementation for creating configuration section remediation services.
    /// Provides type-safe access to remediation services for each configuration section type.
    /// </summary>
    public class ConfigSectionFirstTimeSetupFactory : IConfigSectionRemediationFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the ConfigSectionFirstTimeSetupFactory class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving remediation services</param>
        public ConfigSectionFirstTimeSetupFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets the remediation service for the specified configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to remediate</param>
        /// <returns>The remediation service for the specified section type</returns>
        public IConfigSectionRemediationService GetRemediationService(ConfigSectionTypes sectionType)
        {
            return sectionType switch
            {
                ConfigSectionTypes.VTubeStudioPCConfig =>
                    _serviceProvider.GetRequiredService<VTubeStudioPCConfigRemediationService>(),
                ConfigSectionTypes.VTubeStudioPhoneClientConfig =>
                    _serviceProvider.GetRequiredService<VTubeStudioPhoneClientConfigRemediationService>(),
                ConfigSectionTypes.GeneralSettingsConfig =>
                    _serviceProvider.GetRequiredService<GeneralSettingsConfigRemediationService>(),
                ConfigSectionTypes.TransformationEngineConfig =>
                    _serviceProvider.GetRequiredService<TransformationEngineConfigRemediationService>(),
                _ => throw new ArgumentException($"Unknown section type: {sectionType}", nameof(sectionType))
            };
        }
    }
}
