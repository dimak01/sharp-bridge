using System;
using System.Collections.Generic;
using System.Text;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for formatting tabular data consistently across formatters
    /// </summary>
    public interface ITableFormatter
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
        void AppendTable<T>(StringBuilder builder, string title,
            IEnumerable<T> rows, IList<ITableColumn<T>> columns, int targetColumnCount, int consoleWidth,
            int singleColumnBarWidth = 20, int? singleColumnMaxItems = null);
            
        /// <summary>
        /// Creates a progress bar visualization for a value between 0 and 1
        /// </summary>
        /// <param name="value">Value between 0 and 1</param>
        /// <param name="width">Width of the progress bar (default 20)</param>
        /// <returns>Progress bar string</returns>
        string CreateProgressBar(double value, int width = 20);
        
        /// <summary>
        /// Creates a progress bar visualization for a value within a custom range
        /// </summary>
        /// <param name="value">Current value</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <param name="width">Width of the progress bar (default 20)</param>
        /// <returns>Progress bar string</returns>
        string CreateProgressBar(double value, double min, double max, int width = 20);
    }
} 