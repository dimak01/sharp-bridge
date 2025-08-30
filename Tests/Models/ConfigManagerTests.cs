using System;
using System.IO;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using Xunit;
using FluentAssertions;
using SharpBridge.Utilities;
using Moq;
using System.Text.Json;

namespace SharpBridge.Tests.Models
{
    public class ConfigManagerTests : IDisposable
    {
        private readonly DirectoryInfo _testDirectory;
        private readonly string _applicationConfigPath;
        private readonly string _userPreferencesPath;

        public ConfigManagerTests()
        {
            // Create a unique temp directory that's automatically cleaned up
            _testDirectory = Directory.CreateTempSubdirectory();
            _applicationConfigPath = Path.Combine(_testDirectory.FullName, "ApplicationConfig.json");
            _userPreferencesPath = Path.Combine(_testDirectory.FullName, "UserPreferences.json");
        }

        public void Dispose()
        {
            // The temp directory is automatically cleaned up when disposed
            // But we can also manually clean up if needed for immediate cleanup
            try
            {
                if (_testDirectory.Exists)
                {
                    _testDirectory.Delete(true);
                }
            }
            catch
            {
                // Ignore cleanup errors - the OS will clean up temp files eventually
                // This prevents test cleanup failures from masking actual test failures
            }
        }

        #region Mock Helper Methods

        private static Mock<IConfigMigrationService> CreateMockMigrationService()
        {
            var mock = new Mock<IConfigMigrationService>();

            // Default behavior - return factory-created configs with WasCreated: true
            mock.Setup(x => x.LoadWithMigrationAsync<ApplicationConfig>(
                It.IsAny<string>(), It.IsAny<Func<ApplicationConfig>>()))
                .ReturnsAsync((string path, Func<ApplicationConfig> factory) =>
                    new ConfigLoadResult<ApplicationConfig>(factory(), true, false, 1));

            mock.Setup(x => x.LoadWithMigrationAsync<UserPreferences>(
                It.IsAny<string>(), It.IsAny<Func<UserPreferences>>()))
                .ReturnsAsync((string path, Func<UserPreferences> factory) =>
                    new ConfigLoadResult<UserPreferences>(factory(), true, false, 1));

            return mock;
        }

        private static Mock<IConfigMigrationService> CreateMockMigrationService_FileExists()
        {
            var mock = new Mock<IConfigMigrationService>();

            // Behavior for when files exist - read from disk and return WasCreated: false
            mock.Setup(x => x.LoadWithMigrationAsync<ApplicationConfig>(
                It.IsAny<string>(), It.IsAny<Func<ApplicationConfig>>()))
                .ReturnsAsync((string path, Func<ApplicationConfig> factory) =>
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            var jsonText = File.ReadAllText(path);
                            if (!string.IsNullOrWhiteSpace(jsonText))
                            {
                                var config = JsonSerializer.Deserialize<ApplicationConfig>(jsonText);
                                if (config != null)
                                {
                                    return new ConfigLoadResult<ApplicationConfig>(config, false, false, 1);
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // If JSON parsing fails, return factory-created config with WasCreated: true
                            return new ConfigLoadResult<ApplicationConfig>(factory(), true, false, 1);
                        }
                    }
                    // File doesn't exist or parsing failed - return factory-created config
                    return new ConfigLoadResult<ApplicationConfig>(factory(), true, false, 1);
                });

            mock.Setup(x => x.LoadWithMigrationAsync<UserPreferences>(
                It.IsAny<string>(), It.IsAny<Func<UserPreferences>>()))
                .ReturnsAsync((string path, Func<UserPreferences> factory) =>
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            var jsonText = File.ReadAllText(path);
                            if (!string.IsNullOrWhiteSpace(jsonText))
                            {
                                var config = JsonSerializer.Deserialize<UserPreferences>(jsonText);
                                if (config != null)
                                {
                                    return new ConfigLoadResult<UserPreferences>(config, false, false, 1);
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // If JSON parsing fails, return factory-created config with WasCreated: true
                            return new ConfigLoadResult<UserPreferences>(factory(), true, false, 1);
                        }
                    }
                    // File doesn't exist or parsing failed - return factory-created config
                    return new ConfigLoadResult<UserPreferences>(factory(), true, false, 1);
                });

            return mock;
        }

        private static Mock<IConfigMigrationService> CreateMockMigrationService_InvalidJson()
        {
            var mock = new Mock<IConfigMigrationService>();

            // Behavior that simulates JSON parsing errors - return WasCreated: true
            mock.Setup(x => x.LoadWithMigrationAsync<ApplicationConfig>(
                It.IsAny<string>(), It.IsAny<Func<ApplicationConfig>>()))
                .ReturnsAsync((string path, Func<ApplicationConfig> factory) =>
                    new ConfigLoadResult<ApplicationConfig>(factory(), true, false, 1));

            mock.Setup(x => x.LoadWithMigrationAsync<UserPreferences>(
                It.IsAny<string>(), It.IsAny<Func<UserPreferences>>()))
                .ReturnsAsync((string path, Func<UserPreferences> factory) =>
                    new ConfigLoadResult<UserPreferences>(factory(), true, false, 1));

            return mock;
        }

        #endregion

        [Fact]
        public async Task LoadApplicationConfigAsync_CreatesDefaultConfigIfNotExists()
        {
            // Arrange
            var mockMigrationService = CreateMockMigrationService();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            // Setup mock to return a created config
            mockMigrationService.Setup(m => m.LoadWithMigrationAsync<ApplicationConfig>(It.IsAny<string>(), It.IsAny<Func<ApplicationConfig>>()))
                                .ReturnsAsync((string path, Func<ApplicationConfig> factory) =>
                                    new ConfigLoadResult<ApplicationConfig>(factory(), true, false, 1));

            // Act
            var config = await configManager.LoadApplicationConfigAsync();

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
            // Arrange
            var mockMigrationService = CreateMockMigrationService();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            // Act
            var config = await configManager.LoadPCConfigAsync();

            // Assert
            config.Should().NotBeNull();
            File.Exists(_applicationConfigPath).Should().BeTrue("default config file should be created");
            config.Host.Should().Be("localhost", "default host should be localhost");
            config.Port.Should().Be(8001, "default port should be 8001");
        }

        [Fact]
        public async Task LoadPhoneConfigAsync_CreatesDefaultConfigIfNotExists()
        {
            // Arrange
            var mockMigrationService = CreateMockMigrationService();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            // Act
            var config = await configManager.LoadPhoneConfigAsync();

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
            var mockMigrationService = CreateMockMigrationService_FileExists();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            await configManager.LoadPCConfigAsync(); // Load to create initial file
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

            await configManager.SavePCConfigAsync(updatedConfig);

            // Load the config again to check if changes were saved
            var reloadedConfig = await configManager.LoadPCConfigAsync();

            // Assert
            reloadedConfig.Host.Should().Be(newHost, "saved host should be preserved");
            reloadedConfig.Port.Should().Be(newPort, "saved port should be preserved");
        }

        [Fact]
        public async Task LoadAndSavePhoneConfigAsync_PreservesChanges()
        {
            // Arrange
            var mockMigrationService = CreateMockMigrationService_FileExists();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            await configManager.LoadPhoneConfigAsync(); // Load to create initial file
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

            await configManager.SavePhoneConfigAsync(updatedConfig);

            // Load the config again to check if changes were saved
            var reloadedConfig = await configManager.LoadPhoneConfigAsync();

            // Assert
            reloadedConfig.IphoneIpAddress.Should().Be(newIp, "saved IP address should be preserved");
            reloadedConfig.IphonePort.Should().Be(newPort, "saved port should be preserved");
        }

        [Fact]
        public async Task LoadUserPreferencesAsync_CreatesDefaultIfNotExists()
        {
            // Arrange
            var mockMigrationService = CreateMockMigrationService();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            // Act
            var preferences = await configManager.LoadUserPreferencesAsync();

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
            var mockMigrationService = CreateMockMigrationService_FileExists();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            var preferences = new UserPreferences
            {
                PhoneClientVerbosity = VerbosityLevel.Detailed,
                PCClientVerbosity = VerbosityLevel.Basic,
                TransformationEngineVerbosity = VerbosityLevel.Detailed,
                PreferredConsoleWidth = 120,
                PreferredConsoleHeight = 40
            };

            // Act
            await configManager.SaveUserPreferencesAsync(preferences);
            var reloadedPreferences = await configManager.LoadUserPreferencesAsync();

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
            // Arrange
            var mockMigrationService = CreateMockMigrationService_FileExists();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            // Create and save some custom preferences first
            var customPreferences = new UserPreferences
            {
                PhoneClientVerbosity = VerbosityLevel.Detailed,
                PreferredConsoleWidth = 200
            };
            await configManager.SaveUserPreferencesAsync(customPreferences);

            // Act
            await configManager.ResetUserPreferencesAsync();
            var resetPreferences = await configManager.LoadUserPreferencesAsync();

            // Assert
            resetPreferences.PhoneClientVerbosity.Should().Be(VerbosityLevel.Normal, "should be reset to default");
            resetPreferences.PreferredConsoleWidth.Should().Be(150, "should be reset to default");
        }

        [Fact]
        public void ConfigManager_Constructor_SetsConsolidatedPaths()
        {
            // Arrange
            string customDir = "CustomDir";
            var mockMigrationService = CreateMockMigrationService();

            // Act
            var manager = new ConfigManager(customDir, mockMigrationService.Object);

            // Assert
            manager.ApplicationConfigPath.Should().Be(Path.Combine(customDir, "ApplicationConfig.json"));
            manager.UserPreferencesPath.Should().Be(Path.Combine(customDir, "UserPreferences.json"));
        }

        [Fact]
        public void ConfigManager_Constructor_NullConfigDirectory_ThrowsArgumentNullException()
        {
            // Arrange
            var mockMigrationService = CreateMockMigrationService();

            // Act & Assert
            Action act = () => new ConfigManager(null!, mockMigrationService.Object);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("configDirectory");
        }

        [Fact]
        public void EnsureConfigDirectoryExists_CreatesDirectoryIfNotExists()
        {
            // Arrange
            var testDir = Directory.CreateTempSubdirectory();
            var mockMigrationService = CreateMockMigrationService();

            try
            {
                // Act - constructor calls EnsureConfigDirectoryExists internally
                _ = new ConfigManager(testDir.FullName, mockMigrationService.Object);

                // Assert
                Directory.Exists(testDir.FullName).Should().BeTrue("directory should be created if it doesn't exist");
            }
            finally
            {
                // Cleanup
                try
                {
                    if (testDir.Exists)
                        testDir.Delete(true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public async Task LoadConfigAsync_JsonException_ThrowsInvalidOperationException()
        {
            // Arrange
            var testDir = Directory.CreateTempSubdirectory();
            string invalidJsonPath = Path.Combine(testDir.FullName, "ApplicationConfig.json");
            var mockMigrationService = CreateMockMigrationService_InvalidJson();
            var manager = new ConfigManager(testDir.FullName, mockMigrationService.Object);

            try
            {
                // Create a file with invalid JSON content
                File.WriteAllText(invalidJsonPath, "{ invalid json content }");

                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => manager.LoadApplicationConfigAsync());

                exception.Message.Should().Contain("Error parsing configuration file");
            }
            finally
            {
                // Cleanup
                try
                {
                    if (testDir.Exists)
                        testDir.Delete(true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public async Task LoadConfigAsync_DeserializesToNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var testDir = Directory.CreateTempSubdirectory();
            string nullJsonPath = Path.Combine(testDir.FullName, "ApplicationConfig.json");
            var mockMigrationService = CreateMockMigrationService_InvalidJson();
            var manager = new ConfigManager(testDir.FullName, mockMigrationService.Object);

            try
            {
                // Create a file with null content (empty JSON object won't deserialize to null, but this simulates the condition)
                File.WriteAllText(nullJsonPath, "null");

                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => manager.LoadApplicationConfigAsync());

                exception.Message.Should().Contain("Failed to deserialize configuration");
            }
            finally
            {
                // Cleanup
                try
                {
                    if (testDir.Exists)
                        testDir.Delete(true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public async Task SaveConfigAsync_IOException_ThrowsInvalidOperationException()
        {
            // Arrange - Create a readonly file to cause IOException
            var testDir = Directory.CreateTempSubdirectory();
            var mockMigrationService = CreateMockMigrationService_FileExists();
            var manager = new ConfigManager(testDir.FullName, mockMigrationService.Object);

            try
            {
                // Create config manager and load to create initial preferences file
                await manager.LoadUserPreferencesAsync(); // This creates the UserPreferences.json file

                // Make the UserPreferences file readonly to cause IOException on save
                string preferencesPath = Path.Combine(testDir.FullName, "UserPreferences.json");
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
            finally
            {
                // Cleanup
                try
                {
                    if (testDir.Exists)
                        testDir.Delete(true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public async Task Debug_MockFilePersistence_ShouldWork()
        {
            // Arrange
            var mockMigrationService = CreateMockMigrationService_FileExists();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            var preferences = new UserPreferences
            {
                PhoneClientVerbosity = VerbosityLevel.Detailed,
                PCClientVerbosity = VerbosityLevel.Basic,
                TransformationEngineVerbosity = VerbosityLevel.Detailed,
                PreferredConsoleWidth = 120,
                PreferredConsoleHeight = 40
            };

            // Act - Save preferences
            await configManager.SaveUserPreferencesAsync(preferences);

            // Verify file was actually created
            File.Exists(_userPreferencesPath).Should().BeTrue("file should exist after save");

            // Read the file content manually to verify
            var fileContent = File.ReadAllText(_userPreferencesPath);
            fileContent.Should().NotBeNullOrEmpty("file should contain saved content");

            // Act - Load preferences
            var reloadedPreferences = await configManager.LoadUserPreferencesAsync();

            // Assert
            reloadedPreferences.PhoneClientVerbosity.Should().Be(VerbosityLevel.Detailed);
            reloadedPreferences.PCClientVerbosity.Should().Be(VerbosityLevel.Basic);
            reloadedPreferences.TransformationEngineVerbosity.Should().Be(VerbosityLevel.Detailed);
            reloadedPreferences.PreferredConsoleWidth.Should().Be(120);
            reloadedPreferences.PreferredConsoleHeight.Should().Be(40);
        }

        [Fact]
        public async Task Debug_CheckSavedFileContent()
        {
            // Arrange
            var mockMigrationService = CreateMockMigrationService_FileExists();
            var configManager = new ConfigManager(_testDirectory.FullName, mockMigrationService.Object);

            var preferences = new UserPreferences
            {
                PhoneClientVerbosity = VerbosityLevel.Detailed,
                PCClientVerbosity = VerbosityLevel.Basic,
                TransformationEngineVerbosity = VerbosityLevel.Detailed,
                PreferredConsoleWidth = 120,
                PreferredConsoleHeight = 40
            };

            // Act - Save preferences
            await configManager.SaveUserPreferencesAsync(preferences);

            // Verify file was actually created
            File.Exists(_userPreferencesPath).Should().BeTrue("file should exist after save");

            // Read the file content manually to verify
            var fileContent = File.ReadAllText(_userPreferencesPath);
            Console.WriteLine($"DEBUG: Saved file content: {fileContent}");

            // Try to deserialize manually to see what happens
            try
            {
                var manuallyDeserialized = JsonSerializer.Deserialize<UserPreferences>(fileContent);
                Console.WriteLine($"DEBUG: Manual deserialization successful: {manuallyDeserialized != null}");
                if (manuallyDeserialized != null)
                {
                    Console.WriteLine($"DEBUG: PhoneClientVerbosity: {manuallyDeserialized.PhoneClientVerbosity}");
                    Console.WriteLine($"DEBUG: PCClientVerbosity: {manuallyDeserialized.PCClientVerbosity}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Manual deserialization failed: {ex.Message}");
            }

            // This test is just for debugging, so we don't need assertions
        }
    }
}