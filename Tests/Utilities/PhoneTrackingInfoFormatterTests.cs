using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class PhoneTrackingInfoFormatterTests
    {
        private readonly PhoneTrackingInfoFormatter _formatter;
        private readonly Mock<IConsole> _mockConsole;

        public PhoneTrackingInfoFormatterTests()
        {
            _mockConsole = new Mock<IConsole>();
            _mockConsole.Setup(c => c.WindowWidth).Returns(80); // Default console width for tests
            _formatter = new PhoneTrackingInfoFormatter(_mockConsole.Object);
        }
        
        /// <summary>
        /// Helper method to create ServiceStats for testing
        /// </summary>
        private ServiceStats CreateServiceStats(PhoneTrackingInfo trackingInfo = null, string status = "Connected")
        {
            return new ServiceStats(
                serviceName: "iPhone Tracking Data",
                status: status,
                currentEntity: trackingInfo,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>
                {
                    ["Total Frames"] = 100,
                    ["Failed Frames"] = 5,
                    ["FPS"] = 30
                }
            );
        }

        // Test implementation of IFormattableObject for testing invalid type scenarios
        private class InvalidFormattableObject : IFormattableObject
        {
            public string Data { get; set; }
        }

        [Fact]
        public void Format_WithNullInput_ReturnsNoServiceDataMessage()
        {
            // Act
            var result = _formatter.Format((IServiceStats)null);

            // Assert
            result.Should().Be("No service data available");
        }

        [Fact]
        public void Format_WithInvalidEntityType_ThrowsArgumentException()
        {
            // Arrange
            var invalidEntity = new InvalidFormattableObject();
            var serviceStats = CreateServiceStats();
            serviceStats = new ServiceStats(
                serviceName: "Test Service",
                status: "Connected",
                currentEntity: invalidEntity,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Act & Assert
            Action act = () => _formatter.Format(serviceStats);
            act.Should().Throw<ArgumentException>()
                .WithMessage("CurrentEntity must be of type PhoneTrackingInfo or null");
        }

        [Fact]
        public void CurrentVerbosity_Initially_IsNormal()
        {
            // Assert
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        [Fact]
        public void CycleVerbosity_WhenCalled_ChangesVerbosityLevel()
        {
            // Act & Assert - First cycle (Normal -> Detailed)
            _formatter.CycleVerbosity();
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Detailed);

            // Act & Assert - Second cycle (Detailed -> Basic)
            _formatter.CycleVerbosity();
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Basic);

            // Act & Assert - Third cycle (Basic -> Normal)
            _formatter.CycleVerbosity();
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        [Fact]
        public void Format_WithValidData_ShowsCorrectHeader()
        {
            // Arrange
            var trackingInfo = new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            };
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines[0].Should().Contain("=== iPhone Tracking Data").And.Contain("=== [Alt+O]");
        }

        [Fact]
        public void Format_WithFaceDetected_ShowsFaceDetectedTrue()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain(line => line.Contains("Face Status:") && line.Contains("√ Detected"));
        }

        [Fact]
        public void Format_WithoutFaceDetected_ShowsFaceDetectedFalse()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = false,
                BlendShapes = new List<BlendShape>()
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain(line => line.Contains("Face Status:") && line.Contains("X Not Found"));
        }

        [Fact]
        public void Format_WithBlendShapes_ShowsCorrectCount()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "JawOpen", Value = 0.5f },
                    new BlendShape { Key = "EyeBlinkLeft", Value = 0.3f },
                    new BlendShape { Key = "EyeBlinkRight", Value = 0.3f }
                }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain("BlendShapes:");
            lines.Should().Contain("Total Blend Shapes: 3");
        }

        [Fact]
        public void Format_WithNullBlendShapes_HandlesNullCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = null
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().NotContain("BlendShapes:");
            lines.Should().NotContain("Total Blend Shapes:");
        }

        [Fact]
        public void Format_WithRotationAtNormalVerbosity_ShowsRotation()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Rotation = new Coordinates { X = 10.5f, Y = 20.5f, Z = 30.5f }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain("Head Rotation (X,Y,Z): 10.5°, 20.5°, 30.5°");
        }

        [Fact]
        public void Format_WithRotationAtBasicVerbosity_HidesRotation()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Rotation = new Coordinates { X = 10.5f, Y = 20.5f, Z = 30.5f }
            });

            // Act
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().NotContain(l => l.Contains("Head Rotation"));
        }

        [Fact]
        public void Format_WithNullRotation_HandlesNullCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Rotation = null
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().NotContain(l => l.Contains("Head Rotation"));
        }

        [Fact]
        public void Format_WithExtremeRotationValues_FormatsCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Rotation = new Coordinates { X = -180.0f, Y = 180.0f, Z = 0.0f }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain("Head Rotation (X,Y,Z): -180.0°, 180.0°, 0.0°");
        }

        [Fact]
        public void Format_WithZeroRotationValues_FormatsCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Rotation = new Coordinates { X = 0.0f, Y = 0.0f, Z = 0.0f }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain("Head Rotation (X,Y,Z): 0.0°, 0.0°, 0.0°");
        }

        [Fact]
        public void Format_WithPositionValues_FormatsCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Position = new Coordinates { X = 10.5f, Y = 0.0f, Z = 30.5f }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain("Head Position (X,Y,Z): 10.5, 0.0, 30.5");
        }

        [Fact]
        public void Format_WithPositionAtBasicVerbosity_HidesPosition()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Position = new Coordinates { X = 10.5f, Y = 20.5f, Z = 30.5f }
            });

            // Act
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().NotContain(l => l.Contains("Head Position"));
        }

        [Fact]
        public void Format_WithNullPosition_HandlesNullCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Position = null
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().NotContain(l => l.Contains("Head Position"));
        }

        [Fact]
        public void Format_WithExtremePositionValues_FormatsCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>(),
                Position = new Coordinates { X = -1000.0f, Y = 1000.0f, Z = 0.0f }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain("Head Position (X,Y,Z): -1000.0, 1000.0, 0.0");
        }

        [Fact]
        public void Format_WithBlendShapesAtNormalVerbosity_ShowsTop13()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = CreateBlendShapes(15) // Create 15 blend shapes
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var blendShapeLines = lines.Where(l => l.Contains("█") || l.Contains("░")).ToList();
            blendShapeLines.Should().HaveCount(13);
            lines.Should().Contain("  ... and 2 more");
        }

        [Fact]
        public void Format_WithBlendShapesAtDetailedVerbosity_ShowsAll()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = CreateBlendShapes(15) // Create 15 blend shapes
            });

            // Act
            _formatter.CycleVerbosity(); // Normal -> Detailed
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var blendShapeLines = lines.Where(l => l.Contains("█") || l.Contains("░")).ToList();
            blendShapeLines.Should().HaveCount(15);
            lines.Should().NotContain("and more");
        }

        [Fact]
        public void Format_WithBlendShapesAtBasicVerbosity_HidesBlendShapes()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = CreateBlendShapes(5)
            });

            // Act
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().NotContain("BlendShapes:");
            lines.Should().NotContain("Total Blend Shapes:");
        }

        [Fact]
        public void Format_WithEmptyBlendShapes_HandlesEmptyList()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>()
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().NotContain("BlendShapes:");
            lines.Should().NotContain("Total Blend Shapes:");
        }

        [Fact]
        public void Format_WithBlendShapes_ShowsProgressBars()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PhoneTrackingInfo
            {
                FaceFound = true,
                BlendShapes = new List<BlendShape>
                {
                    new BlendShape { Key = "Full", Value = 1.0f },
                    new BlendShape { Key = "Half", Value = 0.5f },
                    new BlendShape { Key = "Empty", Value = 0.0f }
                }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain(line => line.Contains("Full") && line.Contains("████████████████████") && line.Contains("1.00"));
            lines.Should().Contain(line => line.Contains("Half") && line.Contains("██████████░░░░░░░░░░") && line.Contains("0.50"));
            lines.Should().Contain(line => line.Contains("Empty") && line.Contains("░░░░░░░░░░░░░░░░░░░░") && line.Contains("0.00"));
        }

        private List<BlendShape> CreateBlendShapes(int count)
        {
            var shapes = new List<BlendShape>();
            for (int i = 0; i < count; i++)
            {
                shapes.Add(new BlendShape { Key = $"Shape{i}", Value = 0.5f });
            }
            return shapes;
        }
    }
}