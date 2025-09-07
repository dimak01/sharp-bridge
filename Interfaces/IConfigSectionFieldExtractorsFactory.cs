using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Factory interface for creating configuration section field extractors.
    /// Provides type-safe access to field extractors for each configuration section type.
    /// </summary>
    public interface IConfigSectionFieldExtractorsFactory
    {
        /// <summary>
        /// Gets the field extractor for the specified configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to extract fields from</param>
        /// <returns>The field extractor for the specified section type</returns>
        IConfigSectionFieldExtractor GetExtractor(ConfigSectionTypes sectionType);
    }
}
