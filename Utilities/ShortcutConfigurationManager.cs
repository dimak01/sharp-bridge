using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of IShortcutConfigurationManager for managing keyboard shortcut configurations
    /// </summary>
    public class ShortcutConfigurationManager : IShortcutConfigurationManager
    {
        private readonly IShortcutParser _parser;
        private readonly IAppLogger _logger;
        private readonly Dictionary<ShortcutAction, (ConsoleKey Key, ConsoleModifiers Modifiers)?> _mappedShortcuts = new();
        private readonly List<string> _configurationIssues = new();

        /// <summary>
        /// Initializes a new instance of the ShortcutConfigurationManager
        /// </summary>
        /// <param name="parser">Shortcut parser for converting strings to key combinations</param>
        /// <param name="logger">Logger for recording configuration issues</param>
        public ShortcutConfigurationManager(IShortcutParser parser, IAppLogger logger)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the currently mapped shortcuts. Null values indicate disabled shortcuts.
        /// </summary>
        /// <returns>Dictionary mapping shortcut actions to their key combinations (or null if disabled)</returns>
        public Dictionary<ShortcutAction, (ConsoleKey Key, ConsoleModifiers Modifiers)?> GetMappedShortcuts()
        {
            return new Dictionary<ShortcutAction, (ConsoleKey Key, ConsoleModifiers Modifiers)?>(_mappedShortcuts);
        }

        /// <summary>
        /// Gets a list of configuration issues encountered during loading
        /// </summary>
        /// <returns>List of human-readable error messages for display in help system</returns>
        public List<string> GetConfigurationIssues()
        {
            return new List<string>(_configurationIssues);
        }

        /// <summary>
        /// Loads shortcut configuration from the provided application configuration
        /// </summary>
        /// <param name="config">Application configuration containing shortcut definitions</param>
        public void LoadFromConfiguration(ApplicationConfig config)
        {
            _configurationIssues.Clear();
            _mappedShortcuts.Clear();

            var defaultShortcuts = GetDefaultShortcuts();
            var configShortcuts = config?.Shortcuts ?? new Dictionary<string, string>();

            // Track used key combinations to detect duplicates
            var usedCombinations = new Dictionary<(ConsoleKey, ConsoleModifiers), ShortcutAction>();

            foreach (var action in Enum.GetValues<ShortcutAction>())
            {
                var actionName = action.ToString();

                // Try to get shortcut from config, fallback to default
                var shortcutString = configShortcuts.TryGetValue(actionName, out var configValue)
                    ? configValue
                    : defaultShortcuts.TryGetValue(action, out var defaultValue)
                        ? defaultValue
                        : null;

                if (string.IsNullOrWhiteSpace(shortcutString))
                {
                    _mappedShortcuts[action] = null;
                    _configurationIssues.Add($"{GetActionDisplayName(action)}: No shortcut defined");
                    _logger.Debug("No shortcut defined for action: {0}", action);
                    continue;
                }

                var parsed = _parser.ParseShortcut(shortcutString);
                if (parsed == null)
                {
                    _mappedShortcuts[action] = null;
                    _configurationIssues.Add($"{GetActionDisplayName(action)}: Invalid shortcut format '{shortcutString}'");
                    _logger.Warning("Invalid shortcut format for {0}: {1}", action, shortcutString);
                    continue;
                }

                var (key, modifiers) = parsed.Value;
                var combination = (key, modifiers);

                // Check for duplicate key combinations
                if (usedCombinations.TryGetValue(combination, out var existingAction))
                {
                    _mappedShortcuts[action] = null;
                    _configurationIssues.Add($"{GetActionDisplayName(action)}: Shortcut '{shortcutString}' conflicts with {GetActionDisplayName(existingAction)}");
                    _logger.Warning("Duplicate shortcut {0} for actions {1} and {2}, disabling {1}", shortcutString, action, existingAction);
                    continue;
                }

                // Successfully mapped
                _mappedShortcuts[action] = parsed;
                usedCombinations[combination] = action;
                _logger.Debug("Mapped shortcut {0} to action {1}", shortcutString, action);
            }

            // Log summary
            var enabledCount = _mappedShortcuts.Values.Count(v => v != null);
            var totalCount = _mappedShortcuts.Count;
            _logger.Info("Loaded {0}/{1} shortcuts successfully, {2} issues found", enabledCount, totalCount, _configurationIssues.Count);
        }

        /// <summary>
        /// Gets the default shortcut mappings used when no configuration is provided
        /// </summary>
        /// <returns>Dictionary of default shortcut action to string mappings</returns>
        public Dictionary<ShortcutAction, string> GetDefaultShortcuts()
        {
            return new Dictionary<ShortcutAction, string>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = "Alt+T",
                [ShortcutAction.CyclePCClientVerbosity] = "Alt+P",
                [ShortcutAction.CyclePhoneClientVerbosity] = "Alt+O",
                [ShortcutAction.ReloadTransformationConfig] = "Alt+K",
                [ShortcutAction.OpenConfigInEditor] = "Ctrl+Alt+E",
                [ShortcutAction.ShowSystemHelp] = "F1"
            };
        }

        /// <summary>
        /// Gets a human-readable display name for a shortcut action using Description attributes
        /// </summary>
        private static string GetActionDisplayName(ShortcutAction action)
        {
            return AttributeHelper.GetDescription(action);
        }
    }
}