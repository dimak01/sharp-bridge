using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Static helper for formatting tabular data consistently across formatters
    /// </summary>
    public static class TableFormatter
    {
        /// <summary>
        /// Appends a formatted table to a StringBuilder
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows as (Name, Bar, Value) tuples</param>
        public static void AppendTable(StringBuilder builder, string title, 
            IEnumerable<(string Name, string Bar, string Value)> rows)
        {
            AppendTable(builder, title, rows, 1, 80); // Default to single column, 80 char width
        }
        
        /// <summary>
        /// Appends a formatted multi-column table to a StringBuilder
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows as (Name, Bar, Value) tuples</param>
        /// <param name="columnCount">Number of side-by-side columns to create</param>
        /// <param name="consoleWidth">Available console width</param>
        public static void AppendTable(StringBuilder builder, string title, 
            IEnumerable<(string Name, string Bar, string Value)> rows, int columnCount, int consoleWidth)
        {
            AppendTable(builder, title, rows, columnCount, consoleWidth, null);
        }
        
        /// <summary>
        /// Appends a formatted multi-column table to a StringBuilder with original values for bar recreation
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows as (Name, Bar, Value) tuples</param>
        /// <param name="columnCount">Number of side-by-side columns to create</param>
        /// <param name="consoleWidth">Available console width</param>
        /// <param name="originalValues">Original numeric values for recreating progress bars (optional)</param>
        public static void AppendTable(StringBuilder builder, string title, 
            IEnumerable<(string Name, string Bar, string Value)> rows, int columnCount, int consoleWidth,
            IEnumerable<double> originalValues)
        {
            var rowList = rows.ToList();
            if (!rowList.Any()) return;
            
            // Add title
            builder.AppendLine(title);
            
            if (columnCount <= 1)
            {
                // Single column - use original logic
                AppendSingleColumnTable(builder, rowList);
                return;
            }
            
            // Multi-column layout
            var valuesList = originalValues?.ToList();
            AppendMultiColumnTable(builder, rowList, columnCount, consoleWidth, valuesList);
        }
        
        /// <summary>
        /// Appends a single-column table (original behavior)
        /// </summary>
        private static void AppendSingleColumnTable(StringBuilder builder, List<(string Name, string Bar, string Value)> rows)
        {
            // Calculate column widths
            var nameWidth = Math.Max(rows.Max(r => r.Name.Length), "Expression".Length) + 2;
            var barWidth = 20; // Fixed width for progress bars
            var valueWidth = 6; // Fixed width for values
            
            // Add header row
            builder.AppendLine($"{"Expression".PadRight(nameWidth)} {"Progress".PadRight(barWidth)} Value");
            
            // Add separator line
            var separatorLength = nameWidth + barWidth + valueWidth + 2; // +2 for spaces
            builder.AppendLine(new string('-', separatorLength));
            
            // Add data rows
            foreach (var (name, bar, value) in rows)
            {
                builder.AppendLine($"{name.PadRight(nameWidth)} {bar.PadRight(barWidth)} {value}");
            }
        }
        
        /// <summary>
        /// Appends a multi-column table layout
        /// </summary>
        private static void AppendMultiColumnTable(StringBuilder builder, List<(string Name, string Bar, string Value)> rows, 
            int columnCount, int consoleWidth, List<double> originalValues)
        {
            // Calculate available width per column (with some padding between columns)
            var columnPadding = 4; // Space between columns (increased for better readability)
            var availableWidth = consoleWidth - (columnPadding * (columnCount - 1));
            var widthPerColumn = availableWidth / columnCount;
            
            // Calculate optimal widths within each column
            var maxNameLength = rows.Max(r => r.Name.Length);
            var nameWidth = Math.Min(maxNameLength + 1, widthPerColumn / 3); // Don't let names take more than 1/3 of column
            var valueWidth = 6; // Fixed width for values like "0.25"
            var barWidth = Math.Max(8, widthPerColumn - nameWidth - valueWidth - 2); // Remaining space for bars, minimum 8
            
            // Add column headers
            var headerBuilder = new StringBuilder();
            for (int colIndex = 0; colIndex < columnCount; colIndex++)
            {
                if (colIndex > 0)
                    headerBuilder.Append(new string(' ', columnPadding));
                
                var header = $"{"Expression".PadRight(nameWidth)} {"Progress".PadRight(barWidth)} Value";
                headerBuilder.Append(header.PadRight(widthPerColumn));
            }
            builder.AppendLine(headerBuilder.ToString().TrimEnd());
            
            // Add separator line
            var separatorBuilder = new StringBuilder();
            for (int colIndex = 0; colIndex < columnCount; colIndex++)
            {
                if (colIndex > 0)
                    separatorBuilder.Append(new string(' ', columnPadding));
                
                var separatorLength = nameWidth + barWidth + valueWidth + 1; // +1 for space between bar and value
                separatorBuilder.Append(new string('-', separatorLength).PadRight(widthPerColumn));
            }
            builder.AppendLine(separatorBuilder.ToString().TrimEnd());
            
            // Split rows into columns
            var rowsPerColumn = (int)Math.Ceiling((double)rows.Count / columnCount);
            var columnData = new List<List<(string Name, string Bar, string Value, double OriginalValue)>>();
            
            for (int col = 0; col < columnCount; col++)
            {
                var startIndex = col * rowsPerColumn;
                var endIndex = Math.Min(startIndex + rowsPerColumn, rows.Count);
                var columnRows = new List<(string Name, string Bar, string Value, double OriginalValue)>();
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    var row = rows[i];
                    var originalValue = originalValues != null && i < originalValues.Count ? originalValues[i] : 0.0;
                    columnRows.Add((row.Name, row.Bar, row.Value, originalValue));
                }
                
                columnData.Add(columnRows);
            }
            
            // Find the maximum number of rows in any column
            var maxRowsInAnyColumn = columnData.Max(col => col.Count);
            
            // Build the multi-column output
            for (int rowIndex = 0; rowIndex < maxRowsInAnyColumn; rowIndex++)
            {
                var lineBuilder = new StringBuilder();
                
                for (int colIndex = 0; colIndex < columnCount; colIndex++)
                {
                    if (colIndex > 0)
                        lineBuilder.Append(new string(' ', columnPadding)); // Add padding between columns
                    
                    if (rowIndex < columnData[colIndex].Count)
                    {
                        var (name, _, value, originalValue) = columnData[colIndex][rowIndex];
                        // Recreate bar with the calculated width for this layout
                        var bar = CreateProgressBar(originalValue, barWidth);
                        
                        var columnContent = $"{name.PadRight(nameWidth)} {bar.PadRight(barWidth)} {value}";
                        lineBuilder.Append(columnContent.PadRight(widthPerColumn));
                    }
                    else
                    {
                        // Empty cell - pad to column width
                        lineBuilder.Append(new string(' ', widthPerColumn));
                    }
                }
                
                builder.AppendLine(lineBuilder.ToString().TrimEnd());
            }
        }
        
        /// <summary>
        /// Creates a progress bar visualization for a value between 0 and 1
        /// </summary>
        /// <param name="value">Value between 0 and 1</param>
        /// <param name="width">Width of the progress bar (default 20)</param>
        /// <returns>Progress bar string</returns>
        public static string CreateProgressBar(double value, int width = 20)
        {
            var clampedValue = Math.Max(0, Math.Min(1, value)); // Clamp between 0 and 1
            var barLength = (int)(clampedValue * width);
            return new string('█', barLength) + new string('░', width - barLength);
        }
        
        /// <summary>
        /// Creates a progress bar visualization for a value within a custom range
        /// </summary>
        /// <param name="value">Current value</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <param name="width">Width of the progress bar (default 20)</param>
        /// <returns>Progress bar string</returns>
        public static string CreateProgressBar(double value, double min, double max, int width = 20)
        {
            if (max <= min) return new string('░', width); // Invalid range
            
            var normalizedValue = (value - min) / (max - min);
            return CreateProgressBar(normalizedValue, width);
        }
    }
} 