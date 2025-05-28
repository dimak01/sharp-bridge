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
            public string Name { get; set; }
            public double Value { get; set; }
            public string Status { get; set; }
        }

        private class SimpleItem
        {
            public string Label { get; set; }
            public int Number { get; set; }
        }

        [Fact]
        public void AppendGenericTable_WithEmptyRows_ReturnsEmptyTable()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<TestItem>>
            {
                new TableColumn<TestItem> { Header = "Name", ValueSelector = (item, width) => item.Name },
                new TableColumn<TestItem> { Header = "Value", ValueSelector = (item, width) => item.Value.ToString("F2") }
            };
            var emptyRows = new List<TestItem>();

            // Act
            var result = builder.AppendGenericTable("Test Table", emptyRows, columns, 1, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            builder.ToString().Should().BeEmpty();
        }

        [Fact]
        public void AppendGenericTable_WithSingleColumn_ShowsCorrectLayout()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<TestItem>>
            {
                new TableColumn<TestItem> { Header = "Name", ValueSelector = (item, width) => item.Name },
                new TableColumn<TestItem> { Header = "Value", ValueSelector = (item, width) => item.Value.ToString("F2") },
                new TableColumn<TestItem> { Header = "Status", ValueSelector = (item, width) => item.Status }
            };
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Item1", Value = 1.23, Status = "Active" },
                new TestItem { Name = "LongerItemName", Value = 45.67, Status = "Inactive" }
            };

            // Act
            var result = builder.AppendGenericTable("Test Table", rows, columns, 1, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            // Should have title, header, separator, and 2 data rows
            lines.Should().HaveCount(5);
            lines[0].Should().Be("Test Table");
            lines[1].Should().Contain("Name").And.Contain("Value").And.Contain("Status");
            lines[2].Should().Match("*-*"); // Separator line
            lines[3].Should().Contain("Item1").And.Contain("1.23").And.Contain("Active");
            lines[4].Should().Contain("LongerItemName").And.Contain("45.67").And.Contain("Inactive");
        }

        [Fact]
        public void AppendGenericTable_WithProgressBarColumn_ShowsProgressBars()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<TestItem>>
            {
                new TableColumn<TestItem> { Header = "Name", ValueSelector = (item, width) => item.Name },
                new TableColumn<TestItem> { Header = "Progress", ValueSelector = (item, width) => TableFormatter.CreateProgressBar(item.Value, 10) },
                new TableColumn<TestItem> { Header = "Value", ValueSelector = (item, width) => item.Value.ToString("F2") }
            };
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test1", Value = 0.0, Status = "Start" },
                new TestItem { Name = "Test2", Value = 0.5, Status = "Middle" },
                new TestItem { Name = "Test3", Value = 1.0, Status = "End" }
            };

            // Act
            var result = builder.AppendGenericTable("Progress Test", rows, columns, 1, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            lines[3].Should().Contain("░░░░░░░░░░"); // Empty progress bar for 0.0
            lines[4].Should().Contain("█████░░░░░"); // Half progress bar for 0.5
            lines[5].Should().Contain("██████████"); // Full progress bar for 1.0
        }

        [Fact]
        public void AppendGenericTable_WithDynamicWidthColumn_AdjustsToWidth()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<TestItem>>
            {
                new TableColumn<TestItem> { Header = "Name", ValueSelector = (item, width) => item.Name },
                new TableColumn<TestItem> 
                { 
                    Header = "Dynamic", 
                    ValueSelector = (item, width) => TableFormatter.CreateProgressBar(item.Value, width - 5) // Use available width minus padding
                }
            };
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test", Value = 0.5, Status = "Active" }
            };

            // Act
            var result = builder.AppendGenericTable("Dynamic Width Test", rows, columns, 1, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("█").And.Contain("░"); // Should contain progress bar characters
        }

        [Fact]
        public void AppendGenericTable_WithSingleColumnMaxItems_LimitsRows()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<SimpleItem>>
            {
                new TableColumn<SimpleItem> { Header = "Label", ValueSelector = (item, width) => item.Label },
                new TableColumn<SimpleItem> { Header = "Number", ValueSelector = (item, width) => item.Number.ToString() }
            };
            var rows = new List<SimpleItem>
            {
                new SimpleItem { Label = "Item1", Number = 1 },
                new SimpleItem { Label = "Item2", Number = 2 },
                new SimpleItem { Label = "Item3", Number = 3 },
                new SimpleItem { Label = "Item4", Number = 4 },
                new SimpleItem { Label = "Item5", Number = 5 }
            };

            // Act
            var result = builder.AppendGenericTable("Limited Table", rows, columns, 1, 80, singleColumnMaxItems: 3);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            // Should have title, header, separator, and only 3 data rows (not 5)
            lines.Should().HaveCount(6);
            lines[3].Should().Contain("Item1");
            lines[4].Should().Contain("Item2");
            lines[5].Should().Contain("Item3");
            output.Should().NotContain("Item4").And.NotContain("Item5");
        }

        [Fact]
        public void AppendGenericTable_WithMultipleColumns_FallsBackToSingleColumn()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<TestItem>>
            {
                new TableColumn<TestItem> { Header = "Name", ValueSelector = (item, width) => item.Name },
                new TableColumn<TestItem> { Header = "Value", ValueSelector = (item, width) => item.Value.ToString("F2") }
            };
            var rows = new List<TestItem>
            {
                new TestItem { Name = "Test1", Value = 1.23, Status = "Active" },
                new TestItem { Name = "Test2", Value = 4.56, Status = "Inactive" }
            };

            // Act - Request 2 columns but should fall back to single column (multi-column not implemented yet)
            var result = builder.AppendGenericTable("Multi-Column Test", rows, columns, 2, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn); // Should fall back
            var output = builder.ToString();
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            // Should still show all data in single column format
            lines[0].Should().Be("Multi-Column Test");
            lines.Should().Contain(line => line.Contains("Test1") && line.Contains("1.23"));
            lines.Should().Contain(line => line.Contains("Test2") && line.Contains("4.56"));
        }

        [Fact]
        public void AppendGenericTable_WithNullRows_HandlesGracefully()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<TestItem>>
            {
                new TableColumn<TestItem> { Header = "Name", ValueSelector = (item, width) => item.Name }
            };

            // Act
            var result = builder.AppendGenericTable("Null Test", null, columns, 1, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            builder.ToString().Should().BeEmpty();
        }

        [Fact]
        public void AppendGenericTable_WithComplexValueSelector_WorksCorrectly()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<TestItem>>
            {
                new TableColumn<TestItem> 
                { 
                    Header = "Name", 
                    ValueSelector = (item, width) => item.Name.ToUpper() 
                },
                new TableColumn<TestItem> 
                { 
                    Header = "Formatted", 
                    ValueSelector = (item, width) => $"{item.Status}: {item.Value:P0}" // Percentage format
                }
            };
            var rows = new List<TestItem>
            {
                new TestItem { Name = "test", Value = 0.75, Status = "Good" }
            };

            // Act
            var result = builder.AppendGenericTable("Complex Test", rows, columns, 1, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("TEST"); // Name should be uppercase
            output.Should().Contain("Good: 75%"); // Should show formatted percentage
        }

        [Fact]
        public void AppendGenericTable_WithVaryingContentLengths_AdjustsColumnWidths()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<TestItem>>
            {
                new TableColumn<TestItem> { Header = "Short", ValueSelector = (item, width) => item.Name },
                new TableColumn<TestItem> { Header = "VeryLongHeaderName", ValueSelector = (item, width) => item.Status }
            };
            var rows = new List<TestItem>
            {
                new TestItem { Name = "A", Status = "VeryLongStatusValue" },
                new TestItem { Name = "VeryLongName", Status = "B" }
            };

            // Act
            var result = builder.AppendGenericTable("Width Test", rows, columns, 1, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            // Header should be properly spaced
            var headerLine = lines[1];
            headerLine.Should().Contain("Short").And.Contain("VeryLongHeaderName");
            
            // Data should be properly aligned
            lines.Should().Contain(line => line.Contains("A") && line.Contains("VeryLongStatusValue"));
            lines.Should().Contain(line => line.Contains("VeryLongName") && line.Contains("B"));
        }

        [Fact]
        public void AppendGenericTable_WithZeroTargetColumns_UsesSingleColumn()
        {
            // Arrange
            var builder = new StringBuilder();
            var columns = new List<TableColumn<SimpleItem>>
            {
                new TableColumn<SimpleItem> { Header = "Label", ValueSelector = (item, width) => item.Label }
            };
            var rows = new List<SimpleItem>
            {
                new SimpleItem { Label = "Test", Number = 1 }
            };

            // Act
            var result = builder.AppendGenericTable("Zero Columns Test", rows, columns, 0, 80);

            // Assert
            result.Should().Be(TableLayoutMode.SingleColumn);
            var output = builder.ToString();
            output.Should().Contain("Zero Columns Test").And.Contain("Test");
        }
    }
} 