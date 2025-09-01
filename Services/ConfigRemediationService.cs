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
        private readonly IConfigSectionRemediationServiceFactory _remediationFactory;
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the ConfigRemediationService class.
        /// </summary>
        /// <param name="configManager">Configuration manager for loading and saving sections</param>
        /// <param name="remediationFactory">Factory for creating remediation services</param>
        /// <param name="logger">Optional logger for operation tracking</param>
        public ConfigRemediationService(
            IConfigManager configManager,
            IConfigSectionRemediationServiceFactory remediationFactory,
            IAppLogger? logger = null)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _remediationFactory = remediationFactory ?? throw new ArgumentNullException(nameof(remediationFactory));
            _logger = logger;
        }

        /// <summary>
        /// Remediates all configuration sections by delegating validation and remediation to section services.
        /// </summary>
        /// <returns>True if all configuration issues were resolved, false otherwise</returns>
        public async Task<bool> RemediateConfigurationAsync()
        {
            _logger?.Info("Starting configuration remediation process");
            Dictionary<ConfigSectionTypes, IConfigSection> updatedConfigSections = new();
            try
            {
                // Process each configuration section in order
                var sectionConfigTypes = Enum.GetValues<ConfigSectionTypes>();

                foreach (var sectionType in sectionConfigTypes){
                    // Load current field state for this section
                    var fields = await _configManager.GetSectionFieldsAsync(sectionType);

                    // Always attempt remediation; section services will validate first and no-op if already valid
                    var remediationService = _remediationFactory.GetRemediationService(sectionType);
                    var (success, updatedConfig) = await remediationService.Remediate(fields);

                    if (!success)
                    {
                        _logger?.Error($"Remediation aborted for section {sectionType}");
                        return false;
                    }

                    if (updatedConfig != null) {
                        updatedConfigSections[sectionType] = updatedConfig!;
                    }
                }


                foreach(var sectionType in updatedConfigSections.Keys) {
                    await _configManager.SaveSectionAsync(sectionType, updatedConfigSections[sectionType]);
                    _logger?.Info($"Saved configuration for {sectionType}");
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
    }
}
