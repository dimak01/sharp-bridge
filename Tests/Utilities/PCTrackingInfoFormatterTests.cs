using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class PCTrackingInfoFormatterTests
    {
        private const string TrackingParamName = "PCTrackingParam";
        private readonly PCTrackingInfoFormatter _formatter;

        public PCTrackingInfoFormatterTests()
        {
            _formatter = new PCTrackingInfoFormatter();
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
            lines.Should().HaveCountGreaterThan(4); // At least header lines
            lines[0].Should().Be("=== VTube Studio PC Client (Connected) === [Alt+P]");
            lines[1].Should().Be($"Verbosity: {expectedVerbosity}");
            lines[2].Should().Be($"Face Detected: {faceDetected}");
            lines[3].Should().Be($"Parameter Count: {parameterCount}");
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
        public void Format_WithNullParameters_ShowsZeroParameterCount()
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
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 0);
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
        public void Format_WithParametersHavingNullIds_ThrowsArgumentNullException()
        {
            // Arrange
            var serviceStats = CreateServiceStats(new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam { Id = $"{TrackingParamName}0", Value = 0.5 },
                    new TrackingParam { Id = null, Value = 0.5 },
                    new TrackingParam { Id = $"{TrackingParamName}2", Value = 0.5 }
                }
            });

            // Act & Assert
            var act = () => _formatter.Format(serviceStats);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'key')");
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
            paramLine.Should().Contain("-0.50");
            
            // Check progress bar position (should be at 25% since -0.5 is 25% between -1 and 1)
            var progressBarSection = paramLine.Substring(paramLine.IndexOf(":") + 2, 20);
            var filledCount = progressBarSection.Count(c => c == '█');
            filledCount.Should().Be(5); // 5 out of 20 chars filled (25%)
            
            // Check negative weight formatting
            paramLine.Should().Contain("weight: -0.75");
        }

        [Fact]
        public void Format_WithNullWeight_OmitsWeightPart()
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
            
            // Should not contain weight part
            paramLine.Should().NotContain("weight:");
            
            // Should have correct format without weight
            // Note: The actual format includes spaces for alignment and the full progress bar
            paramLine.Should().Match($"  {TrackingParamName}1 : ██████████░░░░░░░░░░  0.50 (min:  0.00, max:  1.00, default:  0.00)");
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
            
            // Min value should have empty progress bar
            var minLine = lines.First(l => l.Contains("MinValue"));
            var minProgressBar = minLine.Substring(minLine.IndexOf(":") + 2, 20);
            minProgressBar.Should().Be(new string('░', 20));
            
            // Max value should have full progress bar
            var maxLine = lines.First(l => l.Contains("MaxValue"));
            var maxProgressBar = maxLine.Substring(maxLine.IndexOf(":") + 2, 20);
            maxProgressBar.Should().Be(new string('█', 20));
            
            // Middle value should have half-filled progress bar
            var middleLine = lines.First(l => l.Contains("MiddleValue"));
            var middleProgressBar = middleLine.Substring(middleLine.IndexOf(":") + 2, 20);
            var filledCount = middleProgressBar.Count(c => c == '█');
            filledCount.Should().Be(10); // 10 out of 20 chars filled (50%)
        }
    }
} 