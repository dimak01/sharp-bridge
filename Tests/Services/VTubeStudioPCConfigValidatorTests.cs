using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.Validators;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class VTubeStudioPCConfigValidatorTests
    {
        private readonly Mock<IConfigFieldValidator> _mockFieldValidator;
        private readonly VTubeStudioPCConfigValidator _validator;

        public VTubeStudioPCConfigValidatorTests()
        {
            _mockFieldValidator = new Mock<IConfigFieldValidator>();
            _validator = new VTubeStudioPCConfigValidator(_mockFieldValidator.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullFieldValidator_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new VTubeStudioPCConfigValidator(null!));
            exception.ParamName.Should().Be("fieldValidator");
        }

        [Fact]
        public void Constructor_WithValidFieldValidator_InitializesSuccessfully()
        {
            // Arrange
            var mockFieldValidator = new Mock<IConfigFieldValidator>();

            // Act
            var validator = new VTubeStudioPCConfigValidator(mockFieldValidator.Object);

            // Assert
            validator.Should().NotBeNull();
        }

        #endregion

        #region ValidateSection Tests

        [Fact]
        public void ValidateSection_WithEmptyFieldsList_ReturnsValidResult()
        {
            // Arrange
            var fields = new List<ConfigFieldState>();

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void ValidateSection_WithAllValidFields_ReturnsValidResult()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 8001, true, typeof(int), "Port"),
                new("UsePortDiscovery", true, true, typeof(bool), "Use Port Discovery")
            };

            _mockFieldValidator.Setup(x => x.ValidateHost(It.IsAny<ConfigFieldState>()))
                .Returns((FieldValidationIssue?)null);
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>()))
                .Returns((FieldValidationIssue?)null);
            _mockFieldValidator.Setup(x => x.ValidateBoolean(It.IsAny<ConfigFieldState>()))
                .Returns((FieldValidationIssue?)null);

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void ValidateSection_WithInvalidFields_ReturnsInvalidResultWithIssues()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("Host", null, true, typeof(string), "Host"),
                new("Port", 99999, true, typeof(int), "Port")
            };

            var hostIssue = new FieldValidationIssue("Host", typeof(string), "Invalid host", "null");
            var portIssue = new FieldValidationIssue("Port", typeof(int), "Invalid port", "99999");

            _mockFieldValidator.Setup(x => x.ValidateHost(It.IsAny<ConfigFieldState>()))
                .Returns(hostIssue);
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>()))
                .Returns(portIssue);

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Issues.Should().HaveCount(2);
            result.Issues.Should().Contain(i => i.FieldName == "Host");
            result.Issues.Should().Contain(i => i.FieldName == "Port");
        }

        [Fact]
        public void ValidateSection_WithMixedValidAndInvalidFields_ReturnsInvalidResultWithPartialIssues()
        {
            // Arrange
            var fields = new List<ConfigFieldState>
            {
                new("Host", "localhost", true, typeof(string), "Host"),
                new("Port", 99999, true, typeof(int), "Port"),
                new("UnknownField", "value", true, typeof(string), "Unknown Field")
            };

            _mockFieldValidator.Setup(x => x.ValidateHost(It.IsAny<ConfigFieldState>()))
                .Returns((FieldValidationIssue?)null);
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>()))
                .Returns(new FieldValidationIssue("Port", typeof(int), "Invalid port", "99999"));

            // Act
            var result = _validator.ValidateSection(fields);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Issues.Should().HaveCount(2);
            result.Issues.Should().Contain(i => i.FieldName == "Port");
            result.Issues.Should().Contain(i => i.FieldName == "UnknownField");
        }

        #endregion

        #region ValidateSingleField Tests

        [Fact]
        public void ValidateSingleField_WithConnectionTimeoutMs_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("ConnectionTimeoutMs", 5000, true, typeof(int), "Connection Timeout");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithReconnectionDelayMs_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("ReconnectionDelayMs", 1000, true, typeof(int), "Reconnection Delay");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithRecoveryIntervalSeconds_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("RecoveryIntervalSeconds", 30, true, typeof(int), "Recovery Interval");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithMissingField_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("Host", "localhost", false, typeof(string), "Host");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("Host");
            issue.Description.Should().Be("Required field 'Host' is missing");
            issue.ProvidedValueText.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithNullValue_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("Host", null, true, typeof(string), "Host");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("Host");
            issue.Description.Should().Be("Required field 'Host' is missing");
            issue.ProvidedValueText.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithValidHost_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("Host", "localhost", true, typeof(string), "Host");

            _mockFieldValidator.Setup(x => x.ValidateHost(field))
                .Returns((FieldValidationIssue?)null);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
            _mockFieldValidator.Verify(x => x.ValidateHost(field), Times.Once);
        }

        [Fact]
        public void ValidateSingleField_WithValidPort_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("Port", 8001, true, typeof(int), "Port");

            _mockFieldValidator.Setup(x => x.ValidatePort(field))
                .Returns((FieldValidationIssue?)null);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
            _mockFieldValidator.Verify(x => x.ValidatePort(field), Times.Once);
        }

        [Fact]
        public void ValidateSingleField_WithValidUsePortDiscovery_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("UsePortDiscovery", true, true, typeof(bool), "Use Port Discovery");

            _mockFieldValidator.Setup(x => x.ValidateBoolean(field))
                .Returns((FieldValidationIssue?)null);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeTrue();
            issue.Should().BeNull();
            _mockFieldValidator.Verify(x => x.ValidateBoolean(field), Times.Once);
        }

        [Fact]
        public void ValidateSingleField_WithUnknownField_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", "value", true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("UnknownField");
            issue.Description.Should().Contain("Unknown field 'UnknownField' in VTubeStudioPCConfig");
            issue.ProvidedValueText.Should().Be("value");
        }

        #endregion

        #region ValidateFieldValue Tests

        [Fact]
        public void ValidateFieldValue_WithHostField_CallsValidateHost()
        {
            // Arrange
            var field = new ConfigFieldState("Host", "localhost", true, typeof(string), "Host");
            var expectedIssue = new FieldValidationIssue("Host", typeof(string), "Invalid host", "localhost");

            _mockFieldValidator.Setup(x => x.ValidateHost(field))
                .Returns(expectedIssue);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().Be(expectedIssue);
            _mockFieldValidator.Verify(x => x.ValidateHost(field), Times.Once);
        }

        [Fact]
        public void ValidateFieldValue_WithPortField_CallsValidatePort()
        {
            // Arrange
            var field = new ConfigFieldState("Port", 8001, true, typeof(int), "Port");
            var expectedIssue = new FieldValidationIssue("Port", typeof(int), "Invalid port", "8001");

            _mockFieldValidator.Setup(x => x.ValidatePort(field))
                .Returns(expectedIssue);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().Be(expectedIssue);
            _mockFieldValidator.Verify(x => x.ValidatePort(field), Times.Once);
        }

        [Fact]
        public void ValidateFieldValue_WithUsePortDiscoveryField_CallsValidateBoolean()
        {
            // Arrange
            var field = new ConfigFieldState("UsePortDiscovery", true, true, typeof(bool), "Use Port Discovery");
            var expectedIssue = new FieldValidationIssue("UsePortDiscovery", typeof(bool), "Invalid boolean", "true");

            _mockFieldValidator.Setup(x => x.ValidateBoolean(field))
                .Returns(expectedIssue);

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().Be(expectedIssue);
            _mockFieldValidator.Verify(x => x.ValidateBoolean(field), Times.Once);
        }

        [Fact]
        public void ValidateFieldValue_WithUnknownField_ReturnsUnknownFieldError()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", "value", true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("UnknownField");
            issue.Description.Should().Contain("Unknown field 'UnknownField' in VTubeStudioPCConfig");
            issue.ProvidedValueText.Should().Be("value");
        }

        #endregion

        #region CreateUnknownFieldError Tests

        [Fact]
        public void CreateUnknownFieldError_WithStringValue_ReturnsCorrectError()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", "test value", true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("UnknownField");
            issue.ExpectedType.Should().Be(typeof(string));
            issue.Description.Should().Contain("Unknown field 'UnknownField' in VTubeStudioPCConfig");
            issue.ProvidedValueText.Should().Be("test value");
        }

        [Fact]
        public void CreateUnknownFieldError_WithValidValue_ReturnsCorrectError()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", "some value", true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("UnknownField");
            issue.ExpectedType.Should().Be(typeof(string));
            issue.Description.Should().Contain("Unknown field 'UnknownField' in VTubeStudioPCConfig");
            issue.ProvidedValueText.Should().Be("some value");
        }

        [Fact]
        public void CreateUnknownFieldError_WithNonStringValue_ReturnsCorrectError()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", 123, true, typeof(int), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("UnknownField");
            issue.ExpectedType.Should().Be(typeof(int));
            issue.Description.Should().Contain("Unknown field 'UnknownField' in VTubeStudioPCConfig");
            issue.ProvidedValueText.Should().Be("123");
        }

        [Fact]
        public void ValidateSingleField_WithUnknownFieldAndNullValue_ReturnsMissingFieldError()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", null, true, typeof(string), "Unknown Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("UnknownField");
            issue.ExpectedType.Should().Be(typeof(string));
            issue.Description.Should().Be("Required field 'Unknown Field' is missing");
            issue.ProvidedValueText.Should().BeNull();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ValidateSingleField_WithEmptyFieldName_HandlesGracefully()
        {
            // Arrange
            var field = new ConfigFieldState("", "value", true, typeof(string), "Test Field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            isValid.Should().BeFalse();
            issue.Should().NotBeNull();
            issue!.FieldName.Should().Be("");
        }

        [Fact]
        public void ValidateSingleField_WithInternalSettingsAndNullValues_StillReturnsValid()
        {
            // Arrange
            var connectionTimeoutField = new ConfigFieldState("ConnectionTimeoutMs", null, true, typeof(int), "Connection Timeout");
            var reconnectionDelayField = new ConfigFieldState("ReconnectionDelayMs", null, true, typeof(int), "Reconnection Delay");
            var recoveryIntervalField = new ConfigFieldState("RecoveryIntervalSeconds", null, true, typeof(int), "Recovery Interval");

            // Act
            var (connectionTimeoutIsValid, connectionTimeoutIssue) = _validator.ValidateSingleField(connectionTimeoutField);
            var (reconnectionDelayIsValid, reconnectionDelayIssue) = _validator.ValidateSingleField(reconnectionDelayField);
            var (recoveryIntervalIsValid, recoveryIntervalIssue) = _validator.ValidateSingleField(recoveryIntervalField);

            // Assert
            connectionTimeoutIsValid.Should().BeTrue();
            connectionTimeoutIssue.Should().BeNull();
            reconnectionDelayIsValid.Should().BeTrue();
            reconnectionDelayIssue.Should().BeNull();
            recoveryIntervalIsValid.Should().BeTrue();
            recoveryIntervalIssue.Should().BeNull();
        }

        [Fact]
        public void ValidateSingleField_WithInternalSettingsAndMissingFields_StillReturnsValid()
        {
            // Arrange
            var connectionTimeoutField = new ConfigFieldState("ConnectionTimeoutMs", 5000, false, typeof(int), "Connection Timeout");
            var reconnectionDelayField = new ConfigFieldState("ReconnectionDelayMs", 1000, false, typeof(int), "Reconnection Delay");
            var recoveryIntervalField = new ConfigFieldState("RecoveryIntervalSeconds", 30, false, typeof(int), "Recovery Interval");

            // Act
            var (connectionTimeoutIsValid, connectionTimeoutIssue) = _validator.ValidateSingleField(connectionTimeoutField);
            var (reconnectionDelayIsValid, reconnectionDelayIssue) = _validator.ValidateSingleField(reconnectionDelayField);
            var (recoveryIntervalIsValid, recoveryIntervalIssue) = _validator.ValidateSingleField(recoveryIntervalField);

            // Assert
            connectionTimeoutIsValid.Should().BeTrue();
            connectionTimeoutIssue.Should().BeNull();
            reconnectionDelayIsValid.Should().BeTrue();
            reconnectionDelayIssue.Should().BeNull();
            recoveryIntervalIsValid.Should().BeTrue();
            recoveryIntervalIssue.Should().BeNull();
        }

        #endregion
    }
}
