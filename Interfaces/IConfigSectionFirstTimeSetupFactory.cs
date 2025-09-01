using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Factory interface for creating configuration section first-time setup services.
    /// Provides type-safe access to setup services for each configuration section type.
    /// </summary>
    public interface IConfigSectionFirstTimeSetupFactory
    {
        /// <summary>
        /// Gets the first-time setup service for a specific configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to set up</param>
        /// <returns>The setup service for the specified section type</returns>
        IConfigSectionFirstTimeSetupService GetFirstTimeSetupService(ConfigSectionTypes sectionType);
    }
}
