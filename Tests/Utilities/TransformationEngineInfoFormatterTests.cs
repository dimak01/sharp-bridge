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
    public class TransformationEngineInfoFormatterTests
    {
        private const int RULE_DISPLAY_COUNT_NORMAL = 15;
        
        private readonly Mock<IConsole> _mockConsole;
        private readonly Mock<ITableFormatter> _mockTableFormatter;
        private readonly TransformationEngineInfoFormatter _formatter;

        // Mock class for testing wrong entity type
        private class WrongEntityType : IFormattableObject
        {
            public string Data { get; set; } = "Wrong type";
        }

        public TransformationEngineInfoFormatterTests()
        {
            _mockConsole = new Mock<IConsole>();
            _mockConsole.Setup(c => c.WindowWidth).Returns(80);
            _mockConsole.Setup(c => c.WindowHeight).Returns(25);
            
            _mockTableFormatter = new Mock<ITableFormatter>();
            _formatter = new TransformationEngineInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object);
        }

        #region Helper Methods

        private ServiceStats CreateMockServiceStats(string status, TransformationEngineInfo currentEntity = null, 
            bool isHealthy = true, DateTime lastSuccess = default, string lastError = null)
        {
            var counters = new Dictionary<string, long>
            {
                ["Valid Rules"] = 10,
                ["Invalid Rules"] = 2,
                ["Total Transformations"] = 1000,
                ["Successful Transformations"] = 995,
                ["Failed Transformations"] = 5,
                ["Hot Reload Attempts"] = 3,
                ["Hot Reload Successes"] = 2,
                ["Uptime Since Rules Loaded (seconds)"] = 3600
            };

            return new ServiceStats(
                serviceName: "Transformation Engine",
                status: status,
                currentEntity: currentEntity,
                isHealthy: isHealthy,
                lastSuccessfulOperation: lastSuccess,
                lastError: lastError,
                counters: counters);
        }

        private TransformationEngineInfo CreateTransformationEngineInfo(
            string configFilePath = "Configs/test_rules.json",
            int validRulesCount = 10,
            List<RuleInfo> invalidRules = null)
        {
            return new TransformationEngineInfo(
                configFilePath: configFilePath,
                validRulesCount: validRulesCount,
                invalidRules: invalidRules ?? new List<RuleInfo>());
        }

        private List<RuleInfo> CreateInvalidRules(int count = 2)
        {
            var rules = new List<RuleInfo>();
            for (int i = 0; i < count; i++)
            {
                rules.Add(new RuleInfo(
                    name: $"InvalidRule{i + 1}",
                    func: $"invalid_expression_{i + 1}",
                    error: $"Syntax error in rule {i + 1}",
                    type: "Validation"));
            }
            return rules;
        }

        #endregion

        #region Helper Methods

        private string FormatServiceHeader(string status)
        {
            var statusColor = ConsoleColors.GetStatusColor(status);
            return $"=== Transformation Engine ({statusColor}{status}{ConsoleColors.Reset}) === [Alt+T]";
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange & Act
            var formatter = new TransformationEngineInfoFormatter(_mockConsole.Object, _mockTableFormatter.Object);

            // Assert
            formatter.Should().NotBeNull();
            formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        [Fact]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TransformationEngineInfoFormatter(null, _mockTableFormatter.Object));
        }

        [Fact]
        public void Constructor_WithNullTableFormatter_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new TransformationEngineInfoFormatter(_mockConsole.Object, null));
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
            // Arrange - Force an invalid enum value using reflection
            var field = typeof(TransformationEngineInfoFormatter).GetField("<CurrentVerbosity>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_formatter, (VerbosityLevel)999); // Invalid enum value

            // Act
            _formatter.CycleVerbosity();

            // Assert
            _formatter.CurrentVerbosity.Should().Be(VerbosityLevel.Normal);
        }

        #endregion

        #region Service Header Tests

        [Fact]
        public void Format_WithAllRulesActiveStatus_ShowsCorrectServiceHeader()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(FormatServiceHeader("AllRulesActive"));
        }

        [Fact]
        public void Format_WithSomeRulesActiveStatus_ShowsCorrectServiceHeader()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("SomeRulesActive", engineInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain(FormatServiceHeader("SomeRulesActive"));
        }

        [Fact]
        public void Format_ShowsCurrentVerbosityLevel()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Verbosity: Detailed");
        }

        #endregion

        #region Rule Statistics Tests

        [Fact]
        public void Format_WithRuleStatistics_ShowsCorrectRuleCounts()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo(validRulesCount: 15);
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters["Valid Rules"] = 15;
            serviceStats.Counters["Invalid Rules"] = 3;

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Rules Loaded - Total: 18, Valid: 15, Invalid: 3");
        }

        [Fact]
        public void Format_WithUptimeStatistics_ShowsFormattedUptime()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters["Uptime Since Rules Loaded (seconds)"] = 7325; // 2h 2m 5s

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Uptime: 2:02:05");
        }

        [Fact]
        public void Format_WithoutUptimeStatistics_DoesNotShowUptime()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters.Remove("Uptime Since Rules Loaded (seconds)");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Uptime:");
        }

        #endregion

        #region Configuration and Performance Tests

        [Fact]
        public void Format_WithConfigFileInfo_ShowsConfigPath()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo("Configs/custom_rules.json");
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Config File - Path: Configs/custom_rules.json, Status: Loaded");
        }

        [Fact]
        public void Format_WithConfigLoadStatistics_ShowsLoadCounts()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters["Hot Reload Attempts"] = 5;
            serviceStats.Counters["Hot Reload Successes"] = 4;

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Config file loads count - Attempts: 5, Successful: 4");
        }

        [Fact]
        public void Format_WithTransformationMetrics_ShowsTransformationCounts()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters["Total Transformations"] = 2500;
            serviceStats.Counters["Successful Transformations"] = 2495;
            serviceStats.Counters["Failed Transformations"] = 5;

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("Transformations - Total: 2500, Successful: 2495, Failed: 5");
        }

        [Fact]
        public void Format_WithZeroTransformations_DoesNotShowTransformationMetrics()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters["Total Transformations"] = 0;

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Transformations - Total:");
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
        public void Format_WithNullCurrentEntity_ShowsNoDataMessage()
        {
            // Arrange
            var serviceStats = CreateMockServiceStats("AllRulesActive", currentEntity: null);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain("No transformation engine data available");
        }

        [Fact]
        public void Format_WithWrongEntityType_ThrowsArgumentException()
        {
            // Arrange
            var wrongEntity = new WrongEntityType();
            var counters = new Dictionary<string, long>
            {
                ["Valid Rules"] = 10,
                ["Invalid Rules"] = 2
            };

            var serviceStats = new ServiceStats(
                serviceName: "Transformation Engine",
                status: "AllRulesActive",
                currentEntity: wrongEntity, // This will cause the error
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: counters);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _formatter.Format(serviceStats));
        }

        #endregion

        #region Failed Rules Table Tests

        [Fact]
        public void Format_WithNoInvalidRules_DoesNotShowFailedRulesTable()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo(invalidRules: new List<RuleInfo>());
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<RuleInfo>>(),
                It.IsAny<IList<ITableColumn<RuleInfo>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public void Format_WithInvalidRulesAndNormalVerbosity_ShowsFailedRulesTable()
        {
            // Arrange
            var invalidRules = CreateInvalidRules(3);
            var engineInfo = CreateTransformationEngineInfo(invalidRules: invalidRules);
            var serviceStats = CreateMockServiceStats("SomeRulesActive", engineInfo);
            // _formatter starts at Normal verbosity

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                "=== Failed Rules ===",
                It.Is<IEnumerable<RuleInfo>>(rules => rules.Count() == 3),
                It.Is<IList<ITableColumn<RuleInfo>>>(cols => 
                    cols.Count == 3 &&
                    cols[0].Header == "Rule Name" &&
                    cols[1].Header == "Function" &&
                    cols[2].Header == "Error"),
                1, // targetColumns
                80, // console width
                20, // barWidth
                RULE_DISPLAY_COUNT_NORMAL), // maxItems for normal verbosity
                Times.Once);
        }

        [Fact]
        public void Format_WithInvalidRulesAndDetailedVerbosity_ShowsAllFailedRules()
        {
            // Arrange
            var invalidRules = CreateInvalidRules(20); // More than normal display count
            var engineInfo = CreateTransformationEngineInfo(invalidRules: invalidRules);
            var serviceStats = CreateMockServiceStats("SomeRulesActive", engineInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                "=== Failed Rules ===",
                It.Is<IEnumerable<RuleInfo>>(rules => rules.Count() == 20),
                It.Is<IList<ITableColumn<RuleInfo>>>(cols => cols.Count == 3),
                1, // targetColumns
                80, // console width
                20, // barWidth
                null), // No limit for detailed verbosity
                Times.Once);
        }

        [Fact]
        public void Format_WithInvalidRulesAndBasicVerbosity_DoesNotShowFailedRulesTable()
        {
            // Arrange
            var invalidRules = CreateInvalidRules(3);
            var engineInfo = CreateTransformationEngineInfo(invalidRules: invalidRules);
            var serviceStats = CreateMockServiceStats("SomeRulesActive", engineInfo);
            _formatter.CycleVerbosity(); // Normal -> Detailed
            _formatter.CycleVerbosity(); // Detailed -> Basic

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            _mockTableFormatter.Verify(x => x.AppendTable(
                It.IsAny<StringBuilder>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<RuleInfo>>(),
                It.IsAny<IList<ITableColumn<RuleInfo>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public void Format_WithFailedRules_CreatesCorrectTableColumns()
        {
            // Arrange
            var invalidRules = new List<RuleInfo>
            {
                new RuleInfo("TestRule1", "invalid_expression_1", "Syntax error in rule 1", "Validation"),
                new RuleInfo("TestRule2", "this_is_a_very_long_expression_that_should_definitely_be_truncated_because_it_exceeds_80_characters_limit", "This is a very long error message that should also be truncated properly because it exceeds the 80 character limit", "Evaluation")
            };
            var engineInfo = CreateTransformationEngineInfo(invalidRules: invalidRules);
            var serviceStats = CreateMockServiceStats("SomeRulesActive", engineInfo);

            // Capture the columns to verify their behavior
            IList<ITableColumn<RuleInfo>> capturedColumns = null;
            _mockTableFormatter
                .Setup(x => x.AppendTable(
                    It.IsAny<StringBuilder>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<RuleInfo>>(),
                    It.IsAny<IList<ITableColumn<RuleInfo>>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>()))
                .Callback<StringBuilder, string, IEnumerable<RuleInfo>, IList<ITableColumn<RuleInfo>>, int, int, int, int?>(
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
            capturedColumns.Count.Should().Be(3);
            
            // Verify column headers
            capturedColumns[0].Header.Should().Be("Rule Name");
            capturedColumns[1].Header.Should().Be("Function");
            capturedColumns[2].Header.Should().Be("Error");

            // Verify column formatter behavior
            var rule1 = invalidRules[0];
            var rule2 = invalidRules[1];
            
            capturedColumns[0].ValueFormatter(rule1).Should().Be("TestRule1");
            capturedColumns[1].ValueFormatter(rule1).Should().Be("invalid_expression_1");
            capturedColumns[2].ValueFormatter(rule1).Should().Be("Syntax error in rule 1");
            
            // Test truncation behavior
            capturedColumns[1].ValueFormatter(rule2).Should().Contain("this_is_a_very_long_expression").And.EndWith("...");
            capturedColumns[2].ValueFormatter(rule2).Should().Contain("This is a very long error").And.EndWith("...");
        }

        #endregion

        #region Configuration Status Tests

        [Theory]
        [InlineData("AllRulesActive", "Loaded")]
        [InlineData("SomeRulesActive", "Loaded")]
        [InlineData("ConfigErrorCached", "Error (Using Cached)")]
        [InlineData("NoValidRules", "Loaded (No Valid Rules)")]
        [InlineData("ConfigMissing", "Not Found")]
        [InlineData("NeverLoaded", "Never Loaded")]
        [InlineData("UnknownStatus", "Unknown")]
        public void Format_WithDifferentEngineStatuses_ShowsCorrectConfigStatus(string engineStatus, string expectedConfigStatus)
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo("Configs/test_rules.json");
            var serviceStats = CreateMockServiceStats(engineStatus, engineInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().Contain($"Config File - Path: Configs/test_rules.json, Status: {expectedConfigStatus}");
        }

        #endregion

        #region Counter Handling Tests

        [Fact]
        public void Format_WithMissingRuleCounters_HandlesGracefully()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters.Remove("Valid Rules");
            serviceStats.Counters.Remove("Invalid Rules");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Rules Loaded");
        }

        [Fact]
        public void Format_WithMissingHotReloadCounters_HandlesGracefully()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters.Remove("Hot Reload Attempts");
            serviceStats.Counters.Remove("Hot Reload Successes");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Config file loads count");
        }

        [Fact]
        public void Format_WithMissingTransformationCounters_HandlesGracefully()
        {
            // Arrange
            var engineInfo = CreateTransformationEngineInfo();
            var serviceStats = CreateMockServiceStats("AllRulesActive", engineInfo);
            serviceStats.Counters.Remove("Total Transformations");
            serviceStats.Counters.Remove("Successful Transformations");
            serviceStats.Counters.Remove("Failed Transformations");

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            result.Should().NotContain("Transformations - Total:");
        }

        #endregion

        #region Information Hierarchy Tests

        [Fact]
        public void Format_ShowsInformationInCorrectOrder()
        {
            // Arrange
            var invalidRules = CreateInvalidRules(2);
            var engineInfo = CreateTransformationEngineInfo(invalidRules: invalidRules);
            var serviceStats = CreateMockServiceStats("SomeRulesActive", engineInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            
            // Find key sections in order
            var headerIndex = Array.FindIndex(lines, line => line.Contains("=== Transformation Engine"));
            var verbosityIndex = Array.FindIndex(lines, line => line.Contains("Verbosity:"));
            var rulesLoadedIndex = Array.FindIndex(lines, line => line.Contains("Rules Loaded"));
            var configFileIndex = Array.FindIndex(lines, line => line.Contains("Config File"));
            var transformationsIndex = Array.FindIndex(lines, line => line.Contains("Transformations"));
            
            // Verify order
            headerIndex.Should().BeGreaterThanOrEqualTo(0);
            verbosityIndex.Should().BeGreaterThan(headerIndex);
            rulesLoadedIndex.Should().BeGreaterThan(verbosityIndex);
            configFileIndex.Should().BeGreaterThan(rulesLoadedIndex);
            transformationsIndex.Should().BeGreaterThan(configFileIndex);
        }

        [Fact]
        public void Format_HasCorrectSectionSeparation()
        {
            // Arrange
            var invalidRules = CreateInvalidRules(2);
            var engineInfo = CreateTransformationEngineInfo(invalidRules: invalidRules);
            var serviceStats = CreateMockServiceStats("SomeRulesActive", engineInfo);

            // Act
            var result = _formatter.Format(serviceStats);

            // Assert
            // Should have empty lines separating major sections
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var emptyLines = lines.Where((line, index) => string.IsNullOrWhiteSpace(line)).Count();
            emptyLines.Should().BeGreaterThanOrEqualTo(2); // At least 2 empty lines for section separation
        }

        #endregion
    }
} 