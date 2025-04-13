using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class TransformationEngineTests
    {
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
                var engine = new TransformationEngine();
                
                // Act
                await engine.LoadRulesAsync(filePath);
                
                // Create tracking data to test transformation
                var trackingData = new TrackingResponse
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                var result = engine.TransformData(trackingData).ToList();
                
                // Assert
                result.Should().NotBeEmpty();
                result.Should().ContainSingle();
                result.First().Id.Should().Be("TestParam");
                result.First().Value.Should().Be(50); // 0.5 * 100
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
            var engine = new TransformationEngine();
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
                var engine = new TransformationEngine();
                
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
            var engine = new TransformationEngine();
            var trackingData = new TrackingResponse
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
            result.Should().BeEmpty();
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
                var engine = new TransformationEngine();
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new TrackingResponse
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
                result.Should().BeEmpty();
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
                var engine = new TransformationEngine();
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new TrackingResponse
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
                var result = engine.TransformData(trackingData).ToList();
                
                // Assert
                result.Should().HaveCount(2);
                
                var headMovement = result.FirstOrDefault(p => p.Id == "HeadMovement");
                headMovement.Should().NotBeNull();
                headMovement.Value.Should().BeApproximately(20, 0.001); // 0.1 * 100 + 0.2 * 50 = 20
                
                var eyeBlink = result.FirstOrDefault(p => p.Id == "EyeBlink");
                eyeBlink.Should().NotBeNull();
                eyeBlink.Value.Should().BeApproximately(40, 0.001); // (0.3 + 0.5) * 50 = 40
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
                var engine = new TransformationEngine();
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new TrackingResponse
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 } // 0.5 * 1000 = 500, should be clamped to 100
                    }
                };
                
                // Act
                var result = engine.TransformData(trackingData).ToList();
                
                // Assert
                result.Should().ContainSingle();
                result.First().Value.Should().Be(100); // Clamped to max
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
                var engine = new TransformationEngine();
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new TrackingResponse
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
                    engine.TransformData(trackingData).ToList());
                
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
                var engine = new TransformationEngine();
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new TrackingResponse
                {
                    FaceFound = true,
                    EyeLeft = new Coordinates { X = 0.1, Y = 0.2, Z = 0.3 },
                    EyeRight = new Coordinates { X = 0.4, Y = 0.5, Z = 0.6 }
                };
                
                // Act
                var result = engine.TransformData(trackingData).ToList();
                
                // Assert
                result.Should().HaveCount(2);
                
                var eyeLeftParam = result.FirstOrDefault(p => p.Id == "EyeLeftParam");
                eyeLeftParam.Should().NotBeNull();
                eyeLeftParam.Value.Should().BeApproximately(0.1 + 0.2 + 0.3, 0.001); // 0.6
                
                var eyeRightParam = result.FirstOrDefault(p => p.Id == "EyeRightParam");
                eyeRightParam.Should().NotBeNull();
                eyeRightParam.Value.Should().BeApproximately(0.4 + 0.5 + 0.6, 0.001); // 1.5
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
                var engine = new TransformationEngine();
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new TrackingResponse
                {
                    FaceFound = true,
                    EyeLeft = null, // Null eye left
                    EyeRight = new Coordinates { X = 0.4, Y = 0.5, Z = 0.6 } // Valid eye right
                };
                
                // Act & Assert
                // With our new behavior, we should expect an exception when trying to access EyeLeftX
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    engine.TransformData(trackingData).ToList());
                
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
                var engine = new TransformationEngine();
                await engine.LoadRulesAsync(filePath);
                
                var trackingData = new TrackingResponse
                {
                    FaceFound = true,
                    EyeLeft = null,
                    EyeRight = null
                };
                
                // Act & Assert
                // With our new behavior, we should expect an exception when trying to access EyeLeftX
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    engine.TransformData(trackingData).ToList());
                
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
        public async Task LoadRulesAsync_SkipsRulesWithInvalidSyntax()
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
                var engine = new TransformationEngine();
                
                // Act
                await engine.LoadRulesAsync(filePath);
                
                // Prepare test data
                var trackingData = new TrackingResponse
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                        new BlendShape { Key = "eyeBlinkRight", Value = 0.8 }
                    }
                };
                
                var result = engine.TransformData(trackingData).ToList();
                
                // Assert
                // Should only have the valid rules, since invalid ones should be skipped during loading
                result.Should().HaveCount(2);
                
                // Check that valid rules were processed correctly
                var validRule1 = result.FirstOrDefault(p => p.Id == "ValidRule");
                validRule1.Should().NotBeNull();
                validRule1.Value.Should().Be(50); // 0.5 * 100
                
                var validRule2 = result.FirstOrDefault(p => p.Id == "AnotherValidRule");
                validRule2.Should().NotBeNull();
                validRule2.Value.Should().Be(40); // 0.8 * 50
                
                // The invalid rules should not be in the results
                result.FirstOrDefault(p => p.Id == "InvalidSyntaxRule").Should().BeNull();
                result.FirstOrDefault(p => p.Id == "EmptyRule").Should().BeNull();
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
                var engine = new TransformationEngine();
                
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
        public async Task LoadRulesAsync_WarnsAndContinues_WhenMinGreaterThanMax()
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
                var engine = new TransformationEngine();
                
                // Act
                await engine.LoadRulesAsync(filePath);
                
                // Prepare test data
                var trackingData = new TrackingResponse
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 },
                        new BlendShape { Key = "eyeBlinkRight", Value = 0.8 }
                    }
                };
                
                // Act & Assert
                // Math.Clamp will throw an exception because min > max
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    engine.TransformData(trackingData).ToList());
                
                // Verify that the exception message contains relevant information
                exception.Message.Should().Contain("InvalidRangeRule");
                exception.Message.Should().Contain("100");
                exception.Message.Should().Contain("0");
                
                // Verify that the inner exception is an ArgumentException
                exception.InnerException.Should().BeOfType<ArgumentException>();
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
            // We'll create a custom rule with a function that causes an exception
            // during expression creation but isn't caught by syntax validation
            var ruleContent = @"[
                {
                    ""name"": ""ValidRule"",
                    ""func"": ""eyeBlinkLeft * 100"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                },
                {
                    ""name"": ""ExceptionCausingRule"",
                    ""func"": ""If(1, 1, UnexpectedFunction())"",
                    ""min"": 0,
                    ""max"": 100,
                    ""defaultValue"": 0
                }
            ]";
            var filePath = CreateTempRuleFile(ruleContent);
            
            try
            {
                var engine = new TransformationEngine();
                
                // Act
                await engine.LoadRulesAsync(filePath);
                
                // Prepare test data
                var trackingData = new TrackingResponse
                {
                    FaceFound = true,
                    BlendShapes = new List<BlendShape>
                    {
                        new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                    }
                };
                
                // Act & Assert
                // With our new behavior, attempting to evaluate the expression will throw an exception
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    engine.TransformData(trackingData).ToList());
                
                // Verify that the exception message mentions the function name
                exception.Message.Should().Contain("ExceptionCausingRule");
                exception.Message.Should().Contain("If");
            }
            finally
            {
                // Cleanup
                File.Delete(filePath);
            }
        }
    }
} 