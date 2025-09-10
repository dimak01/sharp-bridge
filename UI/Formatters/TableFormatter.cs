using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.UI.Formatters;
using SharpBridge.UI.Utilities;

namespace SharpBridge.UI.Formatters
{
    /// <summary>
    /// Default implementation of table formatting functionality
    /// </summary>
    public class TableFormatter : ITableFormatter
    {
        /// <summary>
        /// Appends a formatted table using generic column definitions
        /// </summary>
        /// <typeparam name="T">The type of data being displayed</typeparam>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows data</param>
        /// <param name="columns">Column definitions</param>
        /// <param name="targetColumnCount">Number of side-by-side table columns to create</param>
        /// <param name="consoleWidth">Available console width</param>
        /// <param name="singleColumnBarWidth">Progress bar width for single-column mode (default 20)</param>
        /// <param name="singleColumnMaxItems">Maximum items to show in single-column mode (default: show all)</param>
        public void AppendTable<T>(StringBuilder builder, string title,
            IEnumerable<T> rows, IList<ITableColumnFormatter<T>> columns, int targetColumnCount, int consoleWidth,
            int singleColumnBarWidth = 20, int? singleColumnMaxItems = null)
        {
            AppendTable(builder, title, rows, columns, targetColumnCount, consoleWidth, singleColumnBarWidth, singleColumnMaxItems, 0);
        }

        /// <summary>
        /// Appends a formatted table using generic column definitions with indentation
        /// </summary>
        /// <typeparam name="T">The type of data being displayed</typeparam>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="title">Table title/header</param>
        /// <param name="rows">Table rows data</param>
        /// <param name="columns">Column definitions</param>
        /// <param name="targetColumnCount">Number of side-by-side table columns to create</param>
        /// <param name="consoleWidth">Available console width</param>
        /// <param name="singleColumnBarWidth">Progress bar width for single-column mode (default 20)</param>
        /// <param name="singleColumnMaxItems">Maximum items to show in single-column mode (default: show all)</param>
        /// <param name="indent">Number of spaces to indent the entire table</param>
        public void AppendTable<T>(StringBuilder builder, string title,
            IEnumerable<T> rows, IList<ITableColumnFormatter<T>> columns, int targetColumnCount, int consoleWidth,
            int singleColumnBarWidth, int? singleColumnMaxItems, int indent)
        {
            // Add title first, regardless of whether we have rows
            AppendIndentedLine(builder, title, indent);

            var rowList = rows?.ToList() ?? new List<T>();
            if (!rowList.Any()) return;

            int itemsDisplayed;

            // Single column or fallback case
            if (targetColumnCount <= 1)
            {
                var singleColumnRows = singleColumnMaxItems.HasValue
                    ? rowList.Take(singleColumnMaxItems.Value).ToList()
                    : rowList;
                AppendSingleColumnTable(builder, singleColumnRows, columns, singleColumnBarWidth, indent);
                itemsDisplayed = singleColumnRows.Count;
            }
            else
            {
                // Try multi-column layout
                if (TryAppendMultiColumnTable(builder, rowList, columns, targetColumnCount, consoleWidth, indent))
                {
                    itemsDisplayed = rowList.Count; // Multi-column shows all items
                }
                else
                {
                    // Fallback to single column
                    var fallbackRows = singleColumnMaxItems.HasValue
                        ? rowList.Take(singleColumnMaxItems.Value).ToList()
                        : rowList;
                    AppendSingleColumnTable(builder, fallbackRows, columns, singleColumnBarWidth, indent);
                    itemsDisplayed = fallbackRows.Count;
                }
            }

            // Add "more items" message if items were truncated
            if (rowList.Count > itemsDisplayed)
            {
                AppendIndentedLine(builder, $"  ... and {rowList.Count - itemsDisplayed} more", indent);
            }
        }

        /// <summary>
        /// Appends a line with the specified indentation
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="line">Line content to append</param>
        /// <param name="indent">Number of spaces to indent</param>
        private static void AppendIndentedLine(StringBuilder builder, string line, int indent)
        {
            if (indent > 0)
            {
                builder.Append(new string(' ', indent));
            }
            builder.AppendLine(line);
        }

        /// <summary>
        /// Appends a single-column table using strongly-typed column definitions
        /// </summary>
        private static void AppendSingleColumnTable<T>(StringBuilder builder, List<T> rows, IList<ITableColumnFormatter<T>> columns, int barWidth, int indent = 0)
        {
            if (!rows.Any() || !columns.Any()) return;

            // Calculate column widths using the new clean approach
            var columnWidths = new int[columns.Count];

            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var headerWidth = ConsoleColors.GetVisualLength(column.Header);
                var minWidth = column.MinWidth;

                // Use ValueFormatter to get natural content width (accounting for ANSI sequences)
                var maxContentWidth = rows.Max(r => ConsoleColors.GetVisualLength(column.ValueFormatter(r)));

                var naturalWidth = Math.Max(Math.Max(headerWidth, minWidth), maxContentWidth);

                // Apply MaxWidth if specified
                columnWidths[i] = column.MaxWidth.HasValue
                    ? Math.Min(naturalWidth, column.MaxWidth.Value)
                    : naturalWidth;
            }

            // Add header row using column's FormatHeader method
            var headerParts = columns.Select((c, i) => c.FormatHeader(columnWidths[i]));
            AppendIndentedLine(builder, string.Join(" ", headerParts), indent);

            // Add separator line
            var separatorLength = columnWidths.Sum() + (columns.Count - 1);
            AppendIndentedLine(builder, new string('-', separatorLength), indent);

            // Add data rows using column's FormatCell method
            foreach (var row in rows)
            {
                var cellParts = columns.Select((c, i) => c.FormatCell(row, columnWidths[i]));
                AppendIndentedLine(builder, string.Join(" ", cellParts), indent);
            }
        }

        /// <summary>
        /// Attempts to append a multi-column table using generic column definitions
        /// </summary>
        private static bool TryAppendMultiColumnTable<T>(StringBuilder builder, List<T> rows,
            IList<ITableColumnFormatter<T>> columns, int targetColumnCount, int consoleWidth, int indent = 0)
        {
            if (!rows.Any() || !columns.Any()) return false;

            // Calculate column widths using the new clean approach
            var columnWidths = new int[columns.Count];
            var totalContentWidth = 0;

            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var headerWidth = ConsoleColors.GetVisualLength(column.Header);
                var minWidth = column.MinWidth;

                // Use ValueFormatter to get natural content width (accounting for ANSI sequences)
                var maxContentWidth = rows.Max(r => ConsoleColors.GetVisualLength(column.ValueFormatter(r)));

                var naturalWidth = Math.Max(Math.Max(headerWidth, minWidth), maxContentWidth);

                // Apply MaxWidth if specified
                columnWidths[i] = column.MaxWidth.HasValue
                    ? Math.Min(naturalWidth, column.MaxWidth.Value)
                    : naturalWidth;

                totalContentWidth += columnWidths[i];
            }

            // Add spaces between columns within each table column
            var spacesPerTableColumn = columns.Count - 1;
            var minWidthPerTableColumn = totalContentWidth + spacesPerTableColumn;

            // Calculate available width per table column
            var columnPadding = 3; // Space between table columns
            var totalPadding = columnPadding * (targetColumnCount - 1);
            var availableWidth = consoleWidth - totalPadding;
            var widthPerTableColumn = availableWidth / targetColumnCount;

            // Check if content can fit
            if (minWidthPerTableColumn > widthPerTableColumn)
            {
                return false; // Cannot fit, need fallback
            }

            // Build headers using new FormatHeader method
            AppendMultiColumnHeaders(builder, targetColumnCount, columns, columnWidths, columnPadding, indent);

            // Split rows into table columns
            var rowsPerTableColumn = (int)Math.Ceiling((double)rows.Count / targetColumnCount);
            var tableColumnData = new List<List<T>>();

            for (int col = 0; col < targetColumnCount; col++)
            {
                var startIndex = col * rowsPerTableColumn;
                var endIndex = Math.Min(startIndex + rowsPerTableColumn, rows.Count);
                var tableColumnRows = rows.Skip(startIndex).Take(endIndex - startIndex).ToList();
                tableColumnData.Add(tableColumnRows);
            }

            // Build data rows using new FormatCell method
            var maxRowsInAnyTableColumn = tableColumnData.Max(col => col.Count);
            for (int rowIndex = 0; rowIndex < maxRowsInAnyTableColumn; rowIndex++)
            {
                AppendMultiColumnDataRow(builder, tableColumnData, rowIndex, targetColumnCount,
                    columns, columnWidths, columnPadding, indent);
            }

            return true; // Success
        }

        /// <summary>
        /// Appends the header rows for generic multi-column layout
        /// </summary>
        private static void AppendMultiColumnHeaders<T>(StringBuilder builder, int targetColumnCount,
            IList<ITableColumnFormatter<T>> columns, int[] columnWidths, int columnPadding, int indent = 0)
        {
            // Header row using column's FormatHeader method
            var headerBuilder = new StringBuilder();
            for (int tableCol = 0; tableCol < targetColumnCount; tableCol++)
            {
                if (tableCol > 0) headerBuilder.Append(new string(' ', columnPadding));

                // Build header for this table column
                var headerParts = columns.Select((c, i) => c.FormatHeader(columnWidths[i]));
                headerBuilder.Append(string.Join(" ", headerParts));
            }
            AppendIndentedLine(builder, headerBuilder.ToString(), indent);

            // Separator row
            var separatorBuilder = new StringBuilder();
            for (int tableCol = 0; tableCol < targetColumnCount; tableCol++)
            {
                if (tableCol > 0) separatorBuilder.Append(new string(' ', columnPadding));

                var separatorLength = columnWidths.Sum() + (columns.Count - 1);
                separatorBuilder.Append(new string('-', separatorLength));
            }
            AppendIndentedLine(builder, separatorBuilder.ToString(), indent);
        }

        /// <summary>
        /// Appends a single data row across all table columns for generic tables
        /// </summary>
        private static void AppendMultiColumnDataRow<T>(StringBuilder builder,
            List<List<T>> tableColumnData, int rowIndex, int targetColumnCount,
            IList<ITableColumnFormatter<T>> columns, int[] columnWidths, int columnPadding, int indent = 0)
        {
            var lineBuilder = new StringBuilder();

            for (int tableCol = 0; tableCol < targetColumnCount; tableCol++)
            {
                if (tableCol > 0) lineBuilder.Append(new string(' ', columnPadding));

                if (rowIndex < tableColumnData[tableCol].Count)
                {
                    var row = tableColumnData[tableCol][rowIndex];

                    // Build content for this table column using FormatCell method
                    var cellParts = columns.Select((c, i) => c.FormatCell(row, columnWidths[i]));
                    lineBuilder.Append(string.Join(" ", cellParts));
                }
                else
                {
                    // Empty cell - pad to match table column width
                    var emptyWidth = columnWidths.Sum() + (columns.Count - 1);
                    lineBuilder.Append(new string(' ', emptyWidth));
                }
            }

            AppendIndentedLine(builder, lineBuilder.ToString(), indent);
        }

        /// <summary>
        /// Creates a progress bar visualization for a value between 0 and 1
        /// </summary>
        /// <param name="value">Value between 0 and 1</param>
        /// <param name="width">Width of the progress bar (default 20)</param>
        /// <returns>Progress bar string</returns>
        public string CreateProgressBar(double value, int width = 20)
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
        public string CreateProgressBar(double value, double min, double max, int width = 20)
        {
            if (max <= min) return new string('░', width); // Invalid range

            var normalizedValue = (value - min) / (max - min);
            return CreateProgressBar(normalizedValue, width);
        }
    }
}