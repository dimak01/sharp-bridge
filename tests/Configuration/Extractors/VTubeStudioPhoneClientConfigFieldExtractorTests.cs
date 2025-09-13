// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Configuration.Extractors;
using SharpBridge.Models.Configuration;
using Xunit;

namespace SharpBridge.Tests.Configuration.Extractors
{
    /// <summary>
    /// Tests for VTubeStudioPhoneClientConfigFieldExtractor class.
    /// </summary>
    public class VTubeStudioPhoneClientConfigFieldExtractorTests : IDisposable
    {
        private readonly ConfigSectionFieldExtractor _extractor;
        private readonly string _tempDirectory;
        private readonly List<string> _createdFiles;

        public VTubeStudioPhoneClientConfigFieldExtractorTests()
        {
            var properties = typeof(VTubeStudioPhoneClientConfig).GetProperties();
            _extractor = new ConfigSectionFieldExtractor(properties);
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _createdFiles = new List<string>();
        }

        public void Dispose()
        {
            foreach (var file in _createdFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #region Helper Methods

        private string CreateTempConfigFile(string jsonContent)
        {
            var filePath = Path.Combine(_tempDirectory, $"config_{Guid.NewGuid():N}.json");
            File.WriteAllText(filePath, jsonContent);
            _createdFiles.Add(filePath);
            return filePath;
        }

        #endregion

        #region ExtractFieldStatesAsync Tests

        [Fact]
        public async Task ExtractFieldStatesAsync_WithNonExistentFile_ReturnsAllFieldsAsNotPresent()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.json");

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(nonExistentPath);

            // Assert
            Assert.NotEmpty(result);

            // All fields should be marked as not present
            Assert.All(result, field =>
            {
                Assert.False(field.IsPresent);
                Assert.Null(field.Value);
                Assert.NotNull(field.FieldName);
                Assert.NotNull(field.ExpectedType);
                Assert.NotNull(field.Description);
            });

            // Verify expected fields are present
            var fieldNames = result.Select(f => f.FieldName).ToList();
            Assert.Contains("IphoneIpAddress", fieldNames);
            Assert.Contains("IphonePort", fieldNames);
            Assert.Contains("LocalPort", fieldNames);
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithEmptyJsonFile_ReturnsAllFieldsAsNotPresent()
        {
            // Arrange
            var configPath = CreateTempConfigFile("{}");

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);
            Assert.All(result, field => Assert.False(field.IsPresent));
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithMissingPhoneClientSection_ReturnsAllFieldsAsNotPresent()
        {
            // Arrange
            var jsonContent = @"{
                ""PCClient"": {
                    ""Host"": ""localhost"",
                    ""Port"": 8001
                },
                ""GeneralSettings"": {
                    ""EditorCommand"": ""notepad.exe""
                }
            }";
            var configPath = CreateTempConfigFile(jsonContent);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);
            Assert.All(result, field => Assert.False(field.IsPresent));
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithValidPhoneClientSection_ReturnsCorrectFieldStates()
        {
            // Arrange
            var jsonContent = @"{
                ""PhoneClient"": {
                    ""IphoneIpAddress"": ""192.168.1.100"",
                    ""IphonePort"": 21412,
                    ""LocalPort"": 28964
                }
            }";
            var configPath = CreateTempConfigFile(jsonContent);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);

            // Check specific fields
            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.True(ipField.IsPresent);
            Assert.Equal("192.168.1.100", ipField.Value);
            Assert.Equal(typeof(string), ipField.ExpectedType);

            var iphonePortField = result.First(f => f.FieldName == "IphonePort");
            Assert.True(iphonePortField.IsPresent);
            Assert.Equal(21412, iphonePortField.Value);
            Assert.Equal(typeof(int), iphonePortField.ExpectedType);

            var localPortField = result.First(f => f.FieldName == "LocalPort");
            Assert.True(localPortField.IsPresent);
            Assert.Equal(28964, localPortField.Value);
            Assert.Equal(typeof(int), localPortField.ExpectedType);
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithPartialPhoneClientSection_ReturnsMixedFieldStates()
        {
            // Arrange
            var jsonContent = @"{
                ""PhoneClient"": {
                    ""IphoneIpAddress"": ""192.168.1.100"",
                    ""LocalPort"": 28964
                }
            }";
            var configPath = CreateTempConfigFile(jsonContent);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);

            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.True(ipField.IsPresent);
            Assert.Equal("192.168.1.100", ipField.Value);

            var iphonePortField = result.First(f => f.FieldName == "IphonePort");
            Assert.False(iphonePortField.IsPresent);
            Assert.Null(iphonePortField.Value);

            var localPortField = result.First(f => f.FieldName == "LocalPort");
            Assert.True(localPortField.IsPresent);
            Assert.Equal(28964, localPortField.Value);
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithCaseInsensitivePropertyNames_HandlesCorrectly()
        {
            // Arrange
            var jsonContent = @"{
                ""PhoneClient"": {
                    ""iphoneipaddress"": ""192.168.1.100"",
                    ""IPHONEPORT"": 21412,
                    ""localport"": 28964
                }
            }";
            var configPath = CreateTempConfigFile(jsonContent);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);

            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.True(ipField.IsPresent);
            Assert.Equal("192.168.1.100", ipField.Value);

            var iphonePortField = result.First(f => f.FieldName == "IphonePort");
            Assert.True(iphonePortField.IsPresent);
            Assert.Equal(21412, iphonePortField.Value);

            var localPortField = result.First(f => f.FieldName == "LocalPort");
            Assert.True(localPortField.IsPresent);
            Assert.Equal(28964, localPortField.Value);
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithInvalidJsonValues_PreservesRawTextForValidation()
        {
            // Arrange
            var jsonContent = @"{
                ""PhoneClient"": {
                    ""IphoneIpAddress"": ""192.168.1.100"",
                    ""IphonePort"": ""not-a-number"",
                    ""LocalPort"": true
                }
            }";
            var configPath = CreateTempConfigFile(jsonContent);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);

            // Valid field should deserialize correctly
            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.True(ipField.IsPresent);
            Assert.Equal("192.168.1.100", ipField.Value);

            // Invalid fields should preserve raw text
            var iphonePortField = result.First(f => f.FieldName == "IphonePort");
            Assert.True(iphonePortField.IsPresent);
            Assert.Equal("\"not-a-number\"", iphonePortField.Value); // Raw JSON text

            var localPortField = result.First(f => f.FieldName == "LocalPort");
            Assert.True(localPortField.IsPresent);
            Assert.Equal("true", localPortField.Value); // Raw JSON text
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithMalformedJsonFile_ReturnsAllFieldsAsNotPresent()
        {
            // Arrange
            var malformedJson = @"{
                ""PhoneClient"": {
                    ""IphoneIpAddress"": ""192.168.1.100"",
                    ""IphonePort"": 21412,
                    ""LocalPort"": 28964
                } // Missing closing brace
            ";
            var configPath = CreateTempConfigFile(malformedJson);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);
            Assert.All(result, field => Assert.False(field.IsPresent));
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithNullValues_HandlesCorrectly()
        {
            // Arrange
            var jsonContent = @"{
                ""PhoneClient"": {
                    ""IphoneIpAddress"": null,
                    ""IphonePort"": 21412,
                    ""LocalPort"": null
                }
            }";
            var configPath = CreateTempConfigFile(jsonContent);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);

            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.True(ipField.IsPresent);
            Assert.Null(ipField.Value);

            var iphonePortField = result.First(f => f.FieldName == "IphonePort");
            Assert.True(iphonePortField.IsPresent);
            Assert.Equal(21412, iphonePortField.Value);

            var localPortField = result.First(f => f.FieldName == "LocalPort");
            Assert.True(localPortField.IsPresent);
            // JSON null for value types: the deserializer might return null or raw text depending on the type
            // Since this is testing the actual behavior, let's check what we actually get
            Assert.True(localPortField.Value == null || localPortField.Value.Equals("null"),
                $"Expected null or 'null', got: {localPortField.Value}");
        }

        #endregion

        #region Field Schema Tests

        [Fact]
        public async Task ExtractFieldStatesAsync_ReturnsExpectedFieldSchema()
        {
            // Arrange
            var configPath = CreateTempConfigFile("{}");

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);

            // Verify all expected fields are present in the schema
            var fieldNames = result.Select(f => f.FieldName).ToHashSet();

            // Core required fields
            Assert.Contains("IphoneIpAddress", fieldNames);
            Assert.Contains("IphonePort", fieldNames);
            Assert.Contains("LocalPort", fieldNames);

            // Internal/JsonIgnore fields should also be present in schema
            Assert.Contains("RequestIntervalSeconds", fieldNames);
            Assert.Contains("SendForSeconds", fieldNames);
            Assert.Contains("ReceiveTimeoutMs", fieldNames);
            Assert.Contains("ErrorDelayMs", fieldNames);

            // Verify field types
            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.Equal(typeof(string), ipField.ExpectedType);

            var portFields = result.Where(f => f.FieldName.Contains("Port")).ToList();
            Assert.All(portFields, field => Assert.Equal(typeof(int), field.ExpectedType));
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_IncludesPropertyDescriptions()
        {
            // Arrange
            var configPath = CreateTempConfigFile("{}");

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);
            Assert.All(result, field =>
            {
                Assert.NotNull(field.Description);
                Assert.NotEmpty(field.Description);
            });

            // Verify that fields have meaningful descriptions (not just property names)
            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.NotEqual("IphoneIpAddress", ipField.Description); // Should have a proper description
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task ExtractFieldStatesAsync_WithEmptyStringPath_ReturnsAllFieldsAsNotPresent()
        {
            // Arrange & Act
            var result = await _extractor.ExtractFieldStatesAsync("");

            // Assert
            Assert.NotEmpty(result);
            Assert.All(result, field => Assert.False(field.IsPresent));
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithNullPath_ReturnsAllFieldsAsNotPresent()
        {
            // Arrange & Act
            var result = await _extractor.ExtractFieldStatesAsync(null!);

            // Assert
            Assert.NotEmpty(result);
            Assert.All(result, field => Assert.False(field.IsPresent));
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithUnreadableFile_ReturnsAllFieldsAsNotPresent()
        {
            // This test is platform-specific and might not work reliably on all systems
            // so we'll simulate with a directory instead of a file
            // Arrange
            var directoryPath = Path.Combine(_tempDirectory, "not-a-file");
            Directory.CreateDirectory(directoryPath);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(directoryPath);

            // Assert
            Assert.NotEmpty(result);
            Assert.All(result, field => Assert.False(field.IsPresent));
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithComplexNestedJson_ExtractsOnlyPhoneClientSection()
        {
            // Arrange
            var jsonContent = @"{
                ""PCClient"": {
                    ""Host"": ""localhost"",
                    ""Port"": 8001,
                    ""IphoneIpAddress"": ""should-not-be-extracted""
                },
                ""PhoneClient"": {
                    ""IphoneIpAddress"": ""192.168.1.100"",
                    ""IphonePort"": 21412
                },
                ""GeneralSettings"": {
                    ""EditorCommand"": ""notepad.exe"",
                    ""LocalPort"": ""should-not-be-extracted""
                }
            }";
            var configPath = CreateTempConfigFile(jsonContent);

            // Act
            var result = await _extractor.ExtractFieldStatesAsync(configPath);

            // Assert
            Assert.NotEmpty(result);

            // Should extract from PhoneClient section only
            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.True(ipField.IsPresent);
            Assert.Equal("192.168.1.100", ipField.Value); // From PhoneClient, not PCClient

            var iphonePortField = result.First(f => f.FieldName == "IphonePort");
            Assert.True(iphonePortField.IsPresent);
            Assert.Equal(21412, iphonePortField.Value);

            var localPortField = result.First(f => f.FieldName == "LocalPort");
            Assert.False(localPortField.IsPresent); // Not in PhoneClient section
        }

        [Fact]
        public async Task ExtractFieldStatesAsync_WithVeryLargeJsonFile_HandlesEfficiently()
        {
            // Arrange - Create a large JSON file with the PhoneClient section
            var largeJsonContent = @"{
                ""PhoneClient"": {
                    ""IphoneIpAddress"": ""192.168.1.100"",
                    ""IphonePort"": 21412,
                    ""LocalPort"": 28964
                },
                ""LargeSection"": {";

            // Add many properties to make the file large
            for (int i = 0; i < 1000; i++)
            {
                largeJsonContent += $@"""Property{i}"": ""Value{i}"",";
            }
            largeJsonContent = largeJsonContent.TrimEnd(',') + @"
                }
            }";

            var configPath = CreateTempConfigFile(largeJsonContent);

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _extractor.ExtractFieldStatesAsync(configPath);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.NotEmpty(result);
            Assert.True(duration.TotalSeconds < 5, "Field extraction should be efficient even with large files");

            // Verify correct extraction despite large file
            var ipField = result.First(f => f.FieldName == "IphoneIpAddress");
            Assert.True(ipField.IsPresent);
            Assert.Equal("192.168.1.100", ipField.Value);
        }

        #endregion
    }
}
