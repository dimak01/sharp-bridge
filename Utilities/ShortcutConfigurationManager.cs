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

        // Core storage
        private readonly Dictionary<ShortcutAction, Shortcut?> _mappedShortcuts = new();

        // Debugging support
        private readonly Dictionary<ShortcutAction, string> _incorrectShortcuts = new();
        private readonly HashSet<ShortcutAction> _explicitlyDisabled = new();

        // Legacy support
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
        /// Gets a list of configuration issues encountered during loading (legacy method)
        /// </summary>
        /// <returns>List of human-readable error messages for display in help system</returns>
        [Obsolete("Use GetShortcutStatus() and GetIncorrectShortcuts() for better error handling")]
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
            _mappedShortcuts.Clear();
            _incorrectShortcuts.Clear();
            _explicitlyDisabled.Clear();
            _configurationIssues.Clear(); // For legacy support

            var defaultShortcuts = GetDefaultShortcuts();
            var configShortcuts = config?.Shortcuts;
            var usedCombinations = new Dictionary<(ConsoleKey, ConsoleModifiers), ShortcutAction>();

            foreach (var action in Enum.GetValues<ShortcutAction>())
            {
                var actionName = action.ToString();

                // Determine source: config vs defaults vs missing
                string? shortcutString;
                if (configShortcuts == null)
                {
                    // No config - use defaults
                    var defaultShortcut = defaultShortcuts[action];
                    shortcutString = _parser.FormatShortcut(defaultShortcut.Key, defaultShortcut.Modifiers);
                }
                else
                {
                    // Config exists - only use explicitly defined shortcuts
                    shortcutString = configShortcuts.TryGetValue(actionName, out var configValue) ? configValue : null;
                }

                if (string.IsNullOrWhiteSpace(shortcutString))
                {
                    _mappedShortcuts[action] = null;
                    _explicitlyDisabled.Add(action);
                    _configurationIssues.Add($"{GetActionDisplayName(action)}: No shortcut defined"); // Legacy
                    _logger.Debug("No shortcut defined for action: {0}", action);
                    continue;
                }

                var parsed = _parser.ParseShortcut(shortcutString);
                if (parsed == null)
                {
                    _mappedShortcuts[action] = null;
                    _incorrectShortcuts[action] = shortcutString;
                    _configurationIssues.Add($"{GetActionDisplayName(action)}: Invalid shortcut format '{shortcutString}'"); // Legacy
                    _logger.Warning("Invalid shortcut format for {0}: {1}", action, shortcutString);
                    continue;
                }

                var (key, modifiers) = parsed.Value;
                var combination = (key, modifiers);

                // Simple conflict resolution: first valid wins
                if (usedCombinations.ContainsKey(combination))
                {
                    _mappedShortcuts[action] = null;
                    _incorrectShortcuts[action] = shortcutString; // Treat as invalid
                    _configurationIssues.Add($"{GetActionDisplayName(action)}: Shortcut '{shortcutString}' conflicts with {GetActionDisplayName(usedCombinations[combination])}"); // Legacy
                    _logger.Warning("Duplicate shortcut {0} for actions {1} and {2}, disabling {1}", shortcutString, action, usedCombinations[combination]);
                    continue;
                }

                // Success
                var shortcut = new Shortcut(key, modifiers);
                _mappedShortcuts[action] = shortcut;
                usedCombinations[combination] = action;
                _logger.Debug("Mapped shortcut {0} to action {1}", shortcutString, action);
            }

            // Log summary
            var enabledCount = _mappedShortcuts.Values.Count(v => v != null);
            var totalCount = _mappedShortcuts.Count;
            _logger.Info("Loaded {0}/{1} shortcuts successfully, {2} issues found", enabledCount, totalCount, _incorrectShortcuts.Count + _explicitlyDisabled.Count);
        }

        /// <summary>
        /// Gets the default shortcut mappings used when no configuration is provided
        /// </summary>
        /// <returns>Dictionary of default shortcut action to Shortcut mappings</returns>
        public Dictionary<ShortcutAction, Shortcut> GetDefaultShortcuts()
        {
            return new Dictionary<ShortcutAction, Shortcut>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt),
                [ShortcutAction.CyclePCClientVerbosity] = new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt),
                [ShortcutAction.CyclePhoneClientVerbosity] = new Shortcut(ConsoleKey.O, ConsoleModifiers.Alt),
                [ShortcutAction.ReloadTransformationConfig] = new Shortcut(ConsoleKey.K, ConsoleModifiers.Alt),
                [ShortcutAction.OpenConfigInEditor] = new Shortcut(ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt),
                [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, 0)
            };
        }

        /// <summary>
        /// Gets the default shortcut mappings as string representations (legacy method)
        /// </summary>
        /// <returns>Dictionary of default shortcut action to string mappings</returns>
        [Obsolete("Use GetDefaultShortcuts() which returns Shortcut objects")]
        public Dictionary<ShortcutAction, string> GetDefaultShortcutsAsStrings()
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