using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Orchestrates configuration validation and remediation using the field-driven system.
    /// Iterates over all configuration sections, validates them, and runs first-time setup for any invalid sections.
    /// </summary>
    public class ConfigRemediationService : IConfigRemediationService
    {
        private readonly IConfigManager _configManager;
        private readonly IConfigSectionValidatorsFactory _validatorsFactory;
        private readonly IConfigSectionFirstTimeSetupFactory _firstTimeSetupFactory;
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the ConfigRemediationService class.
        /// </summary>
        /// <param name="configManager">Configuration manager for loading and saving sections</param>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="firstTimeSetupFactory">Factory for creating first-time setup services</param>
        /// <param name="logger">Optional logger for operation tracking</param>
        public ConfigRemediationService(
            IConfigManager configManager,
            IConfigSectionValidatorsFactory validatorsFactory,
            IConfigSectionFirstTimeSetupFactory firstTimeSetupFactory,
            IAppLogger? logger = null)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _validatorsFactory = validatorsFactory ?? throw new ArgumentNullException(nameof(validatorsFactory));
            _firstTimeSetupFactory = firstTimeSetupFactory ?? throw new ArgumentNullException(nameof(firstTimeSetupFactory));
            _logger = logger;
        }

        /// <summary>
        /// Runs the complete configuration remediation process.
        /// Validates all sections and runs first-time setup for any invalid ones.
        /// </summary>
        /// <returns>True if all configuration issues were resolved, false otherwise</returns>
        public async Task<bool> RemediateConfigurationAsync()
        {
            _logger?.Info("Starting configuration remediation process");

            try
            {
                var sectionConfigTypes = Enum.GetValues<ConfigSectionTypes>();
                var allSectionFields = new Dictionary<ConfigSectionTypes, List<ConfigFieldState>>();
                var allUpdatedConfigs = new Dictionary<ConfigSectionTypes, IConfigSection>();

                // Load field states for all sections
                foreach (var sectionType in sectionConfigTypes)
                {
                    _logger?.Debug("Loading field states for section: {0}", sectionType);
                    var sectionFields = await _configManager.GetSectionFieldsAsync(sectionType);
                    allSectionFields[sectionType] = sectionFields;
                }

                // Validate and fix each section
                foreach (var sectionType in allSectionFields.Keys)
                {
                    _logger?.Debug("Validating section: {0}", sectionType);
                    var fields = allSectionFields[sectionType];
                    var validator = _validatorsFactory.GetValidator(sectionType);
                    var validation = validator.ValidateSection(fields);

                    if (!validation.IsValid)
                    {
                        _logger?.Info("Section {0} validation failed. Missing fields: {1}",
                            sectionType, string.Join(", ", validation.MissingFields.Select(f => f.FieldName)));

                        var setupService = _firstTimeSetupFactory.GetFirstTimeSetupService(sectionType);
                        var (success, updatedConfig) = await setupService.RunSetupAsync(fields);

                        if (!success)
                        {
                            _logger?.Error("Failed to setup section {0} after first attempt", sectionType);
                            return false;
                        }

                        if (updatedConfig != null)
                        {
                            allUpdatedConfigs[sectionType] = updatedConfig;
                            _logger?.Info("Successfully updated section: {0}", sectionType);
                        }
                    }
                    else
                    {
                        _logger?.Debug("Section {0} is valid, no remediation needed", sectionType);
                    }
                }

                // Save all updated sections
                if (allUpdatedConfigs.Any())
                {
                    _logger?.Info("Saving {0} updated configuration sections", allUpdatedConfigs.Count);
                    foreach (var sectionType in allUpdatedConfigs.Keys)
                    {
                        var updatedConfig = allUpdatedConfigs[sectionType];
                        await _configManager.SaveSectionAsync(sectionType, updatedConfig);
                        _logger?.Debug("Saved updated section: {0}", sectionType);
                    }
                }

                _logger?.Info("Configuration remediation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.ErrorWithException("Configuration remediation failed", ex);
                return false;
            }
        }

        /// <summary>
        /// Validates all configuration sections without attempting remediation.
        /// </summary>
        /// <returns>True if all sections are valid, false if any issues are found</returns>
        public async Task<bool> ValidateConfigurationAsync()
        {
            _logger?.Info("Starting configuration validation");

            try
            {
                var sectionConfigTypes = Enum.GetValues<ConfigSectionTypes>();
                var allValid = true;

                foreach (var sectionType in sectionConfigTypes)
                {
                    _logger?.Debug("Validating section: {0}", sectionType);
                    var sectionFields = await _configManager.GetSectionFieldsAsync(sectionType);
                    var validator = _validatorsFactory.GetValidator(sectionType);
                    var validation = validator.ValidateSection(sectionFields);

                    if (!validation.IsValid)
                    {
                        _logger?.Warning("Section {0} validation failed. Missing fields: {1}",
                            sectionType, string.Join(", ", validation.MissingFields.Select(f => f.FieldName)));
                        allValid = false;
                    }
                    else
                    {
                        _logger?.Debug("Section {0} is valid", sectionType);
                    }
                }

                _logger?.Info("Configuration validation completed. All valid: {0}", allValid);
                return allValid;
            }
            catch (Exception ex)
            {
                _logger?.ErrorWithException("Configuration validation failed", ex);
                return false;
            }
        }
    }
}
