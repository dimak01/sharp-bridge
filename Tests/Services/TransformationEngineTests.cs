using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Collections;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class TransformationEngineTests
    {
        private readonly Mock<IAppLogger> _mockLogger;

        public TransformationEngineTests()
        {
            _mockLogger = new Mock<IAppLogger>();
        }
        
        private string CreateTempRuleFile(string content)
        {
            var tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, content);
            return tempPath;
        }
        
        [Fact]
        public async Task LoadRulesAsync_LoadsValidRules()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""TestParam"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act
                await engine.LoadRulesAsync(filePath);
                
                // Create tracking data to test transformation
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.Parameters.Should().NotBeNullOrEmpty();
                result.Parameters.Should().ContainSingle();
                result.Parameters.First().Id.Should().Be("TestParam");
                result.Parameters.First().Value.Should().Be(50); // 0.5 * 100
                
                // Verify parameter definition
                result.ParameterDefinitions.Should().ContainKey("TestParam");
                result.ParameterDefinitions["TestParam"].Min.Should().Be(0);
                result.ParameterDefinitions["TestParam"].Max.Should().Be(100);
                result.ParameterDefinitions["TestParam"].DefaultValue.Should().Be(0);
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsException_WhenFileNotFound()
        {
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(async () => 
                await engine.LoadRulesAsync(nonExistentPath));
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsException_WhenJsonIsInvalid()
        {
            // Arrange
            var filePath = CreateTempRuleFile("{ invalid json }");
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act & Assert
                await Assert.ThrowsAsync<JsonException>(async () => 
                    await engine.LoadRulesAsync(filePath));
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public void TransformData_ReturnsEmptyCollection_WhenNoRulesLoaded()
        {
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData);
            
            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().BeNullOrEmpty();
        }
        
        [Fact]
        public async Task TransformData_ReturnsEmptyCollection_WhenFaceNotFound()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""TestParam"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = false, // Face not found
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                // Act
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.Parameters.Should().BeNullOrEmpty();
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task TransformData_AppliesExpressions_WithCorrectContext()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""HeadMovement"",
                    ""func"": ""HeadPosX * 100 + HeadRotY * 50"",
                    ""min"": -1000,
                    ""max"": 1000,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""EyeBlink"",
                    ""func"": ""(eyeBlinkLeft + eyeBlinkRight) * 50"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    Position = new Coordinates { X = 0.1, Y = 0, Z = 0 },
                    Rotation = new Coordinates { X = 0, Y = 0.2, Z = 0 },
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.3 },
                        new BlendShape { Key = "eyeBlinkRight", Value = 0.5 }
                    }
                };
                
                // Act
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.Parameters.Should().HaveCount(2);
                
                var headMovement = result.Parameters.FirstOrDefault(p => p.Id == "HeadMovement");
                headMovement.Should().NotBeNull();
                headMovement.Value.Should().BeApproximately(20, 0.001); // 0.1 * 100 + 0.2 * 50 = 20
                
                var eyeBlink = result.Parameters.FirstOrDefault(p => p.Id == "EyeBlink");
                eyeBlink.Should().NotBeNull();
                eyeBlink.Value.Should().BeApproximately(40, 0.001); // (0.3 + 0.5) * 50 = 40
                
                // Verify parameter definitions
                result.ParameterDefinitions.Should().ContainKey("HeadMovement");
                result.ParameterDefinitions["HeadMovement"].Min.Should().Be(-1000);
                result.ParameterDefinitions["HeadMovement"].Max.Should().Be(1000);
                result.ParameterDefinitions["HeadMovement"].DefaultValue.Should().Be(0);
                
                result.ParameterDefinitions.Should().ContainKey("EyeBlink");
                result.ParameterDefinitions["EyeBlink"].Min.Should().Be(0);
                result.ParameterDefinitions["EyeBlink"].Max.Should().Be(100);
                result.ParameterDefinitions["EyeBlink"].DefaultValue.Should().Be(0);
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task TransformData_ClampsMappedValuesToMinMax()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""LimitedValue"",
                    ""func"": ""eyeBlinkLeft * 1000"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 } // 0.5 * 1000 = 500, should be clamped to 100
                    }
                };
                
                // Act
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.Parameters.Should().ContainSingle();
                result.Parameters.First().Value.Should().Be(100); // Clamped to max
                
                // Verify parameter definition
                result.ParameterDefinitions.Should().ContainKey("LimitedValue");
                result.ParameterDefinitions["LimitedValue"].Min.Should().Be(0);
                result.ParameterDefinitions["LimitedValue"].Max.Should().Be(100);
                result.ParameterDefinitions["LimitedValue"].DefaultValue.Should().Be(0);
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task TransformData_HandlesInvalidExpressions_Gracefully()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""ValidParam"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""InvalidParam"",
                    ""func"": ""unknownVariable + 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 50
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                // Act & Assert
                // With our new "fail fast" approach, we should expect an exception
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    engine.TransformData(trackingData));
                
                // Verify that the exception message mentions the parameter name
                exception.Message.Should().Contain("InvalidParam");
                exception.Message.Should().Contain("unknownVariable");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task TransformData_SetsEyePositionParameters_Correctly()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""EyeLeftParam"",
                    ""func"": ""EyeLeftX + EyeLeftY + EyeLeftZ"",
                    ""min"": -10,
                    ""max"": 10,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""EyeRightParam"",
                    ""func"": ""EyeRightX + EyeRightY + EyeRightZ"",
                    ""min"": -10,
                    ""max"": 10,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    EyeLeft = new Coordinates { X = 0.1, Y = 0.2, Z = 0.3 },
                    EyeRight = new Coordinates { X = 0.4, Y = 0.5, Z = 0.6 }
                };
                
                // Act
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.Parameters.Should().HaveCount(2);
                
                var eyeLeftParam = result.Parameters.FirstOrDefault(p => p.Id == "EyeLeftParam");
                eyeLeftParam.Should().NotBeNull();
                eyeLeftParam.Value.Should().BeApproximately(0.1 + 0.2 + 0.3, 0.001); // 0.6
                
                var eyeRightParam = result.Parameters.FirstOrDefault(p => p.Id == "EyeRightParam");
                eyeRightParam.Should().NotBeNull();
                eyeRightParam.Value.Should().BeApproximately(0.4 + 0.5 + 0.6, 0.001); // 1.5
                
                // Verify parameter definitions
                result.ParameterDefinitions.Should().ContainKey("EyeLeftParam");
                result.ParameterDefinitions["EyeLeftParam"].Min.Should().Be(-10);
                result.ParameterDefinitions["EyeLeftParam"].Max.Should().Be(10);
                result.ParameterDefinitions["EyeLeftParam"].DefaultValue.Should().Be(0);
                
                result.ParameterDefinitions.Should().ContainKey("EyeRightParam");
                result.ParameterDefinitions["EyeRightParam"].Min.Should().Be(-10);
                result.ParameterDefinitions["EyeRightParam"].Max.Should().Be(10);
                result.ParameterDefinitions["EyeRightParam"].DefaultValue.Should().Be(0);
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task TransformData_HandlesOneNullEyeCoordinate_Gracefully()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""EyeLeftParam"",
                    ""func"": ""EyeLeftX + EyeLeftY + EyeLeftZ"",
                    ""min"": -10,
                    ""max"": 10,
                    ""defaultValue"": 5
                },
                {
                    ""name"": ""EyeRightParam"",
                    ""func"": ""EyeRightX + EyeRightY + EyeRightZ"",
                    ""min"": -10,
                    ""max"": 10,
                    ""defaultValue"": 10
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    EyeLeft = null, // Null eye left
                    EyeRight = new Coordinates { X = 0.4, Y = 0.5, Z = 0.6 } // Valid eye right
                };
                
                // Act & Assert
                // With our new behavior, we should expect an exception when trying to access EyeLeftX
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    engine.TransformData(trackingData));
                
                // Verify that the exception message mentions the parameter
                exception.Message.Should().Contain("EyeLeftParam");
                exception.Message.Should().Contain("EyeLeftX");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task TransformData_HandlesBothNullEyeCoordinates_Gracefully()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""EyeLeftParam"",
                    ""func"": ""EyeLeftX + EyeLeftY + EyeLeftZ"",
                    ""min"": -10,
                    ""max"": 10,
                    ""defaultValue"": 5
                },
                {
                    ""name"": ""EyeRightParam"",
                    ""func"": ""EyeRightX + EyeRightY + EyeRightZ"",
                    ""min"": -10,
                    ""max"": 10,
                    ""defaultValue"": 10
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    EyeLeft = null,
                    EyeRight = null
                };
                
                // Act & Assert
                // With our new behavior, we should expect an exception when trying to access EyeLeftX
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    engine.TransformData(trackingData));
                
                // Verify that the exception message mentions the parameter
                exception.Message.Should().Contain("EyeLeftParam");
                exception.Message.Should().Contain("EyeLeftX");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsJsonException_WhenDeserializationReturnsNull()
        {
            // To test this scenario, we need a valid JSON that deserializes to null
            // For System.Text.Json, an empty array is still a valid array, not null
            // So we'll create a valid JSON that's not an array of TransformRule
            
            // Arrange - this is valid JSON but not a List<TransformRule>
            var jsonContent = "{}"; // An empty object, not an array
            var filePath = CreateTempRuleFile(jsonContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act & Assert
                // This should trigger the `??` operator in the LoadRulesAsync method
                var exception = await Assert.ThrowsAsync<JsonException>(async () => 
                    await engine.LoadRulesAsync(filePath));
                
                // Verify that an exception is thrown - don't check specific message since it depends on JsonSerializer
                exception.Should().NotBeNull();
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsExceptionForRulesWithInvalidSyntax()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""ValidRule"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""InvalidSyntaxRule"",
                    ""func"": ""1 + * 2"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""EmptyRule"",
                    ""func"": """",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""AnotherValidRule"",
                    ""func"": ""eyeBlinkRight * 50"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act & Assert
                // With the new behavior, we should throw an exception during loading
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                    await engine.LoadRulesAsync(filePath));
                
                // Verify that the exception message contains details about the invalid rules
                exception.Message.Should().Contain("Failed to load");
                exception.Message.Should().Contain("InvalidSyntaxRule");
                exception.Message.Should().Contain("EmptyRule");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsExceptionWhenMinGreaterThanMax()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""ValidRule"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""InvalidRangeRule"",
                    ""func"": ""eyeBlinkRight * 50"",
                    ""min"": 100,  
                    ""max"": 0,    
                    ""defaultValue"": 50
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act & Assert
                // With the new behavior, we should throw an exception during loading
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                    await engine.LoadRulesAsync(filePath));
                
                // Verify that the exception message contains details about the invalid rule
                exception.Message.Should().Contain("Failed to load");
                exception.Message.Should().Contain("InvalidRangeRule");
                exception.Message.Should().Contain("Min value");
                exception.Message.Should().Contain("greater than Max value");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_TriggersExceptionHandling_DuringExpressionCreation()
        {
            // Arrange
            // Create a rule that will pass syntax check but fail when evaluated
            var ruleContent = @"[
                {
                    ""name"": ""BadRule"",
                    ""func"": ""If(0 > 1, 1, 1 / 0)"", 
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // First load the rules (this should succeed as the syntax is valid)
                await engine.LoadRulesAsync(filePath);
                
                // Now create tracking data and try to transform it
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>()
                };
                
                // Act & Assert - this should throw when the expression is evaluated
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    engine.TransformData(trackingData));
                
                // Verify the exception details
                exception.Message.Should().Contain("BadRule");
                exception.Message.Should().Contain("evaluating expression");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_HandlesExceptionDuringRuleParsing()
        {
            // Arrange
            // Use a completely invalid JSON rule file
            var ruleContent = @"{ This is not valid JSON }";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act & Assert
                var exception = await Assert.ThrowsAsync<JsonException>(async () => 
                    await engine.LoadRulesAsync(filePath));
                
                // We don't need to check the specific message as it may vary by JSON parser
                exception.Should().NotBeNull();
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsInvalidOperationException_ForRuleValidationFailures()
        {
            // Arrange
            // Create a rule with min > max which fails validation
            var ruleContent = @"[
                {
                    ""name"": ""InvalidRangeRule"",
                    ""func"": ""x + 1"",
                    ""min"": 100,
                    ""max"": 0,  
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await engine.LoadRulesAsync(filePath));
                
                // Verify the exception details 
                exception.Message.Should().Contain("Failed to load 1 transformation rules.");
                exception.Message.Should().Contain("Rule 'InvalidRangeRule' has Min value (100) greater than Max value (0)");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
    }
} 