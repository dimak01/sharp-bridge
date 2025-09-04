using System;
using System.Collections.Generic;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Static utility class providing default keyboard shortcuts for the application.
    /// This avoids duplication between ShortcutConfigurationManager and remediation services.
    /// </summary>
    public static class DefaultShortcutsProvider
    {
        /// <summary>
        /// Gets the default keyboard shortcuts for the application.
        /// </summary>
        /// <returns>Dictionary mapping ShortcutAction to Shortcut objects</returns>
        public static Dictionary<ShortcutAction, Shortcut> GetDefaultShortcuts()
        {
            return new Dictionary<ShortcutAction, Shortcut>
            {
                [ShortcutAction.CycleTransformationEngineVerbosity] = new Shortcut(ConsoleKey.T, ConsoleModifiers.Alt),
                [ShortcutAction.CyclePCClientVerbosity] = new Shortcut(ConsoleKey.P, ConsoleModifiers.Alt),
                [ShortcutAction.CyclePhoneClientVerbosity] = new Shortcut(ConsoleKey.O, ConsoleModifiers.Alt),
                [ShortcutAction.ReloadTransformationConfig] = new Shortcut(ConsoleKey.K, ConsoleModifiers.Alt),
                [ShortcutAction.OpenConfigInEditor] = new Shortcut(ConsoleKey.E, ConsoleModifiers.Control | ConsoleModifiers.Alt),
                [ShortcutAction.ShowSystemHelp] = new Shortcut(ConsoleKey.F1, 0),
                [ShortcutAction.ShowNetworkStatus] = new Shortcut(ConsoleKey.F2, 0)
            };
        }
    }
}
