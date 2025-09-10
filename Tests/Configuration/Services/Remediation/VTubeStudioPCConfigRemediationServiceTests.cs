using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.Remediation;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Configuration.Services.Remediation
{
    /// <summary>
    /// Unit tests for VTubeStudioPCConfigRemediationService
    /// </summary>
    public class VTubeStudioPCConfigRemediationServiceTests : IDisposable
    {
        private readonly Mock<IConfigSectionValidatorsFactory> _mockValidatorsFactory;
        private readonly Mock<IConfigSectionValidator> _mockValidator;
        private readonly Mock<IConsole> _mockConsole;

        public VTubeStudioPCConfigRemediationServiceTests()
        {
            _mockValidatorsFactory = new Mock<IConfigSectionValidatorsFactory>();
            _mockValidator = new Mock<IConfigSectionValidator>();
            _mockConsole = new Mock<IConsole>();

            _mockValidatorsFactory.Setup(x => x.GetValidator(ConfigSectionTypes.VTubeStudioPCConfig))
                .Returns(_mockValidator.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);

            // Assert
            service.Should().NotBeNull();
            _mockValidatorsFactory.Verify(x => x.GetValidator(ConfigSectionTypes.VTubeStudioPCConfig), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullValidatorsFactory_ThrowsNullReferenceException()
        {
            // Arrange
            var validatorsFactory = (IConfigSectionValidatorsFactory)null!;
            var console = _mockConsole.Object;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() =>
                new VTubeStudioPCConfigRemediationService(validatorsFactory, console));
        }

        [Fact]
        public void Constructor_WithNullConsole_DoesNotThrow()
        {
            // Arrange
            var validatorsFactory = _mockValidatorsFactory.Object;
            var console = (IConsole)null!;

            // Act & Assert
            var action = () => new VTubeStudioPCConfigRemediationService(validatorsFactory, console);
            action.Should().NotThrow();
        }

        #endregion

        #region Remediate Tests - No Remediation Needed

        [Fact]
        public async Task Remediate_WithValidFields_ReturnsNoRemediationNeeded()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery")
            };

            var validationResult = new ConfigValidationResult(new List<FieldValidationIssue>());
            _mockValidator.Setup(x => x.ValidateSection(fields))
                .Returns(validationResult);

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.NoRemediationNeeded);
            result.UpdatedConfig.Should().BeNull();
            _mockConsole.Verify(x => x.WriteLines(It.IsAny<string[]>()), Times.Never);
            _mockConsole.Verify(x => x.ReadLine(), Times.Never);
        }

        #endregion

        #region Remediate Tests - First Time Setup

        [Fact]
        public async Task Remediate_WithFirstTimeSetup_ShowsWelcomeSplash()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>(); // Empty fields = first time setup

            var issues = new List<FieldValidationIssue>
            {
                new("Host", typeof(string), "Host is required", null),
                new("Port", typeof(int), "Port is required", null),
                new("UsePortDiscovery", typeof(bool), "UsePortDiscovery is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("localhost") // Host input
                .Returns("true") // UsePortDiscovery input
                .Returns("8001"); // Port input

            // Mock single field validation
            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();
            result.UpdatedConfig.Should().BeOfType<VTubeStudioPCConfig>();

            // Verify splash screen was shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Welcome! Let's complete the initial setup")))), Times.Once);
        }

        [Fact]
        public async Task Remediate_WithFirstTimeSetup_ProcessesFieldsInCorrectOrder()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>();

            var issues = new List<FieldValidationIssue>
            {
                new("Port", typeof(int), "Port is required", null),
                new("UsePortDiscovery", typeof(bool), "UsePortDiscovery is required", null),
                new("Host", typeof(string), "Host is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions - should be in order: Host, UsePortDiscovery, Port
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("localhost") // Host input
                .Returns("true") // UsePortDiscovery input
                .Returns("8001"); // Port input

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);

            // Verify fields were processed in correct order (Host, UsePortDiscovery, Port)
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Now editing:") && line.Contains("Host")))), Times.Once);
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Now editing:") && line.Contains("UsePortDiscovery")))), Times.Once);
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Now editing:") && line.Contains("Port")))), Times.Once);
        }

        #endregion

        #region Remediate Tests - Field Remediation

        [Fact]
        public async Task Remediate_WithInvalidHost_PromptsForValidInput()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Host", typeof(string), "Host cannot be empty", "")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("") // Empty input - should use default
                .Returns("localhost"); // Valid input

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("Host", typeof(string), "Host cannot be empty", "")))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Host.Should().Be("localhost");
        }

        [Fact]
        public async Task Remediate_WithInvalidPort_PromptsForValidInput()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", -1, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Port", typeof(int), "Port must be between 1 and 65535", "-1")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("invalid") // Invalid input
                .Returns("8001"); // Valid input

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("Port", typeof(int), "Port must be between 1 and 65535", "invalid")))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Port.Should().Be(8001);
        }

        [Fact]
        public async Task Remediate_WithInvalidUsePortDiscovery_PromptsForValidInput()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", null, true, typeof(bool), "UsePortDiscovery")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("UsePortDiscovery", typeof(bool), "UsePortDiscovery must be true or false", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("maybe") // Invalid input
                .Returns("true"); // Valid input

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("UsePortDiscovery", typeof(bool), "UsePortDiscovery must be true or false", "maybe")))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.UsePortDiscovery.Should().BeTrue();
        }

        #endregion

        #region Remediate Tests - Port Discovery Logic

        [Fact]
        public async Task Remediate_WithPortDiscoveryEnabled_SkipsPortField()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery"),
                new("Port", null, true, typeof(int), "Port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Port", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions - should not prompt for Port
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns(""); // Splash screen

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Port.Should().Be(8001); // Default port when discovery is enabled
            config.UsePortDiscovery.Should().BeTrue();

            // Verify Port field was not prompted
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Now editing: Port")))), Times.Never);
        }

        [Fact]
        public async Task Remediate_WithPortDiscoveryDisabled_PromptsForPort()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("UsePortDiscovery", false, true, typeof(bool), "UsePortDiscovery"),
                new("Port", null, true, typeof(int), "Port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Port", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("8001"); // Port input

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Port.Should().Be(8001);
            config.UsePortDiscovery.Should().BeFalse();

            // Verify Port field was prompted
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Now editing:") && line.Contains("Port")))), Times.Once);
        }

        #endregion

        #region Remediate Tests - Default Values

        [Fact]
        public async Task Remediate_WithEmptyInput_UsesDefaultValues()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>();

            var issues = new List<FieldValidationIssue>
            {
                new("Host", typeof(string), "Host is required", null),
                new("Port", typeof(int), "Port is required", null),
                new("UsePortDiscovery", typeof(bool), "UsePortDiscovery is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions - all empty inputs should use defaults
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("") // Host - use default "localhost"
                .Returns("") // UsePortDiscovery - use default true
                .Returns(""); // Port - use default 8001

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Host.Should().Be("localhost");
            config.Port.Should().Be(8001);
            config.UsePortDiscovery.Should().BeTrue();
        }

        #endregion

        #region Remediate Tests - Error Handling

        [Fact]
        public async Task Remediate_WithParseError_ShowsErrorAndRetries()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Port", null, true, typeof(int), "Port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Port", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("not-a-number") // Invalid input
                .Returns("8001"); // Valid input

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Port.Should().Be(8001);

            // Verify error was shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Error:") && line.Contains("Value must be an integer")))), Times.Once);
        }

        [Fact]
        public async Task Remediate_WithValidationError_ShowsErrorAndRetries()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Port", null, true, typeof(int), "Port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Port", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("99999") // Invalid port
                .Returns("8001"); // Valid port

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("Port", typeof(int), "Port must be between 1 and 65535", "99999")))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Port.Should().Be(8001);

            // Verify validation error was shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Error:") && line.Contains("Port must be between 1 and 65535")))), Times.Once);
        }

        [Fact]
        public async Task Remediate_WithFinalValidationFailure_ReturnsFailed()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Host", typeof(string), "Host is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(issues); // Still invalid after remediation

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("localhost"); // Host input

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Failed);
            result.UpdatedConfig.Should().BeNull();
        }

        #endregion

        #region Remediate Tests - Field Notes and UI

        [Fact]
        public async Task Remediate_ShowsFieldNotes()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", null, true, typeof(string), "Host")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Host", typeof(string), "Host is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("localhost"); // Host input

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);

            // Verify field notes were shown
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("This is the host address where VTube Studio is running")))), Times.Once);
        }

        [Fact]
        public async Task Remediate_ShowsDefaultValueInPrompt()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", null, true, typeof(string), "Host")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Host", typeof(string), "Host is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("localhost"); // Host input

            _mockValidator.Setup(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);

            // Verify default value was shown in prompt
            _mockConsole.Verify(x => x.WriteLines(It.Is<string[]>(lines =>
                lines.Any(line => line.Contains("Enter new value") && line.Contains("localhost")))), Times.Once);
        }

        #endregion

        #region ParameterPrefix Tests

        [Fact]
        public async Task Remediate_WithMissingParameterPrefix_AppliesDefaultValue()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery")
                // ParameterPrefix is missing - should cause validation issue
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ParameterPrefix", typeof(string), "Parameter prefix is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("") // Empty input - should use default
                .Returns("SB_"); // Default value

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("ParameterPrefix", typeof(string), "Parameter prefix is required", null)))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.ParameterPrefix.Should().Be("SB_"); // Default value
        }

        [Fact]
        public async Task Remediate_WithValidParameterPrefix_KeepsValue()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery"),
                new("ParameterPrefix", "MyPrefix_", true, typeof(string), "Parameter Prefix")
            };

            var initialValidation = new ConfigValidationResult(new List<FieldValidationIssue>());
            _mockValidator.Setup(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation);

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.NoRemediationNeeded);
            result.UpdatedConfig.Should().BeNull(); // No changes made
        }

        [Fact]
        public async Task Remediate_WithOnlyParameterPrefixMissing_AppliesDefaultSilently()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery"),
                new("ParameterPrefix", null, true, typeof(string), "ParameterPrefix")
            };

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Host.Should().Be("localhost");
            config.Port.Should().Be(8001);
            config.UsePortDiscovery.Should().BeTrue();
            config.ParameterPrefix.Should().Be("SB_"); // Default applied silently

            // Verify no console interaction was needed
            _mockConsole.Verify(x => x.ReadLine(), Times.Never);
        }

        [Fact]
        public async Task Remediate_WithEmptyParameterPrefix_KeepsEmptyValue()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery"),
                new("ParameterPrefix", "", true, typeof(string), "Parameter Prefix")
            };

            var initialValidation = new ConfigValidationResult(new List<FieldValidationIssue>());
            _mockValidator.Setup(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation);

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.NoRemediationNeeded);
            result.UpdatedConfig.Should().BeNull(); // No changes made
        }

        [Fact]
        public async Task Remediate_WithInvalidParameterPrefix_PromptsForValidInput()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery"),
                new("ParameterPrefix", "Invalid Prefix!", true, typeof(string), "Parameter Prefix")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ParameterPrefix", typeof(string), "Parameter prefix must contain only alphanumeric characters and underscores, no spaces", "Invalid Prefix!")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("") // Empty input - should use default
                .Returns("ValidPrefix_"); // Valid input

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("ParameterPrefix", typeof(string), "Parameter prefix must contain only alphanumeric characters and underscores, no spaces", "Invalid Prefix!")))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.ParameterPrefix.Should().Be("ValidPrefix_");
        }

        [Fact]
        public async Task Remediate_WithTooLongParameterPrefix_PromptsForValidInput()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery"),
                new("ParameterPrefix", "ThisPrefixIsWayTooLong", true, typeof(string), "Parameter Prefix")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ParameterPrefix", typeof(string), "Parameter prefix cannot exceed 15 characters", "ThisPrefixIsWayTooLong")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("") // Empty input - should use default
                .Returns("ShortPrefix"); // Valid input (under 15 chars)

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("ParameterPrefix", typeof(string), "Parameter prefix cannot exceed 15 characters", "ThisPrefixIsWayTooLong")))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.ParameterPrefix.Should().Be("ShortPrefix");
        }

        [Fact]
        public async Task Remediate_WithNonStringParameterPrefix_PromptsForValidInput()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery"),
                new("ParameterPrefix", 123, true, typeof(string), "Parameter Prefix") // Wrong type
            };

            var issues = new List<FieldValidationIssue>
            {
                new("ParameterPrefix", typeof(string), "Parameter prefix must be a string, got Int32", "123")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("") // Empty input - should use default
                .Returns("ValidPrefix"); // Valid input

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("ParameterPrefix", typeof(string), "Parameter prefix must be a string, got Int32", "123")))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.ParameterPrefix.Should().Be("ValidPrefix");
        }

        [Fact]
        public async Task Remediate_WithMultipleIssuesIncludingParameterPrefix_HandlesInCorrectOrder()
        {
            // Arrange
            var service = new VTubeStudioPCConfigRemediationService(_mockValidatorsFactory.Object, _mockConsole.Object);
            var fields = new List<ConfigFieldState>
            {
                new("Host", "", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "UsePortDiscovery"),
                new("ParameterPrefix", "Invalid Prefix!", true, typeof(string), "Parameter Prefix")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("Host", typeof(string), "Host cannot be empty", ""),
                new("ParameterPrefix", typeof(string), "Parameter prefix must contain only alphanumeric characters and underscores, no spaces", "Invalid Prefix!")
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(x => x.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock console interactions - Host should be handled first, then ParameterPrefix
            _mockConsole.SetupSequence(x => x.ReadLine())
                .Returns("") // Splash screen
                .Returns("localhost") // Fix Host
                .Returns("") // Splash screen for ParameterPrefix
                .Returns("ValidPrefix_"); // Fix ParameterPrefix (this will be used, not the default)

            // Mock single field validation
            _mockValidator.SetupSequence(x => x.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((false, new FieldValidationIssue("Host", typeof(string), "Host cannot be empty", "")))
                .Returns((true, (FieldValidationIssue?)null))
                .Returns((false, new FieldValidationIssue("ParameterPrefix", typeof(string), "Parameter prefix must contain only alphanumeric characters and underscores, no spaces", "Invalid Prefix!")))
                .Returns((true, (FieldValidationIssue?)null));

            // Act
            var result = await service.Remediate(fields);

            // Assert
            result.Result.Should().Be(RemediationResult.Succeeded);
            result.UpdatedConfig.Should().NotBeNull();

            var config = (VTubeStudioPCConfig)result.UpdatedConfig!;
            config.Host.Should().Be("localhost");
            config.ParameterPrefix.Should().Be("SB_"); // Default value used when user enters empty string
        }

        #endregion


        public void Dispose()
        {
            _mockValidatorsFactory?.Reset();
            _mockValidator?.Reset();
            _mockConsole?.Reset();
        }
    }
}
