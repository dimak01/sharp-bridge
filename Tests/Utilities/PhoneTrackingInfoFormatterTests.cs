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
            return $"=== [INFO] Phone Client ({statusColor}{status}{ConsoleColors.Reset}) === [Alt+O]";
        }



        public static string FormatFaceStatus(bool faceFound)
        {
            var icon = faceFound ? "√" : "X";
            var text = faceFound ? "Detected" : "Not Found";
            var color = faceFound ? ConsoleColors.Success : ConsoleColors.Warning;
            return $"Face Status: {color}{icon} {text}{ConsoleColors.Reset}";
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
        private readonly Mock<IParameterColorService> _mockColorService;
        private readonly Mock<IShortcutConfigurationManager> _mockShortcutManager;
        private readonly UserPreferences _userPreferences;
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

            _mockColorService = new Mock<IParameterColorService>();
            // Setup default pass-through behavior for color service
            _mockColorService.Setup(x => x.GetColoredBlendShapeName(It.IsAny<string>())).Returns<string>(s => s);
            _mockColorService.Setup(x => x.GetColoredExpression(It.IsAny<string>())).Returns<string>(s => s);

            _mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            _mockShortcutManager.Setup(m => m.GetDisplayString(It.IsAny<ShortcutAction>())).Returns("Alt+O");

            _userPreferences = new UserPreferences { PhoneClientVerbosity = VerbosityLevel.Normal };

            _formatter = new PhoneTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange & Act
            var mockConsole = new Mock<IConsole>();
            var mockTableFormatter = new Mock<ITableFormatter>();
            var mockColorService = new Mock<IParameterColorService>();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();

            // Act & Assert
            var mockUserPreferences = new Mock<UserPreferences>();
            var formatter = new PhoneTrackingInfoFormatter(mockConsole.Object, mockTableFormatter.Object, mockColorService.Object, mockShortcutManager.Object, mockUserPreferences.Object);
            formatter.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new PhoneTrackingInfoFormatter(null!, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences);
            act.Should().Throw<ArgumentNullException>().WithParameterName("console");
        }

        [Fact]
        public void Constructor_WithNullTableFormatter_ThrowsArgumentNullException()
        {
            // Arrange
            var mockConsole = new Mock<IConsole>();

            // Act & Assert
            Action act = () => new PhoneTrackingInfoFormatter(mockConsole.Object, null!, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences);
            act.Should().Throw<ArgumentNullException>().WithParameterName("tableFormatter");
        }

        [Fact]
        public void Constructor_WithNullColorService_ThrowsArgumentNullException()
        {
            // Arrange
            var mockConsole = new Mock<IConsole>();

            // Act & Assert
            Action act = () => new PhoneTrackingInfoFormatter(mockConsole.Object, _mockTableFormatter.Object, null!, _mockShortcutManager.Object, _userPreferences);
            act.Should().Throw<ArgumentNullException>().WithParameterName("colorService");
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
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            mockShortcutManager.Setup(m => m.GetDisplayString(It.IsAny<ShortcutAction>())).Returns("Alt+O");
            var formatter = new PhoneTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object, mockShortcutManager.Object, _userPreferences);

            // Use reflection to set an invalid verbosity level
            var property = typeof(PhoneTrackingInfoFormatter).GetProperty("CurrentVerbosity");
            property!.SetValue(formatter, (VerbosityLevel)999); // Set to an invalid enum value

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
            var phoneInfo = CreatePhoneTrackingInfo();
            var serviceStats = CreateMockServiceStats("Running", phoneInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain($"=== [DEBUG] Phone Client ({ConsoleColors.Colorize("Running", ConsoleColors.GetStatusColor("Running"))}) === [Alt+O]");
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
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: null!);
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
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: false, blendShapes: null!);
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
                It.Is<IList<ITableColumnFormatter<BlendShape>>>(cols =>
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
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: null!);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("No blend shapes");
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<BlendShape>>(),
                It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
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
                "=== BlendShapes ===",
                It.Is<IEnumerable<BlendShape>>(shapes => shapes.Count() == 3),
                It.Is<IList<ITableColumnFormatter<BlendShape>>>(cols =>
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
                "=== BlendShapes ===",
                It.Is<IEnumerable<BlendShape>>(shapes => shapes.Count() == 3),
                It.Is<IList<ITableColumnFormatter<BlendShape>>>(cols =>
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
                "=== BlendShapes ===",
                It.IsAny<IEnumerable<BlendShape>>(),
                It.Is<IList<ITableColumnFormatter<BlendShape>>>(cols =>
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
                "=== BlendShapes ===",
                It.Is<IEnumerable<BlendShape>>(shapes => shapes.Count() == 3),
                It.Is<IList<ITableColumnFormatter<BlendShape>>>(cols => cols.Count == 3),
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
                "=== BlendShapes ===",
                It.IsAny<IEnumerable<BlendShape>>(),
                It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
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
                It.Is<IList<ITableColumnFormatter<BlendShape>>>(cols =>
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
                It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
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
            var phoneTrackingInfo = CreatePhoneTrackingInfo(faceFound: true, blendShapes: null!);
            var serviceStats = CreateMockServiceStats("Running", phoneTrackingInfo);
            // _formatter starts at Normal verbosity

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(tf => tf.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<BlendShape>>(),
                It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
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
                It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
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
            var result = _formatter.Format(null!);

            // Assert
            result.Should().Be("No service data available");
        }

        [Fact]
        public void Format_WithNullCurrentEntity_ShowsNoTrackingDataMessage()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("Running", currentEntity: null!);

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
                serviceName: "Phone Client",
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
                null!, // This should be filtered out
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
                It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()),
                Times.Once);
        }

        #endregion



        #region Helper Methods

        private static ServiceStats CreateMockServiceStats(string status, PhoneTrackingInfo currentEntity = null!,
            bool isHealthy = true, DateTime lastSuccess = default, string lastError = null!)
        {
            var counters = new Dictionary<string, long>
            {
                ["Total Frames"] = 1000,
                ["Failed Frames"] = 10,
                ["FPS"] = 30
            };

            return new ServiceStats(
                serviceName: "Phone Client",
                status: status,
                currentEntity: currentEntity,
                isHealthy: isHealthy,
                lastSuccessfulOperation: lastSuccess,
                lastError: lastError,
                counters: counters);
        }

        private static PhoneTrackingInfo CreatePhoneTrackingInfo(bool faceFound = true, List<BlendShape> blendShapes = null!)
        {
            return new PhoneTrackingInfo
            {
                FaceFound = faceFound,
                BlendShapes = blendShapes ?? new List<BlendShape>(),
                Rotation = faceFound ? new Coordinates { X = 0, Y = 0, Z = 0 } : null!,
                Position = faceFound ? new Coordinates { X = 0, Y = 0, Z = 0 } : null!
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
            IList<ITableColumnFormatter<BlendShape>> capturedColumns = null!;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<BlendShape>>(),
                    It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<BlendShape>, IList<ITableColumnFormatter<BlendShape>>, int, int, int, int?>(
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
            IList<ITableColumnFormatter<BlendShape>> capturedColumns = null!;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<BlendShape>>(),
                    It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<BlendShape>, IList<ITableColumnFormatter<BlendShape>>, int, int, int, int?>(
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
            IList<ITableColumnFormatter<BlendShape>> capturedColumns = null!;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<BlendShape>>(),
                    It.IsAny<IList<ITableColumnFormatter<BlendShape>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<BlendShape>, IList<ITableColumnFormatter<BlendShape>>, int, int, int, int?>(
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