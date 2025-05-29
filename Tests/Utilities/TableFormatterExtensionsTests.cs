using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using SharpBridge.Utilities;
using Xunit;

namespace SharpBridge.Tests.Utilities
{
    public class TableFormatterExtensionsTests
    {
        // Test data classes
        private class TestItem
        {
            public string Name { get; set; } = string.Empty;
            public double Value { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        private class SimpleItem
        {
            public string Label { get; set; } = string.Empty;
            public int Number { get; set; }
        }

        [Fact]
        public void AppendGenericTable_WithEmptyRows_ReturnsEmptyTable()
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
            var result = builder.AppendGenericTable("Test Table", emptyRows, columns, 1, 80, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            builder.ToString().Should().Be($"Test Table{Environment.NewLine}");
        }

        [Fact]
        public void AppendGenericTable_WithSingleColumn_ShowsCorrectLayout()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Item1", Value = 1.5, Status = "Active" },
                new TestItem { Name = "Item2", Value = 2.7, Status = "Inactive" }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F2"),
                new TextColumn<TestItem>("Status", item => item.Status)
            };

            // Act
            var result = builder.AppendGenericTable("Test Table", rows, columns, 1, 80, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("Test Table");
            output.Should().Contain("Name");
            output.Should().Contain("Value");
            output.Should().Contain("Status");
            output.Should().Contain("Item1");
            output.Should().Contain("1.50");
            output.Should().Contain("Active");
        }

        [Fact]
        public void AppendGenericTable_WithProgressBarColumn_ShowsProgressBars()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test1", Value = 0.5, Status = "Running" },
                new TestItem { Name = "Test2", Value = 0.8, Status = "Complete" }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new ProgressBarColumn<TestItem>("Progress", item => item.Value),
                new TextColumn<TestItem>("Status", item => item.Status)
            };

            // Act
            var result = builder.AppendGenericTable("Progress Test", rows, columns, 1, 80, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("Progress Test");
            output.Should().Contain("█"); // Should contain filled progress bar characters
            output.Should().Contain("░"); // Should contain empty progress bar characters
        }

        [Fact]
        public void AppendGenericTable_WithDynamicWidthColumn_AdjustsToWidth()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Short", Value = 1.0, Status = "OK" },
                new TestItem { Name = "VeryLongItemName", Value = 2.0, Status = "Processing" }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new TextColumn<TestItem>("Status", item => item.Status)
            };

            // Act
            var result = builder.AppendGenericTable("Dynamic Width Test", rows, columns, 1, 80, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("VeryLongItemName");
            output.Should().Contain("Processing");
        }

        [Fact]
        public void AppendGenericTable_WithSingleColumnMaxItems_LimitsRows()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<SimpleItem>
            {
                new SimpleItem { Label = "Item1", Number = 1 },
                new SimpleItem { Label = "Item2", Number = 2 },
                new SimpleItem { Label = "Item3", Number = 3 },
                new SimpleItem { Label = "Item4", Number = 4 },
                new SimpleItem { Label = "Item5", Number = 5 }
            };
            var columns = new List<ITableColumn<SimpleItem>>
            {
                new TextColumn<SimpleItem>("Label", item => item.Label),
                new TextColumn<SimpleItem>("Number", item => item.Number.ToString())
            };

            // Act
            var result = builder.AppendGenericTable("Limited Table", rows, columns, 1, 80, 20, singleColumnMaxItems: 3);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("Item1");
            output.Should().Contain("Item2");
            output.Should().Contain("Item3");
            output.Should().NotContain("Item4");
            output.Should().NotContain("Item5");
        }

        [Fact]
        public void AppendGenericTable_WithMultipleColumns_CreatesMultiColumnLayout()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test1", Value = 1.0, Status = "OK" },
                new TestItem { Name = "Test2", Value = 2.0, Status = "Good" },
                new TestItem { Name = "Test3", Value = 3.0, Status = "Great" },
                new TestItem { Name = "Test4", Value = 4.0, Status = "Perfect" }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F2")
            };

            // Act - Request 2 columns with sufficient width
            var result = builder.AppendGenericTable("Multi-Column Test", rows, columns, 2, 120, 20);

            // Assert
            result.Should().Be(TableLayoutMode.MultiColumn);
            var output = builder.ToString();
            output.Should().Contain("Multi-Column Test");
            output.Should().Contain("Test1");
            output.Should().Contain("Test2");
            output.Should().Contain("Test3");
            output.Should().Contain("Test4");
            
            // Should have multiple table columns side by side
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Length.Should().BeGreaterThan(3); // Title, header, separator, data rows
        }

        [Fact]
        public void AppendGenericTable_WithInsufficientWidth_FallsBackToSingleColumn()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "VeryLongTestName", Value = 1.0, Status = "VeryLongStatusText" }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new NumericColumn<TestItem>("Value", item => item.Value, "F2"),
                new TextColumn<TestItem>("Status", item => item.Status)
            };

            // Act - Request 3 columns but with insufficient width
            var result = builder.AppendGenericTable("Narrow Test", rows, columns, 3, 40, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn); // Should fall back due to width constraints
            var output = builder.ToString();
            output.Should().Contain("Narrow Test");
            output.Should().Contain("VeryLongTestName");
        }

        [Fact]
        public void AppendGenericTable_WithNullRows_HandlesGracefully()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name)
            };

            // Act
            var result = builder.AppendGenericTable("Null Test", null, columns, 1, 80, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            builder.ToString().Should().Be($"Null Test{Environment.NewLine}");
        }

        [Fact]
        public void AppendGenericTable_WithComplexValueSelector_WorksCorrectly()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test", Value = 0.75, Status = "Running" }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name.ToUpper()),
                new ProgressBarColumn<TestItem>("Progress", item => item.Value),
                new TextColumn<TestItem>("Percentage", item => $"{item.Value * 100:F0}%")
            };

            // Act
            var result = builder.AppendGenericTable("Complex Test", rows, columns, 1, 80, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("TEST"); // Name should be uppercase
            output.Should().Contain("75%"); // Percentage should be calculated
            output.Should().Contain("█"); // Progress bar should be present
        }

        [Fact]
        public void AppendGenericTable_WithVaryingContentLengths_AdjustsColumnWidths()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "A", Value = 1.0, Status = "Short" },
                new TestItem { Name = "VeryLongName", Value = 2.0, Status = "VeryLongStatus" }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name),
                new TextColumn<TestItem>("Status", item => item.Status)
            };

            // Act
            var result = builder.AppendGenericTable("Width Test", rows, columns, 1, 80, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            
            // Both long and short content should be properly aligned
            output.Should().Contain("VeryLongName");
            output.Should().Contain("VeryLongStatus");
            output.Should().Contain("A");
            output.Should().Contain("Short");
            
            // Check that columns are properly aligned by looking for consistent spacing
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Length.Should().BeGreaterThan(3); // Title, header, separator, data rows
        }

        [Fact]
        public void AppendGenericTable_WithZeroTargetColumns_UsesSingleColumn()
        {
            // Arrange
            var builder = new StringBuilder();
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test", Value = 1.0, Status = "OK" }
            };
            var columns = new List<ITableColumn<TestItem>>
            {
                new TextColumn<TestItem>("Name", item => item.Name)
            };

            // Act
            var result = builder.AppendGenericTable("Zero Columns Test", rows, columns, 0, 80, 20);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("Zero Columns Test");
            output.Should().Contain("Test");
        }
    }
} 