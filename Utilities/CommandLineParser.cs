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
        public const string ConfigDirectory = "Configs";
        public const string TransformConfigFilename = "default_transform.json";
        public const string PCConfigFilename = "VTubeStudioPCConfig.json";
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
        public string ConfigDirectory { get; set; }
        
        /// <summary>
        /// Gets or sets the transform configuration filename
        /// </summary>
        public string TransformConfigFilename { get; set; }
        
        /// <summary>
        /// Gets or sets the PC configuration filename
        /// </summary>EnsureConfigDirectoryExists
        public string PCConfigFilename { get; set; }
        
        /// <summary>
        /// Gets or sets the phone configuration filename
        /// </summary>
        public string PhoneConfigFilename { get; set; }
        
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

    /// <summary>
    /// Handles parsing of command-line arguments and configuration
    /// </summary>
    public class CommandLineParser
    {
        /// <summary>
        /// Parses command-line arguments and returns the configuration options
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>The parsed command line options</returns>
        public async Task<CommandLineOptions> ParseAsync(string[] args)
        {
            // Create command line options
            var configDirOption = new Option<string>(
                aliases: new[] { "--config-dir" },
                description: "Configuration directory path",
                getDefaultValue: () => CommandLineDefaults.ConfigDirectory);

            var transformOption = new Option<string>(
                aliases: new[] { "--transform-config" },
                description: "Transform configuration filename",
                getDefaultValue: () => CommandLineDefaults.TransformConfigFilename);

            var pcConfigOption = new Option<string>(
                aliases: new[] { "--pc-config" },
                description: "PC configuration filename",
                getDefaultValue: () => CommandLineDefaults.PCConfigFilename);

            var phoneConfigOption = new Option<string>(
                aliases: new[] { "--phone-config" },
                description: "Phone configuration filename",
                getDefaultValue: () => CommandLineDefaults.PhoneConfigFilename);

            // Build the root command
            var rootCommand = new RootCommand("Sharp Bridge - VTube Studio iPhone to PC Bridge");
            rootCommand.AddOption(configDirOption);
            rootCommand.AddOption(transformOption);
            rootCommand.AddOption(pcConfigOption);
            rootCommand.AddOption(phoneConfigOption);

            var options = new CommandLineOptions();

            rootCommand.SetHandler((string configDir, string transform, string pcConfig, string phoneConfig) =>
            {
                options.ConfigDirectory = configDir;
                options.TransformConfigFilename = transform;
                options.PCConfigFilename = pcConfig;
                options.PhoneConfigFilename = phoneConfig;
            },
            configDirOption, transformOption, pcConfigOption, phoneConfigOption);

            await rootCommand.InvokeAsync(args);
            
            // Validate the options
            options.Validate();

            return options;
        }
    }
} 