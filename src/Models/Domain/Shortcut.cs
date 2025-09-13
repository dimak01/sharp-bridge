using System;

namespace SharpBridge.Models.Domain
{
    /// <summary>
    /// Represents a keyboard shortcut with a key and modifier combination
    /// </summary>
    public class Shortcut
    {
        /// <summary>
        /// The primary key for this shortcut
        /// </summary>
        public ConsoleKey Key { get; }

        /// <summary>
        /// The modifier keys (Alt, Ctrl, Shift) for this shortcut
        /// </summary>
        public ConsoleModifiers Modifiers { get; }

        /// <summary>
        /// Initializes a new instance of the Shortcut class
        /// </summary>
        /// <param name="key">The primary key</param>
        /// <param name="modifiers">The modifier keys</param>
        public Shortcut(ConsoleKey key, ConsoleModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        /// <summary>
        /// Creates a Shortcut from a ConsoleKeyInfo (convenience method for KeyboardInputHandler)
        /// </summary>
        /// <param name="keyInfo">The key information from Console.ReadKey()</param>
        /// <returns>A new Shortcut instance</returns>
        public static Shortcut FromKeyInfo(ConsoleKeyInfo keyInfo) =>
            new(keyInfo.Key, keyInfo.Modifiers);
    }
}