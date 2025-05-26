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
    /// </summary>
    public static class TableFormatter
    {
        /// <summary>
        /// Appends a formatted multi-column table to a StringBuilder
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows as (Name, ProgressValue, DisplayValue) tuples</param>
        /// <param name="columnCount">Number of side-by-side columns to create</param>
        /// <param name="consoleWidth">Available console width</param>
        /// <param name="singleColumnBarWidth">Bar width to use in single-column mode (default 20)</param>
        /// <param name="singleColumnMaxItems">Maximum items to show in single-column mode (default: show all)</param>
        /// <returns>The layout mode used (SingleColumn or MultiColumn)</returns>
        public static TableLayoutMode AppendTable(StringBuilder builder, string title, 
            IEnumerable<(string Name, double ProgressValue, string DisplayValue)> rows, int columnCount, int consoleWidth,
            int singleColumnBarWidth = 20, int? singleColumnMaxItems = null)
        {
            var rowList = rows.ToList();
            if (!rowList.Any()) return TableLayoutMode.SingleColumn;
            
            // Add title
            builder.AppendLine(title);
            
            // Single column or fallback case
            if (columnCount <= 1)
            {
                var singleColumnRows = singleColumnMaxItems.HasValue 
                    ? rowList.Take(singleColumnMaxItems.Value).ToList() 
                    : rowList;
                AppendSingleColumnTable(builder, singleColumnRows, singleColumnBarWidth);
                return TableLayoutMode.SingleColumn;
            }
            
            // Try multi-column layout
            if (TryAppendMultiColumnTable(builder, rowList, columnCount, consoleWidth))
            {
                return TableLayoutMode.MultiColumn; // Success!
            }

            // Fallback to single column
            var fallbackRows = singleColumnMaxItems.HasValue 
                ? rowList.Take(singleColumnMaxItems.Value).ToList() 
                : rowList;
            AppendSingleColumnTable(builder, fallbackRows, singleColumnBarWidth);
            return TableLayoutMode.SingleColumn;
        }
        
        /// <summary>
        /// Appends a single-column table
        /// </summary>
        private static void AppendSingleColumnTable(StringBuilder builder, List<(string Name, double ProgressValue, string DisplayValue)> rows, int barWidth)
        {
            // Calculate column widths
            var nameWidth = Math.Max(rows.Max(r => r.Name.Length), "Expression".Length) + 2;
            var valueWidth = Math.Max(rows.Max(r => r.DisplayValue.Length), "Value".Length);
            
            // Add header row
            builder.AppendLine($"{"Expression".PadRight(nameWidth)} {"Progress".PadRight(barWidth)} {"Value".PadRight(valueWidth)}");
            
            // Add separator line
            var separatorLength = nameWidth + barWidth + valueWidth + 2; // +2 for spaces
            builder.AppendLine(new string('-', separatorLength));
            
            // Add data rows
            foreach (var (name, progressValue, displayValue) in rows)
            {
                var bar = CreateProgressBar(progressValue, barWidth);
                builder.AppendLine($"{name.PadRight(nameWidth)} {bar.PadRight(barWidth)} {displayValue.PadRight(valueWidth)}");
            }
        }
        
        /// <summary>
        /// Attempts to append a multi-column table. Returns true if successful, false if fallback needed.
        /// </summary>
        private static bool TryAppendMultiColumnTable(StringBuilder builder, List<(string Name, double ProgressValue, string DisplayValue)> rows, 
            int columnCount, int consoleWidth)
        {
            // Global width calculation - analyze ALL rows first
            var maxNameWidth = Math.Max(rows.Max(r => r.Name.Length), "Expression".Length);
            var maxValueWidth = Math.Max(rows.Max(r => r.DisplayValue.Length), "Value".Length);
            var minBarWidth = 6; // Minimum acceptable bar width
            var columnPadding = 3; // Space between columns
            
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
            var columnData = new List<List<(string Name, double ProgressValue, string DisplayValue)>>();
            
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
                    maxNameWidth, actualBarWidth, maxValueWidth, columnPadding);
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
            List<List<(string Name, double ProgressValue, string DisplayValue)>> columnData, int rowIndex, int columnCount,
            int nameWidth, int barWidth, int valueWidth, int columnPadding)
        {
            var lineBuilder = new StringBuilder();
            
            for (int col = 0; col < columnCount; col++)
            {
                if (col > 0) lineBuilder.Append(new string(' ', columnPadding));
                
                if (rowIndex < columnData[col].Count)
                {
                    var (name, progressValue, displayValue) = columnData[col][rowIndex];
                    
                    // Create bar with the calculated width and progress value
                    var bar = CreateProgressBar(progressValue, barWidth);
                    lineBuilder.Append($"{name.PadRight(nameWidth)} {bar.PadRight(barWidth)} {displayValue.PadRight(valueWidth)}");
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