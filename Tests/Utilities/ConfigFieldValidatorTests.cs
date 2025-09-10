using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using FluentAssertions;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarLint", "S4144", Justification = "Test methods with similar implementations are expected due to Theory/InlineData pattern")]
    public class ConfigFieldValidatorTests
    {
        private readonly ConfigFieldValidator _validator;

        public ConfigFieldValidatorTests()
        {
            _validator = new ConfigFieldValidator();
        }

        #region ValidatePort Tests

        [Fact]
        public void ValidatePort_WithValidPort_ReturnsNull()
        {
            // Arrange
            var field = new ConfigFieldState("Port", 8080, true, typeof(int), "Port number");

            // Act
            var result = _validator.ValidatePort(field);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1024)]
        [InlineData(8080)]
        [InlineData(65535)]
        public void ValidatePort_WithValidPorts_ReturnsNull(int port)
        {
            // Arrange
            var field = new ConfigFieldState("Port", port, true, typeof(int), "Port number");

            // Act
            var result = _validator.ValidatePort(field);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        [InlineData(99999)]
        public void ValidatePort_WithOutOfRangePorts_ReturnsValidationIssue(int port)
        {
            // Arrange
            var field = new ConfigFieldState("Port", port, true, typeof(int), "Port number");

            // Act
            var result = _validator.ValidatePort(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Port");
            result.ExpectedType.Should().Be(typeof(int));
            result.Description.Should().Contain($"Port {port} is out of valid range (1-65535)");
            result.ProvidedValueText.Should().Be(port.ToString());
        }

        [Fact]
        public void ValidatePort_WithNonIntegerValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Port", "8080", true, typeof(int), "Port number");

            // Act
            var result = _validator.ValidatePort(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Port");
            result.ExpectedType.Should().Be(typeof(int));
            result.Description.Should().Contain("Port value must be an integer");
            result.Description.Should().Contain("String");
        }

        [Fact]
        public void ValidatePort_WithNullValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Port", null, true, typeof(int), "Port number");

            // Act
            var result = _validator.ValidatePort(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Port");
            result.ExpectedType.Should().Be(typeof(int));
            result.Description.Should().Contain("Port value must be an integer");
            result.Description.Should().Contain("null");
        }

        [Fact]
        public void ValidatePort_WithPrivilegedPort_ReturnsNull()
        {
            // Arrange - Ports 1-1024 are privileged but should still pass validation
            var field = new ConfigFieldState("Port", 80, true, typeof(int), "Port number");

            // Act
            var result = _validator.ValidatePort(field);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region ValidateIpAddress Tests

        [Theory]
        [InlineData("192.168.1.1")]
        [InlineData("127.0.0.1")]
        [InlineData("10.0.0.1")]
        [InlineData("255.255.255.255")]
        [InlineData("0.0.0.0")]
        public void ValidateIpAddress_WithValidIpAddresses_ReturnsNull(string ipAddress)
        {
            // Arrange
            var field = new ConfigFieldState("IpAddress", ipAddress, true, typeof(string), "IP Address");

            // Act
            var result = _validator.ValidateIpAddress(field);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateIpAddress_WithNullValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("IpAddress", null, true, typeof(string), "IP Address");

            // Act
            var result = _validator.ValidateIpAddress(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("IpAddress");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("IP address cannot be null or empty");
            result.ProvidedValueText.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void ValidateIpAddress_WithEmptyOrWhitespace_ReturnsValidationIssue(string ipAddress)
        {
            // Arrange
            var field = new ConfigFieldState("IpAddress", ipAddress, true, typeof(string), "IP Address");

            // Act
            var result = _validator.ValidateIpAddress(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("IpAddress");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("IP address cannot be null or empty");
            result.ProvidedValueText.Should().Be(ipAddress);
        }

        [Theory]
        [InlineData("invalid-ip")]
        [InlineData("256.256.256.256")]
        [InlineData("192.168.1.1.1")]
        [InlineData("not-an-ip")]
        public void ValidateIpAddress_WithInvalidIpAddresses_ReturnsValidationIssue(string ipAddress)
        {
            // Arrange
            var field = new ConfigFieldState("IpAddress", ipAddress, true, typeof(string), "IP Address");

            // Act
            var result = _validator.ValidateIpAddress(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("IpAddress");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Contain($"'{ipAddress}' is not a valid IP address");
            result.ProvidedValueText.Should().Be(ipAddress);
        }

        [Fact]
        public void ValidateIpAddress_WithPartialIpAddress_PassesValidation()
        {
            // Arrange - "192.168.1" might be accepted by some IP parsers
            var field = new ConfigFieldState("IpAddress", "192.168.1", true, typeof(string), "IP Address");

            // Act
            var result = _validator.ValidateIpAddress(field);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateIpAddress_WithNonStringValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("IpAddress", 12345, true, typeof(string), "IP Address");

            // Act
            var result = _validator.ValidateIpAddress(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("IpAddress");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("IP address cannot be null or empty");
            result.ProvidedValueText.Should().Be("12345");
        }


        [Fact]
        public void ValidateIpAddress_WithLocalhostString_ReturnsValidationIssue()
        {
            // Arrange - "localhost" string should fail IP validation
            var field = new ConfigFieldState("IpAddress", "localhost", true, typeof(string), "IP Address");

            // Act
            var result = _validator.ValidateIpAddress(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("IpAddress");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Contain("'localhost' is not a valid IP address");
            result.ProvidedValueText.Should().Be("localhost");
        }

        #endregion

        #region ValidateHost Tests

        [Theory]
        [InlineData("192.168.1.1")]
        [InlineData("example.com")]
        [InlineData("localhost")]
        [InlineData("subdomain.example.com")]
        [InlineData("host-name")]
        [InlineData("host_name")]
        public void ValidateHost_WithValidHosts_ReturnsNull(string host)
        {
            // Arrange
            var field = new ConfigFieldState("Host", host, true, typeof(string), "Host address");

            // Act
            var result = _validator.ValidateHost(field);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateHost_WithNullValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Host", null, true, typeof(string), "Host address");

            // Act
            var result = _validator.ValidateHost(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Host");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("Host address cannot be null or empty");
            result.ProvidedValueText.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void ValidateHost_WithEmptyOrWhitespace_ReturnsValidationIssue(string host)
        {
            // Arrange
            var field = new ConfigFieldState("Host", host, true, typeof(string), "Host address");

            // Act
            var result = _validator.ValidateHost(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Host");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("Host address cannot be null or empty");
            result.ProvidedValueText.Should().Be(host);
        }

        [Theory]
        [InlineData("invalid..host")]
        [InlineData("host with spaces")]
        [InlineData("host@invalid")]
        [InlineData("host#invalid")]
        [InlineData("host$invalid")]
        public void ValidateHost_WithInvalidHosts_ReturnsValidationIssue(string host)
        {
            // Arrange
            var field = new ConfigFieldState("Host", host, true, typeof(string), "Host address");

            // Act
            var result = _validator.ValidateHost(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Host");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Contain($"'{host}' is not a valid host address");
            result.ProvidedValueText.Should().Be(host);
        }

        [Fact]
        public void ValidateHost_WithNonStringValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Host", 12345, true, typeof(string), "Host address");

            // Act
            var result = _validator.ValidateHost(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Host");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("Host address cannot be null or empty");
            result.ProvidedValueText.Should().Be("12345");
        }

        #endregion

        #region ValidateBoolean Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ValidateBoolean_WithValidBooleans_ReturnsNull(bool value)
        {
            // Arrange
            var field = new ConfigFieldState("Enabled", value, true, typeof(bool), "Enable feature");

            // Act
            var result = _validator.ValidateBoolean(field);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(null)]
        public void ValidateBoolean_WithNonBooleanValues_ReturnsValidationIssue(object value)
        {
            // Arrange
            var field = new ConfigFieldState("Enabled", value, true, typeof(bool), "Enable feature");

            // Act
            var result = _validator.ValidateBoolean(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Enabled");
            result.ExpectedType.Should().Be(typeof(bool));
            result.Description.Should().Contain("Boolean value must be true or false");
            result.ProvidedValueText.Should().Be(value?.ToString());
        }

        #endregion

        #region ValidateString Tests

        [Theory]
        [InlineData("valid string")]
        [InlineData("another valid string")]
        [InlineData("string with numbers 123")]
        public void ValidateString_WithValidStrings_ReturnsNull(string value)
        {
            // Arrange
            var field = new ConfigFieldState("Name", value, true, typeof(string), "Name field");

            // Act
            var result = _validator.ValidateString(field);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("valid string")]
        [InlineData("another valid string")]
        [InlineData("string with numbers 123")]
        [InlineData(null)]
        public void ValidateString_WithAllowEmptyTrue_ReturnsNull(string value)
        {
            // Arrange
            var field = new ConfigFieldState("Name", value, true, typeof(string), "Name field");

            // Act
            var result = _validator.ValidateString(field, allowEmpty: true);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void ValidateString_WithEmptyStringsAndAllowEmptyFalse_ReturnsValidationIssue(string value)
        {
            // Arrange
            var field = new ConfigFieldState("Name", value, true, typeof(string), "Name field");

            // Act
            var result = _validator.ValidateString(field, allowEmpty: false);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Name");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("String value cannot be null or empty");
            result.ProvidedValueText.Should().Be(value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void ValidateString_WithEmptyStringsAndAllowEmptyTrue_ReturnsNull(string value)
        {
            // Arrange
            var field = new ConfigFieldState("Name", value, true, typeof(string), "Name field");

            // Act
            var result = _validator.ValidateString(field, allowEmpty: true);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateString_WithNullValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Name", null, true, typeof(string), "Name field");

            // Act
            var result = _validator.ValidateString(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Name");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Contain("String value cannot be null");
            result.ProvidedValueText.Should().BeNull();
        }


        [Fact]
        public void ValidateString_WithNonStringValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Name", 12345, true, typeof(string), "Name field");

            // Act
            var result = _validator.ValidateString(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Name");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Contain("String value expected");
            result.Description.Should().Contain("Int32");
            result.ProvidedValueText.Should().Be("12345");
        }

        #endregion

        #region ValidateIntegerRange Tests

        [Theory]
        [InlineData(5, 1, 10)]
        [InlineData(1, 1, 10)]
        [InlineData(10, 1, 10)]
        [InlineData(0, -5, 5)]
        [InlineData(-5, -10, 0)]
        public void ValidateIntegerRange_WithValidRanges_ReturnsNull(int value, int minValue, int maxValue)
        {
            // Arrange
            var field = new ConfigFieldState("Value", value, true, typeof(int), "Integer value");

            // Act
            var result = _validator.ValidateIntegerRange(field, minValue, maxValue);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(0, 1, 10)]
        [InlineData(11, 1, 10)]
        [InlineData(-1, 0, 5)]
        [InlineData(6, 0, 5)]
        public void ValidateIntegerRange_WithOutOfRangeValues_ReturnsValidationIssue(int value, int minValue, int maxValue)
        {
            // Arrange
            var field = new ConfigFieldState("Value", value, true, typeof(int), "Integer value");

            // Act
            var result = _validator.ValidateIntegerRange(field, minValue, maxValue);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Value");
            result.ExpectedType.Should().Be(typeof(int));
            result.Description.Should().Contain($"Value {value} is out of valid range ({minValue}-{maxValue})");
            result.ProvidedValueText.Should().Be(value.ToString());
        }

        [Fact]
        public void ValidateIntegerRange_WithNonIntegerValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Value", "not-a-number", true, typeof(int), "Integer value");

            // Act
            var result = _validator.ValidateIntegerRange(field, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Value");
            result.ExpectedType.Should().Be(typeof(int));
            result.Description.Should().Contain("Value must be an integer");
            result.Description.Should().Contain("String");
            result.ProvidedValueText.Should().Be("not-a-number");
        }

        [Fact]
        public void ValidateIntegerRange_WithNullValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Value", null, true, typeof(int), "Integer value");

            // Act
            var result = _validator.ValidateIntegerRange(field, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("Value");
            result.ExpectedType.Should().Be(typeof(int));
            result.Description.Should().Contain("Value must be an integer");
            result.Description.Should().Contain("null");
            result.ProvidedValueText.Should().BeNull();
        }

        #endregion

        #region ValidateFilePath Tests

        [Theory]
        [InlineData("C:\\temp\\file.txt")]
        [InlineData("file.txt")]
        [InlineData("folder\\subfolder\\file.txt")]
        [InlineData("C:\\Program Files\\App\\config.json")]
        public void ValidateFilePath_WithValidPaths_ReturnsNull(string path)
        {
            // Arrange
            var field = new ConfigFieldState("FilePath", path, true, typeof(string), "File path");

            // Act
            var result = _validator.ValidateFilePath(field);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateFilePath_WithNullValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("FilePath", null, true, typeof(string), "File path");

            // Act
            var result = _validator.ValidateFilePath(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("FilePath");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("File path cannot be null or empty");
            result.ProvidedValueText.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void ValidateFilePath_WithEmptyOrWhitespace_ReturnsValidationIssue(string path)
        {
            // Arrange
            var field = new ConfigFieldState("FilePath", path, true, typeof(string), "File path");

            // Act
            var result = _validator.ValidateFilePath(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("FilePath");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("File path cannot be null or empty");
            result.ProvidedValueText.Should().Be(path);
        }

        [Fact]
        public void ValidateFilePath_WithNonStringValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("FilePath", 12345, true, typeof(string), "File path");

            // Act
            var result = _validator.ValidateFilePath(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("FilePath");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("File path cannot be null or empty");
            result.ProvidedValueText.Should().Be("12345");
        }

        [Theory]
        [MemberData(nameof(GetPlatformSpecificInvalidFilePathData))]
        public void ValidateFilePath_WithInvalidCharacters_ReturnsValidationIssue(string path, string expectedMessagePart)
        {
            // Arrange
            var field = new ConfigFieldState("FilePath", path, true, typeof(string), "File path");

            // Act
            var result = _validator.ValidateFilePath(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("FilePath");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Contain(expectedMessagePart);
            result.ProvidedValueText.Should().Be(path);
        }

        public static IEnumerable<object[]> GetPlatformSpecificInvalidFilePathData()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows invalid characters: < > : " | ? * \ (except :\ for drive separator)
                yield return new object[] { "file<invalid>.txt", "File path contains invalid characters" };
                yield return new object[] { "file|invalid.txt", "File path contains invalid characters" };
                yield return new object[] { "file\"invalid.txt", "File path contains invalid characters" };
                yield return new object[] { "file*invalid.txt", "File path contains invalid characters" };
                yield return new object[] { "file?invalid.txt", "File path contains invalid characters" };
                yield return new object[] { "file>invalid.txt", "File path contains invalid characters" };
            }
            else
            {
                // Linux/Unix: null character causes Path.GetFullPath to throw, so different error message
                yield return new object[] { "file\0invalid.txt", "Invalid file path format" };
                yield return new object[] { "file\x00invalid.txt", "Invalid file path format" };
            }
        }


        [Fact]
        public void ValidateFilePath_WithVeryLongPath_HandlesGracefully()
        {
            // Arrange - Create a very long path that might cause issues
            var longPath = new string('a', 300) + ".txt";
            var field = new ConfigFieldState("FilePath", longPath, true, typeof(string), "File path");

            // Act
            var result = _validator.ValidateFilePath(field);

            // Assert
            // Should either pass validation or return a specific error, not crash
            if (result != null)
            {
                result.FieldName.Should().Be("FilePath");
                result.ExpectedType.Should().Be(typeof(string));
                result.Description.Should().NotBeNullOrEmpty();
            }
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Fact]
        public void ValidatePort_WithInt32MaxValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Port", int.MaxValue, true, typeof(int), "Port number");

            // Act
            var result = _validator.ValidatePort(field);

            // Assert
            result.Should().NotBeNull();
            result!.Description.Should().Contain("is out of valid range");
        }

        [Fact]
        public void ValidatePort_WithInt32MinValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("Port", int.MinValue, true, typeof(int), "Port number");

            // Act
            var result = _validator.ValidatePort(field);

            // Assert
            result.Should().NotBeNull();
            result!.Description.Should().Contain("is out of valid range");
        }

        [Fact]
        public void ValidateString_WithVeryLongString_HandlesGracefully()
        {
            // Arrange
            var longString = new string('a', 1000);
            var field = new ConfigFieldState("Name", longString, true, typeof(string), "Name field");

            // Act
            var result = _validator.ValidateString(field);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateString_WithVeryLongStringInError_TruncatesDisplay()
        {
            // Arrange
            var longString = new string('a', 1000);
            var field = new ConfigFieldState("Name", longString, true, typeof(int), "Name field"); // Wrong type

            // Act
            var result = _validator.ValidateString(field);

            // Assert
            result.Should().NotBeNull();
            result!.Description.Should().Contain("Int32 value expected");
            result.ProvidedValueText.Should().HaveLength(128); // Should be truncated to 128 chars
            result.ProvidedValueText.Should().EndWith("...");
        }

        #endregion

        #region Private Method Coverage Tests

        [Theory]
        [InlineData("example.com", true)]
        [InlineData("subdomain.example.com", true)]
        [InlineData("192.168.1.1", true)]
        [InlineData("127.0.0.1", true)]
        [InlineData("invalid..host", false)]
        [InlineData("host with spaces", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void IsValidHost_ThroughValidateHost_CoversAllBranches(string host, bool expectedValid)
        {
            // Arrange
            var field = new ConfigFieldState("Host", host, true, typeof(string), "Host address");

            // Act
            var result = _validator.ValidateHost(field);

            // Assert
            if (expectedValid)
            {
                result.Should().BeNull();
            }
            else
            {
                result.Should().NotBeNull();
            }
        }

        [Fact]
        public void ValidateFilePath_WithInvalidPathFormat_ThrowsArgumentException()
        {
            // Arrange - Use a path that will cause ArgumentException in Path.GetFullPath
            // Try using a path with null characters or other issues that Path.GetFullPath can't handle
            var field = new ConfigFieldState("FilePath", "C:\\test\0file.txt", true, typeof(string), "File Path");

            // Act
            var result = _validator.ValidateFilePath(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("FilePath");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Contain("Invalid file path format");
        }


        #endregion

        #region ValidateParameterPrefix Tests

        [Theory]
        [InlineData("")]
        [InlineData("SB_")]
        [InlineData("SB_")]
        [InlineData("MyPrefix")]
        [InlineData("123")]
        [InlineData("_123_")]
        [InlineData("A1B2C3")]
        [InlineData("prefix_123")]
        [InlineData("_")]
        [InlineData("a")]
        [InlineData("A")]
        [InlineData("1")]
        [InlineData("123456789012345")] // 15 characters (max length)
        public void ValidateParameterPrefix_WithValidPrefixes_ReturnsNull(string prefix)
        {
            // Arrange
            var field = new ConfigFieldState("ParameterPrefix", prefix, true, typeof(string), "Parameter Prefix");

            // Act
            var result = _validator.ValidateParameterPrefix(field);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("1234567890123456")] // 16 characters (exceeds max)
        [InlineData("ThisIsTooLongPrefix")] // 20 characters
        [InlineData("ABBBBBBBBBBBBBBBBBB")] // 20 characters
        public void ValidateParameterPrefix_WithTooLongPrefixes_ReturnsValidationIssue(string prefix)
        {
            // Arrange
            var field = new ConfigFieldState("ParameterPrefix", prefix, true, typeof(string), "Parameter Prefix");

            // Act
            var result = _validator.ValidateParameterPrefix(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("ParameterPrefix");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("Parameter prefix cannot exceed 15 characters");
            result.ProvidedValueText.Should().Be(prefix);
        }

        [Theory]
        [InlineData("My Prefix")] // space
        [InlineData("My-Prefix")] // hyphen
        [InlineData("My.Prefix")] // dot
        [InlineData("My/Prefix")] // slash
        [InlineData("My\\Prefix")] // backslash
        [InlineData("My@Prefix")] // at symbol
        [InlineData("My#Prefix")] // hash
        [InlineData("My$Prefix")] // dollar
        [InlineData("My%Prefix")] // percent
        [InlineData("My&Prefix")] // ampersand
        [InlineData("My*Prefix")] // asterisk
        [InlineData("My+Prefix")] // plus
        [InlineData("My=Prefix")] // equals
        [InlineData("My?Prefix")] // question mark
        [InlineData("My!Prefix")] // exclamation
        [InlineData("My[Prefix")] // bracket
        [InlineData("My]Prefix")] // bracket
        [InlineData("My{Prefix")] // brace
        [InlineData("My}Prefix")] // brace
        [InlineData("My(Prefix")] // parenthesis
        [InlineData("My)Prefix")] // parenthesis
        [InlineData("My<Prefix")] // angle bracket
        [InlineData("My>Prefix")] // angle bracket
        [InlineData("My|Prefix")] // pipe
        [InlineData("My;Prefix")] // semicolon
        [InlineData("My:Prefix")] // colon
        [InlineData("My\"Prefix")] // quote
        [InlineData("My'Prefix")] // apostrophe
        [InlineData("My,Prefix")] // comma
        [InlineData("My~Prefix")] // tilde
        [InlineData("My`Prefix")] // backtick
        public void ValidateParameterPrefix_WithInvalidCharacters_ReturnsValidationIssue(string prefix)
        {
            // Arrange
            var field = new ConfigFieldState("ParameterPrefix", prefix, true, typeof(string), "Parameter Prefix");

            // Act
            var result = _validator.ValidateParameterPrefix(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("ParameterPrefix");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("Parameter prefix must contain only alphanumeric characters and underscores, no spaces");
            result.ProvidedValueText.Should().Be(prefix);
        }

        [Fact]
        public void ValidateParameterPrefix_WithNullValue_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("ParameterPrefix", null, true, typeof(string), "Parameter Prefix");

            // Act
            var result = _validator.ValidateParameterPrefix(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("ParameterPrefix");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("Parameter prefix must be a string, got null");
            result.ProvidedValueText.Should().BeNull();
        }

        [Theory]
        [InlineData(123)]
        [InlineData(true)]
        [InlineData(123.45)]
        public void ValidateParameterPrefix_WithNonStringValue_ReturnsValidationIssue(object value)
        {
            // Arrange
            var field = new ConfigFieldState("ParameterPrefix", value, true, typeof(string), "Parameter Prefix");

            // Act
            var result = _validator.ValidateParameterPrefix(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("ParameterPrefix");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Contain("Parameter prefix must be a string, got");
            result.ProvidedValueText.Should().Be(value.ToString());
        }

        [Fact]
        public void ValidateParameterPrefix_WithWhitespaceOnly_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("ParameterPrefix", "   ", true, typeof(string), "Parameter Prefix");

            // Act
            var result = _validator.ValidateParameterPrefix(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("ParameterPrefix");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("Parameter prefix must contain only alphanumeric characters and underscores, no spaces");
            result.ProvidedValueText.Should().Be("   ");
        }

        [Fact]
        public void ValidateParameterPrefix_WithMixedValidAndInvalidCharacters_ReturnsValidationIssue()
        {
            // Arrange
            var field = new ConfigFieldState("ParameterPrefix", "Valid_123!", true, typeof(string), "Parameter Prefix");

            // Act
            var result = _validator.ValidateParameterPrefix(field);

            // Assert
            result.Should().NotBeNull();
            result!.FieldName.Should().Be("ParameterPrefix");
            result.ExpectedType.Should().Be(typeof(string));
            result.Description.Should().Be("Parameter prefix must contain only alphanumeric characters and underscores, no spaces");
            result.ProvidedValueText.Should().Be("Valid_123!");
        }

        #endregion
    }
}
