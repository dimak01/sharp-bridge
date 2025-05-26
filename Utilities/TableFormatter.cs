using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Indicates the layout mode used by TableFormatter
    /// </summary>
    public enum TableLayoutMode
    {
        SingleColumn,
        MultiColumn
    }

    /// <summary>
    /// Static helper for formatting tabular data consistently across formatters
    /// LEGACY VERSION - kept for reference during refactoring
    /// </summary>
    public static class TableFormatterLegacy
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
            var columnPadding = 4; // Space between columns
            var availableWidth = consoleWidth - (columnPadding * (columnCount - 1));
            var widthPerColumn = availableWidth / columnCount;
            
            // Calculate optimal widths within each column
            // We need: nameWidth + 1 space + barWidth + 1 space + valueWidth = total content width
            var maxNameLength = rows.Max(r => r.Name.Length);
            var valueWidth = 6; // Fixed width for values like "0.25"
            var minBarWidth = 8; // Minimum bar width
            
            // Calculate name width (but don't let it dominate the column)
            var maxNameWidth = Math.Max(maxNameLength, "Expression".Length);
            var nameWidth = Math.Min(maxNameWidth, widthPerColumn / 3);
            
            // Calculate bar width (remaining space after name, value, and 2 spaces)
            var barWidth = Math.Max(minBarWidth, widthPerColumn - nameWidth - valueWidth - 2);
            
            // Recalculate actual content width to ensure it fits
            var actualContentWidth = nameWidth + 1 + barWidth + 1 + valueWidth;
            
            // If content is too wide, reduce bar width
            if (actualContentWidth > widthPerColumn)
            {
                barWidth = Math.Max(minBarWidth, widthPerColumn - nameWidth - valueWidth - 2);
                actualContentWidth = nameWidth + 1 + barWidth + 1 + valueWidth;
            }
            
            // Add column headers
            var headerBuilder = new StringBuilder();
            for (int colIndex = 0; colIndex < columnCount; colIndex++)
            {
                if (colIndex > 0)
                    headerBuilder.Append(new string(' ', columnPadding));
                
                var header = $"{"Expression".PadRight(nameWidth)} {"Progress".PadRight(barWidth)} {"Value".PadRight(valueWidth)}";
                headerBuilder.Append(header);
                
                // Add padding to reach full column width (only if not the last column)
                if (colIndex < columnCount - 1)
                {
                    var paddingNeeded = widthPerColumn - header.Length;
                    if (paddingNeeded > 0)
                        headerBuilder.Append(new string(' ', paddingNeeded));
                }
            }
            builder.AppendLine(headerBuilder.ToString().TrimEnd());
            
            // Add separator line
            var separatorBuilder = new StringBuilder();
            for (int colIndex = 0; colIndex < columnCount; colIndex++)
            {
                if (colIndex > 0)
                    separatorBuilder.Append(new string(' ', columnPadding));
                
                var separatorLength = nameWidth + 1 + barWidth + 1 + valueWidth; // Actual content width
                separatorBuilder.Append(new string('-', separatorLength));
                
                // Add padding to reach full column width (only if not the last column)
                if (colIndex < columnCount - 1)
                {
                    var paddingNeeded = widthPerColumn - separatorLength;
                    if (paddingNeeded > 0)
                        separatorBuilder.Append(new string(' ', paddingNeeded));
                }
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
                        
                        // Format with consistent spacing: name + space + bar + space + value
                        var columnContent = $"{name.PadRight(nameWidth)} {bar.PadRight(barWidth)} {value.PadRight(valueWidth)}";
                        lineBuilder.Append(columnContent);
                        
                        // Add padding to reach full column width (only if not the last column)
                        if (colIndex < columnCount - 1)
                        {
                            var paddingNeeded = widthPerColumn - columnContent.Length;
                            if (paddingNeeded > 0)
                                lineBuilder.Append(new string(' ', paddingNeeded));
                        }
                    }
                    else
                    {
                        // Empty cell - pad to column width (only if not the last column)
                        if (colIndex < columnCount - 1)
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

    /// <summary>
    /// Static helper for formatting tabular data consistently across formatters
    /// NEW SIMPLIFIED VERSION
    /// </summary>
    public static class TableFormatter
    {
        /// <summary>
        /// Appends a formatted table to a StringBuilder
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows as (Name, Bar, Value) tuples</param>
        /// <returns>The layout mode used (SingleColumn or MultiColumn)</returns>
        public static TableLayoutMode AppendTable(StringBuilder builder, string title, 
            IEnumerable<(string Name, string Bar, string Value)> rows)
        {
            return AppendTable(builder, title, rows, 1, 80);
        }
        
        /// <summary>
        /// Appends a formatted multi-column table to a StringBuilder
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows as (Name, Bar, Value) tuples</param>
        /// <param name="columnCount">Number of side-by-side columns to create</param>
        /// <param name="consoleWidth">Available console width</param>
        /// <returns>The layout mode used (SingleColumn or MultiColumn)</returns>
        public static TableLayoutMode AppendTable(StringBuilder builder, string title, 
            IEnumerable<(string Name, string Bar, string Value)> rows, int columnCount, int consoleWidth)
        {
            return AppendTable(builder, title, rows, columnCount, consoleWidth, null);
        }
        
        /// <summary>
        /// Appends a formatted multi-column table to a StringBuilder with original values for progress bars
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows as (Name, Bar, Value) tuples</param>
        /// <param name="columnCount">Number of side-by-side columns to create</param>
        /// <param name="consoleWidth">Available console width</param>
        /// <param name="originalValues">Original numeric values for recreating progress bars</param>
        /// <returns>The layout mode used (SingleColumn or MultiColumn)</returns>
        public static TableLayoutMode AppendTable(StringBuilder builder, string title, 
            IEnumerable<(string Name, string Bar, string Value)> rows, int columnCount, int consoleWidth,
            IEnumerable<double> originalValues)
        {
            var rowList = rows.ToList();
            if (!rowList.Any()) return TableLayoutMode.SingleColumn;
            
            // Add title
            builder.AppendLine(title);
            
            // Single column or fallback case
            if (columnCount <= 1)
            {
                AppendSingleColumnTable(builder, rowList);
                return TableLayoutMode.SingleColumn;
            }
            
            // Try multi-column layout
            if (TryAppendMultiColumnTable(builder, rowList, columnCount, consoleWidth, originalValues?.ToList()))
            {
                return TableLayoutMode.MultiColumn; // Success!
            }

            // Fallback to single column
            var singleColumnHeight = (int)Math.Ceiling((double)rowList.Count / columnCount);
            AppendSingleColumnTable(builder, rowList.Take(singleColumnHeight).ToList());
            return TableLayoutMode.SingleColumn;
        }
        
        /// <summary>
        /// Appends a single-column table
        /// </summary>
        private static void AppendSingleColumnTable(StringBuilder builder, List<(string Name, string Bar, string Value)> rows)
        {
            // Calculate column widths
            var nameWidth = Math.Max(rows.Max(r => r.Name.Length), "Expression".Length) + 2;
            var barWidth = 20; // Fixed width for progress bars
            var valueWidth = Math.Max(rows.Max(r => r.Value.Length), "Value".Length);
            
            // Add header row
            builder.AppendLine($"{"Expression".PadRight(nameWidth)} {"Progress".PadRight(barWidth)} {"Value".PadRight(valueWidth)}");
            
            // Add separator line
            var separatorLength = nameWidth + barWidth + valueWidth + 2; // +2 for spaces
            builder.AppendLine(new string('-', separatorLength));
            
            // Add data rows
            foreach (var (name, bar, value) in rows)
            {
                builder.AppendLine($"{name.PadRight(nameWidth)} {bar.PadRight(barWidth)} {value.PadRight(valueWidth)}");
            }
        }
        
        /// <summary>
        /// Attempts to append a multi-column table. Returns true if successful, false if fallback needed.
        /// </summary>
        private static bool TryAppendMultiColumnTable(StringBuilder builder, List<(string Name, string Bar, string Value)> rows, 
            int columnCount, int consoleWidth, List<double> originalValues)
        {
            // Global width calculation - analyze ALL rows first
            var maxNameWidth = Math.Max(rows.Max(r => r.Name.Length), "Expression".Length);
            var maxValueWidth = Math.Max(rows.Max(r => r.Value.Length), "Value".Length);
            var minBarWidth = 8; // Minimum acceptable bar width
            var columnPadding = 4; // Space between columns
            
            // Calculate available width per column
            var totalPadding = columnPadding * (columnCount - 1);
            var availableWidth = consoleWidth - totalPadding;
            var widthPerColumn = availableWidth / columnCount;
            
            // Check if content can fit
            var requiredWidth = maxNameWidth + 1 + minBarWidth + 1 + maxValueWidth; // +2 for spaces
            if (requiredWidth > widthPerColumn)
            {
                return false; // Cannot fit, need fallback
            }
            
            // Calculate actual bar width (remaining space after name, value, and spaces)
            var actualBarWidth = widthPerColumn - maxNameWidth - maxValueWidth - 2;
            
            // Build headers
            AppendMultiColumnHeaders(builder, columnCount, maxNameWidth, actualBarWidth, maxValueWidth, columnPadding);
            
            // Split rows into columns
            var rowsPerColumn = (int)Math.Ceiling((double)rows.Count / columnCount);
            var columnData = new List<List<(string Name, string Bar, string Value)>>();
            
            for (int col = 0; col < columnCount; col++)
            {
                var startIndex = col * rowsPerColumn;
                var endIndex = Math.Min(startIndex + rowsPerColumn, rows.Count);
                var columnRows = rows.Skip(startIndex).Take(endIndex - startIndex).ToList();
                columnData.Add(columnRows);
            }
            
            // Build data rows
            var maxRowsInAnyColumn = columnData.Max(col => col.Count);
            for (int rowIndex = 0; rowIndex < maxRowsInAnyColumn; rowIndex++)
            {
                AppendMultiColumnDataRow(builder, columnData, rowIndex, columnCount, 
                    maxNameWidth, actualBarWidth, maxValueWidth, columnPadding, originalValues);
            }
            
            return true; // Success
        }
        
        /// <summary>
        /// Appends the header rows for multi-column layout
        /// </summary>
        private static void AppendMultiColumnHeaders(StringBuilder builder, int columnCount, 
            int nameWidth, int barWidth, int valueWidth, int columnPadding)
        {
            // Header row
            var headerBuilder = new StringBuilder();
            for (int col = 0; col < columnCount; col++)
            {
                if (col > 0) headerBuilder.Append(new string(' ', columnPadding));
                headerBuilder.Append($"{"Expression".PadRight(nameWidth)} {"Progress".PadRight(barWidth)} {"Value".PadRight(valueWidth)}");
            }
            builder.AppendLine(headerBuilder.ToString());
            
            // Separator row
            var separatorBuilder = new StringBuilder();
            for (int col = 0; col < columnCount; col++)
            {
                if (col > 0) separatorBuilder.Append(new string(' ', columnPadding));
                var separatorLength = nameWidth + 1 + barWidth + 1 + valueWidth;
                separatorBuilder.Append(new string('-', separatorLength));
            }
            builder.AppendLine(separatorBuilder.ToString());
        }
        
        /// <summary>
        /// Appends a single data row across all columns
        /// </summary>
        private static void AppendMultiColumnDataRow(StringBuilder builder, 
            List<List<(string Name, string Bar, string Value)>> columnData, int rowIndex, int columnCount,
            int nameWidth, int barWidth, int valueWidth, int columnPadding, List<double> originalValues)
        {
            var lineBuilder = new StringBuilder();
            
            for (int col = 0; col < columnCount; col++)
            {
                if (col > 0) lineBuilder.Append(new string(' ', columnPadding));
                
                if (rowIndex < columnData[col].Count)
                {
                    var (name, _, value) = columnData[col][rowIndex];
                    
                    // Calculate the original row index to get the correct original value
                    var rowsPerColumn = (int)Math.Ceiling((double)columnData.Sum(c => c.Count) / columnCount);
                    var originalRowIndex = col * rowsPerColumn + rowIndex;
                    
                    // Get the original value for recreating the progress bar
                    var originalValue = originalValues != null && originalRowIndex < originalValues.Count 
                        ? originalValues[originalRowIndex] 
                        : 0.5; // Fallback value
                    
                    // Recreate bar with the calculated width and original value
                    var bar = CreateProgressBar(originalValue, barWidth);
                    lineBuilder.Append($"{name.PadRight(nameWidth)} {bar.PadRight(barWidth)} {value.PadRight(valueWidth)}");
                }
                else
                {
                    // Empty cell - pad to match column width
                    var emptyContent = new string(' ', nameWidth + 1 + barWidth + 1 + valueWidth);
                    lineBuilder.Append(emptyContent);
                }
            }
            
            builder.AppendLine(lineBuilder.ToString());
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