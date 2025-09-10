using System;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.UI.Formatters;

namespace SharpBridge.UI.Formatters
{
    /// <summary>
    /// A numeric column that displays formatted numbers
    /// </summary>
    /// <typeparam name="T">The type of data being displayed</typeparam>
    public class NumericColumnFormatter<T> : ITableColumnFormatter<T>
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

        private readonly bool _padLeft;

        /// <summary>
        /// Initializes a new instance of the NumericColumn class
        /// </summary>
        /// <param name="header">Column header text</param>
        /// <param name="valueSelector">Function to extract numeric value from data item</param>
        /// <param name="format">Number format string (default "0.##")</param>
        /// <param name="minWidth">Minimum column width</param>
        /// <param name="maxWidth">Maximum column width (null for unlimited)</param>
        /// <param name="padLeft">Whether to pad numbers to the left (right-align)</param>
        public NumericColumnFormatter(string header, Func<T, double> valueSelector, string format = "0.##", int minWidth = 0, int? maxWidth = null, bool padLeft = true)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            MinWidth = Math.Max(minWidth, header.Length);
            MaxWidth = maxWidth;
            ValueFormatter = item => valueSelector(item).ToString(format);
            _padLeft = padLeft;
        }

        /// <summary>
        /// Formats and pads the cell content for this column
        /// </summary>
        /// <param name="item">The data item for this row</param>
        /// <param name="width">The actual allocated width for this column</param>
        /// <returns>The formatted and padded cell content</returns>
        public string FormatCell(T item, int width)
        {
            var content = ValueFormatter(item);
            return _padLeft ? content.PadLeft(width) : content.PadRight(width);
        }

        /// <summary>
        /// Formats and pads the header for this column
        /// </summary>
        /// <param name="width">The actual allocated width for this column</param>
        /// <returns>The formatted and padded header</returns>
        public string FormatHeader(int width)
        {
            return _padLeft ? Header.PadLeft(width) : Header.PadRight(width);
        }
    }
}