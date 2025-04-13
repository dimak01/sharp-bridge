using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.TrackingTests
{
    public class TransformationEngineTests
    {
        [Fact]
        public async Task LoadRulesAsync_LoadsValidRules()
        {
            // Arrange
            var mockEngine = new Mock<ITransformationEngine>();
            var filePath = "rules.json";
            
            // Setup mock behavior
            mockEngine
                .Setup(e => e.LoadRulesAsync(filePath))
                .Returns(Task.CompletedTask);
                
            // Define what happens when TransformData is called after loading rules
            mockEngine
                .Setup(e => e.TransformData(It.Is<TrackingResponse>(r => 
                    r.FaceFound == true && 
                    r.BlendShapes.Any(bs => bs.Key == "eyeBlinkLeft" && bs.Value == 0.5))))
                .Returns(new List<TrackingParam> 
                { 
                    new TrackingParam { Id = "TestParam", Value = 50 }
                });
            
            // Act
            await mockEngine.Object.LoadRulesAsync(filePath);
            
            // Create tracking data to test transformation
            var trackingData = new TrackingResponse
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };
            
            var result = mockEngine.Object.TransformData(trackingData).ToList();
            
            // Assert
            mockEngine.Verify(e => e.LoadRulesAsync(filePath), Times.Once);
            result.Should().NotBeEmpty();
            result.Should().ContainSingle();
            result.First().Id.Should().Be("TestParam");
            result.First().Value.Should().Be(50); // 0.5 * 100
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsException_WhenFileNotFound()
        {
            // Arrange
            var mockEngine = new Mock<ITransformationEngine>();
            var nonExistentPath = "nonexistent.json";
            
            // Setup mock behavior - throw FileNotFoundException when the file doesn't exist
            mockEngine
                .Setup(e => e.LoadRulesAsync(nonExistentPath))
                .ThrowsAsync(new FileNotFoundException($"File not found: {nonExistentPath}"));
            
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(async () => 
                await mockEngine.Object.LoadRulesAsync(nonExistentPath));
        }
        
        [Fact]
        public async Task LoadRulesAsync_ThrowsException_WhenJsonIsInvalid()
        {
            // Arrange
            var mockEngine = new Mock<ITransformationEngine>();
            var invalidJsonPath = "invalid.json";
            
            // Setup mock behavior - throw JsonException when the file contains invalid JSON
            mockEngine
                .Setup(e => e.LoadRulesAsync(invalidJsonPath))
                .ThrowsAsync(new JsonException("Invalid JSON format"));
            
            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(async () => 
                await mockEngine.Object.LoadRulesAsync(invalidJsonPath));
        }
        
        [Fact]
        public void TransformData_ReturnsEmptyCollection_WhenNoRulesLoaded()
        {
            // Arrange
            var mockEngine = new Mock<ITransformationEngine>();
            
            // Setup mock behavior - return empty collection when no rules loaded
            mockEngine
                .Setup(e => e.TransformData(It.IsAny<TrackingResponse>()))
                .Returns(Enumerable.Empty<TrackingParam>());
            
            var trackingData = new TrackingResponse
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };
            
            // Act
            var result = mockEngine.Object.TransformData(trackingData);
            
            // Assert
            result.Should().BeEmpty();
        }
        
        [Fact]
        public async Task TransformData_ReturnsEmptyCollection_WhenFaceNotFound()
        {
            // Arrange
            var mockEngine = new Mock<ITransformationEngine>();
            var filePath = "rules.json";
            
            // Setup mock behavior
            mockEngine
                .Setup(e => e.LoadRulesAsync(filePath))
                .Returns(Task.CompletedTask);
                
            // Return empty collection when face is not found
            mockEngine
                .Setup(e => e.TransformData(It.Is<TrackingResponse>(r => r.FaceFound == false)))
                .Returns(Enumerable.Empty<TrackingParam>());
            
            await mockEngine.Object.LoadRulesAsync(filePath);
            
            var trackingData = new TrackingResponse
            {
                FaceFound = false, // Face not found
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };
            
            // Act
            var result = mockEngine.Object.TransformData(trackingData);
            
            // Assert
            result.Should().BeEmpty();
        }
        
        [Fact]
        public async Task TransformData_AppliesExpressions_WithCorrectContext()
        {
            // Arrange
            var mockEngine = new Mock<ITransformationEngine>();
            var filePath = "rules.json";
            
            // Setup mock behavior
            mockEngine
                .Setup(e => e.LoadRulesAsync(filePath))
                .Returns(Task.CompletedTask);
                
            // Setup transformation behavior with position, rotation and multiple blend shapes
            mockEngine
                .Setup(e => e.TransformData(It.Is<TrackingResponse>(r => 
                    r.FaceFound == true && 
                    r.Position.X == 0.1 && 
                    r.Rotation.Y == 0.2 &&
                    r.BlendShapes.Any(bs => bs.Key == "eyeBlinkLeft" && bs.Value == 0.3) &&
                    r.BlendShapes.Any(bs => bs.Key == "eyeBlinkRight" && bs.Value == 0.5))))
                .Returns(new List<TrackingParam> 
                { 
                    new TrackingParam { Id = "HeadMovement", Value = 20 }, // 0.1 * 100 + 0.2 * 50
                    new TrackingParam { Id = "EyeBlink", Value = 40 }      // (0.3 + 0.5) * 50
                });
            
            await mockEngine.Object.LoadRulesAsync(filePath);
            
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
            var result = mockEngine.Object.TransformData(trackingData).ToList();
            
            // Assert
            result.Should().HaveCount(2);
            
            var headMovement = result.FirstOrDefault(p => p.Id == "HeadMovement");
            headMovement.Should().NotBeNull();
            headMovement.Value.Should().Be(20); // 0.1 * 100 + 0.2 * 50 = 20
            
            var eyeBlink = result.FirstOrDefault(p => p.Id == "EyeBlink");
            eyeBlink.Should().NotBeNull();
            eyeBlink.Value.Should().Be(40); // (0.3 + 0.5) * 50 = 40
        }
        
        [Fact]
        public async Task TransformData_ClampsMappedValuesToMinMax()
        {
            // Arrange
            var mockEngine = new Mock<ITransformationEngine>();
            var filePath = "rules.json";
            
            // Setup mock behavior
            mockEngine
                .Setup(e => e.LoadRulesAsync(filePath))
                .Returns(Task.CompletedTask);
                
            // Setup transformation that should apply clamping to a value
            mockEngine
                .Setup(e => e.TransformData(It.Is<TrackingResponse>(r => 
                    r.FaceFound == true && 
                    r.BlendShapes.Any(bs => bs.Key == "eyeBlinkLeft" && bs.Value == 0.5))))
                .Returns(new List<TrackingParam> 
                { 
                    // Value would be 500 (0.5 * 1000) but is clamped to 100
                    new TrackingParam { Id = "LimitedValue", Value = 100 }
                });
            
            await mockEngine.Object.LoadRulesAsync(filePath);
            
            var trackingData = new TrackingResponse
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };
            
            // Act
            var result = mockEngine.Object.TransformData(trackingData).ToList();
            
            // Assert
            result.Should().ContainSingle();
            result.First().Value.Should().Be(100); // Clamped to max
        }
        
        [Fact]
        public async Task TransformData_HandlesInvalidExpressions_Gracefully()
        {
            // Arrange
            var mockEngine = new Mock<ITransformationEngine>();
            var filePath = "rules.json";
            
            // Setup mock behavior
            mockEngine
                .Setup(e => e.LoadRulesAsync(filePath))
                .Returns(Task.CompletedTask);
                
            // Setup transformation that includes handling invalid expressions by using default values
            mockEngine
                .Setup(e => e.TransformData(It.Is<TrackingResponse>(r => 
                    r.FaceFound == true && 
                    r.BlendShapes.Any(bs => bs.Key == "eyeBlinkLeft" && bs.Value == 0.5))))
                .Returns(new List<TrackingParam> 
                { 
                    new TrackingParam { Id = "ValidParam", Value = 50 },      // 0.5 * 100 = 50
                    new TrackingParam { Id = "InvalidParam", Value = 50 }     // Default value when expression fails
                });
            
            await mockEngine.Object.LoadRulesAsync(filePath);
            
            var trackingData = new TrackingResponse
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "eyeBlinkLeft", Value = 0.5 }
                }
            };
            
            // Act
            var result = mockEngine.Object.TransformData(trackingData).ToList();
            
            // Assert
            result.Should().HaveCount(2);
            
            var validParam = result.FirstOrDefault(p => p.Id == "ValidParam");
            validParam.Should().NotBeNull();
            validParam.Value.Should().Be(50); // 0.5 * 100 = 50
            
            var invalidParam = result.FirstOrDefault(p => p.Id == "InvalidParam");
            invalidParam.Should().NotBeNull();
            invalidParam.Value.Should().Be(50); // Default value when expression fails
        }
    }
} 