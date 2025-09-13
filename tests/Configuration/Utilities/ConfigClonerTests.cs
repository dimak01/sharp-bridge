// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using FluentAssertions;
using SharpBridge.Configuration.Utilities;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Configuration.Utilities
{
    public class ConfigClonerTests
    {
        [Fact]
        public void Clone_ApplicationConfig_CreatesDeepCopy()
        {
            // Arrange
            var original = new ApplicationConfig
            {
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
            cloned.PhoneClient.IphoneIpAddress.Should().Be(original.PhoneClient.IphoneIpAddress);
            cloned.PhoneClient.IphonePort.Should().Be(original.PhoneClient.IphonePort);
            cloned.PCClient.Host.Should().Be(original.PCClient.Host);
            cloned.PCClient.Port.Should().Be(original.PCClient.Port);
        }



        [Fact]
        public void Clone_HandlesNullValues_Gracefully()
        {
            // Act & Assert - should not throw
            var result = ConfigCloner.Clone((ApplicationConfig?)null);
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
