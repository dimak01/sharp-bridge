using System;
using System.Threading.Tasks;
using FluentAssertions;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class CommandLineParserTests
    {
        private readonly CommandLineParser _parser;

        public CommandLineParserTests()
        {
            _parser = new CommandLineParser();
        }

        [Fact]
        public async Task ParseAsync_WithNoArgs_UsesDefaultValues()
        {
            // Act
            var options = await _parser.ParseAsync(Array.Empty<string>());
            
            // Assert
            options.ConfigDirectory.Should().Be(CommandLineDefaults.ConfigDirectory);
            options.TransformConfigFilename.Should().Be(CommandLineDefaults.TransformConfigFilename);
            options.PCConfigFilename.Should().Be(CommandLineDefaults.PCConfigFilename);
            options.PhoneConfigFilename.Should().Be(CommandLineDefaults.PhoneConfigFilename);
        }

        [Fact]
        public async Task ParseAsync_WithCustomArgs_UsesProvidedValues()
        {
            // Arrange
            var customDir = "CustomConfigs";
            var customTransform = "custom_transform.json";
            var customPC = "custom_pc.json";
            var customPhone = "custom_phone.json";
            
            var args = new[]
            {
                "--config-dir", customDir,
                "--transform-config", customTransform,
                "--pc-config", customPC,
                "--phone-config", customPhone
            };
            
            // Act
            var options = await _parser.ParseAsync(args);
            
            // Assert
            options.ConfigDirectory.Should().Be(customDir);
            options.TransformConfigFilename.Should().Be(customTransform);
            options.PCConfigFilename.Should().Be(customPC);
            options.PhoneConfigFilename.Should().Be(customPhone);
        }

        [Fact]
        public async Task ParseAsync_WithEmptyConfigDir_ThrowsArgumentException()
        {
            // Arrange
            var args = new[] { "--config-dir", "" };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _parser.ParseAsync(args));
        }

        [Fact]
        public async Task ParseAsync_WithEmptyTransformConfig_ThrowsArgumentException()
        {
            // Arrange
            var args = new[] { "--transform-config", "" };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _parser.ParseAsync(args));
        }

        [Fact]
        public async Task ParseAsync_WithEmptyPCConfig_ThrowsArgumentException()
        {
            // Arrange
            var args = new[] { "--pc-config", "" };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _parser.ParseAsync(args));
        }

        [Fact]
        public async Task ParseAsync_WithEmptyPhoneConfig_ThrowsArgumentException()
        {
            // Arrange
            var args = new[] { "--phone-config", "" };
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _parser.ParseAsync(args));
        }
    }
} 