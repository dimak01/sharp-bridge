using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Moq;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    /// <summary>
    /// Helper methods for formatting test strings with proper color codes
    /// </summary>
    public static class TestFormattingHelpers
    {
        public static string FormatServiceHeader(string status)
        {
            var statusColor = ConsoleColors.GetStatusColor(status);
            return $"=== iPhone Tracking Data ({statusColor}{status}{ConsoleColors.Reset}) === [Alt+O]";
        }

        public static string FormatHealthStatus(bool isHealthy, string timeAgo, string error = null, int timeWidth = 6)
        {
            var healthIcon = isHealthy ? "√" : "X";
            var healthText = isHealthy ? "Healthy" : "Unhealthy";
            var healthColor = isHealthy ? ConsoleColors.Healthy : ConsoleColors.Error;
            var healthContent = $"{healthIcon} {healthText}";
            var colorizedHealth = $"{healthColor}{healthContent}{ConsoleColors.Reset}";

            // Pad the time string to the specified width, right-aligned
            var paddedTimeAgo = timeAgo.PadLeft(timeWidth);

            var result = $"Health: {colorizedHealth}{Environment.NewLine}Last Success: {paddedTimeAgo}";
            
            if (!isHealthy && error != null)
            {
                var colorizedError = $"{ConsoleColors.Error}{error}{ConsoleColors.Reset}";
                result += $"{Environment.NewLine}Error: {colorizedError}";
            }
            
            return result;
        }

        public static string FormatFaceStatus(bool faceFound)
        {
            var icon = faceFound ? "√" : "X";
            var text = faceFound ? "Detected" : "Not Found";
            var color = faceFound ? ConsoleColors.Success : ConsoleColors.Warning;
            return $"Face Status: {color}{icon} {text}{ConsoleColors.Reset}";
        }

        public static string FormatMetrics(long totalFrames, long failedFrames, long fps)
        {
            var failedStr = failedFrames.ToString().PadLeft(4);
            var fpsStr = fps.ToString().PadLeft(3);
            var framesContent = $"{totalFrames:N0} frames";
            return $"Metrics: {framesContent.PadLeft(6)} | {failedStr} failed | {fpsStr} FPS";
        }

        public static string FormatHeadRotation(float x, float y, float z) =>
            $"Head Rotation (X,Y,Z): {x:F1}°, {y:F1}°, {z:F1}°";

        public static string FormatHeadPosition(float x, float y, float z) =>
            $"Head Position (X,Y,Z): {x:F1}, {y:F1}, {z:F1}";

        public static string FormatBlendShapes(object blendShapes)
        {
            if (blendShapes == null)
            {
                return "No blend shapes";
            }

            var result = new StringBuilder();
            
            if (blendShapes is List<BlendShape> blendShapeList)
            {
                foreach (var blendShape in blendShapeList)
                {
                    result.Append($"{blendShape.Key}: {blendShape.Value:F1} ");
                }
            }
            else if (blendShapes is Dictionary<string, float> blendShapeDict)
            {
                foreach (var blendShape in blendShapeDict)
                {
                    result.Append($"{blendShape.Key}: {blendShape.Value:F1} ");
                }
            }
            
            return result.ToString().Trim();
        }
    }

    public class PhoneTrackingInfoFormatterTests
    {
        private const int TARGET_COLUMN_COUNT = 4;
        private const int TARGET_ROWS_NORMAL = 13;
        
        private readonly Mock<IConsole> _mockConsole;
        private readonly Mock<ITableFormatter> _mockTableFormatter;
        private readonly PhoneTrackingInfoFormatter _formatter;

        // Mock class for testing wrong entity type
        private class WrongEntityType : IFormattableObject
        {
            public string Data { get; set; } = "Wrong type";
        }

        public PhoneTrackingInfoFormatterTests()
        {
            _mockConsole = new Mock<IConsole>();
            _mockConsole.Setup(c => c.WindowWidth).Returns(80);
            _mockConsole.Setup(c => c.WindowHeight).Returns(25);
            
            _mockTableFormatter = new Mock<ITableFormatter>();
            _formatter = new PhoneTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange & Act
            var formatter = new PhoneTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object);

            // Assert
            formatter.Should().NotBeNull();
            formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new PhoneTrackingInfoFormatter(null, _mockTableFormatter.Object));
        }

        [Fact]
        public void Constructor_WithNullTableFormatter_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new PhoneTrackingInfoFormatter(_mockConsole.Object, null));
        }

        #endregion

        #region Verbosity Tests

        [Fact]
        public void CycleVerbosity_FromBasic_ChangesToNormal()
        {
            // Arrange
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic

            // Act
            _formatter.CycleVerbosity();

            // Assert
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        [Fact]
        public void CycleVerbosity_FromNormal_ChangesToDetailed()
        {
            // Arrange - formatter starts at Normal

            // Act
            _formatter.CycleVerbosity();

            // Assert
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Detailed);
        }

        [Fact]
        public void CycleVerbosity_FromDetailed_ChangesToBasic()
        {
            // Arrange
            _formatter.CycleVerbosity(); // Normal -> Detailed

            // Act
            _formatter.CycleVerbosity();

            // Assert
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Basic);
        }

        [Fact]
        public void CycleVerbosity_WithInvalidVerbosityLevel_ResetsToNormal()
        {
            // Arrange
            var formatter = new PhoneTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object);
            
            // Use reflection to set an invalid verbosity level
            var property = typeof(PhoneTrackingInfoFormatter).GetProperty("CurrentVerbosity");
            property.SetValue(formatter, (VerbosityLevel)999); // Set to an invalid enum value

            // Act
            formatter.CycleVerbosity();

            // Assert
            formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        #endregion

        #region Service Header Tests

        [Fact]
        public void Format_WithRunningStatus_ShowsCorrectServiceHeader()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatServiceHeader("Running"));
        }

        [Fact]
        public void Format_WithRunningStatus_ShowsColoredServiceHeader()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatServiceHeader("Running"));
            result.Should().Contain(ConsoleColors.Info); // Cyan for running status
        }

        [Fact]
        public void Format_WithStoppedStatus_ShowsCorrectServiceHeader()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Stopped");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatServiceHeader("Stopped"));
        }

        [Fact]
        public void Format_WithStoppedStatus_ShowsColoredServiceHeader()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Stopped");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatServiceHeader("Stopped"));
            result.Should().Contain(ConsoleColors.Info); // Cyan for stopped status
        }

        [Fact]
        public void Format_ShowsCurrentVerbosityLevel()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running");
            _formatter.CycleVerbosity(); // Normal -> Detailed

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Verbosity: Detailed");
        }

        #endregion

        #region Health Status Tests

        [Fact]
        public void Format_WithHealthyService_ShowsHealthyStatus()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", isHealthy: true, lastSuccess: DateTime.UtcNow.AddMinutes(-1));

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatHealthStatus(true, "1m"));
        }

        [Fact]
        public void Format_WithUnhealthyService_ShowsUnhealthyStatus()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", isHealthy: false, lastSuccess: DateTime.UtcNow.AddMinutes(-1));

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatHealthStatus(false, "1m"));
        }

        [Fact]
        public void Format_WithRecentSuccess_ShowsFormattedTime()
        {
            // Arrange
            var lastSuccess = DateTime.UtcNow.AddSeconds(-30);
            var serviceStats = CreateMockServiceStats("Running", lastSuccess: lastSuccess);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatHealthStatus(true, "30s"));
        }

        [Fact]
        public void Format_WithNoLastSuccess_ShowsNever()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", lastSuccess: DateTime.MinValue);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatHealthStatus(true, "Never", null, 6));
        }

        [Fact]
        public void Format_WithUnhealthyServiceAndError_ShowsErrorMessage()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", isHealthy: false, lastSuccess: DateTime.UtcNow.AddMinutes(-1), lastError: "Connection timeout");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatHealthStatus(false, "1m", "Connection timeout"));
        }

        [Fact]
        public void Format_WithUnhealthyServiceAndError_ShowsColoredErrorMessage()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", isHealthy: false, lastError: "Connection timeout");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain($"Error: {ConsoleColors.Error}Connection timeout{ConsoleColors.Reset}");
            result.Should().Contain(ConsoleColors.Error); // Red color for error message
        }

        [Fact]
        public void Format_WithHealthyService_ShowsColoredHealthStatus()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", isHealthy: true, lastSuccess: DateTime.UtcNow.AddMinutes(-1));

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatHealthStatus(true, "1m"));
            result.Should().Contain(ConsoleColors.Healthy); // Green color for healthy status
        }

        [Fact]
        public void Format_WithUnhealthyService_ShowsColoredHealthStatus()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", isHealthy: false, lastSuccess: DateTime.UtcNow.AddMinutes(-1));

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatHealthStatus(false, "1m"));
            result.Should().Contain(ConsoleColors.Error); // Red color for unhealthy status
        }

        [Fact]
        public void Format_WithUnhealthyServiceAndLongError_TruncatesErrorMessage()
        {
            // Arrange
            var longError = new string('X', 60) + "_END";
            var serviceStats = CreateMockServiceStats("Running", isHealthy: false, lastSuccess: DateTime.UtcNow.AddMinutes(-1), lastError: longError);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Error: ");
            result.Should().Contain("XXX"); // Should contain part of the error
            result.Should().Contain("..."); // Should be truncated
            result.Should().NotContain("_END"); // The ending should be truncated
        }

        #endregion

        #region Metrics Formatting Tests

        [Fact]
        public void Format_WithMetrics_ShowsFormattedMetrics()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running");
            serviceStats.Counters["Total Frames"] = 12345;
            serviceStats.Counters["Failed Frames"] = 42;
            serviceStats.Counters["FPS"] = 60;

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Metrics:");
            result.Should().Contain("12,345 frames");
            result.Should().Contain("42 failed");
            result.Should().Contain("60 FPS");
        }

        [Fact]
        public void Format_WithoutFPSCounter_ShowsZeroFPS()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running");
            serviceStats.Counters["Total Frames"] = 1000;
            serviceStats.Counters["Failed Frames"] = 5;
            serviceStats.Counters.Remove("FPS"); // Remove FPS counter

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().MatchRegex("Metrics:.*\\|\\s*0\\s*FPS");
        }

        [Fact]
        public void Format_WithoutFrameCounters_DoesNotShowMetrics()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running");
            serviceStats.Counters.Clear(); // No frame counters

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Metrics:");
        }

        #endregion

        #region Face Detection Tests

        [Fact]
        public void Format_WithFaceDetected_ShowsDetectedStatus()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatFaceStatus(true));
        }

        [Fact]
        public void Format_WithNoFaceDetected_ShowsNotFoundStatus()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: false);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatFaceStatus(false));
        }

        [Fact]
        public void Format_WithFaceDetectedAndNormalVerbosity_ShowsHeadRotation()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true);
            phoneTrackingInfo.Rotation = new Coordinates { X = 10.5f, Y = -5.2f, Z = 2.1f };
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Head Rotation (X,Y,Z): 10.5°, -5.2°, 2.1°");
        }

        [Fact]
        public void Format_WithFaceDetectedAndNormalVerbosity_ShowsHeadPosition()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true);
            phoneTrackingInfo.Position = new Coordinates { X = 1.2f, Y = 3.4f, Z = -0.8f };
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Head Position (X,Y,Z): 1.2, 3.4, -0.8");
        }

        [Fact]
        public void Format_WithNoFaceDetected_DoesNotShowHeadData()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: false);
            phoneTrackingInfo.Rotation = new Coordinates { X = 10.5f, Y = -5.2f, Z = 2.1f };
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Head Rotation");
            result.Should().NotContain("Head Position");
        }

        [Fact]
        public void Format_WithBasicVerbosity_DoesNotShowHeadData()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true);
            phoneTrackingInfo.Rotation = new Coordinates { X = 10.5f, Y = -5.2f, Z = 2.1f };
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Head Rotation");
            result.Should().NotContain("Head Position");
        }

        [Fact]
        public void Format_WithFaceDetected_ShowsColoredFaceStatus()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatFaceStatus(true));
            result.Should().Contain(ConsoleColors.Success); // Bright green for face detected
        }

        [Fact]
        public void Format_WithNoFaceDetected_ShowsColoredFaceStatus()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: false);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatFaceStatus(false));
            result.Should().Contain(ConsoleColors.Warning); // Yellow for face not found
        }

        [Fact]
        public void Format_WithFaceFound_ShowsFaceFoundStatus()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: null);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatFaceStatus(true));
        }

        [Fact]
        public void Format_WithNoFaceFound_ShowsNoFaceFoundStatus()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: false, blendShapes: null);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(TestFormattingHelpers.FormatFaceStatus(false));
        }

        [Fact]
        public void Format_WithBlendShapes_ShowsBlendShapes()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "jawOpen", Value = 0.5f },
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.3f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.Is<IEnumerable<BlendShape>>(shapes => shapes.Count() == 2),
                It.Is<IList<ITableColumn<BlendShape>>>(cols => 
                    cols.Count == 3 &&
                    cols[0].Header == "Expression" &&
                    cols[1].Header == "" && // Progress bar column
                    cols[2].Header == "Value"),
                TARGET_COLUMN_COUNT,
                _mockConsole.Object.WindowWidth,
                20,
                TARGET_ROWS_NORMAL),
                Times.Once);
        }

        [Fact]
        public void Format_WithNoBlendShapes_ShowsNoBlendShapes()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: null);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("No blend shapes");
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<BlendShape>>(),
                It.IsAny<IList<ITableColumn<BlendShape>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()),
                Times.Never);
        }

        [Fact]
        public void Format_WithDetailedVerbosity_ShowsAllBlendShapes()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "jawOpen", Value = 0.5f },
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.3f },
                new BlendShape { Key = "mouthSmile", Value = 0.8f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                "BlendShapes:",
                It.Is<IEnumerable<BlendShape>>(shapes => shapes.Count() == 3),
                It.Is<IList<ITableColumn<BlendShape>>>(cols => 
                    cols.Count == 3 &&
                    cols[0].Header == "Expression" &&
                    cols[1].Header == "" && // Progress bar column
                    cols[2].Header == "Value"),
                TARGET_COLUMN_COUNT,
                _mockConsole.Object.WindowWidth,
                20,
                null), // No row limit in detailed mode
                Times.Once);
        }

        [Fact]
        public void Format_WithNormalVerbosity_ShowsOnlySignificantBlendShapes()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "jawOpen", Value = 0.5f },
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.3f },
                new BlendShape { Key = "mouthSmile", Value = 0.8f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                "BlendShapes:",
                It.Is<IEnumerable<BlendShape>>(shapes => shapes.Count() == 3),
                It.Is<IList<ITableColumn<BlendShape>>>(cols => 
                    cols.Count == 3 &&
                    cols[0].Header == "Expression" &&
                    cols[1].Header == "" && // Progress bar column
                    cols[2].Header == "Value"),
                TARGET_COLUMN_COUNT,
                _mockConsole.Object.WindowWidth,
                20,
                TARGET_ROWS_NORMAL),
                Times.Once);
        }

        [Fact]
        public void Format_WithBlendShapes_CreatesAllExpectedTableColumns()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "jawOpen", Value = 0.5f },
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.3f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                "BlendShapes:",
                It.IsAny<IEnumerable<BlendShape>>(),
                It.Is<IList<ITableColumn<BlendShape>>>(cols =>
                    cols.Count == 3 &&
                    cols[0].Header == "Expression" &&
                    cols[1].Header == "" &&
                    cols[2].Header == "Value"),
                TARGET_COLUMN_COUNT,
                _mockConsole.Object.WindowWidth,
                20,
                TARGET_ROWS_NORMAL),
                Times.Once);
        }

        #endregion

        #region BlendShape Data Preparation Tests

        [Fact]
        public void Format_WithNormalVerbosityAndBlendShapes_CallsTableFormatter()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.75f },
                new BlendShape { Key = "jawOpen", Value = 0.25f },
                new BlendShape { Key = "mouthSmile", Value = 0.5f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                "BlendShapes:",
                It.Is<IEnumerable<BlendShape>>(shapes => shapes.Count() == 3),
                It.Is<IList<ITableColumn<BlendShape>>>(cols => cols.Count == 3),
                4, // TARGET_COLUMN_COUNT
                80, // console width
                20, // bar width
                13), // TARGET_ROWS_NORMAL for normal verbosity
                Times.Once);
        }

        [Fact]
        public void Format_WithDetailedVerbosityAndBlendShapes_CallsTableFormatterWithNoLimit()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.75f },
                new BlendShape { Key = "jawOpen", Value = 0.25f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                "BlendShapes:",
                It.IsAny<IEnumerable<BlendShape>>(),
                It.IsAny<IList<ITableColumn<BlendShape>>>(),
                4, // TARGET_COLUMN_COUNT
                80, // console width
                20, // bar width
                null), // No limit for detailed verbosity
                Times.Once);
        }

        [Fact]
        public void Format_WithBlendShapes_PassesSortedDataToTableFormatter()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "zzzLast", Value = 0.1f },
                new BlendShape { Key = "aaaFirst", Value = 0.9f },
                new BlendShape { Key = "mmmMiddle", Value = 0.5f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.Is<IEnumerable<BlendShape>>(shapes => 
                    shapes.First().Key == "aaaFirst" && 
                    shapes.Last().Key == "zzzLast"),
                It.IsAny<IList<ITableColumn<BlendShape>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()),
                Times.Once);
        }

        [Fact]
        public void Format_WithBlendShapes_CreatesCorrectColumnDefinitions()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.75f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<BlendShape>>(),
                It.Is<IList<ITableColumn<BlendShape>>>(cols => 
                    cols.Count == 3 &&
                    cols[0].Header == "Expression" &&
                    cols[1].Header == "" && // Progress bar column
                    cols[2].Header == "Value"),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()),
                Times.Once);
        }

        [Fact]
        public void Format_WithBasicVerbosity_DoesNotCallTableFormatter()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.75f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<BlendShape>>(),
                It.IsAny<IList<ITableColumn<BlendShape>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()),
                Times.Never);
        }

        [Fact]
        public void Format_WithBlendShapes_ShowsTotalCount()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.75f },
                new BlendShape { Key = "jawOpen", Value = 0.25f },
                new BlendShape { Key = "mouthSmile", Value = 0.5f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Total Blend Shapes: 3");
        }

        [Fact]
        public void Format_WithNullBlendShapes_DoesNotCallTableFormatter()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: null);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<BlendShape>>(),
                It.IsAny<IList<ITableColumn<BlendShape>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()),
                Times.Never);
        }

        [Fact]
        public void Format_WithEmptyBlendShapes_DoesNotCallTableFormatter()
        {
            // Arrange
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: new List<BlendShape>());
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<BlendShape>>(),
                It.IsAny<IList<ITableColumn<BlendShape>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()),
                Times.Never);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void Format_WithNullServiceStats_ReturnsErrorMessage()
        {
            // Act
            var result = _formatter.Format(null);

            // Assert
            result.Should().Be("No service data available");
        }

        [Fact]
        public void Format_WithNullCurrentEntity_ShowsNoTrackingDataMessage()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", currentEntity: null);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("No current tracking data available");
        }

        [Fact]
        public void Format_WithWrongEntityType_ThrowsArgumentException()
        {
            // Arrange - Create ServiceStats with wrong entity type
            var wrongEntity = new WrongEntityType(); // Not PhoneTrackingInfo
            var counters = new Dictionary<string, long>
            {
                ["Total Frames"] = 1000,
                ["Failed Frames"] = 10,
                ["FPS"] = 30
            };

            var serviceStats = new ServiceStats(
                serviceName: "iPhone Tracking Data",
                status: "Running",
                currentEntity: wrongEntity, // This will cause the error
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow.AddMinutes(-1),
                lastError: null,
                counters: counters);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _formatter.Format(serviceStats));
        }

        [Fact]
        public void Format_FiltersOutNullBlendShapes()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.75f },
                null, // This should be filtered out
                new BlendShape { Key = "jawOpen", Value = 0.25f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.Is<IEnumerable<BlendShape>>(shapes => shapes.Count() == 2), // Null filtered out
                It.IsAny<IList<ITableColumn<BlendShape>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()),
                Times.Once);
        }

        #endregion

        #region FormatTimeAgo Tests

        [Theory]
        [InlineData(0, "0s")]
        [InlineData(1, "1s")]
        [InlineData(59, "59s")]
        [InlineData(60, "1m")]
        [InlineData(61, "1m")]
        [InlineData(119, "2m")]  // 1.98 minutes rounds up to 2m
        [InlineData(120, "2m")]
        [InlineData(3599, "60m")]  // 59.98 minutes rounds up to 60m
        [InlineData(3600, "1h")]
        [InlineData(3601, "1h")]
        [InlineData(7199, "2h")]  // 1.999 hours rounds up to 2h
        [InlineData(7200, "2h")]
        [InlineData(86399, "24h")]  // 23.999 hours rounds up to 24h
        [InlineData(86400, "1d")]
        [InlineData(86401, "1d")]
        [InlineData(172799, "2d")]  // 1.999 days rounds up to 2d
        [InlineData(172800, "2d")]
        public void Format_WithDifferentLastSuccessTimes_ShowsCorrectTimeAgo(int seconds, string expected)
        {
            // Arrange
            var currentTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _formatter.CurrentTime = currentTime;
            var lastSuccess = currentTime.AddSeconds(-seconds);
            var serviceStats = CreateMockServiceStats("Running", isHealthy: true, lastSuccess: lastSuccess);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            // The time is padded to 6 characters and may include color codes
            result.Should().MatchRegex($"Last Success:\\s*{expected}");
        }

        [Fact]
        public void Format_WithFutureLastSuccessTime_ShowsZeroSeconds()
        {
            // Arrange
            var currentTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _formatter.CurrentTime = currentTime;
            var lastSuccess = currentTime.AddSeconds(1); // Future time
            var serviceStats = CreateMockServiceStats("Running", isHealthy: true, lastSuccess: lastSuccess);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().MatchRegex("Last Success:\\s*0s");
        }

        #endregion

        #region Helper Methods

        private ServiceStats CreateMockServiceStats(string status, PhoneTrackingInfo currentEntity = null, 
            bool isHealthy = true, DateTime lastSuccess = default, string lastError = null)
        {
            var counters = new Dictionary<string, long>
            {
                ["Total Frames"] = 1000,
                ["Failed Frames"] = 10,
                ["FPS"] = 30
            };

            return new ServiceStats(
                serviceName: "iPhone Tracking Data",
                status: status,
                currentEntity: currentEntity,
                isHealthy: isHealthy,
                lastSuccessfulOperation: lastSuccess,
                lastError: lastError,
                counters: counters);
        }

        private PhoneTrackingInfo CreatePhoneTrackingInfo(bool faceFound = true, List<BlendShape> blendShapes = null)
        {
            return new PhoneTrackingInfo
            {
                FaceFound = faceFound,
                BlendShapes = blendShapes ?? new List<BlendShape>(),
                Rotation = faceFound ? new Coordinates { X = 0, Y = 0, Z = 0 } : null,
                Position = faceFound ? new Coordinates { X = 0, Y = 0, Z = 0 } : null
            };
        }

        #endregion

        [Fact]
        public void Format_WithBlendShapes_ShowsCorrectColumnFormatters()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "jawOpen", Value = 0.5f },
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.3f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Capture the columns to verify their behavior
            IList<ITableColumn<BlendShape>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<BlendShape>>(),
                    It.IsAny<IList<ITableColumn<BlendShape>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<BlendShape>, IList<ITableColumn<BlendShape>>, int, int, int, int?>(
                    (builder, title, rows, columns, targetCols, width, barWidth, maxItems) =>
                    {
                        capturedColumns = columns;
                        // Simulate table formatter behavior by executing column formatters
                        foreach (var row in rows)
                        {
                            foreach (var col in columns)
                            {
                                col.ValueFormatter(row);
                            }
                        }
                    });

            // Act
            _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            capturedColumns.Count.Should().Be(3);
            
            // Verify column headers
            capturedColumns[0].Header.Should().Be("Expression");
            capturedColumns[1].Header.Should().Be(""); // Progress bar column
            capturedColumns[2].Header.Should().Be("Value");

            // Verify column formatter behavior
            var shape = blendShapes.First();
            capturedColumns[0].ValueFormatter(shape).Should().Be("jawOpen");
            capturedColumns[2].ValueFormatter(shape).Should().Be("0.50");
        }

        [Fact]
        public void Format_WithBlendShapes_ShowsCorrectProgressBar()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "jawOpen", Value = 0.5f },
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.3f },
                new BlendShape { Key = "mouthSmile", Value = 0.0f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Setup mock table formatter to return progress bars
            _mockTableFormatter
                .Setup(x => x.CreateProgressBar(It.IsAny<double>(), It.IsAny<int>()))
                .Returns<double, int>((value, width) =>
                {
                    var clampedValue = Math.Max(0, Math.Min(1, value));
                    var barLength = (int)(clampedValue * width);
                    return new string('█', barLength) + new string('░', width - barLength);
                });

            // Capture the columns to verify their behavior
            IList<ITableColumn<BlendShape>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<BlendShape>>(),
                    It.IsAny<IList<ITableColumn<BlendShape>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<BlendShape>, IList<ITableColumn<BlendShape>>, int, int, int, int?>(
                    (builder, title, rows, columns, targetCols, width, barWidth, maxItems) =>
                    {
                        capturedColumns = columns;
                    });

            // Act
            _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            capturedColumns.Count.Should().Be(3);
            
            // Verify progress bar column behavior
            var progressBarColumn = capturedColumns[1];
            progressBarColumn.Header.Should().Be(""); // Progress bar column has no header

            // Test different blend shape values
            var shapes = blendShapes.ToList();
            var shape1 = shapes[0]; // Value = 0.5
            var shape2 = shapes[1]; // Value = 0.3
            var shape3 = shapes[2]; // Value = 0.0

            // Verify progress bar formatting for different values using FormatCell with a reasonable width
            const int progressBarWidth = 10;
            progressBarColumn.FormatCell(shape1, progressBarWidth).Should().Contain("█").And.Contain("░");
            progressBarColumn.FormatCell(shape2, progressBarWidth).Should().Contain("█").And.Contain("░");
            progressBarColumn.FormatCell(shape3, progressBarWidth).Should().Contain("░");

            // Verify the progress bar length is consistent
            var bar1 = progressBarColumn.FormatCell(shape1, progressBarWidth);
            var bar2 = progressBarColumn.FormatCell(shape2, progressBarWidth);
            var bar3 = progressBarColumn.FormatCell(shape3, progressBarWidth);
            bar1.Length.Should().Be(bar2.Length).And.Be(bar3.Length).And.Be(progressBarWidth);
        }

        [Fact]
        public void Format_WithBlendShapes_ShowsCorrectNumericValues()
        {
            // Arrange
            var blendShapes = new List<BlendShape>
            {
                new BlendShape { Key = "jawOpen", Value = 0.5f },
                new BlendShape { Key = "eyeBlinkLeft", Value = 0.3f },
                new BlendShape { Key = "mouthSmile", Value = 0.0f }
            };
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: blendShapes);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Capture the columns to verify their behavior
            IList<ITableColumn<BlendShape>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<BlendShape>>(),
                    It.IsAny<IList<ITableColumn<BlendShape>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<BlendShape>, IList<ITableColumn<BlendShape>>, int, int, int, int?>(
                    (builder, title, rows, columns, targetCols, width, barWidth, maxItems) =>
                    {
                        capturedColumns = columns;
                    });

            // Act
            _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            capturedColumns.Count.Should().Be(3);
            
            // Verify numeric column behavior
            var numericColumn = capturedColumns[2];
            numericColumn.Header.Should().Be("Value");

            // Test different blend shape values
            var shapes = blendShapes.ToList();
            numericColumn.ValueFormatter(shapes[0]).Should().Be("0.50"); // 0.5f
            numericColumn.ValueFormatter(shapes[1]).Should().Be("0.30"); // 0.3f
            numericColumn.ValueFormatter(shapes[2]).Should().Be("0.00"); // 0.0f
        }
    }
}