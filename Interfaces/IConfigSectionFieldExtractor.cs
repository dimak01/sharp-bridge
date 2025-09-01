using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for extracting field states from configuration sections.
    /// Each configuration section type should have its own field extractor implementation.
    /// </summary>
    public interface IConfigSectionFieldExtractor
    {
        /// <summary>
        /// Extracts field states from a configuration file for validation and remediation purposes.
        /// </summary>
        /// <param name="configFilePath">Path to the configuration file</param>
        /// <returns>List of field states representing the raw configuration data</returns>
        Task<List<ConfigFieldState>> ExtractFieldStatesAsync(string configFilePath);
    }
}
