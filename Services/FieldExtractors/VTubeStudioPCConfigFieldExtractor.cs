using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.FieldExtractors
{
    /// <summary>
    /// Field extractor for VTubeStudioPCConfig configuration sections.
    /// </summary>
    public class VTubeStudioPCConfigFieldExtractor : IConfigSectionFieldExtractor
    {
        /// <summary>
        /// Extracts field states from the PCClient section of the configuration file.
        /// </summary>
        /// <param name="configFilePath">Path to the ApplicationConfig.json file</param>
        /// <returns>List of field states for the PC client configuration</returns>
        public async Task<List<ConfigFieldState>> ExtractFieldStatesAsync(string configFilePath)
        {
            // TODO: Implement PC config field extraction similar to phone client
            // Navigate to "PCClient" section and extract Host, Port, TokenFilePath fields
            await Task.CompletedTask;
            return new List<ConfigFieldState>();
        }
    }
}
