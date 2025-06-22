using System;
using System.IO;
using System.Threading.Tasks;
using SharpBridge.Models;
using Xunit;
using FluentAssertions;
using SharpBridge.Utilities;

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

        [Fact]
        public void ConfigManager_Constructor_NullConfigDirectory_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Action act = () => new ConfigManager(null!, "pc.json", "phone.json");
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("configDirectory");
        }

        [Fact]
        public void ConfigManager_Constructor_NullPCConfigFilename_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Action act = () => new ConfigManager("Configs", null!, "phone.json");
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("pcConfigFilename");
        }

        [Fact]
        public void ConfigManager_Constructor_NullPhoneConfigFilename_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Action act = () => new ConfigManager("Configs", "pc.json", null!);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("phoneConfigFilename");
        }

        [Fact]
        public void EnsureConfigDirectoryExists_CreatesDirectoryIfNotExists()
        {
            // Arrange
            string testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // Make sure directory doesn't exist
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }

                // Act - constructor calls EnsureConfigDirectoryExists internally
                _ = new ConfigManager(testDir, "pc.json", "phone.json");

                // Assert
                Directory.Exists(testDir).Should().BeTrue("directory should be created if it doesn't exist");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }

        [Fact]
        public async Task LoadConfigAsync_JsonException_ThrowsInvalidOperationException()
        {
            // Arrange
            string testDir = Path.Combine(_testDirectory, "InvalidJson");
            Directory.CreateDirectory(testDir);
            string invalidJsonPath = Path.Combine(testDir, "invalid.json");

            // Create a file with invalid JSON content
            File.WriteAllText(invalidJsonPath, "{ this is not valid json }");

            var manager = new ConfigManager(testDir, "invalid.json", "phone.json");

            // Act & Assert
            Func<Task> act = async () => await manager.LoadPCConfigAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Error parsing configuration file*");
        }

        [Fact]
        public async Task LoadConfigAsync_DeserializesToNull_ThrowsInvalidOperationException()
        {
            // Arrange
            string testDir = Path.Combine(_testDirectory, "NullConfig");
            Directory.CreateDirectory(testDir);
            string nullConfigPath = Path.Combine(testDir, "null.json");

            // Create a file with JSON null
            File.WriteAllText(nullConfigPath, "null");

            var manager = new ConfigManager(testDir, "null.json", "phone.json");

            // Act & Assert
            Func<Task> act = async () => await manager.LoadPCConfigAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Failed to deserialize configuration*");
        }

        [Fact]
        public async Task SaveConfigAsync_IOException_ThrowsInvalidOperationException()
        {
            // This scenario relies on Windows read-only semantics. Skip on other platforms
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            // Arrange
            // Create a readonly directory to cause an IO exception on save
            string readOnlyDir = Path.Combine(_testDirectory, "ReadOnly");
            Directory.CreateDirectory(readOnlyDir);

            var manager = new ConfigManager(readOnlyDir, "readonly.json", "phone.json");
            var config = new VTubeStudioPCConfig();

            try
            {
                // Create a file and make it read-only to cause an exception
                string readOnlyPath = Path.Combine(readOnlyDir, "readonly.json");
                File.WriteAllText(readOnlyPath, "{}");
                File.SetAttributes(readOnlyPath, FileAttributes.ReadOnly);

                // Act & Assert
                Func<Task> act = async () => await manager.SavePCConfigAsync(config);
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*Error saving configuration*");
            }
            finally
            {
                // Cleanup - reset readonly attribute to allow deletion
                string readOnlyPath = Path.Combine(readOnlyDir, "readonly.json");
                if (File.Exists(readOnlyPath))
                {
                    File.SetAttributes(readOnlyPath, FileAttributes.Normal);
                }
            }
        }
    }
}