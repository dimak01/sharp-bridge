using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Factory interface for creating configuration section validators.
    /// Provides type-safe access to validators for each configuration section type.
    /// </summary>
    public interface IConfigSectionValidatorsFactory
    {
        /// <summary>
        /// Gets the validator for a specific configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to validate</param>
        /// <returns>The validator for the specified section type</returns>
        IConfigSectionValidator GetValidator(ConfigSectionTypes sectionType);
    }
}
