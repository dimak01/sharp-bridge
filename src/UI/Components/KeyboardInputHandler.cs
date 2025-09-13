using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Interfaces.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpBridge.UI.Components
{
    /// <summary>
    /// Handles keyboard input and shortcuts for the application
    /// </summary>
    public class KeyboardInputHandler : IKeyboardInputHandler
    {
        private readonly IAppLogger _logger;
        private readonly Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), (Action Action, string Description)> _shortcuts = new();

        /// <summary>
        /// Initializes a new instance of the KeyboardInputHandler class
        /// </summary>
        /// <param name="logger">The logger to use for error reporting</param>
        public KeyboardInputHandler(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks for keyboard input and executes the associated action if a shortcut is pressed
        /// </summary>
        public void CheckForKeyboardInput()
        {
            if (!Console.KeyAvailable)
                return;

            try
            {
                var keyInfo = Console.ReadKey(true); // true means don't echo to screen

                var shortcutKey = (keyInfo.Key, keyInfo.Modifiers);
                if (_shortcuts.TryGetValue(shortcutKey, out var shortcut))
                {
                    _logger.Debug("Executing shortcut: {0} + {1}", keyInfo.Modifiers, keyInfo.Key);
                    shortcut.Action();
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error processing keyboard input", ex);
            }
        }

        /// <summary>
        /// Registers a keyboard shortcut with an associated action
        /// </summary>
        /// <param name="key">The key to listen for</param>
        /// <param name="modifiers">The modifier keys required (Alt, Ctrl, etc.)</param>
        /// <param name="action">The action to execute when the shortcut is pressed</param>
        /// <param name="description">Description of what the shortcut does</param>
        public void RegisterShortcut(ConsoleKey key, ConsoleModifiers modifiers, Action action, string description)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be null or whitespace", nameof(description));

            var shortcutKey = (key, modifiers);
            _shortcuts[shortcutKey] = (action, description);

            _logger.Debug("Registered shortcut: {0} + {1} - {2}", modifiers, key, description);
        }

        /// <summary>
        /// Gets a list of all registered shortcuts and their descriptions
        /// </summary>
        /// <returns>Array of tuples containing shortcut key, modifiers, and description</returns>
        public (ConsoleKey Key, ConsoleModifiers Modifiers, string Description)[] GetRegisteredShortcuts()
        {
            return _shortcuts.Select(s => (s.Key.Key, s.Key.Modifiers, s.Value.Description)).ToArray();
        }
    }
}