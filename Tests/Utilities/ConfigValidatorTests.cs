using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class ConfigValidatorTests
    {
        private readonly ConfigValidator _validator;

        public ConfigValidatorTests()
        {
            _validator = new ConfigValidator();
        }

        [Fact]
        public void ValidateConfiguration_WithNullConfig_ReturnsAllFieldsMissing()
        {
            // Act
            var result = _validator.ValidateConfiguration(null);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.RequiresSetup.Should().BeTrue();
            result.MissingFields.Should().Contain(new[]
            {
                MissingField.PhoneIpAddress,
                MissingField.PhonePort,
                MissingField.PCHost,
                MissingField.PCPort
            });
        }

        [Fact]
        public void ValidateConfiguration_WithValidConfig_ReturnsValid()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    IphoneIpAddress = "192.168.1.200", // Not a default IP
                    IphonePort = 21412
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001
                }
            };

            // Act
            var result = _validator.ValidateConfiguration(config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.RequiresSetup.Should().BeFalse();
            result.MissingFields.Should().BeEmpty();
        }

        [Fact]
        public void ValidateConfiguration_WithDefaultPhoneIp_ReturnsPhoneIpMissing()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    IphoneIpAddress = "192.168.1.178", // Default IP
                    IphonePort = 21412
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001
                }
            };

            // Act
            var result = _validator.ValidateConfiguration(config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.RequiresSetup.Should().BeTrue();
            result.MissingFields.Should().Contain(MissingField.PhoneIpAddress);
        }

        [Fact]
        public void ValidateConfiguration_WithInvalidPhoneIp_ReturnsPhoneIpMissing()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    IphoneIpAddress = "invalid-ip",
                    IphonePort = 21412
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001
                }
            };

            // Act
            var result = _validator.ValidateConfiguration(config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.RequiresSetup.Should().BeTrue();
            result.MissingFields.Should().Contain(MissingField.PhoneIpAddress);
        }

        [Fact]
        public void ValidateConfiguration_WithInvalidPort_ReturnsPortMissing()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    IphoneIpAddress = "192.168.1.200",
                    IphonePort = 0 // Invalid port
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001
                }
            };

            // Act
            var result = _validator.ValidateConfiguration(config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.RequiresSetup.Should().BeTrue();
            result.MissingFields.Should().Contain(MissingField.PhonePort);
        }

        [Fact]
        public void ValidateConfiguration_WithEmptyPCHost_ReturnsPCHostMissing()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PhoneClient = new VTubeStudioPhoneClientConfig
                {
                    IphoneIpAddress = "192.168.1.200",
                    IphonePort = 21412
                },
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "", // Empty host
                    Port = 8001
                }
            };

            // Act
            var result = _validator.ValidateConfiguration(config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.RequiresSetup.Should().BeTrue();
            result.MissingFields.Should().Contain(MissingField.PCHost);
        }

        [Fact]
        public void ValidateConfiguration_WithNullPhoneClient_ReturnsPhoneFieldsMissing()
        {
            // Arrange
            var config = new ApplicationConfig
            {
                PhoneClient = null,
                PCClient = new VTubeStudioPCConfig
                {
                    Host = "localhost",
                    Port = 8001
                }
            };

            // Act
            var result = _validator.ValidateConfiguration(config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.RequiresSetup.Should().BeTrue();
            result.MissingFields.Should().Contain(new[]
            {
                MissingField.PhoneIpAddress,
                MissingField.PhonePort
            });
        }
    }
}
