using System;
using System.IO;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using Xunit;
using FluentAssertions;
using SharpBridge.Utilities;

namespace SharpBridge.Tests.Models
{
    public class ConfigManagerTests : IDisposable
    {
        private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "ConfigManagerTests_" + Guid.NewGuid().ToString("N")[0..8]);
        private readonly string _applicationConfigPath;
        private readonly string _userPreferencesPath;
        private readonly ConfigManager _configManager;

        public ConfigManagerTests()
        {
            // Create test directory 
            Directory.CreateDirectory(_testDirectory);

            // Create config manager with test directory
            _configManager = new ConfigManager(_testDirectory);
            _applicationConfigPath = Path.Combine(_testDirectory, "ApplicationConfig.json");
            _userPreferencesPath = Path.Combine(_testDirectory, "UserPreferences.json");
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
        public async Task LoadApplicationConfigAsync_CreatesDefaultConfigIfNotExists()
        {
            // Arrange - Make sure config doesn't exist
            if (File.Exists(_applicationConfigPath))
            {
                File.Delete(_applicationConfigPath);
            }

            // Act
            var config = await _configManager.LoadApplicationConfigAsync();

            // Assert
            config.Should().NotBeNull();
            File.Exists(_applicationConfigPath).Should().BeTrue("default config file should be created");
            config.PCClient.Should().NotBeNull("PCClient config should exist");
            config.PhoneClient.Should().NotBeNull("PhoneClient config should exist");
            config.GeneralSettings.Should().NotBeNull("GeneralSettings config should exist");
            config.TransformationEngine.Should().NotBeNull("TransformationEngine config should exist");
        }

        [Fact]
        public async Task LoadPCConfigAsync_CreatesDefaultConfigIfNotExists()
        {
            // Arrange - Make sure config doesn't exist
            if (File.Exists(_applicationConfigPath))
            {
                File.Delete(_applicationConfigPath);
            }

            // Act
            var config = await _configManager.LoadPCConfigAsync();

            // Assert
            config.Should().NotBeNull();
            File.Exists(_applicationConfigPath).Should().BeTrue("default config file should be created");
            config.Host.Should().Be("localhost", "default host should be localhost");
            config.Port.Should().Be(8001, "default port should be 8001");
        }

        [Fact]
        public async Task LoadPhoneConfigAsync_CreatesDefaultConfigIfNotExists()
        {
            // Arrange - Make sure config doesn't exist
            if (File.Exists(_applicationConfigPath))
            {
                File.Delete(_applicationConfigPath);
            }

            // Act
            var config = await _configManager.LoadPhoneConfigAsync();

            // Assert
            config.Should().NotBeNull();
            File.Exists(_applicationConfigPath).Should().BeTrue("default config file should be created");
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
        public async Task LoadUserPreferencesAsync_CreatesDefaultIfNotExists()
        {
            // Arrange - Make sure preferences don't exist
            if (File.Exists(_userPreferencesPath))
            {
                File.Delete(_userPreferencesPath);
            }

            // Act
            var preferences = await _configManager.LoadUserPreferencesAsync();

            // Assert
            preferences.Should().NotBeNull();
            File.Exists(_userPreferencesPath).Should().BeTrue("default preferences file should be created");
            preferences.PhoneClientVerbosity.Should().NotBe(null, "default verbosity should be set");
            preferences.PCClientVerbosity.Should().NotBe(null, "default verbosity should be set");
            preferences.TransformationEngineVerbosity.Should().NotBe(null, "default verbosity should be set");
        }

        [Fact]
        public async Task SaveAndLoadUserPreferencesAsync_PreservesChanges()
        {
            // Arrange
            var preferences = new UserPreferences
            {
                PhoneClientVerbosity = VerbosityLevel.Detailed,
                PCClientVerbosity = VerbosityLevel.Basic,
                TransformationEngineVerbosity = VerbosityLevel.Detailed,
                PreferredConsoleWidth = 120,
                PreferredConsoleHeight = 40
            };

            // Act
            await _configManager.SaveUserPreferencesAsync(preferences);
            var reloadedPreferences = await _configManager.LoadUserPreferencesAsync();

            // Assert
            reloadedPreferences.PhoneClientVerbosity.Should().Be(VerbosityLevel.Detailed);
            reloadedPreferences.PCClientVerbosity.Should().Be(VerbosityLevel.Basic);
            reloadedPreferences.TransformationEngineVerbosity.Should().Be(VerbosityLevel.Detailed);
            reloadedPreferences.PreferredConsoleWidth.Should().Be(120);
            reloadedPreferences.PreferredConsoleHeight.Should().Be(40);
        }

        [Fact]
        public async Task ResetUserPreferencesAsync_CreatesDefaultPreferences()
        {
            // Arrange - Create and save some custom preferences first
            var customPreferences = new UserPreferences
            {
                PhoneClientVerbosity = VerbosityLevel.Detailed,
                PreferredConsoleWidth = 200
            };
            await _configManager.SaveUserPreferencesAsync(customPreferences);

            // Act
            await _configManager.ResetUserPreferencesAsync();
            var resetPreferences = await _configManager.LoadUserPreferencesAsync();

            // Assert
            resetPreferences.PhoneClientVerbosity.Should().Be(VerbosityLevel.Normal, "should be reset to default");
            resetPreferences.PreferredConsoleWidth.Should().Be(150, "should be reset to default");
        }

        [Fact]
        public void ConfigManager_Constructor_SetsConsolidatedPaths()
        {
            // Arrange
            string customDir = "CustomDir";

            // Act
            var manager = new ConfigManager(customDir);

            // Assert
            manager.ApplicationConfigPath.Should().Be(Path.Combine(customDir, "ApplicationConfig.json"));
            manager.UserPreferencesPath.Should().Be(Path.Combine(customDir, "UserPreferences.json"));
        }

        [Fact]
        public void ConfigManager_Constructor_NullConfigDirectory_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Action act = () => new ConfigManager(null!);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("configDirectory");
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
                _ = new ConfigManager(testDir);

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
            string invalidJsonPath = Path.Combine(testDir, "ApplicationConfig.json");

            // Create a file with invalid JSON content
            File.WriteAllText(invalidJsonPath, "{ invalid json content }");

            var manager = new ConfigManager(testDir);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => manager.LoadApplicationConfigAsync());

            exception.Message.Should().Contain("Error parsing configuration file");
        }

        [Fact]
        public async Task LoadConfigAsync_DeserializesToNull_ThrowsInvalidOperationException()
        {
            // Arrange
            string testDir = Path.Combine(_testDirectory, "NullDeserialization");
            Directory.CreateDirectory(testDir);
            string nullJsonPath = Path.Combine(testDir, "ApplicationConfig.json");

            // Create a file with null content (empty JSON object won't deserialize to null, but this simulates the condition)
            File.WriteAllText(nullJsonPath, "null");

            var manager = new ConfigManager(testDir);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => manager.LoadApplicationConfigAsync());

            exception.Message.Should().Contain("Failed to deserialize configuration");
        }

        [Fact]
        public async Task SaveConfigAsync_IOException_ThrowsInvalidOperationException()
        {
            // Arrange - Create a readonly file to cause IOException
            string testDir = Path.Combine(_testDirectory, "ReadOnlyFile");
            Directory.CreateDirectory(testDir);

            // Create config manager and load to create initial preferences file
            var manager = new ConfigManager(testDir);
            await manager.LoadUserPreferencesAsync(); // This creates the UserPreferences.json file

            // Make the UserPreferences file readonly to cause IOException on save
            string preferencesPath = Path.Combine(testDir, "UserPreferences.json");
            var fileInfo = new FileInfo(preferencesPath);
            fileInfo.IsReadOnly = true;

            try
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => manager.SaveUserPreferencesAsync(new UserPreferences()));

                exception.Message.Should().Contain("Error saving configuration");
            }
            finally
            {
                // Cleanup - remove readonly attribute
                var fileInfo2 = new FileInfo(preferencesPath);
                if (fileInfo2.Exists)
                {
                    fileInfo2.IsReadOnly = false;
                }
            }
        }
    }
}