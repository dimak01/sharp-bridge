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


        /// <summary>
        /// Initializes a new instance of the ShortcutConfigurationManager
        /// </summary>
        /// <param name="parser">Shortcut parser for converting strings to key combinations</param>
        /// <param name="logger">Logger for recording configuration issues</param>
        public ShortcutConfigurationManager(IShortcutParser parser, IAppLogger logger)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize with defaults - ensures shortcuts are always available
            InitializeWithDefaults();
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
        /// Gets a human-readable display name for a shortcut action using Description attributes
        /// </summary>
        private static string GetActionDisplayName(ShortcutAction action)
        {
            return AttributeHelper.GetDescription(action);
        }
    }
}