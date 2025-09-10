namespace SharpBridge.Models.Infrastructure
{
    /// <summary>
    /// Represents the status of a keyboard shortcut configuration
    /// </summary>
    public enum ShortcutStatus
    {
        /// <summary>
        /// Shortcut is valid and ready to use
        /// </summary>
        Active,

        /// <summary>
        /// Shortcut parsing failed, original string preserved for debugging
        /// </summary>
        Invalid,

        /// <summary>
        /// User explicitly disabled this shortcut with "None" or "Disabled"
        /// </summary>
        ExplicitlyDisabled
    }
}