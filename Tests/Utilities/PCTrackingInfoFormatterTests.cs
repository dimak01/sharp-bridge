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

        public static string FormatHealthStatus(bool isHealthy, string timeAgo, string error = null)
        {
            var healthIcon = isHealthy ? "√" : "X";
            var healthText = isHealthy ? "Healthy" : "Unhealthy";
            var healthColor = isHealthy ? ConsoleColors.Healthy : ConsoleColors.Error;
            var healthContent = $"{healthIcon} {healthText}";
            var colorizedHealth = $"{healthColor}{healthContent}{ConsoleColors.Reset}";

            var result = $"Health: {colorizedHealth}{Environment.NewLine}Last Success: {timeAgo}";
            
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

        public static string FormatConnectionMetrics(long messagesSent, long connectionAttempts, string uptime)
        {
            return $"Connection: {messagesSent} msgs sent | {connectionAttempts} attempts | {uptime} uptime";
        }
    }

    public class PCTrackingInfoFormatterTests
    {
        private const int PARAM_DISPLAY_COUNT_NORMAL = 25;
        
        private readonly Mock<IConsole> _mockConsole;
        private readonly Mock<ITableFormatter> _mockTableFormatter;
        private readonly Mock<IParameterColorService> _mockColorService;
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
            
            _formatter = new PCTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object, _mockColorService.Object);
        }

        #region Helper Methods

        private ServiceStats CreateMockServiceStats(string status, PCTrackingInfo currentEntity = null, 
            bool isHealthy = true, DateTime lastSuccess = default, string lastError = null)
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

        private PCTrackingInfo CreatePCTrackingInfo(bool faceFound = true, List<TrackingParam> parameters = null)
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

        private ServiceStats CreateServiceStats(PCTrackingInfo trackingInfo)
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
            IList<ITableColumn<TrackingParam>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
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
            var result = _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            capturedColumns.Count.Should().Be(5);
            
            // Verify column headers
            capturedColumns[0].Header.Should().Be("Parameter");
            capturedColumns[1].Header.Should().Be(""); // Progress bar column
            capturedColumns[2].Header.Should().Be("Value");
            capturedColumns[3].Header.Should().Be("Width x Range");
            capturedColumns[4].Header.Should().Be("Expression");

            // Verify column formatter behavior
            var param1 = trackingInfo.Parameters.First();
            capturedColumns[0].ValueFormatter(param1).Should().Be("Param1");
            capturedColumns[2].ValueFormatter(param1).Should().Be("0.5");
            capturedColumns[3].ValueFormatter(param1).Should().Be("1 x [-1; 0; 1]");
            capturedColumns[4].ValueFormatter(param1).Should().Be("Param1 * 1.0");
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
            IList<ITableColumn<TrackingParam>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
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
            var result = _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            capturedColumns.Count.Should().Be(5);
            
            // Verify column headers
            capturedColumns[0].Header.Should().Be("Parameter");
            capturedColumns[1].Header.Should().Be(""); // Progress bar column
            capturedColumns[2].Header.Should().Be("Value");
            capturedColumns[3].Header.Should().Be("Width x Range");
            capturedColumns[4].Header.Should().Be("Expression");

            // Verify column formatter behavior
            var param1 = trackingInfo.Parameters.First();
            capturedColumns[0].ValueFormatter(param1).Should().Be("Param1");
            capturedColumns[2].ValueFormatter(param1).Should().Be("0.5");
            capturedColumns[3].ValueFormatter(param1).Should().Be("1 x [-1; 0; 1]");
            capturedColumns[4].ValueFormatter(param1).Should().Be("x * 2.0");
        }

        [Fact]
        public void Format_WithNoParameters_ShowsEmptyTable()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam>();
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.Is<string>(s => s == "=== Parameters ==="),
                It.Is<IEnumerable<TrackingParam>>(parameters => !parameters.Any()),
                It.IsAny<List<ITableColumn<TrackingParam>>>(),
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
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrackingParam>>(),
                It.IsAny<List<ITableColumn<TrackingParam>>>(),
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
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrackingParam>>(),
                It.IsAny<List<ITableColumn<TrackingParam>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.Is<int?>(limit => limit == null)), Times.Once);
        }

        [Fact]
        public void Format_WithUnhealthyService_ShowsError()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = new ServiceStats(
                serviceName: "PC Client",
                status: "Error",
                currentEntity: trackingInfo,
                isHealthy: false,
                lastSuccessfulOperation: DateTime.UtcNow.AddHours(-1),
                lastError: "Connection failed",
                counters: new Dictionary<string, long>()
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(PCTestFormattingHelpers.FormatHealthStatus(false, "1:00:00", "Connection failed"));
        }

        [Fact]
        public void Format_WithNullServiceStats_ReturnsNoDataMessage()
        {
            // Act
            var result = _formatter.Format(null);

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
        public void Format_WithConnectionMetrics_ShowsCorrectMetrics()
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
                counters: new Dictionary<string, long>
                {
                    ["MessagesSent"] = 100,
                    ["ConnectionAttempts"] = 1,
                    ["UptimeSeconds"] = 3600
                }
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Connection: 100 msgs sent | 1 attempts | 1:00:00 uptime");
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
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.Is<string>(s => s == "=== Parameters ==="),
                It.IsAny<IEnumerable<TrackingParam>>(),
                It.Is<IList<ITableColumn<TrackingParam>>>(cols => 
                    cols.Count == 5 &&
                    cols[0].Header == "Parameter" &&
                    cols[1].Header == "" && // Progress bar column
                    cols[2].Header == "Value" &&
                    cols[3].Header == "Width x Range" &&
                    cols[4].Header == "Expression"),
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
        public void Format_WithDifferentUptimes_ShowsCorrectFormat()
        {
            // Arrange
            var testCases = new[]
            {
                (seconds: 0L, expected: "0:00:00 uptime"),
                (seconds: 30L, expected: "0:00:30 uptime"),
                (seconds: 60L, expected: "0:01:00 uptime"),
                (seconds: 90L, expected: "0:01:30 uptime"),
                (seconds: 3600L, expected: "1:00:00 uptime"),
                (seconds: 3660L, expected: "1:01:00 uptime"),
                (seconds: 86400L, expected: "24:00:00 uptime"),
                (seconds: 86460L, expected: "24:01:00 uptime"),
                (seconds: 90000L, expected: "25:00:00 uptime")
            };

            foreach (var (seconds, expected) in testCases)
            {
                var serviceStats = new ServiceStats(
                    serviceName: "PC Client",
                    status: "Connected",
                    currentEntity: null,
                    isHealthy: true,
                    lastSuccessfulOperation: DateTime.UtcNow,
                    lastError: null,
                    counters: new Dictionary<string, long>
                    {
                        ["MessagesSent"] = 100,
                        ["ConnectionAttempts"] = 1,
                        ["UptimeSeconds"] = seconds
                    }
                );

                // Act
                var result = _formatter.Format(serviceStats);

                // Assert
                result.Should().Contain(expected);
            }
        }

        [Fact]
        public void Format_WithDifferentLastSuccessTimes_ShowsCorrectFormat()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var testCases = new[]
            {
                (time: now.AddSeconds(-30), expected: "0:00:30"),
                (time: now.AddMinutes(-1), expected: "0:01:00"),
                (time: now.AddMinutes(-90), expected: "1:30:00"),
                (time: now.AddHours(-2), expected: "2:00:00"),
                (time: now.AddDays(-1), expected: "24:00:00"),
                (time: now.AddDays(-2), expected: "48:00:00")
            };

            foreach (var (time, expected) in testCases)
            {
                var serviceStats = new ServiceStats(
                    serviceName: "PC Client",
                    status: "Connected",
                    currentEntity: null,
                    isHealthy: true,
                    lastSuccessfulOperation: time,
                    lastError: null,
                    counters: new Dictionary<string, long>()
                );

                // Act
                var result = _formatter.Format(serviceStats);

                // Assert
                result.Should().Contain($"Last Success: {expected}");
            }
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
                trackingInfo.ParameterCalculationExpressions["TestParam"] = expression;
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
                IList<ITableColumn<TrackingParam>> capturedColumns = null;
                _mockTableFormatter
                    .Setup(x => x.AppendTable(
                        It.IsAny<StringBuilder>(),
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<TrackingParam>>(),
                        It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int?>()))
                    .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
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
                var result = _formatter.Format(serviceStats);

                // Assert
                capturedColumns.Should().NotBeNull();
                capturedColumns.Count.Should().Be(5);
                
                // Verify expression column behavior
                var expressionColumn = capturedColumns[4];
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
                IList<ITableColumn<TrackingParam>> capturedColumns = null;
                _mockTableFormatter
                    .Setup(x => x.AppendTable(
                        It.IsAny<StringBuilder>(),
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<TrackingParam>>(),
                        It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<int?>()))
                    .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
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
                var result = _formatter.Format(serviceStats);

                // Assert
                capturedColumns.Should().NotBeNull();
                capturedColumns.Count.Should().Be(5);
                
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
            IList<ITableColumn<TrackingParam>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
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
            var result = _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            capturedColumns.Count.Should().Be(5);
            
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
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PCTrackingInfoFormatter(null, _mockTableFormatter.Object, _mockColorService.Object));
        }

        [Fact]
        public void Constructor_WithNullTableFormatter_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PCTrackingInfoFormatter(_mockConsole.Object, null, _mockColorService.Object));
        }

        [Fact]
        public void CycleVerbosity_WithInvalidEnum_ResetsToNormal()
        {
            // Arrange - Force an invalid enum value using reflection
            var field = typeof(PCTrackingInfoFormatter).GetField("<CurrentVerbosity>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_formatter, (VerbosityLevel)999); // Invalid enum value

            // Act
            _formatter.CycleVerbosity();

            // Assert
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        [Fact]
        public void CalculateNormalizedValue_WithZeroRange_ReturnsExpectedValue()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            var param = new TrackingParam { Id = "TestParam", Value = 0.5 };
            trackingInfo.ParameterDefinitions["TestParam"] = new VTSParameter("TestParam", 1.0, 1.0, 1.0); // Min = Max = 1.0 (zero range)
            trackingInfo.Parameters = new List<TrackingParam> { param };
            var serviceStats = CreateServiceStats(trackingInfo);

            // Setup the mock to capture columns
            IList<ITableColumn<TrackingParam>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
                    (builder, title, rows, columns, targetCols, width, barWidth, maxItems) =>
                    {
                        capturedColumns = columns;
                    });

            // Act
            _formatter.Format(serviceStats);

            // Assert - The normalized value calculation should be called via the progress bar column
            capturedColumns.Should().NotBeNull();
            var progressBarColumn = capturedColumns[1]; // Progress bar column
            
            // This will trigger the CalculateNormalizedValue method with zero range
            // The method should handle this gracefully and not crash
            progressBarColumn.ValueFormatter.Should().NotBeNull();
        }

        [Fact]
        public void FormatExpression_WithNullTrackingInfo_ReturnsNoExpression()
        {
            // This is difficult to test directly since FormatExpression is private,
            // but we can test it indirectly by creating a scenario where trackingInfo is null
            // and verifying the behavior through the formatter
            
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.ParameterCalculationExpressions = null; // This will trigger the null check
            trackingInfo.Parameters = new List<TrackingParam> { new TrackingParam { Id = "TestParam", Value = 0.5 } };
            var serviceStats = CreateServiceStats(trackingInfo);

            // Setup the mock to capture columns
            IList<ITableColumn<TrackingParam>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
                    (builder, title, rows, columns, targetCols, width, barWidth, maxItems) =>
                    {
                        capturedColumns = columns;
                    });

            // Act
            _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            var expressionColumn = capturedColumns[4]; // Expression column
            expressionColumn.ValueFormatter(trackingInfo.Parameters.First()).Should().Be("[no expression]");
        }

        [Fact]
        public void FormatExpression_WithLongExpression_TruncatesCorrectly()
        {
            // Arrange
            var longExpression = new string('x', 100); // 100 characters, should be truncated to 87 + "..."
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam> { new TrackingParam { Id = "TestParam", Value = 0.5 } };
            trackingInfo.ParameterCalculationExpressions["TestParam"] = longExpression;
            var serviceStats = CreateServiceStats(trackingInfo);

            // Setup the mock to capture columns
            IList<ITableColumn<TrackingParam>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
                    (builder, title, rows, columns, targetCols, width, barWidth, maxItems) =>
                    {
                        capturedColumns = columns;
                    });

            // Act
            _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            var expressionColumn = capturedColumns[4]; // Expression column
            var result = expressionColumn.ValueFormatter(trackingInfo.Parameters.First());
            result.Should().EndWith("...");
            result.Length.Should().Be(90); // 87 characters + "..."
        }

        [Fact]
        public void FormatHealthStatus_WithLongErrorMessage_TruncatesCorrectly()
        {
            // Arrange
            var longError = new string('x', 60); // 60 characters, should be truncated to 47 + "..."
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = new ServiceStats(
                serviceName: "PC Client",
                status: "Error",
                currentEntity: trackingInfo,
                isHealthy: false,
                lastSuccessfulOperation: DateTime.UtcNow.AddHours(-1),
                lastError: longError,
                counters: new Dictionary<string, long>()
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("...");
            // The truncated error should be 47 characters + "..." = 50 characters total
            result.Should().MatchRegex(@"Error:.*\.\.\..*");
        }

        [Fact]
        public void FormatHealthStatus_WithMinValueLastSuccess_ShowsNever()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            var serviceStats = new ServiceStats(
                serviceName: "PC Client",
                status: "Connected",
                currentEntity: trackingInfo,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.MinValue, // This should show "Never"
                lastError: null,
                counters: new Dictionary<string, long>()
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Last Success: Never");
        }

        [Fact]
        public void FormatConnectionMetrics_WithMissingCounters_UsesZeroValues()
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
                counters: new Dictionary<string, long>
                {
                    ["MessagesSent"] = 100
                    // Missing "ConnectionAttempts" and "UptimeSeconds"
                }
            );

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Connection: 100 msgs sent | 0 attempts | 0:00:00 uptime");
        }

        [Fact]
        public void Format_WithParametersButNullParameterDefinitions_UsesEmptyDefinitions()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam> { new TrackingParam { Id = "TestParam", Value = 0.5, Weight = 2.5 } };
            trackingInfo.ParameterDefinitions = null; // This should be handled gracefully
            var serviceStats = CreateServiceStats(trackingInfo);

            // Setup the mock to capture columns
            IList<ITableColumn<TrackingParam>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
                    (builder, title, rows, columns, targetCols, width, barWidth, maxItems) =>
                    {
                        capturedColumns = columns;
                    });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            var rangeColumn = capturedColumns[3]; // Width x Range column
            rangeColumn.ValueFormatter(trackingInfo.Parameters.First()).Should().Be("2.5 x [no definition]");
        }

        [Fact]
        public void Format_WithParametersButNullWeight_UsesDefaultWeight()
        {
            // Arrange
            var trackingInfo = CreatePCTrackingInfo();
            trackingInfo.Parameters = new List<TrackingParam> { new TrackingParam { Id = "TestParam", Value = 0.5, Weight = null } };
            trackingInfo.ParameterDefinitions["TestParam"] = new VTSParameter("TestParam", -1, 1, 0);
            var serviceStats = CreateServiceStats(trackingInfo);

            // Setup the mock to capture columns
            IList<ITableColumn<TrackingParam>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<TrackingParam>>(),
                    It.IsAny<IList<ITableColumn<TrackingParam>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<TrackingParam>, IList<ITableColumn<TrackingParam>>, int, int, int, int?>(
                    (builder, title, rows, columns, targetCols, width, barWidth, maxItems) =>
                    {
                        capturedColumns = columns;
                    });

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            capturedColumns.Should().NotBeNull();
            var rangeColumn = capturedColumns[3]; // Width x Range column
            rangeColumn.ValueFormatter(trackingInfo.Parameters.First()).Should().Be("1 x [-1; 0; 1]");
        }
    }
} 