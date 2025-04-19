using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;
using Xunit;

namespace SharpBridge.Tests.Services
{
    public class TransformConfigTests
    {
        private readonly string _configPath = Path.Combine("Tests", "TestData", "transform-config.json");
        private readonly Mock<IAppLogger> _mockLogger;

        public TransformConfigTests()
        {
            _mockLogger = new Mock<IAppLogger>();
        }
        
        [Fact]
        public async Task FaceAngleY_TransformsCorrectly()
        {
            // Skip if the config file doesn't exist
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = 0, Y = 0.5, Z = 0 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var faceAngleY = result.FirstOrDefault(p => p.Id == "FaceAngleY");
            faceAngleY.Should().NotBeNull();
            
            // Expected: -HeadRotY * 1 = -0.5
            faceAngleY.Value.Should().BeApproximately(-0.5, 0.001);
        }
        
        [Fact]
        public async Task FaceAngleX_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = 0.3, Y = 0.2, Z = 0.1 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var faceAngleX = result.FirstOrDefault(p => p.Id == "FaceAngleX");
            faceAngleX.Should().NotBeNull();
            
            // Expected: (((HeadRotX * ((90 - Abs(HeadRotY)) / 90)) + (HeadRotZ * (HeadRotY / 45))))
            // = (((0.3 * ((90 - Abs(0.2)) / 90)) + (0.1 * (0.2 / 45))))
            var expected = ((0.3 * ((90 - Math.Abs(0.2)) / 90)) + (0.1 * (0.2 / 45)));
            faceAngleX.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task FacePositionX_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0.5, Y = 0, Z = 0 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var facePositionX = result.FirstOrDefault(p => p.Id == "FacePositionX");
            facePositionX.Should().NotBeNull();
            
            // Expected: HeadPosX * -1 = -0.5
            facePositionX.Value.Should().BeApproximately(-0.5, 0.001);
        }
        
        [Fact]
        public async Task MouthOpen_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "JawOpen", Value = 0.6 },
                    new BlendShape { Key = "MouthClose", Value = 0.1 },
                    new BlendShape { Key = "MouthRollUpper", Value = 0.2 },
                    new BlendShape { Key = "MouthRollLower", Value = 0.1 },
                    new BlendShape { Key = "MouthFunnel", Value = 0.3 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var mouthOpen = result.FirstOrDefault(p => p.Id == "MouthOpen");
            mouthOpen.Should().NotBeNull();
            
            // Expected: (((JawOpen - MouthClose) - ((MouthRollUpper + MouthRollLower) * 0.2) + (MouthFunnel * 0.2)))
            // = (((0.6 - 0.1) - ((0.2 + 0.1) * 0.2) + (0.3 * 0.2)))
            var expected = (0.6 - 0.1) - ((0.2 + 0.1) * 0.2) + (0.3 * 0.2);
            mouthOpen.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task EyeRightX_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeLookInLeft", Value = 0.4 },
                    new BlendShape { Key = "EyeLookOutLeft", Value = 0.2 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var eyeRightX = result.FirstOrDefault(p => p.Id == "EyeRightX");
            eyeRightX.Should().NotBeNull();
            
            // Expected: (EyeLookInLeft - 0.1) - EyeLookOutLeft = (0.4 - 0.1) - 0.2 = 0.1
            var expected = (0.4 - 0.1) - 0.2;
            eyeRightX.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BoundaryTest_ClampingToMax()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            // Create extreme values that should trigger max clamping
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = 1000, Y = 0, Z = 0 }, // Extreme rotation
                Position = new Coordinates { X = 1000, Y = 0, Z = 0 }, // Extreme position
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeBlinkLeft", Value = 10 }, // Extreme value (should be 0-1)
                    new BlendShape { Key = "EyeBlinkRight", Value = 10 } // Extreme value (should be 0-1)
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            // FaceAngleX max is 40
            var faceAngleX = result.FirstOrDefault(p => p.Id == "FaceAngleX");
            faceAngleX.Should().NotBeNull();
            faceAngleX.Value.Should().BeLessThanOrEqualTo(40);
            
            // FacePositionX max is 15
            var facePositionX = result.FirstOrDefault(p => p.Id == "FacePositionX");
            facePositionX.Should().NotBeNull();
            facePositionX.Value.Should().BeLessThanOrEqualTo(15);
            
            // EyeOpenLeft max is 1
            var eyeOpenLeft = result.FirstOrDefault(p => p.Id == "EyeOpenLeft");
            if (eyeOpenLeft != null)
            {
                eyeOpenLeft.Value.Should().BeLessThanOrEqualTo(1);
            }
        }
        
        [Fact]
        public async Task BoundaryTest_ClampingToMin()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            // Create extreme values that should trigger min clamping
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = -1000, Y = 0, Z = 0 }, // Extreme negative rotation
                Position = new Coordinates { X = -1000, Y = 0, Z = 0 }, // Extreme negative position
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeBlinkLeft", Value = -2 }, // Negative value (should be 0-1)
                    new BlendShape { Key = "EyeBlinkRight", Value = -2 } // Negative value (should be 0-1)
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            // FaceAngleX min is -40
            var faceAngleX = result.FirstOrDefault(p => p.Id == "FaceAngleX");
            faceAngleX.Should().NotBeNull();
            faceAngleX.Value.Should().BeGreaterThanOrEqualTo(-40);
            
            // FacePositionX min is -15
            var facePositionX = result.FirstOrDefault(p => p.Id == "FacePositionX");
            facePositionX.Should().NotBeNull();
            facePositionX.Value.Should().BeGreaterThanOrEqualTo(-15);
            
            // MouthOpen min is 0
            var mouthOpen = result.FirstOrDefault(p => p.Id == "MouthOpen");
            if (mouthOpen != null)
            {
                mouthOpen.Value.Should().BeGreaterThanOrEqualTo(0);
            }
        }
        
        [Fact]
        public async Task DefaultValue_UsedWhenExpressionFails()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            // Create tracking data with missing values required by expressions
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                // No Position or Rotation
                BlendShapes = new List<BlendShape>() // Empty blend shapes
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            // Check default values for various parameters
            
            // FaceAngleY should use default value 0
            var faceAngleY = result.FirstOrDefault(p => p.Id == "FaceAngleY");
            faceAngleY.Should().NotBeNull();
            faceAngleY.Value.Should().Be(0); // Default value
            
            // MouthOpen should use default value 0
            var mouthOpen = result.FirstOrDefault(p => p.Id == "MouthOpen");
            if (mouthOpen != null)
            {
                mouthOpen.Value.Should().Be(0); // Default value
            }
            
            // Brows should use default value 0.5
            var brows = result.FirstOrDefault(p => p.Id == "Brows");
            if (brows != null)
            {
                brows.Value.Should().Be(0.5); // Default value
            }
        }
        
        [Fact]
        public async Task FaceAngleZ_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = 0.3, Y = 0.2, Z = 0.1 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var faceAngleZ = result.FirstOrDefault(p => p.Id == "FaceAngleZ");
            faceAngleZ.Should().NotBeNull();
            
            // Expected: ((HeadRotZ * ((90 - Abs(HeadRotY)) / 90)) - (HeadRotX * (HeadRotY / 45)))
            // = ((0.1 * ((90 - Abs(0.2)) / 90)) - (0.3 * (0.2 / 45)))
            var expected = ((0.1 * ((90 - Math.Abs(0.2)) / 90)) - (0.3 * (0.2 / 45)));
            faceAngleZ.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task FacePositionY_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0, Y = 0.5, Z = 0 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var facePositionY = result.FirstOrDefault(p => p.Id == "FacePositionY");
            facePositionY.Should().NotBeNull();
            
            // Expected: HeadPosY = 0.5
            facePositionY.Value.Should().BeApproximately(0.5, 0.001);
        }
        
        [Fact]
        public async Task FacePositionZ_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0, Y = 0, Z = 0.5 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var facePositionZ = result.FirstOrDefault(p => p.Id == "FacePositionZ");
            facePositionZ.Should().NotBeNull();
            
            // Expected: HeadPosZ = 0.5
            facePositionZ.Value.Should().BeApproximately(0.5, 0.001);
        }
        
        [Fact]
        public async Task EyeRightY_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = 0.3, Y = 0, Z = 0 },
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeLookUpLeft", Value = 0.4 },
                    new BlendShape { Key = "EyeLookDownLeft", Value = 0.1 },
                    new BlendShape { Key = "BrowOuterUpLeft", Value = 0.2 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var eyeRightY = result.FirstOrDefault(p => p.Id == "EyeRightY");
            eyeRightY.Should().NotBeNull();
            
            // Expected: (EyeLookUpLeft - EyeLookDownLeft) + (BrowOuterUpLeft * 0.15) + (HeadRotX / 30)
            // = (0.4 - 0.1) + (0.2 * 0.15) + (0.3 / 30)
            var expected = (0.4 - 0.1) + (0.2 * 0.15) + (0.3 / 30);
            eyeRightY.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task EyeOpenLeft_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeBlinkLeft", Value = 0.3 },
                    new BlendShape { Key = "EyeWideLeft", Value = 0.2 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var eyeOpenLeft = result.FirstOrDefault(p => p.Id == "EyeOpenLeft");
            eyeOpenLeft.Should().NotBeNull();
            
            // Expected: 0.5 + ((EyeBlinkLeft * -0.8) + (EyeWideLeft * 0.8))
            // = 0.5 + ((0.3 * -0.8) + (0.2 * 0.8))
            var expected = 0.5 + ((0.3 * -0.8) + (0.2 * 0.8));
            eyeOpenLeft.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task EyeOpenRight_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeBlinkRight", Value = 0.4 },
                    new BlendShape { Key = "EyeWideRight", Value = 0.1 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var eyeOpenRight = result.FirstOrDefault(p => p.Id == "EyeOpenRight");
            eyeOpenRight.Should().NotBeNull();
            
            // Expected: 0.5 + ((EyeBlinkRight * -0.8) + (EyeWideRight * 0.8))
            // = 0.5 + ((0.4 * -0.8) + (0.1 * 0.8))
            var expected = 0.5 + ((0.4 * -0.8) + (0.1 * 0.8));
            eyeOpenRight.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task MouthSmile_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "MouthFrownLeft", Value = 0.1 },
                    new BlendShape { Key = "MouthFrownRight", Value = 0.1 },
                    new BlendShape { Key = "MouthPucker", Value = 0.2 },
                    new BlendShape { Key = "MouthSmileRight", Value = 0.3 },
                    new BlendShape { Key = "MouthSmileLeft", Value = 0.4 },
                    new BlendShape { Key = "MouthDimpleLeft", Value = 0.5 },
                    new BlendShape { Key = "MouthDimpleRight", Value = 0.6 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var mouthSmile = result.FirstOrDefault(p => p.Id == "MouthSmile");
            mouthSmile.Should().NotBeNull();
            
            // Expected: (2 - ((MouthFrownLeft + MouthFrownRight + MouthPucker) / 1) + ((MouthSmileRight + MouthSmileLeft + ((MouthDimpleLeft + MouthDimpleRight) / 2)) / 1)) / 4
            var expected = (2 - ((0.1 + 0.1 + 0.2) / 1) + ((0.3 + 0.4 + ((0.5 + 0.6) / 2)) / 1)) / 4;
            mouthSmile.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task EyeSquintL_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeSquintLeft", Value = 0.7 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var eyeSquintL = result.FirstOrDefault(p => p.Id == "EyeSquintL");
            eyeSquintL.Should().NotBeNull();
            
            // Expected: EyeSquintLeft = 0.7
            eyeSquintL.Value.Should().BeApproximately(0.7, 0.001);
        }
        
        [Fact]
        public async Task EyeSquintR_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeSquintRight", Value = 0.6 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var eyeSquintR = result.FirstOrDefault(p => p.Id == "EyeSquintR");
            eyeSquintR.Should().NotBeNull();
            
            // Expected: EyeSquintRight = 0.6
            eyeSquintR.Value.Should().BeApproximately(0.6, 0.001);
        }
        
        [Fact]
        public async Task MouthX_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "MouthLeft", Value = 0.3 },
                    new BlendShape { Key = "MouthRight", Value = 0.1 },
                    new BlendShape { Key = "MouthSmileLeft", Value = 0.5 },
                    new BlendShape { Key = "MouthSmileRight", Value = 0.2 },
                    new BlendShape { Key = "TongueOut", Value = 0.4 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var mouthX = result.FirstOrDefault(p => p.Id == "MouthX");
            mouthX.Should().NotBeNull();
            
            // Expected: (((MouthLeft - MouthRight) + (MouthSmileLeft - MouthSmileRight)) * (1 - TongueOut))
            // = (((0.3 - 0.1) + (0.5 - 0.2)) * (1 - 0.4))
            var expected = (((0.3 - 0.1) + (0.5 - 0.2)) * (1 - 0.4));
            mouthX.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task CheekPuff_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "CheekPuff", Value = 0.8 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var cheekPuff = result.FirstOrDefault(p => p.Id == "CheekPuff");
            cheekPuff.Should().NotBeNull();
            
            // Expected: CheekPuff = 0.8
            cheekPuff.Value.Should().BeApproximately(0.8, 0.001);
        }
        
        [Fact]
        public async Task TongueOut_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "TongueOut", Value = 0.9 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var tongueOut = result.FirstOrDefault(p => p.Id == "TongueOut");
            tongueOut.Should().NotBeNull();
            
            // Expected: TongueOut = 0.9
            tongueOut.Value.Should().BeApproximately(0.9, 0.001);
        }
        
        [Fact]
        public async Task MouthPucker_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "MouthDimpleRight", Value = 0.4 },
                    new BlendShape { Key = "MouthDimpleLeft", Value = 0.3 },
                    new BlendShape { Key = "MouthPucker", Value = 0.2 },
                    new BlendShape { Key = "TongueOut", Value = 0.1 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var mouthPucker = result.FirstOrDefault(p => p.Id == "MouthPucker");
            mouthPucker.Should().NotBeNull();
            
            // Expected: (((MouthDimpleRight + MouthDimpleLeft) * 2) - MouthPucker) * (1 - TongueOut)
            // = (((0.4 + 0.3) * 2) - 0.2) * (1 - 0.1)
            var expected = (((0.4 + 0.3) * 2) - 0.2) * (1 - 0.1);
            mouthPucker.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task MouthFunnel_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "MouthFunnel", Value = 0.7 },
                    new BlendShape { Key = "TongueOut", Value = 0.2 },
                    new BlendShape { Key = "JawOpen", Value = 0.3 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var mouthFunnel = result.FirstOrDefault(p => p.Id == "MouthFunnel");
            mouthFunnel.Should().NotBeNull();
            
            // Expected: (MouthFunnel * (1 - TongueOut)) - (JawOpen * 0.2)
            // = (0.7 * (1 - 0.2)) - (0.3 * 0.2)
            var expected = (0.7 * (1 - 0.2)) - (0.3 * 0.2);
            mouthFunnel.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task JawOpen_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "JawOpen", Value = 0.6 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var jawOpen = result.FirstOrDefault(p => p.Id == "JawOpen");
            jawOpen.Should().NotBeNull();
            
            // Expected: JawOpen = 0.6
            jawOpen.Value.Should().BeApproximately(0.6, 0.001);
        }
        
        [Fact]
        public async Task MouthPressLipOpen_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "MouthUpperUpRight", Value = 0.2 },
                    new BlendShape { Key = "MouthUpperUpLeft", Value = 0.3 },
                    new BlendShape { Key = "MouthLowerDownRight", Value = 0.4 },
                    new BlendShape { Key = "MouthLowerDownLeft", Value = 0.5 },
                    new BlendShape { Key = "MouthRollLower", Value = 0.1 },
                    new BlendShape { Key = "MouthRollUpper", Value = 0.2 },
                    new BlendShape { Key = "TongueOut", Value = 0.3 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var mouthPressLipOpen = result.FirstOrDefault(p => p.Id == "MouthPressLipOpen");
            mouthPressLipOpen.Should().NotBeNull();
            
            // Expected: (((MouthUpperUpRight + MouthUpperUpLeft + MouthLowerDownRight + MouthLowerDownLeft) / 1.8) - (MouthRollLower + MouthRollUpper)) * (1 - TongueOut)
            // = (((0.2 + 0.3 + 0.4 + 0.5) / 1.8) - (0.1 + 0.2)) * (1 - 0.3)
            var expected = (((0.2 + 0.3 + 0.4 + 0.5) / 1.8) - (0.1 + 0.2)) * (1 - 0.3);
            mouthPressLipOpen.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task MouthShrug_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "MouthShrugUpper", Value = 0.1 },
                    new BlendShape { Key = "MouthShrugLower", Value = 0.2 },
                    new BlendShape { Key = "MouthPressRight", Value = 0.3 },
                    new BlendShape { Key = "MouthPressLeft", Value = 0.4 },
                    new BlendShape { Key = "TongueOut", Value = 0.5 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var mouthShrug = result.FirstOrDefault(p => p.Id == "MouthShrug");
            mouthShrug.Should().NotBeNull();
            
            // Expected: ((MouthShrugUpper + MouthShrugLower + MouthPressRight + MouthPressLeft) / 4) * (1 - TongueOut)
            // = ((0.1 + 0.2 + 0.3 + 0.4) / 4) * (1 - 0.5)
            var expected = ((0.1 + 0.2 + 0.3 + 0.4) / 4) * (1 - 0.5);
            mouthShrug.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BrowInnerUp_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "BrowInnerUp", Value = 0.7 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var browInnerUp = result.FirstOrDefault(p => p.Id == "BrowInnerUp");
            browInnerUp.Should().NotBeNull();
            
            // Expected: BrowInnerUp = 0.7
            browInnerUp.Value.Should().BeApproximately(0.7, 0.001);
        }
        
        [Fact]
        public async Task BrowLeftY_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "BrowOuterUpLeft", Value = 0.4 },
                    new BlendShape { Key = "BrowDownLeft", Value = 0.1 },
                    new BlendShape { Key = "MouthRight", Value = 0.5 },
                    new BlendShape { Key = "MouthLeft", Value = 0.2 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var browLeftY = result.FirstOrDefault(p => p.Id == "BrowLeftY");
            browLeftY.Should().NotBeNull();
            
            // Expected: 0.5 + (BrowOuterUpLeft - BrowDownLeft) + ((MouthRight - MouthLeft) / 8)
            // = 0.5 + (0.4 - 0.1) + ((0.5 - 0.2) / 8)
            var expected = 0.5 + (0.4 - 0.1) + ((0.5 - 0.2) / 8);
            browLeftY.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BrowRightY_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "BrowOuterUpRight", Value = 0.5 },
                    new BlendShape { Key = "BrowDownRight", Value = 0.2 },
                    new BlendShape { Key = "MouthLeft", Value = 0.6 },
                    new BlendShape { Key = "MouthRight", Value = 0.3 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var browRightY = result.FirstOrDefault(p => p.Id == "BrowRightY");
            browRightY.Should().NotBeNull();
            
            // Expected: 0.5 + (BrowOuterUpRight - BrowDownRight) + ((MouthLeft - MouthRight) / 8)
            // = 0.5 + (0.5 - 0.2) + ((0.6 - 0.3) / 8)
            var expected = 0.5 + (0.5 - 0.2) + ((0.6 - 0.3) / 8);
            browRightY.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task Brows_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "BrowOuterUpRight", Value = 0.4 },
                    new BlendShape { Key = "BrowOuterUpLeft", Value = 0.3 },
                    new BlendShape { Key = "BrowDownLeft", Value = 0.1 },
                    new BlendShape { Key = "BrowDownRight", Value = 0.2 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var brows = result.FirstOrDefault(p => p.Id == "Brows");
            brows.Should().NotBeNull();
            
            // Expected: 0.5 + (BrowOuterUpRight + BrowOuterUpLeft - BrowDownLeft - BrowDownRight) / 4
            // = 0.5 + (0.4 + 0.3 - 0.1 - 0.2) / 4
            var expected = 0.5 + (0.4 + 0.3 - 0.1 - 0.2) / 4;
            brows.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task VoiceFrequencyPlusMouthSmile_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "MouthFrownLeft", Value = 0.1 },
                    new BlendShape { Key = "MouthFrownRight", Value = 0.1 },
                    new BlendShape { Key = "MouthPucker", Value = 0.2 },
                    new BlendShape { Key = "MouthSmileRight", Value = 0.3 },
                    new BlendShape { Key = "MouthSmileLeft", Value = 0.4 },
                    new BlendShape { Key = "MouthDimpleLeft", Value = 0.5 },
                    new BlendShape { Key = "MouthDimpleRight", Value = 0.6 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var voiceFrequency = result.FirstOrDefault(p => p.Id == "VoiceFrequencyPlusMouthSmile");
            voiceFrequency.Should().NotBeNull();
            
            // This has the same formula as MouthSmile:
            // Expected: (2 - ((MouthFrownLeft + MouthFrownRight + MouthPucker) / 1) + ((MouthSmileRight + MouthSmileLeft + ((MouthDimpleLeft + MouthDimpleRight) / 2)) / 1)) / 4
            var expected = (2 - ((0.1 + 0.1 + 0.2) / 1) + ((0.3 + 0.4 + ((0.5 + 0.6) / 2)) / 1)) / 4;
            voiceFrequency.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BodyAngleX_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = 0, Y = 0.4, Z = 0 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var bodyAngleX = result.FirstOrDefault(p => p.Id == "BodyAngleX");
            bodyAngleX.Should().NotBeNull();
            
            // Expected: -HeadRotY * 1.5 = -0.4 * 1.5 = -0.6
            var expected = -0.4 * 1.5;
            bodyAngleX.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BodyAngleY_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = 0.3, Y = 0, Z = 0 },
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "EyeBlinkLeft", Value = 0.2 },
                    new BlendShape { Key = "EyeBlinkRight", Value = 0.3 }
                }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var bodyAngleY = result.FirstOrDefault(p => p.Id == "BodyAngleY");
            bodyAngleY.Should().NotBeNull();
            
            // Expected: (-HeadRotX * 1.5) + ((EyeBlinkLeft + EyeBlinkRight) * -1)
            // = (-0.3 * 1.5) + ((0.2 + 0.3) * -1)
            var expected = (-0.3 * 1.5) + ((0.2 + 0.3) * -1);
            bodyAngleY.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BodyAngleZ_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Rotation = new Coordinates { X = 0, Y = 0, Z = 0.2 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var bodyAngleZ = result.FirstOrDefault(p => p.Id == "BodyAngleZ");
            bodyAngleZ.Should().NotBeNull();
            
            // Expected: HeadRotZ * 1.5 = 0.2 * 1.5 = 0.3
            var expected = 0.2 * 1.5;
            bodyAngleZ.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BodyPositionX_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0.3, Y = 0, Z = 0 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var bodyPositionX = result.FirstOrDefault(p => p.Id == "BodyPositionX");
            bodyPositionX.Should().NotBeNull();
            
            // Expected: HeadPosX * -1 = 0.3 * -1 = -0.3
            var expected = 0.3 * -1;
            bodyPositionX.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BodyPositionY_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0, Y = 0.4, Z = 0 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var bodyPositionY = result.FirstOrDefault(p => p.Id == "BodyPositionY");
            bodyPositionY.Should().NotBeNull();
            
            // Expected: HeadPosY * 1 = 0.4 * 1 = 0.4
            var expected = 0.4 * 1;
            bodyPositionY.Value.Should().BeApproximately(expected, 0.001);
        }
        
        [Fact]
        public async Task BodyPositionZ_TransformsCorrectly()
        {
            if (!File.Exists(_configPath))
                return;
            
            // Arrange
            var engine = new TransformationEngine(_mockLogger.Object);
            await engine.LoadRulesAsync(_configPath);
            
            var trackingData = new PhoneTrackingInfo
            {
                FaceFound = true,
                Position = new Coordinates { X = 0, Y = 0, Z = 0.5 }
            };
            
            // Act
            var result = engine.TransformData(trackingData).ToList();
            
            // Assert
            var bodyPositionZ = result.FirstOrDefault(p => p.Id == "BodyPositionZ");
            bodyPositionZ.Should().NotBeNull();
            
            // Expected: HeadPosZ * -0.5 = 0.5 * -0.5 = -0.25
            var expected = 0.5 * -0.5;
            bodyPositionZ.Value.Should().BeApproximately(expected, 0.001);
        }
    }
} 