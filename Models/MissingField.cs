namespace SharpBridge.Models
{
    /// <summary>
    /// Identifies fields that are missing or require user configuration
    /// </summary>
    public enum MissingField
    {
        /// <summary>
        /// iPhone IP address is not set or invalid
        /// </summary>
        PhoneIpAddress,

        /// <summary>
        /// iPhone port is not set or invalid
        /// </summary>
        PhonePort,

        /// <summary>
        /// PC host address is not set or requires confirmation
        /// </summary>
        PCHost,

        /// <summary>
        /// PC port is not set or requires confirmation
        /// </summary>
        PCPort
    }
}
