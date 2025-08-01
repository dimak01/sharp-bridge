using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of IParameterTableConfigurationManager for managing parameter table column configurations
    /// </summary>
    public class ParameterTableConfigurationManager : IParameterTableConfigurationManager
    {
        private readonly IAppLogger _logger;
        private ParameterTableColumn[] _currentColumns;

        /// <summary>
        /// Initializes a new instance of the ParameterTableConfigurationManager
        /// </summary>
        /// <param name="logger">Logger for recording configuration issues</param>
        public ParameterTableConfigurationManager(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentColumns = GetDefaultParameterTableColumns();
        }

        /// <summary>
        /// Gets the currently configured parameter table columns
        /// </summary>
        /// <returns>Array of parameter table columns to display</returns>
        public ParameterTableColumn[] GetParameterTableColumns()
        {
            return _currentColumns;
        }

        /// <summary>
        /// Gets the default parameter table columns used when no configuration is provided
        /// </summary>
        /// <returns>Array of default parameter table columns</returns>
        public ParameterTableColumn[] GetDefaultParameterTableColumns()
        {
            return new[]
            {
                ParameterTableColumn.ParameterName,
                ParameterTableColumn.ProgressBar,
                ParameterTableColumn.Value,
                ParameterTableColumn.Range,
                ParameterTableColumn.MinMax,
                ParameterTableColumn.Expression,
                ParameterTableColumn.Interpolation
            };
        }

        /// <summary>
        /// Loads parameter table configuration from user preferences
        /// </summary>
        /// <param name="userPreferences">User preferences containing column configuration</param>
        public void LoadFromUserPreferences(UserPreferences userPreferences)
        {
            if (userPreferences?.PCParameterTableColumns == null ||
                userPreferences.PCParameterTableColumns.Length == 0)
            {
                _logger.Debug("No parameter table columns configured in user preferences, using defaults");
                _currentColumns = GetDefaultParameterTableColumns();
                return;
            }

            // Validate and filter columns
            var validColumns = new List<ParameterTableColumn>();
            var invalidColumns = new List<string>();

            foreach (var column in userPreferences.PCParameterTableColumns)
            {
                if (Enum.IsDefined(typeof(ParameterTableColumn), column))
                {
                    validColumns.Add(column);
                }
                else
                {
                    invalidColumns.Add(column.ToString());
                }
            }

            if (invalidColumns.Count > 0)
            {
                var invalidColumnsString = string.Join(", ", invalidColumns);
                _logger.Warning("Invalid parameter table columns found: {0}. These will be ignored.",
                    invalidColumnsString);
            }

            if (validColumns.Count == 0)
            {
                _logger.Warning("No valid parameter table columns found, using defaults");
                _currentColumns = GetDefaultParameterTableColumns();
            }
            else
            {
                _currentColumns = validColumns.ToArray();
                _logger.Debug("Loaded {0} parameter table columns from user preferences", validColumns.Count);
            }
        }

        /// <summary>
        /// Gets the display string for a column (for debugging/help purposes)
        /// </summary>
        /// <param name="column">The parameter table column</param>
        /// <returns>Human-readable column name for display</returns>
        public string GetColumnDisplayName(ParameterTableColumn column)
        {
            return AttributeHelper.GetDescription(column);
        }
    }
}