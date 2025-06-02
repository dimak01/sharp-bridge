using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// A simple text column that displays string content
    /// </summary>
    /// <typeparam name="T">The type of data being displayed</typeparam>
    public class TextColumn<T> : ITableColumn<T>
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
        /// Initializes a new instance of the TextColumn class
        /// </summary>
        /// <param name="header">Column header text</param>
        /// <param name="valueSelector">Function to extract string value from data item</param>
        /// <param name="minWidth">Minimum column width</param>
        /// <param name="maxWidth">Maximum column width (null for unlimited)</param>
        /// <param name="padLeft">Whether to pad text to the left (right-align)</param>
        public TextColumn(string header, Func<T, string> valueSelector, int minWidth = 0, int? maxWidth = null, bool padLeft = false)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            MinWidth = Math.Max(minWidth, header.Length);
            MaxWidth = maxWidth;
            ValueFormatter = valueSelector ?? throw new ArgumentNullException(nameof(valueSelector));
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
            // Truncate if content exceeds width
            if (content.Length > width)
            {
                content = content.Length > 3 ? content.Substring(0, width - 3) + "..." : content.Substring(0, width);
            }
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