using SharpBridge.Models.Configuration;
using SharpBridge.Models.UI;

namespace SharpBridge.Interfaces.Configuration.Managers
{
    /// <summary>
    /// Interface for managing parameter table column configurations with graceful degradation
    /// </summary>
    public interface IParameterTableConfigurationManager
    {
        /// <summary>
        /// Gets the currently configured parameter table columns
        /// </summary>
        /// <returns>Array of parameter table columns to display</returns>
        ParameterTableColumn[] GetParameterTableColumns();

        /// <summary>
        /// Gets the default parameter table columns used when no configuration is provided
        /// </summary>
        /// <returns>Array of default parameter table columns</returns>
        ParameterTableColumn[] GetDefaultParameterTableColumns();

        /// <summary>
        /// Loads parameter table configuration from user preferences
        /// </summary>
        /// <param name="userPreferences">User preferences containing column configuration</param>
        void LoadFromUserPreferences(UserPreferences userPreferences);

        /// <summary>
        /// Gets the display string for a column (for debugging/help purposes)
        /// </summary>
        /// <param name="column">The parameter table column</param>
        /// <returns>Human-readable column name for display</returns>
        string GetColumnDisplayName(ParameterTableColumn column);
    }
}