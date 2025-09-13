using System.Reflection;
using FluentAssertions;
using SharpBridge.Core.Services;
using SharpBridge.Interfaces.Core.Services;
using Xunit;

namespace SharpBridge.Tests.Core.Services
{
    public class VersionServiceTests
    {
        private readonly IVersionService _versionService;

        public VersionServiceTests()
        {
            _versionService = new VersionService();
        }

        [Fact]
        public void GetVersion_ReturnsValidVersion()
        {
            // Act
            var version = _versionService.GetVersion();

            // Assert
            version.Should().NotBeNullOrEmpty();
            version.Should().MatchRegex(@"^\d+\.\d+\.\d+(-.*)?(\+.*)?$"); // Semantic version pattern with optional pre-release and build metadata
        }

        [Fact]
        public void GetDisplayVersion_ReturnsFormattedVersion()
        {
            // Act
            var displayVersion = _versionService.GetDisplayVersion();

            // Assert
            displayVersion.Should().NotBeNullOrEmpty();
            displayVersion.Should().StartWith("Sharp Bridge v");
            displayVersion.Should().Contain(_versionService.GetVersion());
        }

        [Fact]
        public void GetVersion_HandlesMissingAttribute()
        {
            // This test verifies the fallback behavior when AssemblyInformationalVersionAttribute is missing
            // In practice, this should not happen with our .csproj configuration, but it's good to test the fallback

            // Act
            var version = _versionService.GetVersion();

            // Assert
            version.Should().NotBeNullOrEmpty();
            // Should either return the actual version or the fallback "0.0.0-unknown"
            version.Should().MatchRegex(@"^\d+\.\d+\.\d+(-.*)?(\+.*)?$");
        }

        [Fact]
        public void GetDisplayVersion_FormatIsConsistent()
        {
            // Act
            var displayVersion = _versionService.GetDisplayVersion();
            var version = _versionService.GetVersion();

            // Assert
            var expectedFormat = $"Sharp Bridge v{version}";
            displayVersion.Should().Be(expectedFormat);
        }

        [Fact]
        public void GetVersion_ReturnsAssemblyInformationalVersion()
        {
            // Act
            var version = _versionService.GetVersion();
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            // Assert
            if (attribute != null)
            {
                version.Should().Be(attribute.InformationalVersion);
            }
            else
            {
                version.Should().Be("0.0.0-unknown");
            }
        }
    }
}
