using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SharpBridge.Interfaces.Configuration.Factories;
using SharpBridge.Interfaces.Configuration.Services.Validators;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Configuration.Services.Remediation;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Tests.Configuration.Services.Remediation
{
    /// <summary>
    /// Tests for VTubeStudioPhoneClientConfigRemediationService class.
    /// </summary>
    public class VTubeStudioPhoneClientConfigRemediationServiceTests
    {
        private readonly Mock<IConfigSectionValidatorsFactory> _mockValidatorsFactory;
        private readonly Mock<IConfigSectionValidator> _mockValidator;
        private readonly Mock<IConsole> _mockConsole;
        private readonly VTubeStudioPhoneClientConfigRemediationService _service;

        public VTubeStudioPhoneClientConfigRemediationServiceTests()
        {
            _mockValidatorsFactory = new Mock<IConfigSectionValidatorsFactory>();
            _mockValidator = new Mock<IConfigSectionValidator>();
            _mockConsole = new Mock<IConsole>();

            _mockValidatorsFactory.Setup(f => f.GetValidator(ConfigSectionTypes.VTubeStudioPhoneClientConfig))
                .Returns(_mockValidator.Object);

            _service = new VTubeStudioPhoneClientConfigRemediationService(
                _mockValidatorsFactory.Object,
                _mockConsole.Object);
        }


        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange - Create a fresh mock for this test to avoid interference
            var freshMockFactory = new Mock<IConfigSectionValidatorsFactory>();
            var freshMockValidator = new Mock<IConfigSectionValidator>();
            freshMockFactory.Setup(f => f.GetValidator(ConfigSectionTypes.VTubeStudioPhoneClientConfig))
                .Returns(freshMockValidator.Object);

            // Act
            var service = new VTubeStudioPhoneClientConfigRemediationService(
                freshMockFactory.Object,
                _mockConsole.Object);

            // Assert
            Assert.NotNull(service);
            freshMockFactory.Verify(f => f.GetValidator(ConfigSectionTypes.VTubeStudioPhoneClientConfig), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullValidatorsFactory_ThrowsNullReferenceException()
        {
            // Arrange & Act & Assert
            // The constructor doesn't validate parameters - it will throw NullReferenceException when trying to call GetValidator
            Assert.Throws<NullReferenceException>(() =>
                new VTubeStudioPhoneClientConfigRemediationService(null!, _mockConsole.Object));
        }

        [Fact]
        public void Constructor_WithNullConsole_InitializesCorrectly()
        {
            // Arrange & Act
            // The console parameter is just stored, so null is technically allowed (though not recommended)
            var service = new VTubeStudioPhoneClientConfigRemediationService(_mockValidatorsFactory.Object, null!);

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        #region Remediate Tests - No Remediation Needed

        [Fact]
        public async Task Remediate_WithValidSection_ReturnsNoRemediationNeeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", "192.168.1.100", true, typeof(string), "iPhone IP address"),
                new("IphonePort", 21412, true, typeof(int), "iPhone port"),
                new("LocalPort", 28964, true, typeof(int), "Local port")
            };

            var validationResult = new ConfigValidationResult(new List<FieldValidationIssue>());
            _mockValidator.Setup(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(validationResult);

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.NoRemediationNeeded, result.Result);
            Assert.Null(result.UpdatedConfig);

            // Should not have any console interactions
            _mockConsole.Verify(c => c.WriteLines(It.IsAny<string[]>()), Times.Never);
            _mockConsole.Verify(c => c.ReadLine(), Times.Never);
        }

        #endregion

        #region Remediate Tests - Successful Remediation

        [Fact]
        public async Task Remediate_WithSingleInvalidField_RemediatesSuccessfully()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", null, false, typeof(string), "iPhone IP address"),
                new("IphonePort", 21412, true, typeof(int), "iPhone port"),
                new("LocalPort", 28964, true, typeof(int), "Local port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("IphoneIpAddress", typeof(string), "IP address is required", null)
            };

            // Initial validation shows invalid
            var initialValidation = new ConfigValidationResult(issues);
            // Final validation shows valid after remediation
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock single field validation to return valid for our test input
            _mockValidator.Setup(v => v.ValidateSingleField(It.Is<ConfigFieldState>(f =>
                f.FieldName == "IphoneIpAddress" && f.Value!.Equals("192.168.1.100"))))
                .Returns((true, (FieldValidationIssue?)null));

            // Mock console interactions - splash screen read, then field input read
            _mockConsole.SetupSequence(c => c.ReadLine())
                .Returns("") // User presses Enter on splash screen
                .Returns("192.168.1.100"); // User inputs valid IP

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.Succeeded, result.Result);
            Assert.NotNull(result.UpdatedConfig);

            // Verify console interactions occurred
            _mockConsole.Verify(c => c.WriteLines(It.IsAny<string[]>()), Times.AtLeastOnce);
            _mockConsole.Verify(c => c.ReadLine(), Times.Exactly(2)); // Splash + field input
        }

        [Fact]
        public async Task Remediate_WithMultipleInvalidFields_RemediatesAllSuccessfully()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", null, false, typeof(string), "iPhone IP address"),
                new("IphonePort", null, false, typeof(int), "iPhone port"),
                new("LocalPort", 28964, true, typeof(int), "Local port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("IphoneIpAddress", typeof(string), "IP address is required", null),
                new("IphonePort", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock field validations
            _mockValidator.Setup(v => v.ValidateSingleField(It.Is<ConfigFieldState>(f =>
                f.FieldName == "IphoneIpAddress" && f.Value!.Equals("192.168.1.100"))))
                .Returns((true, (FieldValidationIssue?)null));

            _mockValidator.Setup(v => v.ValidateSingleField(It.Is<ConfigFieldState>(f =>
                f.FieldName == "IphonePort" && f.Value!.Equals(21412))))
                .Returns((true, (FieldValidationIssue?)null));

            // Mock console to return different values for each field
            _mockConsole.SetupSequence(c => c.ReadLine())
                .Returns("") // Splash screen
                .Returns("192.168.1.100") // IP address
                .Returns("21412");        // Port

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.Succeeded, result.Result);
            Assert.NotNull(result.UpdatedConfig);

            // Should have prompted for splash + each field
            _mockConsole.Verify(c => c.ReadLine(), Times.Exactly(3));
        }

        #endregion

        #region Remediate Tests - Default Values

        [Fact]
        public async Task Remediate_WithEmptyInputForLocalPort_UsesDefaultValue()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", "192.168.1.100", true, typeof(string), "iPhone IP address"),
                new("IphonePort", 21412, true, typeof(int), "iPhone port"),
                new("LocalPort", null, false, typeof(int), "Local port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("LocalPort", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock field validation to accept default port
            _mockValidator.Setup(v => v.ValidateSingleField(It.Is<ConfigFieldState>(f =>
                f.FieldName == "LocalPort" && f.Value!.Equals(28964))))
                .Returns((true, (FieldValidationIssue?)null));

            // Mock console to return empty input (should use default)
            _mockConsole.SetupSequence(c => c.ReadLine())
                .Returns("") // Splash screen
                .Returns(""); // Empty input for LocalPort (should use default)

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.Succeeded, result.Result);
            Assert.NotNull(result.UpdatedConfig);

            var config = result.UpdatedConfig as VTubeStudioPhoneClientConfig;
            Assert.NotNull(config);
            Assert.Equal(28964, config.LocalPort); // Should use default value
        }

        #endregion

        #region Remediate Tests - Input Validation and Retry

        [Fact]
        public async Task Remediate_WithInvalidThenValidInput_RetriesUntilValid()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphonePort", null, false, typeof(int), "iPhone port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("IphonePort", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock field validation - first call invalid, second call valid
            _mockValidator.SetupSequence(v => v.ValidateSingleField(It.Is<ConfigFieldState>(f => f.FieldName == "IphonePort")))
                .Returns((false, new FieldValidationIssue("IphonePort", typeof(int), "Port out of range", "70000")))
                .Returns((true, (FieldValidationIssue?)null));

            // Mock console inputs - splash, first invalid, then valid
            _mockConsole.SetupSequence(c => c.ReadLine())
                .Returns("") // Splash screen
                .Returns("70000")  // Invalid port (out of range)
                .Returns("21412"); // Valid port

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.Succeeded, result.Result);
            Assert.NotNull(result.UpdatedConfig);

            // Should have prompted for splash + retry
            _mockConsole.Verify(c => c.ReadLine(), Times.Exactly(3));
            // Should have written multiple frames (initial + retry)
            _mockConsole.Verify(c => c.WriteLines(It.IsAny<string[]>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task Remediate_WithUnparsableInput_ShowsParseErrorAndRetries()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphonePort", null, false, typeof(int), "iPhone port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("IphonePort", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            _mockValidator.Setup(v => v.ValidateSingleField(It.Is<ConfigFieldState>(f =>
                f.FieldName == "IphonePort" && f.Value!.Equals(21412))))
                .Returns((true, (FieldValidationIssue?)null));

            // Mock console inputs - splash, first unparsable, then valid
            _mockConsole.SetupSequence(c => c.ReadLine())
                .Returns("") // Splash screen
                .Returns("not-a-number") // Unparsable input
                .Returns("21412");       // Valid port

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.Succeeded, result.Result);
            Assert.NotNull(result.UpdatedConfig);

            // Should have prompted for splash + parse retry
            _mockConsole.Verify(c => c.ReadLine(), Times.Exactly(3));
        }

        #endregion

        #region Remediate Tests - Edge Cases

        [Fact]
        public async Task Remediate_WithEmptyFieldsList_ReturnsNoRemediationNeeded()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>();
            var validationResult = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.Setup(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(validationResult);

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.NoRemediationNeeded, result.Result);
            Assert.Null(result.UpdatedConfig);
        }

        [Fact]
        public async Task Remediate_WhenFinalValidationFails_ReturnsFailedResult()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", null, false, typeof(string), "iPhone IP address")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("IphoneIpAddress", typeof(string), "IP address is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            // Final validation still shows invalid (edge case)
            var finalValidation = new ConfigValidationResult(issues);

            _mockValidator.SetupSequence(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation); // Still invalid after remediation

            _mockValidator.Setup(v => v.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(c => c.ReadLine())
                .Returns("") // Splash screen  
                .Returns("192.168.1.100");

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.Failed, result.Result);
            Assert.Null(result.UpdatedConfig);
        }

        #endregion

        #region Helper Method Tests

        [Fact]
        public async Task Remediate_CreatesValidConfigObject()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", null, false, typeof(string), "iPhone IP address"),
                new("IphonePort", null, false, typeof(int), "iPhone port"),
                new("LocalPort", null, false, typeof(int), "Local port")
            };

            var issues = new List<FieldValidationIssue>
            {
                new("IphoneIpAddress", typeof(string), "IP address is required", null),
                new("IphonePort", typeof(int), "Port is required", null),
                new("LocalPort", typeof(int), "Port is required", null)
            };

            var initialValidation = new ConfigValidationResult(issues);
            var finalValidation = new ConfigValidationResult(new List<FieldValidationIssue>());

            _mockValidator.SetupSequence(v => v.ValidateSection(It.IsAny<List<ConfigFieldState>>()))
                .Returns(initialValidation)
                .Returns(finalValidation);

            // Mock all field validations as valid
            _mockValidator.Setup(v => v.ValidateSingleField(It.IsAny<ConfigFieldState>()))
                .Returns((true, (FieldValidationIssue?)null));

            _mockConsole.SetupSequence(c => c.ReadLine())
                .Returns("") // Splash screen
                .Returns("192.168.1.100") // IP
                .Returns("21412")         // iPhone port
                .Returns("28964");        // Local port

            // Act
            var result = await _service.Remediate(fieldsState);

            // Assert
            Assert.Equal(RemediationResult.Succeeded, result.Result);
            Assert.NotNull(result.UpdatedConfig);

            var config = result.UpdatedConfig as VTubeStudioPhoneClientConfig;
            Assert.NotNull(config);
            Assert.Equal("192.168.1.100", config.IphoneIpAddress);
            Assert.Equal(21412, config.IphonePort);
            Assert.Equal(28964, config.LocalPort);
        }

        #endregion
    }
}
