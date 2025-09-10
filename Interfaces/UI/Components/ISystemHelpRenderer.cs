using System.Text;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Infrastructure;

namespace SharpBridge.Interfaces.UI.Components
{
    /// <summary>
    /// Interface for rendering the system help display (F2 functionality)
    /// </summary>
    public interface ISystemHelpRenderer
    {
        /// <summary>
        /// Renders the complete system help display including all application configuration sections and keyboard shortcuts
        /// </summary>
        /// <param name="applicationConfig">Complete application configuration to display</param>
        /// <param name="consoleWidth">Available console width for formatting</param>
        /// <param name="networkStatus">Optional network status to include in troubleshooting section</param>
        /// <returns>Formatted help content as a string</returns>
        string RenderSystemHelp(ApplicationConfig applicationConfig, int consoleWidth, NetworkStatus? networkStatus = null);

        /// <summary>
        /// Renders just the application configuration sections
        /// </summary>
        /// <param name="applicationConfig">Complete application configuration to display</param>
        /// <returns>Formatted configuration sections</returns>
        string RenderApplicationConfiguration(ApplicationConfig applicationConfig);

        /// <summary>
        /// Renders just the keyboard shortcuts section with status information
        /// </summary>
        /// <param name="consoleWidth">Available console width for table formatting</param>
        /// <returns>Formatted shortcuts section</returns>
        string RenderKeyboardShortcuts(int consoleWidth);

        /// <summary>
        /// Renders the parameter table column configuration section
        /// </summary>
        /// <param name="consoleWidth">Available console width for table formatting</param>
        /// <returns>Formatted parameter table column configuration section</returns>
        string RenderParameterTableColumns(int consoleWidth);
    }
}