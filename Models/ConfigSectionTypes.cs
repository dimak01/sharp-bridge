namespace SharpBridge.Models
{
    /// <summary>
    /// Enumeration of all configuration section types in the application.
    /// Used for type-safe identification of configuration sections during validation and remediation.
    /// </summary>
    public enum ConfigSectionTypes
    {
        /// <summary>
        /// VTube Studio PC client configuration
        /// </summary>
        VTubeStudioPCConfig,

        /// <summary>
        /// VTube Studio Phone client configuration
        /// </summary>
        VTubeStudioPhoneClientConfig,

        /// <summary>
        /// General application settings configuration
        /// </summary>
        GeneralSettingsConfig,

        /// <summary>
        /// Transformation engine configuration
        /// </summary>
        TransformationEngineConfig
    }
}
