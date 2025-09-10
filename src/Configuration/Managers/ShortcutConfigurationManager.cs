using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Interfaces.Infrastructure;
using SharpBridge.Models.Domain;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Infrastructure;
using SharpBridge.UI.Utilities;
using SharpBridge.UI.Providers;
using SharpBridge.Configuration.Utilities;

namespace SharpBridge.Configuration.Managers
{
    /// <summary>
    /// Implementation of IShortcutConfigurationManager for managing keyboard shortcut configurations
    /// </summary>
    public class ShortcutConfigurationManager : IShortcutConfigurationManager, IDisposable
    {
        private readonly IShortcutParser _parser;
        private readonly IAppLogger _logger;
        private readonly IConfigManager _configManager;
        private readonly IFileChangeWatcher? _appConfigWatcher;

        // Core storage
        private readonly Dictionary<ShortcutAction, Shortcut?> _mappedShortcuts = new();

        // Debugging support
        private readonly Dictionary<ShortcutAction, string> _incorrectShortcuts = new();
        private readonly HashSet<ShortcutAction> _explicitlyDisabled = new();

        // Legacy support
        private readonly GeneralSettingsConfig _config;

        // Disposal tracking
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the ShortcutConfigurationManager
        /// </summary>
        /// <param name="parser">Shortcut parser for converting strings to key combinations</param>
        /// <param name="logger">Logger for recording configuration issues</param>
        /// <param name="configManager">Configuration manager for loading configs</param>
        /// <param name="appConfigWatcher">Optional file watcher for application config changes</param>
        public ShortcutConfigurationManager(IShortcutParser parser, IAppLogger logger, IConfigManager configManager, IFileChangeWatcher? appConfigWatcher = null)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _appConfigWatcher = appConfigWatcher;

            // Initialize with defaults - ensures shortcuts are always available
            InitializeWithDefaults();

            // Load initial config
            _config = _configManager.LoadSectionAsync<GeneralSettingsConfig>().GetAwaiter().GetResult();

            // Subscribe to application config changes if watcher is provided
            if (_appConfigWatcher != null)
            {
                _appConfigWatcher.FileChanged += OnApplicationConfigChanged;
            }
        }

        /// <summary>
        /// Releases all resources used by the shortcut configuration manager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the shortcut configuration manager
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _logger.Debug("Disposing ShortcutConfigurationManager");

                // Unsubscribe from file watcher events
                if (_appConfigWatcher != null)
                {
                    _appConfigWatcher.FileChanged -= OnApplicationConfigChanged;
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Initializes the shortcut mappings with default values
        /// </summary>
        private void InitializeWithDefaults()
        {
            var defaults = GetDefaultShortcuts();
            foreach (var (action, shortcut) in defaults)
            {
                _mappedShortcuts[action] = shortcut;
            }
        }

        /// <summary>
        /// Gets the currently mapped shortcuts. Null values indicate disabled shortcuts.
        /// </summary>
        /// <returns>Dictionary mapping shortcut actions to their shortcuts (or null if disabled)</returns>
        public Dictionary<ShortcutAction, Shortcut?> GetMappedShortcuts()
        {
            return new Dictionary<ShortcutAction, Shortcut?>(_mappedShortcuts);
        }

        /// <summary>
        /// Gets the original invalid shortcut strings for debugging purposes
        /// </summary>
        /// <returns>Dictionary mapping shortcut actions to their invalid configuration strings</returns>
        public Dictionary<ShortcutAction, string> GetIncorrectShortcuts()
        {
            return new Dictionary<ShortcutAction, string>(_incorrectShortcuts);
        }

        /// <summary>
        /// Gets the display string for a shortcut action (e.g., "Alt+T", "None", "Ctrl+K (Invalid)")
        /// </summary>
        /// <param name="action">The shortcut action</param>
        /// <returns>Human-readable shortcut string for display</returns>
        public string GetDisplayString(ShortcutAction action)
        {
            var mappedShortcuts = GetMappedShortcuts();
            var incorrectShortcuts = GetIncorrectShortcuts();

            if (mappedShortcuts[action] != null)
            {
                return _parser.FormatShortcut(mappedShortcuts[action]!);
            }

            if (incorrectShortcuts.TryGetValue(action, out var invalidString))
            {
                return $"{invalidString} (Invalid)";
            }

            return "None";
        }

        /// <summary>
        /// Gets the status of a specific shortcut action
        /// </summary>
        /// <param name="action">The shortcut action to check</param>
        /// <returns>The current status of the shortcut</returns>
        public ShortcutStatus GetShortcutStatus(ShortcutAction action)
        {
            if (_mappedShortcuts[action] != null)
                return ShortcutStatus.Active;

            if (_incorrectShortcuts.ContainsKey(action))
                return ShortcutStatus.Invalid;

            return ShortcutStatus.ExplicitlyDisabled;
        }

        /// <summary>
        /// Loads shortcut configuration from the provided application configuration
        /// </summary>
        /// <param name="config">Application configuration containing shortcut definitions</param>
        public void LoadFromConfiguration(GeneralSettingsConfig config)
        {
            _mappedShortcuts.Clear();
            _incorrectShortcuts.Clear();
            _explicitlyDisabled.Clear();

            // If shortcuts dictionary is empty, populate it with defaults for serialization
            if (config?.Shortcuts != null && config.Shortcuts.Count == 0)
            {
                var defaults = GetDefaultShortcuts();
                foreach (var (action, shortcut) in defaults)
                {
                    config.Shortcuts[action.ToString()] = _parser.FormatShortcut(shortcut);
                }
            }

            var defaultShortcuts = GetDefaultShortcuts();
            var configShortcuts = config?.Shortcuts;
            var usedCombinations = new Dictionary<Shortcut, ShortcutAction>(ShortcutComparer.Instance);

            foreach (var action in Enum.GetValues<ShortcutAction>())
            {
                var actionName = action.ToString();

                // Determine source: config vs defaults vs missing
                Shortcut? shortcut;
                string? originalString;

                if (configShortcuts == null)
                {
                    // No config - use defaults
                    shortcut = defaultShortcuts[action];
                    originalString = _parser.FormatShortcut(shortcut);
                }
                else
                {
                    // Config exists - only use explicitly defined shortcuts
                    originalString = configShortcuts.TryGetValue(actionName, out var configValue) ? configValue : null;

                    if (string.IsNullOrWhiteSpace(originalString))
                    {
                        shortcut = null;
                    }
                    else
                    {
                        shortcut = _parser.ParseShortcut(originalString);
                    }
                }

                // Handle disabled shortcuts
                if (shortcut == null)
                {
                    _mappedShortcuts[action] = null;

                    if (string.IsNullOrWhiteSpace(originalString))
                    {
                        _explicitlyDisabled.Add(action);

                        _logger.Debug("No shortcut defined for action: {0}", action);
                    }
                    else
                    {
                        _incorrectShortcuts[action] = originalString;

                        _logger.Warning("Invalid shortcut format for {0}: {1}", action, originalString);
                    }
                    continue;
                }

                // Handle conflicts - simple resolution: first valid wins
                if (usedCombinations.TryGetValue(shortcut, out var conflictingAction))
                {
                    _mappedShortcuts[action] = null;
                    _incorrectShortcuts[action] = originalString!; // Treat as invalid due to conflict

                    _logger.Warning("Duplicate shortcut {0} for actions {1} and {2}, disabling {1}", originalString!, action, conflictingAction);
                    continue;
                }

                // Success - register the shortcut
                _mappedShortcuts[action] = shortcut;
                usedCombinations[shortcut] = action;
                _logger.Debug("Mapped shortcut {0} to action {1}", originalString!, action);
            }

            // Log summary
            var enabledCount = _mappedShortcuts.Values.Count(v => v != null);
            var totalCount = _mappedShortcuts.Count;
            _logger.Info("Loaded {0}/{1} shortcuts successfully, {2} issues found",
                        enabledCount, totalCount, _incorrectShortcuts.Count + _explicitlyDisabled.Count);
        }

        /// <summary>
        /// Gets the default shortcuts for all actions
        /// </summary>
        /// <returns>Dictionary mapping actions to their default shortcuts</returns>
        public Dictionary<ShortcutAction, Shortcut> GetDefaultShortcuts()
        {
            return DefaultShortcutsProvider.GetDefaultShortcuts();
        }

        /// <summary>
        /// Handles application configuration changes
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The file change event arguments</param>
        private async void OnApplicationConfigChanged(object? sender, FileChangeEventArgs e)
        {
            try
            {
                _logger.Debug("Application config changed, checking if general settings were affected");

                // Load new config and compare general settings section
                var newConfig = await _configManager.LoadSectionAsync<GeneralSettingsConfig>();
                if (!ConfigComparers.GeneralSettingsEqual(_config, newConfig))
                {
                    _logger.Info("General settings changed, updating internal config and reloading shortcuts");
                    UpdateConfig(newConfig);
                    LoadFromConfiguration(newConfig);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error handling application config change: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Updates the internal configuration with new values
        /// </summary>
        /// <param name="newGeneralSettings">The new general settings configuration</param>
        private void UpdateConfig(GeneralSettingsConfig newGeneralSettings)
        {
            // Update all config properties
            _config.EditorCommand = newGeneralSettings.EditorCommand;
            _config.Shortcuts = newGeneralSettings.Shortcuts;

            _logger.Debug("General settings config updated");
        }
    }
}