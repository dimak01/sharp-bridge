using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// A simple text column that displays string content
    /// </summary>
    /// <typeparam name="T">The type of data being displayed</typeparam>
    public class TextColumnFormatter<T> : ITableColumnFormatter<T>
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
        public TextColumnFormatter(string header, Func<T, string> valueSelector, int minWidth = 0, int? maxWidth = null, bool padLeft = false)
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
            var visualLength = ConsoleColors.GetVisualLength(content);

            // Truncate if visual content exceeds width
            if (visualLength > width)
            {
                // For ANSI-colored content, we need to be more careful with truncation
                content = TruncateWithAnsiSupport(content, width);
            }

            // Pad based on visual length, not total length
            var paddingNeeded = width - ConsoleColors.GetVisualLength(content);
            if (paddingNeeded > 0)
            {
                var padding = new string(' ', paddingNeeded);
                content = _padLeft ? padding + content : content + padding;
            }

            return content;
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

        /// <summary>
        /// Truncates content while preserving ANSI escape sequences
        /// </summary>
        /// <param name="content">Content that may contain ANSI sequences</param>
        /// <param name="maxVisualWidth">Maximum visual width (excluding ANSI sequences)</param>
        /// <returns>Truncated content with ellipsis if needed</returns>
        private static string TruncateWithAnsiSupport(string content, int maxVisualWidth)
        {
            if (maxVisualWidth <= 0) return "";
            if (maxVisualWidth <= 3)
            {
                // For very small widths, just truncate without ellipsis
                return TruncateToVisualLength(content, maxVisualWidth);
            }

            // Try to fit with ellipsis
            var truncated = TruncateToVisualLength(content, maxVisualWidth - 3);
            return truncated + "...";
        }

        /// <summary>
        /// Truncates content to a specific visual length while preserving ANSI sequences
        /// </summary>
        /// <param name="content">Content that may contain ANSI sequences</param>
        /// <param name="targetVisualLength">Target visual length</param>
        /// <returns>Truncated content</returns>
        private static string TruncateToVisualLength(string content, int targetVisualLength)
        {
            if (targetVisualLength <= 0) return "";

            // Strategy: Build result character by character, skipping over ANSI sequences
            var result = new System.Text.StringBuilder();
            var visualCharCount = 0;
            var i = 0;

            while (i < content.Length && visualCharCount < targetVisualLength)
            {
                // Check if we're at the start of an ANSI escape sequence
                if (IsAnsiEscapeStart(content, i))
                {
                    // Find and include the entire ANSI sequence without counting it
                    var ansiSequence = ExtractAnsiSequence(content, i);
                    result.Append(ansiSequence);
                    i += ansiSequence.Length;
                }
                else
                {
                    // Regular visible character - include it and count it
                    result.Append(content[i]);
                    visualCharCount++;
                    i++;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Checks if the current position is the start of an ANSI escape sequence
        /// </summary>
        private static bool IsAnsiEscapeStart(string content, int position)
        {
            return position < content.Length - 1 &&
                   content[position] == '\u001b' &&
                   content[position + 1] == '[';
        }

        /// <summary>
        /// Extracts a complete ANSI escape sequence starting at the given position
        /// </summary>
        private static string ExtractAnsiSequence(string content, int startPosition)
        {
            var i = startPosition + 2; // Skip '\u001b['

            // Find the end of the sequence (digits, semicolons, then 'm')
            while (i < content.Length && (char.IsDigit(content[i]) || content[i] == ';'))
            {
                i++;
            }

            // Include the terminating 'm' if present
            if (i < content.Length && content[i] == 'm')
            {
                i++;
            }

            return content.Substring(startPosition, i - startPosition);
        }
    }
}