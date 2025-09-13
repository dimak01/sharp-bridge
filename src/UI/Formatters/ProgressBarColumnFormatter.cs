using System;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.UI.Formatters;

namespace SharpBridge.UI.Formatters
{
    /// <summary>
    /// A progress bar column that displays a visual progress bar
    /// </summary>
    /// <typeparam name="T">The type of data being displayed</typeparam>
    public class ProgressBarColumnFormatter<T> : ITableColumnFormatter<T>
    {
        /// <summary>
        /// The header text for this column
        /// </summary>
        public string Header { get; }

        /// <summary>
        /// Minimum width this column requires (including header)
        /// </summary>
        public int MinWidth { get; }

        /// <summary>
        /// Maximum width this column should use (null = unlimited)
        /// </summary>
        public int? MaxWidth { get; }

        /// <summary>
        /// Formats the value without width constraints - used for measuring natural content size
        /// </summary>
        public Func<T, string> ValueFormatter { get; }

        private readonly Func<T, double> _valueSelector;
        private readonly ITableFormatter _tableFormatter;

        /// <summary>
        /// Initializes a new instance of the ProgressBarColumn class
        /// </summary>
        /// <param name="header">Column header text</param>
        /// <param name="valueSelector">Function to extract numeric value (0.0 to 1.0) from data item</param>
        /// <param name="minWidth">Minimum column width</param>
        /// <param name="maxWidth">Maximum column width (null for unlimited)</param>
        /// <param name="tableFormatter">Table formatter instance for creating progress bars</param>
        public ProgressBarColumnFormatter(string header, Func<T, double> valueSelector, int minWidth = 6, int? maxWidth = null, ITableFormatter? tableFormatter = null)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            MinWidth = Math.Max(minWidth, header.Length);
            MaxWidth = maxWidth;
            _valueSelector = valueSelector ?? throw new ArgumentNullException(nameof(valueSelector));
            _tableFormatter = tableFormatter ?? new TableFormatter(); // Default fallback
            // ValueFormatter returns a sample bar for width calculation
            ValueFormatter = item => FormatCell(item, MinWidth);
        }

        /// <summary>
        /// Formats and pads the cell content for this column
        /// </summary>
        /// <param name="item">The data item for this row</param>
        /// <param name="width">The actual allocated width for this column</param>
        /// <returns>The formatted and padded cell content</returns>
        public string FormatCell(T item, int width)
        {
            var value = _valueSelector(item);
            return _tableFormatter.CreateProgressBar(value, width);
        }

        /// <summary>
        /// Formats and pads the header for this column
        /// </summary>
        /// <param name="width">The actual allocated width for this column</param>
        /// <returns>The formatted and padded header</returns>
        public string FormatHeader(int width)
        {
            return Header.PadRight(width); // Progress bars typically left-align headers
        }
    }
}