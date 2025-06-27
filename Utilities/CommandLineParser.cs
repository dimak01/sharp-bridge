using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Default configuration values for command-line options
    /// </summary>
    public static class CommandLineDefaults
    {
        /// <summary>
        /// Default configuration directory name
        /// </summary>
        public const string ConfigDirectory = "Configs";

        /// <summary>
        /// Default transform configuration filename
        /// </summary>
        public const string TransformConfigFilename = "vts_transforms.json";

        /// <summary>
        /// Default PC configuration filename
        /// </summary>
        public const string PCConfigFilename = "VTubeStudioPCConfig.json";

        /// <summary>
        /// Default phone configuration filename
        /// </summary>
        public const string PhoneConfigFilename = "VTubeStudioPhoneConfig.json";
    }

    /// <summary>
    /// Results from parsing command-line arguments
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// Gets or sets the configuration directory path
        /// </summary>
        public string ConfigDirectory { get; set; } = CommandLineDefaults.ConfigDirectory;

        /// <summary>
        /// Gets or sets the transform configuration filename
        /// </summary>
        public string TransformConfigFilename { get; set; } = CommandLineDefaults.TransformConfigFilename;

        /// <summary>
        /// Gets or sets the PC configuration filename
        /// </summary>
        public string PCConfigFilename { get; set; } = CommandLineDefaults.PCConfigFilename;

        /// <summary>
        /// Gets or sets the phone configuration filename
        /// </summary>
        public string PhoneConfigFilename { get; set; } = CommandLineDefaults.PhoneConfigFilename;

        /// <summary>
        /// Gets the full path to the transform configuration file
        /// </summary>
        public string TransformConfigPath => Path.Combine(ConfigDirectory, TransformConfigFilename);

        /// <summary>
        /// Gets the full path to the PC configuration file
        /// </summary>
        public string PCConfigPath => Path.Combine(ConfigDirectory, PCConfigFilename);

        /// <summary>
        /// Gets the full path to the phone configuration file
        /// </summary>
        public string PhoneConfigPath => Path.Combine(ConfigDirectory, PhoneConfigFilename);

        /// <summary>
        /// Validates the options
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when options are invalid</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConfigDirectory))
                throw new ArgumentException("Configuration directory cannot be empty", nameof(ConfigDirectory));

            if (string.IsNullOrWhiteSpace(TransformConfigFilename))
                throw new ArgumentException("Transform configuration filename cannot be empty", nameof(TransformConfigFilename));

            if (string.IsNullOrWhiteSpace(PCConfigFilename))
                throw new ArgumentException("PC configuration filename cannot be empty", nameof(PCConfigFilename));

            if (string.IsNullOrWhiteSpace(PhoneConfigFilename))
                throw new ArgumentException("Phone configuration filename cannot be empty", nameof(PhoneConfigFilename));
        }
    }
}