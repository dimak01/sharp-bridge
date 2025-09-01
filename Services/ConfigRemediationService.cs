using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Service that orchestrates configuration validation and remediation.
    /// Iterates over all configuration sections, validates them, and runs remediation for any invalid sections.
    /// </summary>
    public class ConfigRemediationService : IConfigRemediationService
    {
        private readonly IConfigManager _configManager;
        private readonly IConfigSectionValidatorsFactory _validatorsFactory;
        private readonly IConfigSectionRemediationFactory _remediationFactory;
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the ConfigRemediationService class.
        /// </summary>
        /// <param name="configManager">Configuration manager for loading and saving sections</param>
        /// <param name="validatorsFactory">Factory for creating section validators</param>
        /// <param name="remediationFactory">Factory for creating remediation services</param>
        /// <param name="logger">Optional logger for operation tracking</param>
        public ConfigRemediationService(
            IConfigManager configManager,
            IConfigSectionValidatorsFactory validatorsFactory,
            IConfigSectionRemediationFactory remediationFactory,
            IAppLogger? logger = null)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _validatorsFactory = validatorsFactory ?? throw new ArgumentNullException(nameof(validatorsFactory));
            _remediationFactory = remediationFactory ?? throw new ArgumentNullException(nameof(remediationFactory));
            _logger = logger;
        }

        /// <summary>
        /// Remediates all configuration sections by validating them and running remediation for any invalid ones.
        /// </summary>
        /// <returns>True if all configuration issues were resolved, false otherwise</returns>
        public async Task<bool> RemediateConfigurationAsync()
        {
            _logger?.Info("Starting configuration remediation process");

            try
            {
                // Get all configuration section types
                var sectionConfigTypes = Enum.GetValues<ConfigSectionTypes>();
                var allSectionFields = new Dictionary<ConfigSectionTypes, List<ConfigFieldState>>();
                var allUpdatedConfigs = new Dictionary<ConfigSectionTypes, IConfigSection>();

                // Load field states for all sections
                foreach (var sectionType in sectionConfigTypes)
                {
                    var fields = await _configManager.GetSectionFieldsAsync(sectionType);
                    allSectionFields[sectionType] = fields;
                }

                // Validate each section
                foreach (var (sectionType, fields) in allSectionFields)
                {
                    _logger?.Info($"Validating {sectionType} configuration section");

                    var validator = _validatorsFactory.GetValidator(sectionType);
                    var validation = validator.ValidateSection(fields);

                    if (!validation.IsValid)
                    {
                        _logger?.Warning($"Configuration section {sectionType} has validation issues: " +
                            string.Join(", ", validation.Issues.Select(f => f.FieldName)));

                        var remediationService = _remediationFactory.GetRemediationService(sectionType);
                        var (success, updatedConfig) = await remediationService.Remediate(fields);

                        if (!success)
                        {
                            _logger?.Error($"Failed to remediate configuration section {sectionType}");
                            return false;
                        }

                        if (updatedConfig != null)
                        {
                            allUpdatedConfigs[sectionType] = updatedConfig;
                        }
                    }
                }

                // If we have updated configs, save them
                if (allUpdatedConfigs.Any())
                {
                    _logger?.Info("Saving remediated configuration sections");

                    foreach (var (sectionType, updatedConfig) in allUpdatedConfigs)
                    {
                        await _configManager.SaveSectionAsync(sectionType, updatedConfig);
                        _logger?.Info($"Saved remediated configuration for {sectionType}");
                    }
                }

                _logger?.Info("Configuration remediation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Configuration remediation failed: {ex.Message}");
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
                            sectionType, string.Join(", ", validation.Issues.Select(f => f.FieldName)));
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
