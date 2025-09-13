// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SharpBridge.Interfaces.Configuration.Extractors;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Configuration.Extractors
{
    /// <summary>
    /// Generic field extractor for any configuration section type.
    /// Extracts field states from JSON configuration files using reflection-based property discovery.
    /// </summary>
    public class ConfigSectionFieldExtractor : IConfigSectionFieldExtractor
    {
        private readonly IEnumerable<PropertyInfo> _properties;

        /// <summary>
        /// Initializes a new instance of the ConfigSectionFieldExtractor class.
        /// </summary>
        /// <param name="properties">The properties to extract from the configuration section</param>
        public ConfigSectionFieldExtractor(IEnumerable<PropertyInfo> properties)
        {
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        /// <summary>
        /// Extracts field states from the specified configuration section of the configuration file.
        /// </summary>
        /// <param name="configFilePath">Path to the ApplicationConfig.json file</param>
        /// <returns>List of field states for the configuration section</returns>
        public async Task<List<ConfigFieldState>> ExtractFieldStatesAsync(string configFilePath)
        {
            var fieldStates = new List<ConfigFieldState>();

            // Get expected field schema from the provided properties
            foreach (var property in _properties)
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

                // Navigate to the section (this will be determined by the factory)
                var sectionName = GetSectionNameFromProperty(property);
                if (!document.RootElement.TryGetProperty(sectionName, out var sectionElement))
                {
                    // Section doesn't exist - field is not present
                    return new ConfigFieldState(property.Name, null, false, property.PropertyType, description);
                }

                // Look for the property in the section (case-insensitive)
                if (!TryGetPropertyIgnoreCase(sectionElement, property.Name, out var jsonElement))
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

        private static string GetSectionNameFromProperty(PropertyInfo property)
        {
            // This is a temporary solution - the factory should pass the section name
            // For now, we'll determine it from the property's declaring type
            var declaringType = property.DeclaringType;
            if (declaringType == typeof(VTubeStudioPhoneClientConfig))
                return "PhoneClient";
            if (declaringType == typeof(VTubeStudioPCConfig))
                return "PCClient";
            if (declaringType == typeof(GeneralSettingsConfig))
                return "GeneralSettings";
            if (declaringType == typeof(TransformationEngineConfig))
                return "TransformationEngine";

            // Fallback to type name
            return declaringType?.Name ?? "Unknown";
        }
    }
}
