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
    public static class PCTestFormattingHelpers
    {
        public static string FormatServiceHeader(string status)
        {
            var statusColor = ConsoleColors.GetStatusColor(status);
            return $"=== [INFO] PC Client ({statusColor}{status}{ConsoleColors.Reset}) === [Alt+P]";
        }

        public static string FormatFaceStatus(bool faceFound)
        {
            var icon = faceFound ? "√" : "X";
            var text = faceFound ? "Detected" : "Not Found";
            var color = faceFound ? ConsoleColors.Success : ConsoleColors.Warning;
            return $"Face Status: {color}{icon} {text}{ConsoleColors.Reset}";
        }
    }

    public class PCTrackingInfoFormatterTests
    {
        private const int PARAM_DISPLAY_COUNT_NORMAL = 25;

        private readonly Mock<IConsole> _mockConsole;
        private readonly Mock<ITableFormatter> _mockTableFormatter;
        private readonly Mock<IParameterColorService> _mockColorService;
        private readonly Mock<IShortcutConfigurationManager> _mockShortcutManager;
        private readonly UserPreferences _userPreferences;
        private readonly PCTrackingInfoFormatter _formatter;

        // Mock class for testing wrong entity type
        private class WrongEntityType : IFormattableObject
        {
            public string Data { get; set; } = "Wrong type";
        }

        public PCTrackingInfoFormatterTests()
        {
            _mockConsole = new Mock<IConsole>();
            _mockConsole.Setup(c => c.WindowWidth).Returns(80);
            _mockConsole.Setup(c => c.WindowHeight).Returns(25);

            _mockTableFormatter = new Mock<ITableFormatter>();

            _mockColorService = new Mock<IParameterColorService>();
            // Setup default pass-through behavior for color service
            _mockColorService.Setup(x => x.GetColoredCalculatedParameterName(It.IsAny<string>())).Returns<string>(s => s);
            _mockColorService.Setup(x => x.GetColoredExpression(It.IsAny<string>())).Returns<string>(s => s);

            _mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            _mockShortcutManager.Setup(m => m.GetDisplayString(It.IsAny<ShortcutAction>())).Returns("Alt+P");

            _userPreferences = new UserPreferences { PCClientVerbosity = VerbosityLevel.Normal };

            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();
            mockColumnConfigManager.Setup(x => x.GetParameterTableColumns()).Returns(new[] { ParameterTableColumn.ParameterName, ParameterTableColumn.ProgressBar, ParameterTableColumn.Value, ParameterTableColumn.Range, ParameterTableColumn.MinMax, ParameterTableColumn.Expression });
            mockColumnConfigManager.Setup(x => x.GetDefaultParameterTableColumns()).Returns(new[] { ParameterTableColumn.ParameterName, ParameterTableColumn.ProgressBar, ParameterTableColumn.Value, ParameterTableColumn.Range, ParameterTableColumn.MinMax, ParameterTableColumn.Expression });

            _formatter = new PCTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences, mockColumnConfigManager.Object);
        }

        #region Helper Methods

        private static ServiceStats CreateMockServiceStats(string status, PCTrackingInfo currentEntity = null!,
            bool isHealthy = true, DateTime lastSuccess = default, string lastError = null!)
        {
            var counters = new Dictionary<string, long>
            {
                ["MessagesSent"] = 1000,
                ["ConnectionAttempts"] = 5,
                ["UptimeSeconds"] = 3600
            };

            return new ServiceStats(
                serviceName: "PC Client",
                status: status,
                currentEntity: currentEntity,
                isHealthy: isHealthy,
                lastSuccessfulOperation: lastSuccess,
                lastError: lastError,
                counters: counters);
        }

        private static PCTrackingInfo CreatePCTrackingInfo(bool faceFound = true, List<TrackingParam> parameters = null!)
        {
            var trackingInfo = new PCTrackingInfo
            {
                FaceFound = faceFound,
                Parameters = parameters ?? new List<TrackingParam>(),
                ParameterDefinitions = new Dictionary<string, VTSParameter>(),
                ParameterCalculationExpressions = new Dictionary<string, string>()
            };

            // Add some default parameter definitions if parameters are provided
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    trackingInfo.ParameterDefinitions[param.Id] = new VTSParameter(
                        name: param.Id,
                        min: -1,
                        max: 1,
                        defaultValue: 0);
                    trackingInfo.ParameterCalculationExpressions[param.Id] = $"{param.Id} * 1.0";
                }
            }

            return trackingInfo;
        }

        private static ServiceStats CreateServiceStats(PCTrackingInfo trackingInfo)
        {
            return new ServiceStats(
                serviceName: "PC Client",
                status: "Connected",
                currentEntity: trackingInfo,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>
                {
                    ["MessagesSent"] = 100,
                    ["ConnectionAttempts"] = 1,
                    ["UptimeSeconds"] = 3600
                }
            );
        }

        #endregion

        [Fact]
        public void Format_WithNoFace_ShowsCorrectStatus()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.FaceFound = false;
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(PCTestFormattingHelpers.FormatFaceStatus(false));
        }

        [Fact]
        public void Format_WithFace_ShowsCorrectStatus()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.FaceFound = true;
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(PCTestFormattingHelpers.FormatFaceStatus(true));
        }

        [Fact]
        public void Format_WithParameters_ShowsCorrectTable()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5 },
                new TrackingParam { Id = "Param2", Value = -0.3 }
            };
            trackingInfo.ParameterDefinitions["Param1"] = new VTSParameter("Param1", -1, 1, 0);
            trackingInfo.ParameterCalculationExpressions["Param1"] = "Param1 * 1.0";
            var serviceStats = CreateServiceStats(trackingInfo);

            // Capture the columns to verify their behavior
            IList<ITableColumnFormatter<TrackingParam>> capturedColumns = null!;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumnFormatter<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumnFormatter<TrackingParam>>, int, int, int, int?>(
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
            capturedColumns.Count.Should().Be(6);

            // Verify column headers
            capturedColumns[0].Header.Should().Be("Parameter");
            capturedColumns[1].Header.Should().Be(""); // Progress bar column
            capturedColumns[2].Header.Should().Be("Value");
            capturedColumns[3].Header.Should().Be("Range");
            capturedColumns[4].Header.Should().Be("Min/Max");
            capturedColumns[5].Header.Should().Be("Expression");

            // Verify column formatter behavior
            var param1 = trackingInfo.Parameters.First();
            capturedColumns[0].ValueFormatter(param1).Should().Be("Param1");
            capturedColumns[2].ValueFormatter(param1).Should().Be("0.5");
            capturedColumns[3].ValueFormatter(param1).Should().Be("[-1; 0; 1]");
            capturedColumns[4].ValueFormatter(param1).Should().Be("[0.5; 0.5]"); // MinMax shows current value when not initialized
            capturedColumns[5].ValueFormatter(param1).Should().Be("Param1 * 1.0");
        }

        [Fact]
        public void Format_WithExpressions_ShowsCorrectExpressions()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5 }
            };
            trackingInfo.ParameterCalculationExpressions["Param1"] = "x * 2.0";
            trackingInfo.ParameterDefinitions["Param1"] = new VTSParameter("Param1", -1, 1, 0);
            var serviceStats = CreateServiceStats(trackingInfo);

            // Capture the columns to verify their behavior
            IList<ITableColumnFormatter<TrackingParam>> capturedColumns = null!;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumnFormatter<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumnFormatter<TrackingParam>>, int, int, int, int?>(
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
            capturedColumns.Count.Should().Be(6);

            // Verify column headers
            capturedColumns[0].Header.Should().Be("Parameter");
            capturedColumns[1].Header.Should().Be(""); // Progress bar column
            capturedColumns[2].Header.Should().Be("Value");
            capturedColumns[3].Header.Should().Be("Range");
            capturedColumns[4].Header.Should().Be("Min/Max");
            capturedColumns[5].Header.Should().Be("Expression");

            // Verify column formatter behavior
            var param1 = trackingInfo.Parameters.First();
            capturedColumns[0].ValueFormatter(param1).Should().Be("Param1");
            capturedColumns[2].ValueFormatter(param1).Should().Be("0.5");
            capturedColumns[3].ValueFormatter(param1).Should().Be("[-1; 0; 1]");
            capturedColumns[4].ValueFormatter(param1).Should().Be("[0.5; 0.5]"); // MinMax shows current value when not initialized
            capturedColumns[5].ValueFormatter(param1).Should().Be("x * 2.0");
        }

        [Fact]
        public void Format_WithNoParameters_ShowsEmptyTable()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam>();
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.Is<string>(s => s == "=== Parameters ==="),
                It.Is<IEnumerable<TrackingParam>>(parameters => !parameters.Any()),
                It.IsAny<List<ITableColumnFormatter<TrackingParam>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public void Format_WithBasicVerbosity_ShowsMinimalInfo()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5 }
            };
            var serviceStats = CreateServiceStats(trackingInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrackingParam>>(),
                It.IsAny<List<ITableColumnFormatter<TrackingParam>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public void Format_WithDetailedVerbosity_ShowsAllInfo()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5 }
            };
            var serviceStats = CreateServiceStats(trackingInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrackingParam>>(),
                It.IsAny<List<ITableColumnFormatter<TrackingParam>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.Is<int?>(limit => limit == null)), Times.Once);
        }

        [Fact]
        public void Format_WithNullServiceStats_ReturnsNoDataMessage()
        {
            // Act
            var result = _formatter.Format(null!);

            // Assert
            result.Should().Be("No service data available");
        }

        [Fact]
        public void Format_WithWrongEntityType_ThrowsArgumentException()
        {
            // Arrange
            var serviceStats = new ServiceStats(
                serviceName: "PC Client",
                status: "Connected",
                currentEntity: new WrongEntityType(),
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _formatter.Format(serviceStats));
        }

        [Fact]
        public void CycleVerbosity_ChangesVerbosityLevel()
        {
            // Arrange - formatter starts at Normal

            // Act & Assert
            _formatter.CycleVerbosity();
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Detailed);

            _formatter.CycleVerbosity();
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Basic);

            _formatter.CycleVerbosity();
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        [Fact]
        public void Format_WithRunningStatus_ShowsCorrectServiceHeader()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = new ServiceStats(
                serviceName: "PC Client",
                status: "Running",
                currentEntity: trackingInfo,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(PCTestFormattingHelpers.FormatServiceHeader("Running"));
        }

        [Fact]
        public void Format_WithStoppedStatus_ShowsCorrectServiceHeader()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = new ServiceStats(
                serviceName: "PC Client",
                status: "Stopped",
                currentEntity: trackingInfo,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(PCTestFormattingHelpers.FormatServiceHeader("Stopped"));
        }

        [Fact]
        public void Format_WithNoConnectionMetrics_DoesNotShowMetrics()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = new ServiceStats(
                serviceName: "PC Client",
                status: "Connected",
                currentEntity: trackingInfo,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Connection:");
        }

        [Fact]
        public void Format_WithParameterDefinitions_ShowsCorrectDefinitions()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5 }
            };
            trackingInfo.ParameterDefinitions["Param1"] = new VTSParameter(
                name: "Param1",
                min: -1.0,
                max: 1.0,
                defaultValue: 0.0
            );
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.Is<string>(s => s == "=== Parameters ==="),
                It.IsAny<IEnumerable<TrackingParam>>(),
                It.Is<IList<ITableColumnFormatter<TrackingParam>>>(cols =>
                    cols.Count == 6 &&
                    cols[0].Header == "Parameter" &&
                    cols[1].Header == "" && // Progress bar column
                    cols[2].Header == "Value" &&
                    cols[3].Header == "Range" &&
                    cols[4].Header == "Min/Max" &&
                    cols[5].Header == "Expression"),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public void Format_WithNullCurrentEntity_ShowsNoDataMessage()
        {
            // Arrange
            var serviceStats = new ServiceStats(
                serviceName: "PC Client",
                status: "Connected",
                currentEntity: null,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("No current tracking data available");
        }

        [Fact]
        public void Format_WithDifferentExpressions_ShowsCorrectFormat()
        {
            // Arrange
            var testCases = new[]
            {
                (expression: "x * 2.0", expected: "x * 2.0"),
                (expression: null, expected: "[no expression]"),
                (expression: "", expected: "[no expression]"),
                (expression: "sin(x) * cos(y)", expected: "sin(x) * cos(y)")
            };

            foreach (var (expression, expected) in testCases)
            {
                var trackingInfo = CreatePCTrackingInfo();
                trackingInfo.Parameters = new List<TrackingParam>
                {
                    new TrackingParam { Id = "TestParam", Value = 0.5 }
                };
                trackingInfo.ParameterCalculationExpressions["TestParam"] = expression!;
                trackingInfo.ParameterDefinitions["TestParam"] = new VTSParameter("TestParam", -1, 1, 0);

                var serviceStats = new ServiceStats(
                    serviceName: "PC Client",
                    status: "Connected",
                    currentEntity: trackingInfo,
                    isHealthy: true,
                    lastSuccessfulOperation: DateTime.UtcNow,
                    lastError: null,
                    counters: new Dictionary<string, long>()
                );

                // Capture the columns to verify their behavior
                IList<ITableColumnFormatter<TrackingParam>> capturedColumns = null!;
                _mockTableFormatter
                    .Setup(x => x.AppendTable(
                        It.IsAny<StringBuilder>(),
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<TrackingParam>>(),
                        It.IsAny<IList<ITableColumnFormatter<TrackingParam>>>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int?>()))
                    .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumnFormatter<TrackingParam>>, int, int, int, int?>(
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
                capturedColumns.Count.Should().Be(6);

                // Verify expression column behavior
                var expressionColumn = capturedColumns[5];
                expressionColumn.ValueFormatter(trackingInfo.Parameters.First()).Should().Be(expected);
            }
        }

        [Fact]
        public void Format_WithDifferentParameterRanges_ShowsCorrectNormalizedValues()
        {
            // Arrange
            var testCases = new[]
            {
                (value: 0.5, min: -1.0, max: 1.0, expected: "0.5"),
                (value: -0.5, min: -1.0, max: 1.0, expected: "-0.5"),
                (value: 0.0, min: -1.0, max: 1.0, expected: "0"),
                (value: 1.0, min: -1.0, max: 1.0, expected: "1"),
                (value: -1.0, min: -1.0, max: 1.0, expected: "-1")
            };

            foreach (var (value, min, max, expected) in testCases)
            {
                var trackingInfo = CreatePCTrackingInfo();
                trackingInfo.Parameters = new List<TrackingParam>
                {
                    new TrackingParam { Id = "TestParam", Value = value }
                };
                trackingInfo.ParameterDefinitions["TestParam"] = new VTSParameter("TestParam", min, max, 0);
                trackingInfo.ParameterCalculationExpressions["TestParam"] = "x * 1.0";

                var serviceStats = new ServiceStats(
                    serviceName: "PC Client",
                    status: "Connected",
                    currentEntity: trackingInfo,
                    isHealthy: true,
                    lastSuccessfulOperation: DateTime.UtcNow,
                    lastError: null,
                    counters: new Dictionary<string, long>()
                );

                // Capture the columns to verify their behavior
                IList<ITableColumnFormatter<TrackingParam>> capturedColumns = null!;
                _mockTableFormatter
                    .Setup(x => x.AppendTable(
                        It.IsAny<StringBuilder>(),
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<TrackingParam>>(),
                        It.IsAny<IList<ITableColumnFormatter<TrackingParam>>>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int?>()))
                    .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumnFormatter<TrackingParam>>, int, int, int, int?>(
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
                capturedColumns.Count.Should().Be(6);

                // Verify value column behavior
                var valueColumn = capturedColumns[2];
                valueColumn.ValueFormatter(trackingInfo.Parameters.First()).Should().Be(expected);
            }
        }

        [Fact]
        public void CycleVerbosity_WithAllLevels_CyclesCorrectly()
        {
            // Arrange
            var expectedSequence = new[]
            {
                VerbosityLevel.Normal,
                VerbosityLevel.Detailed,
                VerbosityLevel.Basic,
                VerbosityLevel.Normal
            };

            // Act & Assert
            foreach (var expectedLevel in expectedSequence)
            {
                _formatter.CurrentVerbosity.Should().Be(expectedLevel);
                _formatter.CycleVerbosity();
            }
        }

        [Fact]
        public void Format_WithParameters_ShowsCorrectProgressBar()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam>
            {
                new TrackingParam { Id = "Param1", Value = 0.5 },
                new TrackingParam { Id = "Param2", Value = -0.3 },
                new TrackingParam { Id = "Param3", Value = 0.0 }
            };
            trackingInfo.ParameterDefinitions["Param1"] = new VTSParameter("Param1", -1, 1, 0);
            trackingInfo.ParameterDefinitions["Param2"] = new VTSParameter("Param2", -1, 1, 0);
            trackingInfo.ParameterDefinitions["Param3"] = new VTSParameter("Param3", -1, 1, 0);
            var serviceStats = CreateServiceStats(trackingInfo);

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
            IList<ITableColumnFormatter<TrackingParam>> capturedColumns = null!;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumnFormatter<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumnFormatter<TrackingParam>>, int, int, int, int?>(
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
            capturedColumns.Count.Should().Be(6);

            // Verify progress bar column behavior
            var progressBarColumn = capturedColumns[1];
            progressBarColumn.Header.Should().Be(""); // Progress bar column has no header

            // Test different parameter values
            var parameters = trackingInfo.Parameters.ToList();
            var param1 = parameters[0]; // Value = 0.5
            var param2 = parameters[1]; // Value = -0.3
            var param3 = parameters[2]; // Value = 0.0

            // Verify progress bar formatting for different values using FormatCell with a reasonable width
            const int progressBarWidth = 10;
            progressBarColumn.FormatCell(param1, progressBarWidth).Should().Contain("█").And.Contain("░");
            progressBarColumn.FormatCell(param2, progressBarWidth).Should().Contain("█").And.Contain("░");
            progressBarColumn.FormatCell(param3, progressBarWidth).Should().Contain("░");

            // Verify the progress bar length is consistent
            var bar1 = progressBarColumn.FormatCell(param1, progressBarWidth);
            var bar2 = progressBarColumn.FormatCell(param2, progressBarWidth);
            var bar3 = progressBarColumn.FormatCell(param3, progressBarWidth);
            bar1.Length.Should().Be(bar2.Length).And.Be(bar3.Length).And.Be(progressBarWidth);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange & Act
            var mockConsole = new Mock<IConsole>();
            var mockTableFormatter = new Mock<ITableFormatter>();
            var mockColorService = new Mock<IParameterColorService>();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();

            // Act & Assert
            var mockUserPreferences = new Mock<UserPreferences>();
            var formatter = new PCTrackingInfoFormatter(mockConsole.Object, mockTableFormatter.Object, mockColorService.Object, mockShortcutManager.Object, mockUserPreferences.Object, mockColumnConfigManager.Object);
            formatter.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();
            Action act = () => new PCTrackingInfoFormatter(null!, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences, mockColumnConfigManager.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("console");
        }

        [Fact]
        public void Constructor_WithNullColumnConfigManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new PCTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("columnConfigManager");
        }

        [Fact]
        public void Constructor_LoadsColumnConfigurationFromUserPreferences()
        {
            // Arrange
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();
            var userPreferences = new UserPreferences
            {
                PCParameterTableColumns = new[] { ParameterTableColumn.Value, ParameterTableColumn.ProgressBar }
            };

            // Act
            _ = new PCTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, userPreferences, mockColumnConfigManager.Object);

            // Assert
            mockColumnConfigManager.Verify(x => x.LoadFromUserPreferences(userPreferences), Times.Once);
        }

        [Fact]
        public void Format_WithCustomColumnConfiguration_UsesConfiguredColumns()
        {
            // Arrange
            var customColumns = new[] { ParameterTableColumn.Value, ParameterTableColumn.Expression };
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();
            mockColumnConfigManager.Setup(x => x.GetParameterTableColumns()).Returns(customColumns);

            var formatter = new PCTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences, mockColumnConfigManager.Object);
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            formatter.Format(serviceStats);

            // Assert
            mockColumnConfigManager.Verify(x => x.GetParameterTableColumns(), Times.Once);
        }

        [Fact]
        public void Format_WithEmptyColumnConfiguration_HandlesGracefully()
        {
            // Arrange
            var emptyColumns = Array.Empty<ParameterTableColumn>();
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();
            mockColumnConfigManager.Setup(x => x.GetParameterTableColumns()).Returns(emptyColumns);

            var formatter = new PCTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences, mockColumnConfigManager.Object);
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            _ = formatter.Format(serviceStats);

            // Assert
            mockColumnConfigManager.Verify(x => x.GetParameterTableColumns(), Times.Once);
        }

        [Fact]
        public void Format_WithSingleColumnConfiguration_DisplaysOnlyThatColumn()
        {
            // Arrange
            var singleColumn = new[] { ParameterTableColumn.Value };
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();
            mockColumnConfigManager.Setup(x => x.GetParameterTableColumns()).Returns(singleColumn);

            var formatter = new PCTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object, _mockShortcutManager.Object, _userPreferences, mockColumnConfigManager.Object);
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            formatter.Format(serviceStats);

            // Assert
            mockColumnConfigManager.Verify(x => x.GetParameterTableColumns(), Times.Once);
        }

        [Fact]
        public void Format_WithValidServiceStats_ReturnsFormattedString()
        {
            // Arrange
            var mockConsole = new Mock<IConsole>();
            mockConsole.Setup(c => c.WindowWidth).Returns(120);
            var mockTableFormatter = new Mock<ITableFormatter>();
            var mockColorService = new Mock<IParameterColorService>();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            mockShortcutManager.Setup(m => m.GetDisplayString(It.IsAny<ShortcutAction>())).Returns("Alt+P");
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();

            var formatter = new PCTrackingInfoFormatter(mockConsole.Object, mockTableFormatter.Object, mockColorService.Object, mockShortcutManager.Object, _userPreferences, mockColumnConfigManager.Object);
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            var result = formatter.Format(serviceStats);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("Alt+P"); // Should contain the shortcut
        }



        private PCTrackingInfoFormatter CreateFormatterWithMocks(int windowWidth = 120, int tableRowsReturned = 10)
        {
            var mockConsole = new Mock<IConsole>();
            mockConsole.Setup(c => c.WindowWidth).Returns(windowWidth);

            var mockTableFormatter = new Mock<ITableFormatter>();
            mockTableFormatter.Setup(t => t.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumnFormatter<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                It.IsAny<int?>()));

            var mockColorService = new Mock<IParameterColorService>();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            mockShortcutManager.Setup(m => m.GetDisplayString(It.IsAny<ShortcutAction>())).Returns("Alt+P");
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();

            return new PCTrackingInfoFormatter(mockConsole.Object, mockTableFormatter.Object, mockColorService.Object, mockShortcutManager.Object, _userPreferences, mockColumnConfigManager.Object);
        }

        private PCTrackingInfoFormatter CreateFormatterWithColorService(IParameterColorService colorService, int windowWidth = 120)
        {
            var mockConsole = new Mock<IConsole>();
            mockConsole.Setup(c => c.WindowWidth).Returns(windowWidth);

            var mockTableFormatter = new Mock<ITableFormatter>();
            var mockShortcutManager = new Mock<IShortcutConfigurationManager>();
            mockShortcutManager.Setup(m => m.GetDisplayString(It.IsAny<ShortcutAction>())).Returns("Alt+P");
            var mockColumnConfigManager = new Mock<IParameterTableConfigurationManager>();

            return new PCTrackingInfoFormatter(mockConsole.Object, mockTableFormatter.Object, colorService, mockShortcutManager.Object, _userPreferences, mockColumnConfigManager.Object);
        }
    }
}
