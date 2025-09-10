using System;
using System.Collections.Generic;
using SharpBridge.Models;
using SharpBridge.Models.Domain;

namespace SharpBridge.UI.Utilities
{
    /// <summary>
    /// Custom equality comparer for Shortcut objects
    /// Keeps comparison logic separate from the Shortcut class
    /// </summary>
    public class ShortcutComparer : IEqualityComparer<Shortcut>
    {
        /// <summary>
        /// Singleton instance for use with dictionaries and collections
        /// </summary>
        public static readonly ShortcutComparer Instance = new();

        /// <summary>
        /// Determines whether two Shortcut objects are equal
        /// </summary>
        /// <param name="x">The first Shortcut to compare</param>
        /// <param name="y">The second Shortcut to compare</param>
        /// <returns>True if the shortcuts have the same key and modifiers</returns>
        public bool Equals(Shortcut? x, Shortcut? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.Key == y.Key && x.Modifiers == y.Modifiers;
        }

        /// <summary>
        /// Returns a hash code for the specified Shortcut
        /// </summary>
        /// <param name="obj">The Shortcut for which to get a hash code</param>
        /// <returns>A hash code for the specified object</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null</exception>
        public int GetHashCode(Shortcut obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return HashCode.Combine(obj.Key, obj.Modifiers);
        }
    }
}