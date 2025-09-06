using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services.Validators;

namespace SharpBridge.Tests.Services
{
    /// <summary>
    /// Tests for VTubeStudioPhoneClientConfigValidator class.
    /// </summary>
    public class VTubeStudioPhoneClientConfigValidatorTests
    {
        private readonly VTubeStudioPhoneClientConfigValidator _validator;
        private readonly Mock<IConfigFieldValidator> _mockFieldValidator;

        public VTubeStudioPhoneClientConfigValidatorTests()
        {
            _mockFieldValidator = new Mock<IConfigFieldValidator>();
            _validator = new VTubeStudioPhoneClientConfigValidator(_mockFieldValidator.Object);
        }

        /// <summary>
        /// Sets up the mock field validator to return null (valid) for all field validations.
        /// </summary>
        private void SetupValidFieldValidator()
        {
            _mockFieldValidator.Setup(x => x.ValidateIpAddress(It.IsAny<ConfigFieldState>())).Returns((FieldValidationIssue?)null);
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>())).Returns((FieldValidationIssue?)null);
        }

        /// <summary>
        /// Sets up the mock field validator to return validation issues for invalid IP addresses.
        /// </summary>
        private void SetupInvalidIpFieldValidator()
        {
            _mockFieldValidator.Setup(x => x.ValidateIpAddress(It.IsAny<ConfigFieldState>()))
                .Returns<ConfigFieldState>(field => new FieldValidationIssue(field.FieldName, field.ExpectedType, "Invalid IP address", field.Value?.ToString()));
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>())).Returns((FieldValidationIssue?)null);
        }

        /// <summary>
        /// Sets up the mock field validator to return validation issues for invalid ports.
        /// </summary>
        private void SetupInvalidPortFieldValidator()
        {
            _mockFieldValidator.Setup(x => x.ValidateIpAddress(It.IsAny<ConfigFieldState>())).Returns((FieldValidationIssue?)null);
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>()))
                .Returns<ConfigFieldState>(field => new FieldValidationIssue(field.FieldName, field.ExpectedType, "Invalid port", field.Value?.ToString()));
        }

        #region ValidateSection Tests

        [Fact]
        public void ValidateSection_WithValidFields_ReturnsValidResult()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", "192.168.1.100", true, typeof(string), "iPhone IP address"),
                new("IphonePort", 21412, true, typeof(int), "iPhone port number"),
                new("LocalPort", 28964, true, typeof(int), "Local UDP port")
            };

            SetupValidFieldValidator();

            // Act
            var result = _validator.ValidateSection(fieldsState);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void ValidateSection_WithMissingRequiredFields_ReturnsInvalidResult()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", null, false, typeof(string), "iPhone IP address"),
                new("IphonePort", null, false, typeof(int), "iPhone port number"),
                new("LocalPort", 28964, true, typeof(int), "Local UDP port")
            };

            SetupValidFieldValidator();

            // Act
            var result = _validator.ValidateSection(fieldsState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(2, result.Issues.Count);

            var ipIssue = result.Issues.Find(i => i.FieldName == "IphoneIpAddress");
            Assert.NotNull(ipIssue);
            Assert.Equal(typeof(string), ipIssue.ExpectedType);
            Assert.Null(ipIssue.ProvidedValueText);

            var portIssue = result.Issues.Find(i => i.FieldName == "IphonePort");
            Assert.NotNull(portIssue);
            Assert.Equal(typeof(int), portIssue.ExpectedType);
            Assert.Null(portIssue.ProvidedValueText);
        }

        [Fact]
        public void ValidateSection_WithInvalidFieldValues_ReturnsInvalidResult()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", "invalid-ip", true, typeof(string), "iPhone IP address"),
                new("IphonePort", 70000, true, typeof(int), "iPhone port number"),
                new("LocalPort", -1, true, typeof(int), "Local UDP port")
            };

            // Setup mock to return validation issues for all invalid values
            _mockFieldValidator.Setup(x => x.ValidateIpAddress(It.IsAny<ConfigFieldState>()))
                .Returns<ConfigFieldState>(field => new FieldValidationIssue(field.FieldName, field.ExpectedType, "Invalid IP address", field.Value?.ToString()));
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>()))
                .Returns<ConfigFieldState>(field => new FieldValidationIssue(field.FieldName, field.ExpectedType, "Invalid port", field.Value?.ToString()));

            // Act
            var result = _validator.ValidateSection(fieldsState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(3, result.Issues.Count);

            // Verify each specific validation error
            Assert.Contains(result.Issues, i => i.FieldName == "IphoneIpAddress" && i.ProvidedValueText == "invalid-ip");
            Assert.Contains(result.Issues, i => i.FieldName == "IphonePort" && i.ProvidedValueText == "70000");
            Assert.Contains(result.Issues, i => i.FieldName == "LocalPort" && i.ProvidedValueText == "-1");
        }

        [Fact]
        public void ValidateSection_WithJsonIgnoreFields_IgnoresThemCorrectly()
        {
            // Arrange - Include JsonIgnore fields that should be skipped
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", "192.168.1.100", true, typeof(string), "iPhone IP address"),
                new("IphonePort", 21412, true, typeof(int), "iPhone port number"),
                new("LocalPort", 28964, true, typeof(int), "Local UDP port"),
                new("RequestIntervalSeconds", null, false, typeof(int), "Request interval"), // JsonIgnore
                new("SendForSeconds", null, false, typeof(int), "Send duration"), // JsonIgnore
                new("ReceiveTimeoutMs", null, false, typeof(int), "Receive timeout"), // JsonIgnore
                new("ErrorDelayMs", null, false, typeof(int), "Error delay") // JsonIgnore
            };

            // Act
            var result = _validator.ValidateSection(fieldsState);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void ValidateSection_WithEmptyFieldsList_ReturnsValidResult()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>();

            // Act
            var result = _validator.ValidateSection(fieldsState);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        #endregion

        #region ValidateSingleField Tests

        [Fact]
        public void ValidateSingleField_WithJsonIgnoreField_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("RequestIntervalSeconds", null, false, typeof(int), "Request interval");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.True(isValid);
            Assert.Null(issue);
        }

        [Fact]
        public void ValidateSingleField_WithMissingRequiredField_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("IphoneIpAddress", null, false, typeof(string), "iPhone IP address");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("IphoneIpAddress", issue.FieldName);
            Assert.Equal(typeof(string), issue.ExpectedType);
            Assert.Null(issue.ProvidedValueText);
        }

        [Fact]
        public void ValidateSingleField_WithValidField_ReturnsValid()
        {
            // Arrange
            var field = new ConfigFieldState("IphoneIpAddress", "192.168.1.100", true, typeof(string), "iPhone IP address");

            SetupValidFieldValidator();

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.True(isValid);
            Assert.Null(issue);
        }

        #endregion

        #region IP Address Validation Tests

        [Theory]
        [InlineData("192.168.1.1")]
        [InlineData("10.0.0.1")]
        [InlineData("172.16.0.1")]
        [InlineData("8.8.8.8")]
        [InlineData("127.0.0.1")] // localhost is allowed
        [InlineData("::1")] // IPv6 localhost
        [InlineData("2001:db8::1")] // IPv6
        public void ValidateSingleField_WithValidIpAddress_ReturnsValid(string ipAddress)
        {
            // Arrange
            var field = new ConfigFieldState("IphoneIpAddress", ipAddress, true, typeof(string), "iPhone IP address");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.True(isValid);
            Assert.Null(issue);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateSingleField_WithEmptyIpAddress_ReturnsInvalid(string invalidIp)
        {
            // Arrange
            var field = new ConfigFieldState("IphoneIpAddress", invalidIp, true, typeof(string), "iPhone IP address");

            SetupInvalidIpFieldValidator();

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("IphoneIpAddress", issue.FieldName);
            Assert.Equal(typeof(string), issue.ExpectedType);
            Assert.Contains("Invalid IP address", issue.Description);
        }

        [Theory]
        [InlineData("invalid-ip")]
        [InlineData("999.999.999.999")]
        [InlineData("192.168.1.256")]
        [InlineData("not-an-ip-address")]
        public void ValidateSingleField_WithInvalidIpAddressFormat_ReturnsInvalid(string invalidIp)
        {
            // Arrange
            var field = new ConfigFieldState("IphoneIpAddress", invalidIp, true, typeof(string), "iPhone IP address");

            SetupInvalidIpFieldValidator();

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("IphoneIpAddress", issue.FieldName);
            Assert.Equal(typeof(string), issue.ExpectedType);
            Assert.Contains("Invalid IP address", issue.Description);
        }

        [Fact]
        public void ValidateSingleField_WithNullIpAddress_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("IphoneIpAddress", null, false, typeof(string), "iPhone IP address");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("IphoneIpAddress", issue.FieldName);
            Assert.Equal(typeof(string), issue.ExpectedType);
            // Null fields are handled as missing fields, not empty string validation
            Assert.Equal("Required field 'iPhone IP address' is missing", issue.Description);
        }

        [Fact]
        public void ValidateSingleField_WithNonStringIpAddress_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("IphoneIpAddress", 12345, true, typeof(string), "iPhone IP address");

            SetupInvalidIpFieldValidator();

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("IphoneIpAddress", issue.FieldName);
            Assert.Contains("Invalid IP address", issue.Description);
        }

        #endregion

        #region Port Validation Tests

        [Theory]
        [InlineData(1)]
        [InlineData(80)]
        [InlineData(443)]
        [InlineData(1024)]
        [InlineData(8080)]
        [InlineData(21412)]
        [InlineData(28964)]
        [InlineData(65535)]
        public void ValidateSingleField_WithValidPort_ReturnsValid(int port)
        {
            // Arrange
            var field = new ConfigFieldState("IphonePort", port, true, typeof(int), "iPhone port number");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.True(isValid);
            Assert.Null(issue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(65536)]
        [InlineData(70000)]
        [InlineData(100000)]
        public void ValidateSingleField_WithInvalidPortRange_ReturnsInvalid(int invalidPort)
        {
            // Arrange
            var field = new ConfigFieldState("LocalPort", invalidPort, true, typeof(int), "Local UDP port");

            SetupInvalidPortFieldValidator();

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("LocalPort", issue.FieldName);
            Assert.Equal(typeof(int), issue.ExpectedType);
            Assert.Contains("Invalid port", issue.Description);
            Assert.Equal(invalidPort.ToString(), issue.ProvidedValueText);
        }

        [Theory]
        [InlineData("not-a-number")]
        [InlineData("8080")]
        [InlineData(3.14)]
        public void ValidateSingleField_WithNonIntegerPort_ReturnsInvalid(object? invalidPort)
        {
            // Arrange
            var field = new ConfigFieldState("IphonePort", invalidPort, true, typeof(int), "iPhone port number");

            SetupInvalidPortFieldValidator();

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("IphonePort", issue.FieldName);
            Assert.Equal(typeof(int), issue.ExpectedType);
            Assert.Contains("Invalid port", issue.Description);
        }

        [Fact]
        public void ValidateSingleField_WithNullPort_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("IphonePort", null, false, typeof(int), "iPhone port number");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("IphonePort", issue.FieldName);
            Assert.Equal(typeof(int), issue.ExpectedType);
            // Null fields are handled as missing fields, not type validation
            Assert.Equal("Required field 'iPhone port number' is missing", issue.Description);
        }

        [Fact]
        public void ValidateSingleField_WithBothPortTypes_ValidatesSeparately()
        {
            // Arrange
            var iphonePortField = new ConfigFieldState("IphonePort", 21412, true, typeof(int), "iPhone port number");
            var localPortField = new ConfigFieldState("LocalPort", 28964, true, typeof(int), "Local UDP port");

            // Act
            var (iphoneValid, iphoneIssue) = _validator.ValidateSingleField(iphonePortField);
            var (localValid, localIssue) = _validator.ValidateSingleField(localPortField);

            // Assert
            Assert.True(iphoneValid);
            Assert.Null(iphoneIssue);
            Assert.True(localValid);
            Assert.Null(localIssue);
        }

        #endregion

        #region Unknown Field Tests

        [Fact]
        public void ValidateSingleField_WithUnknownField_ReturnsInvalid()
        {
            // Arrange
            var field = new ConfigFieldState("UnknownField", "some-value", true, typeof(string), "Unknown field");

            // Act
            var (isValid, issue) = _validator.ValidateSingleField(field);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(issue);
            Assert.Equal("UnknownField", issue.FieldName);
            Assert.Equal(typeof(string), issue.ExpectedType);
            Assert.Contains("Unknown field 'UnknownField'", issue.Description);
            Assert.Equal("some-value", issue.ProvidedValueText);
        }

        #endregion

        #region Edge Cases and Comprehensive Scenarios

        [Fact]
        public void ValidateSection_WithMixedValidAndInvalidFields_ReturnsCorrectResult()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", "192.168.1.100", true, typeof(string), "iPhone IP address"), // Valid
                new("IphonePort", 70000, true, typeof(int), "iPhone port number"), // Invalid - out of range
                new("LocalPort", 28964, true, typeof(int), "Local UDP port"), // Valid
                new("RequestIntervalSeconds", null, false, typeof(int), "Request interval") // JsonIgnore - should be skipped
            };

            // Setup mock to return validation issues for invalid ports only
            _mockFieldValidator.Setup(x => x.ValidateIpAddress(It.IsAny<ConfigFieldState>())).Returns((FieldValidationIssue?)null);
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>()))
                .Returns<ConfigFieldState>(field => field.Value?.ToString() == "70000"
                    ? new FieldValidationIssue(field.FieldName, field.ExpectedType, "Invalid port", field.Value?.ToString())
                    : (FieldValidationIssue?)null);

            // Act
            var result = _validator.ValidateSection(fieldsState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Issues);
            Assert.Equal("IphonePort", result.Issues[0].FieldName);
            Assert.Contains("Invalid port", result.Issues[0].Description);
        }

        [Fact]
        public void ValidateSection_WithAllJsonIgnoreFields_ReturnsValid()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("RequestIntervalSeconds", null, false, typeof(int), "Request interval"),
                new("SendForSeconds", null, false, typeof(int), "Send duration"),
                new("ReceiveTimeoutMs", null, false, typeof(int), "Receive timeout"),
                new("ErrorDelayMs", null, false, typeof(int), "Error delay")
            };

            // Act
            var result = _validator.ValidateSection(fieldsState);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void ValidateSection_WithCompleteInvalidConfiguration_ReturnsAllIssues()
        {
            // Arrange
            var fieldsState = new List<ConfigFieldState>
            {
                new("IphoneIpAddress", "", true, typeof(string), "iPhone IP address"), // Invalid - empty
                new("IphonePort", "not-a-port", true, typeof(int), "iPhone port number"), // Invalid - wrong type
                new("LocalPort", 0, true, typeof(int), "Local UDP port"), // Invalid - out of range
                new("UnknownField", "value", true, typeof(string), "Unknown field") // Invalid - unknown field
            };

            // Setup mock to return validation issues for all invalid values
            _mockFieldValidator.Setup(x => x.ValidateIpAddress(It.IsAny<ConfigFieldState>()))
                .Returns<ConfigFieldState>(field => new FieldValidationIssue(field.FieldName, field.ExpectedType, "Invalid IP address", field.Value?.ToString()));
            _mockFieldValidator.Setup(x => x.ValidatePort(It.IsAny<ConfigFieldState>()))
                .Returns<ConfigFieldState>(field => new FieldValidationIssue(field.FieldName, field.ExpectedType, "Invalid port", field.Value?.ToString()));

            // Act
            var result = _validator.ValidateSection(fieldsState);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(4, result.Issues.Count);

            // Verify all expected issues are present
            Assert.Contains(result.Issues, i => i.FieldName == "IphoneIpAddress");
            Assert.Contains(result.Issues, i => i.FieldName == "IphonePort");
            Assert.Contains(result.Issues, i => i.FieldName == "LocalPort");
            Assert.Contains(result.Issues, i => i.FieldName == "UnknownField");
        }

        #endregion
    }
}
