using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ConfigMigrationServiceTests : IDisposable
    {
        private readonly ConfigMigrationService _migrationService;
        private readonly string _testDirectory;

        public ConfigMigrationServiceTests()
        {
            _migrationService = new ConfigMigrationService();
            _testDirectory = Path.Combine(Path.GetTempPath(), "ConfigMigrationServiceTests_" + Guid.NewGuid().ToString("N")[0..8]);
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task LoadWithMigrationAsync_WhenFileDoesNotExist_CreatesDefaultConfig()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "nonexistent.json");

            // Act
            var result = await _migrationService.LoadWithMigrationAsync<ApplicationConfig>(
                filePath,
                () => new ApplicationConfig());

            // Assert
            result.Should().NotBeNull();
            result.Config.Should().NotBeNull();
            result.WasCreated.Should().BeTrue();
            result.WasMigrated.Should().BeFalse();
            result.OriginalVersion.Should().Be(ApplicationConfig.CurrentVersion);
        }

        [Fact]
        public async Task LoadWithMigrationAsync_WhenFileExistsWithCurrentVersion_LoadsDirectly()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "current_version.json");
            var config = new ApplicationConfig { Version = ApplicationConfig.CurrentVersion };

            await File.WriteAllTextAsync(filePath, System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            // Act
            var result = await _migrationService.LoadWithMigrationAsync<ApplicationConfig>(
                filePath,
                () => new ApplicationConfig());

            // Assert
            result.Should().NotBeNull();
            result.Config.Should().NotBeNull();
            result.WasCreated.Should().BeFalse();
            result.WasMigrated.Should().BeFalse();
            result.OriginalVersion.Should().Be(ApplicationConfig.CurrentVersion);
        }

        [Fact]
        public void ProbeVersion_WhenFileDoesNotExist_ReturnsZero()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "nonexistent.json");

            // Act
            var version = _migrationService.ProbeVersion(filePath);

            // Assert
            version.Should().Be(0);
        }

        [Fact]
        public void ProbeVersion_WhenFileHasVersion_ReturnsVersion()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "versioned.json");
            var jsonContent = """{"Version": 2, "SomeProperty": "test"}""";
            File.WriteAllText(filePath, jsonContent);

            // Act
            var version = _migrationService.ProbeVersion(filePath);

            // Assert
            version.Should().Be(2);
        }

        [Fact]
        public void ProbeVersion_WhenFileHasNoVersion_ReturnsZero()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "no_version.json");
            var jsonContent = """{"SomeProperty": "test"}""";
            File.WriteAllText(filePath, jsonContent);

            // Act
            var version = _migrationService.ProbeVersion(filePath);

            // Assert
            version.Should().Be(0);
        }
    }
}
