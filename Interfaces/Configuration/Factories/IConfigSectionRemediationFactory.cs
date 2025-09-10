using SharpBridge.Interfaces.Configuration.Services.Remediation;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Interfaces.Configuration.Factories
{
    /// <summary>
    /// Factory interface for creating configuration section remediation services.
    /// Provides type-safe access to remediation services for each configuration section type.
    /// </summary>
    public interface IConfigSectionRemediationServiceFactory
    {
        /// <summary>
        /// Gets the remediation service for the specified configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to remediate</param>
        /// <returns>The remediation service for the specified section type</returns>
        IConfigSectionRemediationService GetRemediationService(ConfigSectionTypes sectionType);
    }
}
