using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using SharpBridge.Interfaces;
using SharpBridge.Utilities;
using Xunit;

namespace Tests.Utilities
{
    public class TableFormatterTests
    {
        private readonly ITableFormatter _tableFormatter;

        public TableFormatterTests()
        {
            _tableFormatter = new TableFormatter();
        }

        [Fact]
        public void AppendTable_WithEmptyRows_ReturnsEmptyTable()
        {
            // Arrange
            var builder = new StringBuilder();
            var emptyRows = new List<TestItem>();
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F2")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Test Table", emptyRows, columns, 1, 80, 20);

            // Assert
            builder.ToString().Should().Be($"Test Table{Environment.NewLine}");
        }

        [Fact]
        public void AppendTable_WithSingleColumn_ShowsCorrectLayout()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Item1", Value = 1.5 },
                new TestItem { Name = "Item2", Value = 2.7 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F2")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Test Table", rows, columns, 1, 80, 20);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Test Table");
            result.Should().Contain("Name");
            result.Should().Contain("Value");
            result.Should().Contain("Item1");
            result.Should().Contain("Item2");
            result.Should().Contain("1.50");
            result.Should().Contain("2.70");
        }

        [Fact]
        public void AppendTable_WithProgressBarColumn_ShowsProgressBars()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Half", Value = 0.5 },
                new TestItem { Name = "Full", Value = 1.0 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new ProgressBarColumn<TestItem>("Progress", item => item.Value, tableFormatter: _tableFormatter)
            };

            // Act
            _tableFormatter.AppendTable(builder, "Progress Test", rows, columns, 1, 80, 20);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Progress Test");
            result.Should().Contain("█"); // Progress bar filled character
            result.Should().Contain("░"); // Progress bar empty character
        }

        [Fact]
        public void AppendTable_WithDynamicWidthColumn_AdjustsToWidth()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Short", Value = 1.0 },
                new TestItem { Name = "VeryLongItemName", Value = 2.0 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name, maxWidth: 10),
                new NumericColumn<TestItem>("Value", item => item.Value, "F1")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Dynamic Width Test", rows, columns, 1, 80, 20);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Dynamic Width Test");
            result.Should().Contain("Short");
            result.Should().Contain("VeryLon..."); // Should be truncated
        }

        [Fact]
        public void AppendTable_WithSingleColumnMaxItems_LimitsRows()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Item1", Value = 1.0 },
                new TestItem { Name = "Item2", Value = 2.0 },
                new TestItem { Name = "Item3", Value = 3.0 },
                new TestItem { Name = "Item4", Value = 4.0 },
                new TestItem { Name = "Item5", Value = 5.0 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F0")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Limited Table", rows, columns, 1, 80, 20, singleColumnMaxItems: 3);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Limited Table");
            result.Should().Contain("Item1");
            result.Should().Contain("Item2");
            result.Should().Contain("Item3");
            result.Should().NotContain("Item4");
            result.Should().NotContain("Item5");
            result.Should().Contain("... and 2 more");
        }

        [Fact]
        public void AppendTable_WithMultipleColumns_CreatesMultiColumnLayout()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "A", Value = 1.0 },
                new TestItem { Name = "B", Value = 2.0 },
                new TestItem { Name = "C", Value = 3.0 },
                new TestItem { Name = "D", Value = 4.0 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F0")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Multi-Column Test", rows, columns, 2, 120, 20);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Multi-Column Test");
            result.Should().Contain("A");
            result.Should().Contain("B");
            result.Should().Contain("C");
            result.Should().Contain("D");
        }

        [Fact]
        public void AppendTable_WithInsufficientWidth_FallsBackToSingleColumn()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Item1", Value = 1.0 },
                new TestItem { Name = "Item2", Value = 2.0 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F2")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Narrow Test", rows, columns, 3, 40, 20);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Narrow Test");
            result.Should().Contain("Item1");
            result.Should().Contain("Item2");
        }

        [Fact]
        public void AppendTable_WithNullRows_HandlesGracefully()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F2")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Null Test", null, columns, 1, 80, 20);

            // Assert
            builder.ToString().Should().Be($"Null Test{Environment.NewLine}");
        }

        [Fact]
        public void AppendTable_WithComplexValueSelector_WorksCorrectly()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test", Value = 42.123 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Upper", item => item.Name.ToUpper()),
                new NumericColumn<TestItem>("Rounded", item => Math.Round(item.Value), "F0")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Complex Test", rows, columns, 1, 80, 20);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Complex Test");
            result.Should().Contain("TEST");
            result.Should().Contain("42");
        }

        [Fact]
        public void AppendTable_WithVaryingContentLengths_AdjustsColumnWidths()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "A", Value = 1.0 },
                new TestItem { Name = "VeryLongName", Value = 2.0 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F1")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Width Test", rows, columns, 1, 80, 20);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Width Test");
            result.Should().Contain("A");
            result.Should().Contain("VeryLongName");
            result.Should().Contain("1.0");
            result.Should().Contain("2.0");
        }

        [Fact]
        public void AppendTable_WithZeroTargetColumns_UsesSingleColumn()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test", Value = 1.0 }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F1")
            };

            // Act
            _tableFormatter.AppendTable(builder, "Zero Columns Test", rows, columns, 0, 80, 20);

            // Assert
            var result = builder.ToString();
            result.Should().Contain("Zero Columns Test");
            result.Should().Contain("Test");
            result.Should().Contain("1.0");
        }

        private class TestItem
        {
            public string Name { get; set; } = string.Empty;
            public double Value { get; set; }
        }
    }
} 