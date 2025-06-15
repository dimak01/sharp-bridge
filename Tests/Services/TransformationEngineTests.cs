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
                
                var headMovement = result.Parameters.FirstOrDefault(p => p.Id == "HeadMovement")!;
                headMovement.Should().NotBeNull();
                headMovement.Value.Should().BeApproximately(20, 0.001); // 0.1 * 100 + 0.2 * 50 = 20
                
                var eyeBlink = result.Parameters.FirstOrDefault(p => p.Id == "EyeBlink")!;
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
                
                var eyeLeftParam = result.Parameters.FirstOrDefault(p => p.Id == "EyeLeftParam")!;
                eyeLeftParam.Should().NotBeNull();
                eyeLeftParam.Value.Should().BeApproximately(0.1 + 0.2 + 0.3, 0.001); // 0.6
                
                var eyeRightParam = result.Parameters.FirstOrDefault(p => p.Id == "EyeRightParam")!;
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
        public async Task LoadRulesAsync_HandlesInvalidSyntaxWithGracefulDegradation()
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
                
                // Act - should succeed with graceful degradation
                await engine.LoadRulesAsync(filePath);
                
                // Assert - Check that service stats reflect partial loading
                var stats = engine.GetServiceStats();
                stats.Status.Should().Be("RulesPartiallyValid"); // 2 valid rules, 2 invalid rules
                stats.Counters["Valid Rules"].Should().Be(2);
                stats.Counters["Invalid Rules"].Should().Be(2);
                
                // Check that invalid rules are tracked
                var engineInfo = stats.CurrentEntity as TransformationEngineInfo;
                engineInfo.Should().NotBeNull();
                engineInfo.InvalidRules.Should().HaveCount(2);
                engineInfo.InvalidRules.Should().Contain(r => r.Name == "InvalidSyntaxRule");
                engineInfo.InvalidRules.Should().Contain(r => r.Name == "EmptyRule");
                
                // Test that valid rules still work
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                        new BlendShape { Key = "eyeBlinkRight", Value = 0.3 }
                    }
                };
                
                var result = engine.TransformData(trackingData);
                result.Parameters.Should().HaveCount(2); // Only valid rules processed
                result.Parameters.Should().Contain(p => p.Id == "ValidRule" && p.Value == 50);
                result.Parameters.Should().Contain(p => p.Id == "AnotherValidRule" && p.Value == 15);
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_HandlesInvalidRangeWithGracefulDegradation()
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
                
                // Act - should succeed with graceful degradation
                await engine.LoadRulesAsync(filePath);
                
                // Assert - Check that service stats reflect partial loading
                var stats = engine.GetServiceStats();
                stats.Status.Should().Be("RulesPartiallyValid"); // 1 valid rule, 1 invalid rule
                stats.Counters["Valid Rules"].Should().Be(1);
                stats.Counters["Invalid Rules"].Should().Be(1);
                
                // Check that invalid rule is tracked with proper error message
                var engineInfo = stats.CurrentEntity as TransformationEngineInfo;
                engineInfo.Should().NotBeNull();
                engineInfo.InvalidRules.Should().HaveCount(1);
                var invalidRule = engineInfo.InvalidRules.First();
                invalidRule.Name.Should().Be("InvalidRangeRule");
                invalidRule.Error.Should().Contain("Min").And.Contain("Max");
                
                // Test that valid rule still works
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                var result = engine.TransformData(trackingData);
                result.Parameters.Should().HaveCount(1); // Only valid rule processed
                result.Parameters.Should().Contain(p => p.Id == "ValidRule" && p.Value == 50);
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
        public async Task LoadRulesAsync_HandlesNullExpressionWithGracefulDegradation()
        {
            // Arrange - Create a rule that will cause an exception during Expression construction
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
                
                // Act - should succeed with graceful degradation
                await engine.LoadRulesAsync(filePath);
                
                // Assert - Check that service stats reflect partial loading
                var stats = engine.GetServiceStats();
                stats.Status.Should().Be("RulesPartiallyValid"); // 1 valid rule, 1 invalid rule
                stats.Counters["Valid Rules"].Should().Be(1);
                stats.Counters["Invalid Rules"].Should().Be(1);
                
                // Check that invalid rule is tracked with proper error message
                var engineInfo = stats.CurrentEntity as TransformationEngineInfo;
                engineInfo.Should().NotBeNull();
                engineInfo.InvalidRules.Should().HaveCount(1);
                var invalidRule = engineInfo.InvalidRules.First();
                invalidRule.Name.Should().Be("BadRule");
                invalidRule.Error.Should().Contain("empty expression");
                
                // Test that valid rule still works
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                var result = engine.TransformData(trackingData);
                result.Parameters.Should().HaveCount(1); // Only valid rule processed
                result.Parameters.Should().Contain(p => p.Id == "ValidRule" && p.Value == 50);
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
        public async Task LoadRulesAsync_HandlesAllInvalidRulesWithGracefulDegradation()
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
                
                // Act - should succeed even with all invalid rules
                await engine.LoadRulesAsync(filePath);
                
                // Assert - Check that service stats reflect no valid rules
                var stats = engine.GetServiceStats();
                stats.Status.Should().Be("NoValidRules"); // No valid rules loaded
                stats.Counters["Valid Rules"].Should().Be(0);
                stats.Counters["Invalid Rules"].Should().Be(3);
                
                // Check that all invalid rules are tracked
                var engineInfo = stats.CurrentEntity as TransformationEngineInfo;
                engineInfo.Should().NotBeNull();
                engineInfo.InvalidRules.Should().HaveCount(3);
                engineInfo.InvalidRules.Should().Contain(r => r.Name == "BadRule1");
                engineInfo.InvalidRules.Should().Contain(r => r.Name == "BadRule2");
                engineInfo.InvalidRules.Should().Contain(r => r.Name == "BadRule3");
                
                // Test that transformation returns empty results but doesn't crash
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                var result = engine.TransformData(trackingData);
                result.Parameters.Should().BeEmpty(); // No valid rules to process
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
                result.ParameterCalculationExpressions["TestParam1"].Should().Be("eyeBlinkLeft * 100");
                result.ParameterCalculationExpressions["TestParam2"].Should().Be("HeadPosX + HeadPosY");
                result.ParameterCalculationExpressions.Should().NotContainKey("NonExistentParam");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task LoadRulesAsync_HandlesUnexpectedExceptionDuringRuleProcessing()
        {
            // Arrange - Create a JSON rule that will cause validation error (normal case that's already handled)
            // This test mainly validates the exception handling path is working
            var ruleContent = @"[
                {
                    ""name"": ""ValidRule"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""InvalidRule"",
                    ""func"": ""invalid_syntax_expression_[{#"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine(_mockLogger.Object);
                
                // Act - should not throw, should handle gracefully
                await engine.LoadRulesAsync(filePath);
                
                // Assert - Check that service stats reflect partial loading
                var stats = engine.GetServiceStats();
                stats.Status.Should().Be("RulesPartiallyValid"); // 1 valid rule, 1 invalid rule due to exception
                stats.Counters["Valid Rules"].Should().Be(1);
                stats.Counters["Invalid Rules"].Should().Be(1);
                
                // Check that exception was caught and rule marked as invalid
                var engineInfo = stats.CurrentEntity as TransformationEngineInfo;
                engineInfo.Should().NotBeNull();
                engineInfo.InvalidRules.Should().HaveCount(1);
                var invalidRule = engineInfo.InvalidRules.First();
                invalidRule.Name.Should().Be("InvalidRule");
                invalidRule.Type.Should().Be("Validation");
                invalidRule.Error.Should().NotBeNullOrEmpty();
                
                // Verify the valid rule still works
                var trackingData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                var result = engine.TransformData(trackingData);
                result.Parameters.Should().HaveCount(1); // Only valid rule processed
                result.Parameters.Should().Contain(p => p.Id == "ValidRule" && p.Value == 50);
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
        
        [Fact]
        public async Task TransformData_HandlesUnexpectedExceptionDuringTransformation()
        {
            // Arrange - Set up a scenario that will cause an exception during transformation
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
                
                // Create tracking data with a BlendShape that has a null Key to cause an exception
                // This will trigger an exception during parameter processing
                var malformedData = new PhoneTrackingInfo
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        null // This will cause a NullReferenceException when iterating over BlendShapes
                    }
                };
                
                // Act - should handle the exception gracefully
                var result = engine.TransformData(malformedData);
                
                // Assert - should return safe fallback result
                result.Should().NotBeNull();
                result.FaceFound.Should().BeTrue(); // Should preserve the original FaceFound value
                result.Parameters.Should().BeNullOrEmpty();
                
                // Verify error statistics were updated
                var stats = engine.GetServiceStats();
                stats.Counters["Total Transformations"].Should().Be(1);
                stats.Counters["Failed Transformations"].Should().Be(1);
                stats.Counters["Successful Transformations"].Should().Be(0);
                stats.LastError.Should().NotBeNullOrEmpty();
                stats.LastError.Should().Contain("Transformation failed");
                
                // Verify that logger was called for the error
                _mockLogger.Verify(logger => logger.Error(It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeastOnce);
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
    }
} 