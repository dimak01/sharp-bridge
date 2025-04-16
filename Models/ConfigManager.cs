using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SharpBridge.Models
{
    /// <summary>
    /// Manages configuration file loading and saving.
    /// </summary>
    public class ConfigManager
    {
        private const string DEFAULT_CONFIG_DIRECTORY = "Configs";
        private const string DEFAULT_PC_CONFIG_FILENAME = "VTubeStudioPCConfig.json";
        private const string DEFAULT_PHONE_CONFIG_FILENAME = "VTubeStudioPhoneConfig.json";
        
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _configDirectory;
        private readonly string _pcConfigFilename;
        private readonly string _phoneConfigFilename;
        
        /// <summary>
        /// Initializes a new instance of the ConfigManager class with default paths.
        /// </summary>
        public ConfigManager() : this(DEFAULT_CONFIG_DIRECTORY, DEFAULT_PC_CONFIG_FILENAME, DEFAULT_PHONE_CONFIG_FILENAME)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the ConfigManager class with custom paths.
        /// </summary>
        /// <param name="configDirectory">The directory where config files are stored</param>
        /// <param name="pcConfigFilename">Filename for the PC configuration</param>
        /// <param name="phoneConfigFilename">Filename for the Phone configuration</param>
        public ConfigManager(string configDirectory, string pcConfigFilename, string phoneConfigFilename)
        {
            _configDirectory = configDirectory ?? DEFAULT_CONFIG_DIRECTORY;
            _pcConfigFilename = pcConfigFilename ?? DEFAULT_PC_CONFIG_FILENAME;
            _phoneConfigFilename = phoneConfigFilename ?? DEFAULT_PHONE_CONFIG_FILENAME;
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
            
            EnsureConfigDirectoryExists();
        }
        
        /// <summary>
        /// Gets the path to the PC configuration file.
        /// </summary>
        public string PCConfigPath => Path.Combine(_configDirectory, _pcConfigFilename);
        
        /// <summary>
        /// Gets the path to the Phone configuration file.
        /// </summary>
        public string PhoneConfigPath => Path.Combine(_configDirectory, _phoneConfigFilename);
        
        /// <summary>
        /// Loads the PC configuration from file or creates a default one if it doesn't exist.
        /// </summary>
        /// <returns>The PC configuration.</returns>
        public async Task<VTubeStudioPCConfig> LoadPCConfigAsync()
        {
            return await LoadConfigAsync<VTubeStudioPCConfig>(PCConfigPath, () => new VTubeStudioPCConfig());
        }
        
        /// <summary>
        /// Loads the Phone configuration from file or creates a default one if it doesn't exist.
        /// </summary>
        /// <returns>The Phone configuration.</returns>
        public async Task<VTubeStudioPhoneClientConfig> LoadPhoneConfigAsync()
        {
            return await LoadConfigAsync<VTubeStudioPhoneClientConfig>(PhoneConfigPath, () => new VTubeStudioPhoneClientConfig());
        }
        
        /// <summary>
        /// Saves the PC configuration to file.
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SavePCConfigAsync(VTubeStudioPCConfig config)
        {
            await SaveConfigAsync(PCConfigPath, config);
        }
        
        /// <summary>
        /// Saves the Phone configuration to file.
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SavePhoneConfigAsync(VTubeStudioPhoneClientConfig config)
        {
            await SaveConfigAsync(PhoneConfigPath, config);
        }
        
        private async Task<T> LoadConfigAsync<T>(string path, Func<T> defaultConfigFactory) where T : class
        {
            if (!File.Exists(path))
            {
                var defaultConfig = defaultConfigFactory();
                await SaveConfigAsync(path, defaultConfig);
                return defaultConfig;
            }
            
            try
            {
                using var fileStream = File.OpenRead(path);
                var config = await JsonSerializer.DeserializeAsync<T>(fileStream, _jsonOptions);
                
                if (config == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize configuration from {path}");
                }
                
                return config;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Error parsing configuration file {path}: {ex.Message}", ex);
            }
        }
        
        private async Task SaveConfigAsync<T>(string path, T config) where T : class
        {
            try
            {
                using var fileStream = File.Create(path);
                await JsonSerializer.SerializeAsync(fileStream, config, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving configuration to {path}: {ex.Message}", ex);
            }
        }
        
        private void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }
    }
} 