using System;
using System.Linq;
using Moq;
using SharpBridge.Configuration.Managers;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.UI;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Configuration.Managers
{
    /// <summary>
    /// Unit tests for ParameterTableConfigurationManager
    /// </summary>
    public class ParameterTableConfigurationManagerTests
    {
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly ParameterTableConfigurationManager _configManager;

        public ParameterTableConfigurationManagerTests()
        {
            _mockLogger = new Mock<IAppLogger>();
            _configManager = new ParameterTableConfigurationManager(_mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ParameterTableConfigurationManager(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesWithDefaultColumns()
        {
            // Act
            var columns = _configManager.GetParameterTableColumns();

            // Assert
            Assert.NotNull(columns);
            Assert.Equal(7, columns.Length);
            Assert.Equal(ParameterTableColumn.ParameterName, columns[0]);
            Assert.Equal(ParameterTableColumn.ProgressBar, columns[1]);
            Assert.Equal(ParameterTableColumn.Value, columns[2]);
            Assert.Equal(ParameterTableColumn.Range, columns[3]);
            Assert.Equal(ParameterTableColumn.MinMax, columns[4]);
            Assert.Equal(ParameterTableColumn.Expression, columns[5]);
            Assert.Equal(ParameterTableColumn.Interpolation, columns[6]);
        }

        #endregion

        #region GetDefaultParameterTableColumns Tests

        [Fact]
        public void GetDefaultParameterTableColumns_ReturnsAllColumnsInCorrectOrder()
        {
            // Act
            var defaultColumns = _configManager.GetDefaultParameterTableColumns();

            // Assert
            Assert.NotNull(defaultColumns);
            Assert.Equal(7, defaultColumns.Length);
            Assert.Equal(ParameterTableColumn.ParameterName, defaultColumns[0]);
            Assert.Equal(ParameterTableColumn.ProgressBar, defaultColumns[1]);
            Assert.Equal(ParameterTableColumn.Value, defaultColumns[2]);
            Assert.Equal(ParameterTableColumn.Range, defaultColumns[3]);
            Assert.Equal(ParameterTableColumn.MinMax, defaultColumns[4]);
            Assert.Equal(ParameterTableColumn.Expression, defaultColumns[5]);
            Assert.Equal(ParameterTableColumn.Interpolation, defaultColumns[6]);
        }

        [Fact]
        public void GetDefaultParameterTableColumns_ReturnsAllEnumValues()
        {
            // Act
            var defaultColumns = _configManager.GetDefaultParameterTableColumns();
            var allEnumValues = Enum.GetValues<ParameterTableColumn>();

            // Assert
            Assert.Equal(allEnumValues.Length, defaultColumns.Length);
            Assert.True(defaultColumns.All(col => allEnumValues.Contains(col)));
        }

        #endregion

        #region LoadFromUserPreferences Tests

        [Fact]
        public void LoadFromUserPreferences_WithNullUserPreferences_UsesDefaults()
        {
            // Act
            _configManager.LoadFromUserPreferences(null!);

            // Assert
            var columns = _configManager.GetParameterTableColumns();
            var defaultColumns = _configManager.GetDefaultParameterTableColumns();
            Assert.Equal(defaultColumns, columns);
            _mockLogger.Verify(x => x.Debug("No parameter table columns configured in user preferences, using defaults"), Times.Once);
        }

        [Fact]
        public void LoadFromUserPreferences_WithNullPCParameterTableColumns_UsesDefaults()
        {
            // Arrange
            var userPreferences = new UserPreferences { PCParameterTableColumns = null! };

            // Act
            _configManager.LoadFromUserPreferences(userPreferences);

            // Assert
            var columns = _configManager.GetParameterTableColumns();
            var defaultColumns = _configManager.GetDefaultParameterTableColumns();
            Assert.Equal(defaultColumns, columns);
            _mockLogger.Verify(x => x.Debug("No parameter table columns configured in user preferences, using defaults"), Times.Once);
        }

        [Fact]
        public void LoadFromUserPreferences_WithEmptyPCParameterTableColumns_UsesDefaults()
        {
            // Arrange
            var userPreferences = new UserPreferences { PCParameterTableColumns = Array.Empty<ParameterTableColumn>() };

            // Act
            _configManager.LoadFromUserPreferences(userPreferences);

            // Assert
            var columns = _configManager.GetParameterTableColumns();
            var defaultColumns = _configManager.GetDefaultParameterTableColumns();
            Assert.Equal(defaultColumns, columns);
            _mockLogger.Verify(x => x.Debug("No parameter table columns configured in user preferences, using defaults"), Times.Once);
        }

        [Fact]
        public void LoadFromUserPreferences_WithValidColumns_LoadsCorrectly()
        {
            // Arrange
            var customColumns = new[]
            {
                ParameterTableColumn.Value,
                ParameterTableColumn.ProgressBar,
                ParameterTableColumn.ParameterName
            };
            var userPreferences = new UserPreferences { PCParameterTableColumns = customColumns };

            // Act
            _configManager.LoadFromUserPreferences(userPreferences);

            // Assert
            var columns = _configManager.GetParameterTableColumns();
            Assert.Equal(customColumns, columns);
            _mockLogger.Verify(x => x.Debug("Loaded {0} parameter table columns from user preferences", 3), Times.Once);
        }

        [Fact]
        public void LoadFromUserPreferences_WithInvalidColumns_LogsWarningAndUsesDefaults()
        {
            // Arrange
            var invalidColumns = new[]
            {
                (ParameterTableColumn)999, // Invalid enum value
                (ParameterTableColumn)888, // Another invalid enum value
                (ParameterTableColumn)777  // Third invalid enum value
            };
            var userPreferences = new UserPreferences { PCParameterTableColumns = invalidColumns };

            // Act
            _configManager.LoadFromUserPreferences(userPreferences);

            // Assert
            var columns = _configManager.GetParameterTableColumns();
            var defaultColumns = _configManager.GetDefaultParameterTableColumns();
            Assert.Equal(defaultColumns, columns);
            _mockLogger.Verify(x => x.Warning("Invalid parameter table columns found: {0}. These will be ignored.", "999, 888, 777"), Times.Once);
            _mockLogger.Verify(x => x.Warning("No valid parameter table columns found, using defaults"), Times.Once);
        }

        [Fact]
        public void LoadFromUserPreferences_WithMixedValidAndInvalidColumns_KeepsValidOnes()
        {
            // Arrange
            var mixedColumns = new[]
            {
                ParameterTableColumn.Value,
                (ParameterTableColumn)999, // Invalid
                ParameterTableColumn.ProgressBar,
                (ParameterTableColumn)888, // Invalid
                ParameterTableColumn.ParameterName
            };
            var userPreferences = new UserPreferences { PCParameterTableColumns = mixedColumns };

            // Act
            _configManager.LoadFromUserPreferences(userPreferences);

            // Assert
            var columns = _configManager.GetParameterTableColumns();
            var expectedValidColumns = new[]
            {
                ParameterTableColumn.Value,
                ParameterTableColumn.ProgressBar,
                ParameterTableColumn.ParameterName
            };
            Assert.Equal(expectedValidColumns, columns);
            _mockLogger.Verify(x => x.Warning("Invalid parameter table columns found: {0}. These will be ignored.", "999, 888"), Times.Once);
            _mockLogger.Verify(x => x.Debug("Loaded {0} parameter table columns from user preferences", 3), Times.Once);
        }

        [Fact]
        public void LoadFromUserPreferences_WithAllInvalidColumns_UsesDefaults()
        {
            // Arrange
            var allInvalidColumns = new[]
            {
                (ParameterTableColumn)999,
                (ParameterTableColumn)888,
                (ParameterTableColumn)777
            };
            var userPreferences = new UserPreferences { PCParameterTableColumns = allInvalidColumns };

            // Act
            _configManager.LoadFromUserPreferences(userPreferences);

            // Assert
            var columns = _configManager.GetParameterTableColumns();
            var defaultColumns = _configManager.GetDefaultParameterTableColumns();
            Assert.Equal(defaultColumns, columns);
            _mockLogger.Verify(x => x.Warning("Invalid parameter table columns found: {0}. These will be ignored.", "999, 888, 777"), Times.Once);
            _mockLogger.Verify(x => x.Warning("No valid parameter table columns found, using defaults"), Times.Once);
        }

        #endregion

        #region GetColumnDisplayName Tests

        [Theory]
        [InlineData(ParameterTableColumn.ParameterName, "Parameter Name")]
        [InlineData(ParameterTableColumn.ProgressBar, "Progress Bar")]
        [InlineData(ParameterTableColumn.Value, "Value")]
        [InlineData(ParameterTableColumn.Range, "Range")]
        [InlineData(ParameterTableColumn.Expression, "Expression")]
        [InlineData(ParameterTableColumn.Interpolation, "Interpolation")]
        public void GetColumnDisplayName_ReturnsCorrectDescription(ParameterTableColumn column, string expectedDescription)
        {
            // Act
            var displayName = _configManager.GetColumnDisplayName(column);

            // Assert
            Assert.Equal(expectedDescription, displayName);
        }

        [Fact]
        public void GetColumnDisplayName_WithAllEnumValues_ReturnsValidStrings()
        {
            // Act & Assert
            foreach (var column in Enum.GetValues<ParameterTableColumn>())
            {
                var displayName = _configManager.GetColumnDisplayName(column);
                Assert.NotNull(displayName);
                Assert.NotEmpty(displayName);
            }
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void LoadFromUserPreferences_ThenGetParameterTableColumns_ReturnsLoadedColumns()
        {
            // Arrange
            var customColumns = new[]
            {
                ParameterTableColumn.Expression,
                ParameterTableColumn.Value
            };
            var userPreferences = new UserPreferences { PCParameterTableColumns = customColumns };

            // Act
            _configManager.LoadFromUserPreferences(userPreferences);
            var result = _configManager.GetParameterTableColumns();

            // Assert
            Assert.Equal(customColumns, result);
        }

        [Fact]
        public void MultipleLoadFromUserPreferences_Calls_UpdatesColumnsCorrectly()
        {
            // Arrange
            var firstColumns = new[] { ParameterTableColumn.Value, ParameterTableColumn.ProgressBar };
            var secondColumns = new[] { ParameterTableColumn.ParameterName, ParameterTableColumn.Expression };
            var firstPreferences = new UserPreferences { PCParameterTableColumns = firstColumns };
            var secondPreferences = new UserPreferences { PCParameterTableColumns = secondColumns };

            // Act
            _configManager.LoadFromUserPreferences(firstPreferences);
            var firstResult = _configManager.GetParameterTableColumns();
            _configManager.LoadFromUserPreferences(secondPreferences);
            var secondResult = _configManager.GetParameterTableColumns();

            // Assert
            Assert.Equal(firstColumns, firstResult);
            Assert.Equal(secondColumns, secondResult);
        }

        #endregion
    }
}