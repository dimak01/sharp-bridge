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
            return $"=== PC Client ({statusColor}{status}{ConsoleColors.Reset}) === [Alt+P]";
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
            _formatter = new PCTrackingInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object);
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
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.Is<string>(s => s == "Parameters"),
                It.IsAny<List<TrackingParam>>(),
                It.IsAny<List<ITableColumn<TrackingParam>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Once);
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
            var serviceStats = CreateServiceStats(trackingInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.Is<string>(s => s == "Parameters"),
                It.IsAny<List<TrackingParam>>(),
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
                It.Is<string>(s => s == "Parameters"),
                It.Is<List<TrackingParam>>(parameters => parameters.Count == 0),
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
                It.Is<string>(s => s == "Parameters"),
                It.IsAny<List<TrackingParam>>(),
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
                It.Is<string>(s => s == "Parameters"),
                It.IsAny<List<TrackingParam>>(),
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
            result.Should().Contain(PCTestFormattingHelpers.FormatHealthStatus(false, "1h", "Connection failed"));
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
            result.Should().Contain("Connection: 100 msgs sent | 1 attempts | 1h uptime");
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
                It.Is<string>(s => s == "Parameters"),
                It.IsAny<List<TrackingParam>>(),
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
    }
} 