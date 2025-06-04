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
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, content);
            return filePath;
        }
        
        private PhoneTrackingInfo CreateValidTrackingData()
        {
            return new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0.1, Y = 0.2, Z = 0.3 },
                Rotation = new Coordinates { X = 0.4, Y = 0.5, Z = 0.6 },
                EyeLeft = new Coordinates { X = 0.7, Y = 0.8, Z = 0.9 },
                EyeRight = new Coordinates { X = 1.0, Y = 1.1, Z = 1.2 },
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                    new BlendShape { Key = "eyeBlinkRight", Value = 0.6 },
                    new BlendShape { Key = "mouthOpen", Value = 0.3 }
                }
            };
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
                
                // Act
                // With graceful degradation, this should succeed with partial results
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.Parameters.Should().ContainSingle(); // Only the valid parameter should be evaluated
                
                var validParam = result.Parameters.First();
                validParam.Id.Should().Be("ValidParam");
                validParam.Value.Should().Be(50); // 0.5 * 100 = 50
                
                // The invalid parameter should not be present in results
                result.Parameters.Should().NotContain(p => p.Id == "InvalidParam");
                
                // Parameter definitions should only include successfully evaluated rules
                result.ParameterDefinitions.Should().ContainSingle();
                result.ParameterDefinitions.Should().ContainKey("ValidParam");
                result.ParameterDefinitions.Should().NotContainKey("InvalidParam");
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
                
                // Act
                // With graceful degradation, this should succeed with partial results
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.Parameters.Should().ContainSingle(); // Only EyeRightParam should be evaluated
                
                var eyeRightParam = result.Parameters.First();
                eyeRightParam.Id.Should().Be("EyeRightParam");
                eyeRightParam.Value.Should().BeApproximately(1.5, 0.001); // 0.4 + 0.5 + 0.6 = 1.5
                
                // EyeLeftParam should not be present since EyeLeft coordinates are not available
                result.Parameters.Should().NotContain(p => p.Id == "EyeLeftParam");
                
                // Parameter definitions should only include successfully evaluated rules
                result.ParameterDefinitions.Should().ContainSingle();
                result.ParameterDefinitions.Should().ContainKey("EyeRightParam");
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
                
                // Act
                // With graceful degradation, this should succeed but with no evaluated parameters
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.Parameters.Should().BeEmpty(); // No parameters can be evaluated since both eyes are null
                
                // Parameter definitions should only include successfully evaluated rules
                result.ParameterDefinitions.Should().BeEmpty(); // No rules could be evaluated
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
            // Arrange - Create a file with content that deserializes to null
            var filePath = Path.GetTempFileName();
            await File.WriteAllTextAsync(filePath, "null");
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act & Assert
                var exception = await Assert.ThrowsAsync<JsonException>(() => engine.LoadRulesAsync(filePath));
                exception.Message.Should().Be("Failed to deserialize transformation rules");
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
                
                // Act & Assert - should not throw, should handle gracefully
                await engine.LoadRulesAsync(filePath);
                
                // Verify that evaluation fails gracefully at transform time
                var result = engine.TransformData(CreateValidTrackingData());
                result.Parameters.Should().BeEmpty(); // Rule couldn't be evaluated due to division by zero
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_HandlesExpressionParsingExceptions_Gracefully()
        {
            // Arrange - Create a rule that will cause an exception during Expression construction
            // This is tricky since NCalc is quite forgiving, but we can try with very malformed syntax
            var ruleContent = @"[
                {
                    ""name"": ""ValidRule"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""BadRule"",
                    ""func"": null,
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act & Assert - should handle the null expression gracefully
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => engine.LoadRulesAsync(filePath));
                exception.Message.Should().Contain("Failed to load 1 transformation rules");
                exception.Message.Should().Contain("Valid rules: 1");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new TransformationEngine(null));
            exception.ParamName.Should().Be("logger");
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsInvalidOperationException_WhenNoValidRulesFound()
        {
            // Arrange - Create rules that will all fail validation
            var ruleContent = @"[
                {
                    ""name"": ""BadRule1"",
                    ""func"": """",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""BadRule2"",
                    ""func"": ""invalid syntax +++"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""BadRule3"",
                    ""func"": ""validExpression"",
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
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => engine.LoadRulesAsync(filePath));
                exception.Message.Should().Contain("Failed to load 3 transformation rules");
                exception.Message.Should().Contain("Valid rules: 0");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task GetParameterDefinitions_ReturnsAllLoadedRules()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""TestParam1"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""TestParam2"",
                    ""func"": ""eyeBlinkRight * 50"",
                    ""min"": -50,
                    ""max"": 50,
                    ""defaultValue"": 25
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                await engine.LoadRulesAsync(filePath);
                
                // Act
                var parameters = engine.GetParameterDefinitions().ToList();
                
                // Assert
                parameters.Should().HaveCount(2);
                
                var param1 = parameters.First(p => p.Name == "TestParam1");
                param1.Min.Should().Be(0);
                param1.Max.Should().Be(100);
                param1.DefaultValue.Should().Be(0);
                
                var param2 = parameters.First(p => p.Name == "TestParam2");
                param2.Min.Should().Be(-50);
                param2.Max.Should().Be(50);
                param2.DefaultValue.Should().Be(25);
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }

        [Fact]
        public void GetParameterDefinitions_NoRules_ReturnsEmptyCollection()
        {
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            
            // Act
            var parameters = engine.GetParameterDefinitions();
            
            // Assert
            parameters.Should().BeEmpty();
        }

        [Fact]
        public async Task TransformData_PopulatesParameterCalculationExpressions()
        {
            // Arrange
            var ruleContent = @"[
                {
                    ""name"": ""TestParam1"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""TestParam2"",
                    ""func"": ""HeadPosX + HeadPosY"",
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
                    Position = new Coordinates { X = 0.1, Y = 0.2, Z = 0 },
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                // Act
                var result = engine.TransformData(trackingData);
                
                // Assert
                result.Should().NotBeNull();
                result.ParameterCalculationExpressions.Should().NotBeNull();
                result.ParameterCalculationExpressions.Should().HaveCount(2);
                
                // Verify expressions are stored correctly
                result.ParameterCalculationExpressions.Should().ContainKey("TestParam1");
                result.ParameterCalculationExpressions["TestParam1"].Should().Be("eyeBlinkLeft * 100");
                
                result.ParameterCalculationExpressions.Should().ContainKey("TestParam2");
                result.ParameterCalculationExpressions["TestParam2"].Should().Be("HeadPosX + HeadPosY");
                
                // Verify GetExpression helper method works
                result.GetExpression("TestParam1").Should().Be("eyeBlinkLeft * 100");
                result.GetExpression("TestParam2").Should().Be("HeadPosX + HeadPosY");
                result.GetExpression("NonExistentParam").Should().BeNull();
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
    }
} 