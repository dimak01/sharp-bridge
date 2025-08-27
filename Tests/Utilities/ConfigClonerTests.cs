using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ConfigClonerTests
    {
        [Fact]
        public void Clone_ApplicationConfig_CreatesDeepCopy()
        {
            // Arrange
            var original = new ApplicationConfig
            {
                Version = 1,
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    IphoneIpAddress = "192.168.1.100",
                    IphonePort = 21412
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001
                }
            };

            // Act
            var cloned = ConfigCloner.Clone(original);

            // Assert
            cloned.Should().NotBeSameAs(original);
            cloned.PhoneClient.Should().NotBeSameAs(original.PhoneClient);
            cloned.PCClient.Should().NotBeSameAs(original.PCClient);

            // Values should be equal
            cloned.Version.Should().Be(original.Version);
            cloned.PhoneClient.IphoneIpAddress.Should().Be(original.PhoneClient.IphoneIpAddress);
            cloned.PhoneClient.IphonePort.Should().Be(original.PhoneClient.IphonePort);
            cloned.PCClient.Host.Should().Be(original.PCClient.Host);
            cloned.PCClient.Port.Should().Be(original.PCClient.Port);
        }

        [Fact]
        public void WithPhoneIpAddress_CreatesNewConfigWithUpdatedIp()
        {
            // Arrange
            var original = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    IphoneIpAddress = "192.168.1.100",
                    IphonePort = 21412
                }
            };
            var newIp = "192.168.1.200";

            // Act
            var updated = ConfigCloner.WithPhoneIpAddress(original, newIp);

            // Assert
            updated.Should().NotBeSameAs(original);
            updated.PhoneClient.Should().NotBeSameAs(original.PhoneClient);

            // Original should be unchanged
            original.PhoneClient.IphoneIpAddress.Should().Be("192.168.1.100");

            // Updated should have new IP
            updated.PhoneClient.IphoneIpAddress.Should().Be(newIp);

            // Other values should be preserved
            updated.PhoneClient.IphonePort.Should().Be(original.PhoneClient.IphonePort);
        }

        [Fact]
        public void WithPCHost_CreatesNewConfigWithUpdatedHost()
        {
            // Arrange
            var original = new ApplicationConfig
            {
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001
                }
            };
            var newHost = "192.168.1.50";

            // Act
            var updated = ConfigCloner.WithPCHost(original, newHost);

            // Assert
            updated.Should().NotBeSameAs(original);
            updated.PCClient.Should().NotBeSameAs(original.PCClient);

            // Original should be unchanged
            original.PCClient.Host.Should().Be("localhost");

            // Updated should have new host
            updated.PCClient.Host.Should().Be(newHost);

            // Other values should be preserved
            updated.PCClient.Port.Should().Be(original.PCClient.Port);
        }

        [Fact]
        public void Clone_HandlesNullValues_Gracefully()
        {
            // Act & Assert - should not throw
            var result = ConfigCloner.Clone((ApplicationConfig)null);
            result.Should().NotBeNull();
            result.Should().BeOfType<ApplicationConfig>();
        }

        [Fact]
        public void Clone_GeneralSettingsConfig_CopiesShortcuts()
        {
            // Arrange
            var original = new GeneralSettingsConfig
            {
                EditorCommand = "code \"%f\"",
                Shortcuts = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "ToggleMode", "Alt+T" },
                    { "Exit", "Ctrl+Q" }
                }
            };

            // Act
            var cloned = ConfigCloner.Clone(original);

            // Assert
            cloned.Should().NotBeSameAs(original);
            cloned.Shortcuts.Should().NotBeSameAs(original.Shortcuts);
            cloned.EditorCommand.Should().Be(original.EditorCommand);
            cloned.Shortcuts.Should().BeEquivalentTo(original.Shortcuts);
        }
    }
}
