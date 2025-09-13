// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;

namespace SharpBridge.Interfaces.UI.Components
{
    /// <summary>
    /// Interface for handling keyboard input and shortcuts in the application
    /// </summary>
    public interface IKeyboardInputHandler
    {
        /// <summary>
        /// Checks for keyboard input and executes appropriate actions
        /// </summary>
        void CheckForKeyboardInput();

        /// <summary>
        /// Registers a keyboard shortcut with an associated action
        /// </summary>
        /// <param name="key">The key to listen for</param>
        /// <param name="modifiers">The modifier keys required (Alt, Ctrl, etc.)</param>
        /// <param name="action">The action to execute when the shortcut is pressed</param>
        /// <param name="description">Description of what the shortcut does</param>
        void RegisterShortcut(ConsoleKey key, ConsoleModifiers modifiers, Action action, string description);

        /// <summary>
        /// Gets a list of all registered shortcuts and their descriptions
        /// </summary>
        /// <returns>Array of tuples containing shortcut key, modifiers, and description</returns>
        (ConsoleKey Key, ConsoleModifiers Modifiers, string Description)[] GetRegisteredShortcuts();
    }
}