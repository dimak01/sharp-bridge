using System;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using System.Collections.Generic;

namespace SharpBridge.Services
{
    /// <summary>
    /// Automatically validates and remediates configuration issues during service instantiation.
    /// Runs validation and first-time setup if needed, ensuring the application has valid configuration
    /// before any config-dependent services are created.
    /// </summary>
    public class ConfigRemediationService : IConfigRemediationService
    {
        private readonly IConfigManager _configManager;
        private readonly IConfigValidator _configValidator;
        private readonly IFirstTimeSetupService _firstTimeSetupService;
        private readonly IAppLogger _logger;

        /// <summary>
        /// Initializes a new instance of the ConfigRemediationService.
        /// Note: Remediation is not automatically triggered during construction.
        /// Call RemediateConfiguration() explicitly when you want to run validation and setup.
        /// </summary>
        /// <param name="configManager">Configuration manager for loading and saving configs</param>
        /// <param name="configValidator">Configuration validator for detecting issues</param>
        /// <param name="firstTimeSetupService">Service for handling first-time setup</param>
        /// <param name="logger">Application logger for recording remediation activities</param>
        public ConfigRemediationService(
            IConfigManager configManager,
            IConfigValidator configValidator,
            IFirstTimeSetupService firstTimeSetupService,
            IAppLogger logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            _firstTimeSetupService = firstTimeSetupService ?? throw new ArgumentNullException(nameof(firstTimeSetupService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs configuration validation and remediation.
        /// This method is called automatically during service construction, but is also
        /// exposed publicly for testing and explicit invocation if needed.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration cannot be remediated after 3 attempts.</exception>
        public void RemediateConfiguration()
        {
            try
            {
                _logger.Info("Starting configuration validation and remediation...");

                var appConfig = LoadApplicationConfiguration();
                var validationResult = ValidateConfiguration(appConfig);

                if (validationResult.IsValid)
                {
                    _logger.Info("Configuration validation passed - all required fields are present");
                    return;
                }

                _logger.Warning("Configuration validation failed - missing required fields: {0}",
                    string.Join(", ", validationResult.MissingFields));

                var updatedConfig = AttemptRemediationWithRetries(validationResult.MissingFields, appConfig);
                SaveRemediatedConfiguration(updatedConfig);

                _logger.Info("Configuration remediation completed successfully");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                var errorMessage = $"Unexpected error during configuration remediation: {ex.Message}";
                _logger.ErrorWithException(errorMessage, ex);
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        /// <summary>
        /// Loads the current application configuration from the config manager.
        /// </summary>
        /// <returns>The loaded application configuration.</returns>
        private ApplicationConfig LoadApplicationConfiguration()
        {
            var appConfig = _configManager.LoadApplicationConfigAsync().GetAwaiter().GetResult();
            _logger.Debug("Application configuration loaded successfully");
            return appConfig;
        }

        /// <summary>
        /// Validates the application configuration using the config validator.
        /// </summary>
        /// <param name="appConfig">The application configuration to validate.</param>
        /// <returns>The validation result.</returns>
        private ConfigValidationResult ValidateConfiguration(ApplicationConfig appConfig)
        {
            return _configValidator.ValidateConfiguration(appConfig);
        }

        /// <summary>
        /// Runs first-time setup to remediate missing configuration fields.
        /// </summary>
        /// <param name="missingFields">The fields that are missing from the configuration.</param>
        /// <param name="currentConfig">The current configuration to update.</param>
        /// <returns>The updated configuration after first-time setup.</returns>
        /// <exception cref="InvalidOperationException">Thrown when first-time setup fails.</exception>
        private ApplicationConfig RunFirstTimeSetup(IEnumerable<MissingField> missingFields, ApplicationConfig currentConfig)
        {
            _logger.Info("Starting first-time setup to remediate missing configuration fields...");

            var (setupSuccessful, updatedConfig) = _firstTimeSetupService.RunSetupAsync(
                missingFields, currentConfig).GetAwaiter().GetResult();

            if (!setupSuccessful || updatedConfig == null)
            {
                var errorMessage = "First-time setup was cancelled or failed. Sharp Bridge cannot start without the required configuration.";
                _logger.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return updatedConfig;
        }

        /// <summary>
        /// Saves the remediated configuration to the configuration file.
        /// </summary>
        /// <param name="updatedConfig">The updated configuration to save.</param>
        private void SaveRemediatedConfiguration(ApplicationConfig updatedConfig)
        {
            _logger.Info("Saving remediated configuration to file...");
            _configManager.SaveApplicationConfigAsync(updatedConfig).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Verifies that the remediation was successful by re-validating the configuration.
        /// </summary>
        /// <param name="updatedConfig">The updated configuration to verify.</param>
        /// <returns>True if verification was successful, false otherwise.</returns>
        private bool VerifyRemediationSuccess(ApplicationConfig updatedConfig)
        {
            var revalidationResult = _configValidator.ValidateConfiguration(updatedConfig);
            if (revalidationResult.IsValid)
            {
                _logger.Info("Configuration verification successful.");
                return true;
            }
            _logger.Warning("Configuration verification failed. Missing fields: {0}", string.Join(", ", revalidationResult.MissingFields));
            return false;
        }

        /// <summary>
        /// Attempts to remediate configuration issues with up to 3 retry attempts.
        /// </summary>
        /// <param name="missingFields">The fields that are missing from the configuration.</param>
        /// <param name="currentConfig">The current configuration to update.</param>
        /// <returns>The successfully remediated configuration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when all 3 attempts fail.</exception>
        private ApplicationConfig AttemptRemediationWithRetries(IEnumerable<MissingField> missingFields, ApplicationConfig currentConfig)
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _logger.Info("First-time setup attempt {0} of {1}...", attempt, maxAttempts);

                var updatedConfig = RunFirstTimeSetup(missingFields, currentConfig);

                if (VerifyRemediationSuccess(updatedConfig))
                {
                    _logger.Info("Configuration remediation successful on attempt {0}", attempt);
                    return updatedConfig;
                }
            }

            var errorMessage = $"Configuration remediation failed after {maxAttempts} attempts. Sharp Bridge cannot start without valid configuration.";
            _logger.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }
}
