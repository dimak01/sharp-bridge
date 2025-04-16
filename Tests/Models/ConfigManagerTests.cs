using System;
using System.IO;
using System.Threading.Tasks;
using SharpBridge.Models;
using Xunit;
using FluentAssertions;

namespace SharpBridge.Tests.Models
{
    public class ConfigManagerTests : IDisposable
    {
        private readonly string _testDirectory = "TestConfigs";
        private readonly string _pcConfigPath;
        private readonly string _phoneConfigPath;
        private readonly ConfigManager _configManager;
        
        public ConfigManagerTests()
        {
            // Set up test directory and config manager
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
            
            Directory.CreateDirectory(_testDirectory);
            
            // Create config manager with test directory
            _configManager = new ConfigManager(_testDirectory, "VTubeStudioPCConfig.json", "VTubeStudioPhoneConfig.json");
            _pcConfigPath = Path.Combine(_testDirectory, "VTubeStudioPCConfig.json");
            _phoneConfigPath = Path.Combine(_testDirectory, "VTubeStudioPhoneConfig.json");
        }
        
        public void Dispose()
        {
            // Clean up test files
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        
        [Fact]
        public async Task LoadPCConfigAsync_CreatesDefaultConfigIfNotExists()
        {
            // Arrange - Make sure config doesn't exist
            if (File.Exists(_pcConfigPath))
            {
                File.Delete(_pcConfigPath);
            }
            
            // Act
            var config = await _configManager.LoadPCConfigAsync();
            
            // Assert
            config.Should().NotBeNull();
            File.Exists(_pcConfigPath).Should().BeTrue("default config file should be created");
            config.Host.Should().Be("localhost", "default host should be localhost");
            config.Port.Should().Be(8001, "default port should be 8001");
        }
        
        [Fact]
        public async Task LoadPhoneConfigAsync_CreatesDefaultConfigIfNotExists()
        {
            // Arrange - Make sure config doesn't exist
            if (File.Exists(_phoneConfigPath))
            {
                File.Delete(_phoneConfigPath);
            }
            
            // Act
            var config = await _configManager.LoadPhoneConfigAsync();
            
            // Assert
            config.Should().NotBeNull();
            File.Exists(_phoneConfigPath).Should().BeTrue("default config file should be created");
            config.IphoneIpAddress.Should().NotBeNullOrEmpty("default IP address should be set");
            config.IphonePort.Should().BeGreaterThan(0, "default port should be valid");
        }
        
        [Fact]
        public async Task LoadAndSavePCConfigAsync_PreservesChanges()
        {
            // Arrange
            await _configManager.LoadPCConfigAsync(); // Load to create initial file
            const string newHost = "test-host";
            const int newPort = 9999;
            
            // Act - Create a new config with updated values
            var updatedConfig = new VTubeStudioPCConfig
            {
                Host = newHost,
                Port = newPort,
                // Keep default values for other properties
                PluginName = "SharpBridge",
                PluginDeveloper = "SharpBridge Developer",
                TokenFilePath = "auth_token.txt",
                ConnectionTimeoutMs = 5000,
                ReconnectionDelayMs = 2000,
                UsePortDiscovery = true
            };
            
            await _configManager.SavePCConfigAsync(updatedConfig);
            
            // Load the config again to check if changes were saved
            var reloadedConfig = await _configManager.LoadPCConfigAsync();
            
            // Assert
            reloadedConfig.Host.Should().Be(newHost, "saved host should be preserved");
            reloadedConfig.Port.Should().Be(newPort, "saved port should be preserved");
        }
        
        [Fact]
        public async Task LoadAndSavePhoneConfigAsync_PreservesChanges()
        {
            // Arrange
            await _configManager.LoadPhoneConfigAsync(); // Load to create initial file
            const string newIp = "10.0.0.42";
            const int newPort = 9876;
            
            // Act - Create a new config with updated values (not modifying directly since some properties are init-only)
            var updatedConfig = new VTubeStudioPhoneClientConfig
            {
                IphoneIpAddress = newIp,
                IphonePort = newPort,
                // Keep other default values
                LocalPort = 28964,
                RequestIntervalSeconds = 5,
                SendForSeconds = 10,
                ReceiveTimeoutMs = 100
            };
            
            await _configManager.SavePhoneConfigAsync(updatedConfig);
            
            // Load the config again to check if changes were saved
            var reloadedConfig = await _configManager.LoadPhoneConfigAsync();
            
            // Assert
            reloadedConfig.IphoneIpAddress.Should().Be(newIp, "saved IP address should be preserved");
            reloadedConfig.IphonePort.Should().Be(newPort, "saved port should be preserved");
        }
        
        [Fact]
        public void ConfigManager_Constructor_SetsCustomPaths()
        {
            // Arrange
            string customDir = "CustomDir";
            string customPcFile = "custom-pc.json";
            string customPhoneFile = "custom-phone.json";
            
            // Act
            var manager = new ConfigManager(customDir, customPcFile, customPhoneFile);
            
            // Assert
            manager.PCConfigPath.Should().Be(Path.Combine(customDir, customPcFile));
            manager.PhoneConfigPath.Should().Be(Path.Combine(customDir, customPhoneFile));
        }
    }
} 