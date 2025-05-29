using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;
using Moq;

namespace SharpBridge.Tests.Utilities
{
    public class PCTrackingInfoFormatterTests
    {
        private const string TrackingParamName = "PCTrackingParam";
        private readonly PCTrackingInfoFormatter _formatter;
        private readonly Mock<IConsole> _mockConsole;

        public PCTrackingInfoFormatterTests()
        {
            _mockConsole = new Mock<IConsole>();
            _mockConsole.Setup(c => c.WindowWidth).Returns(120);
            _mockConsole.Setup(c => c.WindowHeight).Returns(30);
            _formatter = new PCTrackingInfoFormatter(_mockConsole.Object);
        }

        /// <summary>
        /// Helper method to create ServiceStats for testing
        /// </summary>
        private ServiceStats CreateServiceStats(PCTrackingInfo trackingInfo = null, string status = "Connected")
        {
            return new ServiceStats(
                serviceName: "VTube Studio PC Client",
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

        /// <summary>
        /// Verifies the header format of the formatted output
        /// </summary>
        private void VerifyHeaderFormat(string formattedOutput, VerbosityLevel expectedVerbosity, bool faceDetected, int parameterCount)
        {
            var lines = formattedOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines[0].Should().Contain("=== PC Client").And.Contain("=== [Alt+P]");
            lines[1].Should().Be($"Verbosity: {expectedVerbosity}");
            lines.Should().Contain(line => line.Contains("Face Status:") && line.Contains(faceDetected ? "√ Detected" : "X Not Found"));
            lines.Should().Contain($"Parameter Count: {parameterCount}");
        }

        /// <summary>
        /// Creates a list of tracking parameters with sequential IDs
        /// </summary>
        private List<TrackingParam> CreateParameters(int count)
        {
            var parameters = new List<TrackingParam>();
            for (int i = 0; i < count; i++)
            {
                parameters.Add(new TrackingParam { Id = $"{TrackingParamName}{i}", Value = 0.5 });
            }
            return parameters;
        }

        /// <summary>
        /// Verifies that parameters are displayed correctly in the output
        /// </summary>
        private void VerifyParameterDisplay(string formattedOutput, int expectedCount, bool shouldShowAll, bool shouldShowMoreLine)
        {
            var lines = formattedOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            
            if (expectedCount > 0)
            {
                lines.Should().Contain("Top Parameters:");
                var parameterLines = lines.Where(l => l.Contains(TrackingParamName)).ToList();
                var linesToCheck = shouldShowAll ? parameterLines : parameterLines.Take(10);
                
                linesToCheck.Should().HaveCount(shouldShowAll ? expectedCount : 10);
                foreach (var (line, i) in linesToCheck.Select((line, i) => (line, i)))
                {
                    line.Should().Contain($"{TrackingParamName}{i}");
                }
            }
            else
            {
                lines.Should().NotContain("Top Parameters:");
                lines.Should().NotContain(l => l.Contains(TrackingParamName));
            }

            if (shouldShowMoreLine)
            {
                lines.Should().Contain($"  ... and {expectedCount - 10} more");
            }
            else
            {
                lines.Should().NotContain(l => l.Contains("and") && l.Contains("more"));
            }
        }

        [Fact]
        public void Format_WithNullInput_ReturnsNoTrackingDataMessage()
        {
            // Act
            var result = _formatter.Format(null);

            // Assert
            result.Should().Be("No service data available");
        }

        [Fact]
        public void Format_WithInvalidType_ThrowsArgumentException()
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
                .WithMessage("CurrentEntity must be of type PCTrackingInfo or null");
        }

        [Fact]
        public void Format_WithValidPCTrackingInfo_ReturnsFormattedString()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotBeNullOrEmpty();
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 0);
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
        public void Format_WithFaceDetected_ShowsFaceDetectedTrue()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 0);
        }

        [Fact]
        public void Format_WithoutFaceDetected_ShowsFaceDetectedFalse()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = false,
                Parameters = new List<TrackingParam>()
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, false, 0);
        }

        [Fact]
        public void Format_WithParameters_ShowsCorrectParameterCount()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam { Id = "Param1" },
                    new TrackingParam { Id = "Param2" },
                    new TrackingParam { Id = "Param3" }
                }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 3);
        }

        [Fact]
        public void Format_WithNullParameters_ShowsNoParameters()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = null
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(formattedOutput: result,
                             expectedVerbosity: VerbosityLevel.Normal,
                             faceDetected: true,
                             parameterCount: 0);
            VerifyParameterDisplay(formattedOutput: result,
                                 expectedCount: 0,
                                 shouldShowAll: false,
                                 shouldShowMoreLine: false);
        }

        [Fact]
        public void Format_WithEmptyParameters_ShowsZeroParameterCount()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 0);
        }

        [Fact]
        public void Format_WithDifferentVerbosity_ShowsCorrectVerbosityLevel()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            });

            // Act - Set to Detailed verbosity
            _formatter.CycleVerbosity();
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Detailed, true, 0);
        }

        [Fact]
        public void Format_WithNormalVerbosity_ShowsTop10Parameters()
        {
            // Arrange
            const int parameterCount = 15;
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = CreateParameters(parameterCount)
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(formattedOutput: result,
                             expectedVerbosity: VerbosityLevel.Normal,
                             faceDetected: true,
                             parameterCount: parameterCount);
            VerifyParameterDisplay(formattedOutput: result,
                                 expectedCount: parameterCount,
                                 shouldShowAll: false,
                                 shouldShowMoreLine: true);
        }

        [Fact]
        public void Format_WithDetailedVerbosity_ShowsAllParameters()
        {
            // Arrange
            const int parameterCount = 15;
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = CreateParameters(parameterCount)
            });

            // Act - Set to Detailed verbosity
            _formatter.CycleVerbosity();
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(formattedOutput: result,
                             expectedVerbosity: VerbosityLevel.Detailed,
                             faceDetected: true,
                             parameterCount: parameterCount);
            VerifyParameterDisplay(formattedOutput: result,
                                 expectedCount: parameterCount,
                                 shouldShowAll: true,
                                 shouldShowMoreLine: false);
        }

        [Fact]
        public void Format_WithBasicVerbosity_ShowsNoParameters()
        {
            // Arrange
            const int parameterCount = 5;
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = CreateParameters(parameterCount)
            });

            // Act - Set to Basic verbosity
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(formattedOutput: result,
                             expectedVerbosity: VerbosityLevel.Basic,
                             faceDetected: true,
                             parameterCount: parameterCount);
            VerifyParameterDisplay(formattedOutput: result,
                                 expectedCount: 0,
                                 shouldShowAll: false,
                                 shouldShowMoreLine: false);
        }

        [Fact]
        public void Format_WithEmptyParameters_ShowsNoParameters()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            VerifyHeaderFormat(formattedOutput: result,
                             expectedVerbosity: VerbosityLevel.Normal,
                             faceDetected: true,
                             parameterCount: 0);
            VerifyParameterDisplay(formattedOutput: result,
                                 expectedCount: 0,
                                 shouldShowAll: false,
                                 shouldShowMoreLine: false);
        }

        [Fact]
        public void Format_WithNegativeValues_FormatsCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam 
                    { 
                        Id = $"{TrackingParamName}1",
                        Value = -0.5,
                        Weight = -0.75
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    [$"{TrackingParamName}1"] = new VTSParameter($"{TrackingParamName}1", -1.0, 1.0, 0.0)
                }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var paramLine = lines.First(l => l.Contains($"{TrackingParamName}1"));
            
            // Check negative value formatting (should not have extra space)
            paramLine.Should().Contain("-0.5");
            
            // Check negative weight formatting in compact range
            paramLine.Should().Contain("-0.75 x [-1; 0; 1]");
        }

        [Fact]
        public void Format_WithNullWeight_UsesDefaultWeight()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam 
                    { 
                        Id = $"{TrackingParamName}1",
                        Value = 0.5,
                        Weight = null
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    [$"{TrackingParamName}1"] = new VTSParameter($"{TrackingParamName}1", 0.0, 1.0, 0.0)
                }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var paramLine = lines.First(l => l.Contains($"{TrackingParamName}1"));
            
            // Should use default weight of 1
            paramLine.Should().Contain("1 x [0; 0; 1]");
            
            // Should contain the value
            paramLine.Should().Contain("0.5");
        }

        [Fact]
        public void Format_WithExtremeValues_HandlesProgressBarCorrectly()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam 
                    { 
                        Id = "MinValue",
                        Value = -100.0
                    },
                    new TrackingParam 
                    { 
                        Id = "MaxValue",
                        Value = 100.0
                    },
                    new TrackingParam 
                    { 
                        Id = "MiddleValue",
                        Value = 0.0
                    }
                },
                ParameterDefinitions = new Dictionary<string, VTSParameter>
                {
                    ["MinValue"] = new VTSParameter("MinValue", -100.0, 100.0, 0.0),
                    ["MaxValue"] = new VTSParameter("MaxValue", -100.0, 100.0, 0.0),
                    ["MiddleValue"] = new VTSParameter("MiddleValue", -100.0, 100.0, 0.0)
                }
            });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            
            // Check that all parameters are present in the output
            lines.Should().Contain(line => line.Contains("MinValue"));
            lines.Should().Contain(line => line.Contains("MaxValue"));
            lines.Should().Contain(line => line.Contains("MiddleValue"));
            
            // Check values are formatted correctly (no unnecessary decimals)
            lines.Should().Contain(line => line.Contains("MinValue") && line.Contains("-100"));
            lines.Should().Contain(line => line.Contains("MaxValue") && line.Contains("100"));
            lines.Should().Contain(line => line.Contains("MiddleValue") && line.Contains("0"));
            
            // Check compact range format (no unnecessary decimals)
            lines.Should().Contain(line => line.Contains("1 x [-100; 0; 100]"));
        }
    }
} 