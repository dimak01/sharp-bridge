using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services.FieldExtractors
{
    /// <summary>
    /// Field extractor for VTubeStudioPhoneClientConfig configuration sections.
    /// </summary>
    public class VTubeStudioPhoneClientConfigFieldExtractor : IConfigSectionFieldExtractor
    {
        /// <summary>
        /// Extracts field states from the PhoneClient section of the configuration file.
        /// </summary>
        /// <param name="configFilePath">Path to the ApplicationConfig.json file</param>
        /// <returns>List of field states for the phone client configuration</returns>
        public async Task<List<ConfigFieldState>> ExtractFieldStatesAsync(string configFilePath)
        {
            var fieldStates = new List<ConfigFieldState>();
            var phoneConfigType = typeof(VTubeStudioPhoneClientConfig);
            var properties = phoneConfigType.GetProperties();

            // Get expected field schema from the DTO type
            foreach (var property in properties)
            {
                var description = GetPropertyDescription(property);
                var fieldState = await ExtractFieldState(configFilePath, property, description);
                fieldStates.Add(fieldState);
            }

            return fieldStates;
        }

        private static async Task<ConfigFieldState> ExtractFieldState(string configFilePath, PropertyInfo property, string description)
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    // Config file doesn't exist - field is not present
                    return new ConfigFieldState(property.Name, null, false, property.PropertyType, description);
                }

                var jsonText = await File.ReadAllTextAsync(configFilePath);
                using var document = JsonDocument.Parse(jsonText);

                // Navigate to PhoneClient section
                if (!document.RootElement.TryGetProperty("PhoneClient", out var phoneClientSection))
                {
                    // PhoneClient section doesn't exist - field is not present
                    return new ConfigFieldState(property.Name, null, false, property.PropertyType, description);
                }

                // Look for the property in the PhoneClient section (case-insensitive)
                if (!TryGetPropertyIgnoreCase(phoneClientSection, property.Name, out var jsonElement))
                {
                    // Property not found in JSON - field is not present
                    return new ConfigFieldState(property.Name, null, false, property.PropertyType, description);
                }

                // Try to deserialize the JSON value to the expected type
                try
                {
                    var value = jsonElement.Deserialize(property.PropertyType);
                    return new ConfigFieldState(property.Name, value, true, property.PropertyType, description);
                }
                catch (JsonException)
                {
                    // Deserialization failed - keep raw value for validation error display
                    var rawValue = jsonElement.GetRawText();
                    return new ConfigFieldState(property.Name, rawValue, true, property.PropertyType, description);
                }
            }
            catch (Exception)
            {
                // Any other error (IO, JSON parsing) - treat as not present
                return new ConfigFieldState(property.Name, null, false, property.PropertyType, description);
            }
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
        {
            // Try exact match first
            if (element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            // Try case-insensitive match
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static string GetPropertyDescription(PropertyInfo property)
        {
            var descriptionAttr = property.GetCustomAttribute<DescriptionAttribute>();
            return descriptionAttr?.Description ?? property.Name;
        }
    }
}
