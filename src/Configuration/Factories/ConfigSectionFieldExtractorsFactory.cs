// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using SharpBridge.Configuration.Extractors;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration.Extractors;
using SharpBridge.Interfaces.Configuration.Factories;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Configuration.Factories
{
    /// <summary>
    /// Factory implementation for creating configuration section field extractors.
    /// Provides type-safe access to field extractors for each configuration section type.
    /// </summary>
    public class ConfigSectionFieldExtractorsFactory : IConfigSectionFieldExtractorsFactory
    {
        /// <summary>
        /// Initializes a new instance of the ConfigSectionFieldExtractorsFactory class.
        /// </summary>
        public ConfigSectionFieldExtractorsFactory()
        {
        }

        /// <summary>
        /// Gets the field extractor for the specified configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section to extract fields from</param>
        /// <returns>The field extractor for the specified section type</returns>
        public IConfigSectionFieldExtractor GetExtractor(ConfigSectionTypes sectionType)
        {
            var properties = GetPropertiesForSection(sectionType);
            return new ConfigSectionFieldExtractor(properties);
        }

        /// <summary>
        /// Gets the properties for the specified configuration section type.
        /// </summary>
        /// <param name="sectionType">The type of configuration section</param>
        /// <returns>Collection of PropertyInfo objects for the section type</returns>
        private static PropertyInfo[] GetPropertiesForSection(ConfigSectionTypes sectionType)
        {
            var allProperties = sectionType switch
            {
                ConfigSectionTypes.VTubeStudioPCConfig => typeof(VTubeStudioPCConfig).GetProperties(),
                ConfigSectionTypes.VTubeStudioPhoneClientConfig => typeof(VTubeStudioPhoneClientConfig).GetProperties(),
                ConfigSectionTypes.GeneralSettingsConfig => typeof(GeneralSettingsConfig).GetProperties(),
                ConfigSectionTypes.TransformationEngineConfig => typeof(TransformationEngineConfig).GetProperties(),
                _ => throw new ArgumentException($"Unknown section type: {sectionType}", nameof(sectionType))
            };

            // Filter out properties marked with [JsonIgnore] - these are internal fields
            // that should be set from defaults, not exposed to user during remediation
            return allProperties
                .Where(p => !p.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                .ToArray();
        }
    }
}
