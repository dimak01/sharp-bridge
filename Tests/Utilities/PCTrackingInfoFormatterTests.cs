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
            lines[0].Should().Be("=== VTube Studio Parameters === [Alt+P]");
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
            result.Should().Be("No PC tracking data");
        }

        [Fact]
        public void Format_WithInvalidType_ThrowsArgumentException()
        {
            // Arrange
            var invalidEntity = new InvalidFormattableObject();

            // Act & Assert
            Action act = () => _formatter.Format(invalidEntity);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Entity must be of type PCTrackingInfo (Parameter 'formattableEntity')");
        }

        [Fact]
        public void Format_WithValidPCTrackingInfo_ReturnsFormattedString()
        {
            // Arrange
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            };

            // Act
            var result = _formatter.Format(trackingInfo);

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
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            };

            // Act
            var result = _formatter.Format(trackingInfo);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 0);
        }

        [Fact]
        public void Format_WithoutFaceDetected_ShowsFaceDetectedFalse()
        {
            // Arrange
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = false,
                Parameters = new List<TrackingParam>()
            };

            // Act
            var result = _formatter.Format(trackingInfo);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, false, 0);
        }

        [Fact]
        public void Format_WithParameters_ShowsCorrectParameterCount()
        {
            // Arrange
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam { Id = "Param1" },
                    new TrackingParam { Id = "Param2" },
                    new TrackingParam { Id = "Param3" }
                }
            };

            // Act
            var result = _formatter.Format(trackingInfo);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 3);
        }

        [Fact]
        public void Format_WithNullParameters_ShowsZeroParameterCount()
        {
            // Arrange
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = null
            };

            // Act
            var result = _formatter.Format(trackingInfo);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 0);
        }

        [Fact]
        public void Format_WithEmptyParameters_ShowsZeroParameterCount()
        {
            // Arrange
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            };

            // Act
            var result = _formatter.Format(trackingInfo);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Normal, true, 0);
        }

        [Fact]
        public void Format_WithDifferentVerbosity_ShowsCorrectVerbosityLevel()
        {
            // Arrange
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            };

            // Act - Set to Detailed verbosity
            _formatter.CycleVerbosity();
            var result = _formatter.Format(trackingInfo);

            // Assert
            VerifyHeaderFormat(result, VerbosityLevel.Detailed, true, 0);
        }

        [Fact]
        public void Format_WithNormalVerbosity_ShowsTop10Parameters()
        {
            // Arrange
            const int parameterCount = 15;
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = CreateParameters(parameterCount)
            };

            // Act
            var result = _formatter.Format(trackingInfo);

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
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = CreateParameters(parameterCount)
            };

            // Act - Set to Detailed verbosity
            _formatter.CycleVerbosity();
            var result = _formatter.Format(trackingInfo);

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
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = CreateParameters(parameterCount)
            };

            // Act - Set to Basic verbosity
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic
            var result = _formatter.Format(trackingInfo);

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
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>()
            };

            // Act
            var result = _formatter.Format(trackingInfo);

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
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = null
            };

            // Act
            var result = _formatter.Format(trackingInfo);

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
        public void Format_WithParametersHavingNullIds_HandlesNullIdsCorrectly()
        {
            // Arrange
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam { Id = $"{TrackingParamName}0", Value = 0.5 },
                    new TrackingParam { Id = null, Value = 0.5 },
                    new TrackingParam { Id = $"{TrackingParamName}2", Value = 0.5 }
                }
            };

            // Act
            var result = _formatter.Format(trackingInfo);

            // Assert
            VerifyHeaderFormat(formattedOutput: result,
                             expectedVerbosity: VerbosityLevel.Normal,
                             faceDetected: true,
                             parameterCount: 3);
            
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines.Should().Contain("Top Parameters:");
            var parameterLines = lines.Where(l => l.StartsWith("  ") && l.Contains("█")).ToList();
            parameterLines.Should().HaveCount(3);
            
            // Verify the first parameter has the correct ID
            parameterLines[0].Should().Contain($"{TrackingParamName}0");
            // Verify the second parameter has no ID (just spaces)
            parameterLines[1].Should().MatchRegex(@"^\s+:\s+");
            // Verify the third parameter has the correct ID
            parameterLines[2].Should().Contain($"{TrackingParamName}2");
        }

        [Fact]
        public void Format_WithNegativeValues_FormatsCorrectly()
        {
            // Arrange
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam 
                    { 
                        Id = $"{TrackingParamName}1",
                        Value = -0.5,
                        Min = -1.0,
                        Max = 1.0,
                        DefaultValue = 0.0,
                        Weight = -0.75
                    }
                }
            };

            // Act
            var result = _formatter.Format(trackingInfo);

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
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam 
                    { 
                        Id = $"{TrackingParamName}1",
                        Value = 0.5,
                        Min = 0.0,
                        Max = 1.0,
                        DefaultValue = 0.0,
                        Weight = null
                    }
                }
            };

            // Act
            var result = _formatter.Format(trackingInfo);

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
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = true,
                Parameters = new List<TrackingParam>
                {
                    new TrackingParam 
                    { 
                        Id = "MinValue",
                        Value = -100.0,
                        Min = -100.0,
                        Max = 100.0
                    },
                    new TrackingParam 
                    { 
                        Id = "MaxValue",
                        Value = 100.0,
                        Min = -100.0,
                        Max = 100.0
                    },
                    new TrackingParam 
                    { 
                        Id = "MiddleValue",
                        Value = 0.0,
                        Min = -100.0,
                        Max = 100.0
                    }
                }
            };

            // Act
            var result = _formatter.Format(trackingInfo);

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