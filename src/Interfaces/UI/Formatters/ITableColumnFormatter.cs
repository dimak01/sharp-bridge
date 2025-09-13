// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;

namespace SharpBridge.Interfaces.UI.Formatters
{
    /// <summary>
    /// Base interface for all table column types
    /// </summary>
    /// <typeparam name="T">The type of data being displayed in the table</typeparam>
    public interface ITableColumnFormatter<T>
    {
        /// <summary>
        /// The header text for this column
        /// </summary>
        string Header { get; }

        /// <summary>
        /// Minimum width this column requires (including header)
        /// </summary>
        int MinWidth { get; }

        /// <summary>
        /// Maximum width this column should use (null = unlimited)
        /// </summary>
        int? MaxWidth { get; }

        /// <summary>
        /// Formats the value without width constraints - used for measuring natural content size
        /// </summary>
        Func<T, string> ValueFormatter { get; }

        /// <summary>
        /// Formats and pads the cell content for this column
        /// </summary>
        /// <param name="item">The data item for this row</param>
        /// <param name="width">The actual allocated width for this column</param>
        /// <returns>The formatted and padded cell content</returns>
        string FormatCell(T item, int width);

        /// <summary>
        /// Formats and pads the header for this column
        /// </summary>
        /// <param name="width">The actual allocated width for this column</param>
        /// <returns>The formatted and padded header</returns>
        string FormatHeader(int width);
    }
}