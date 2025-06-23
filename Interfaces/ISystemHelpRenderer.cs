using System.Text;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for rendering the system help display (F1 functionality)
    /// </summary>
    public interface ISystemHelpRenderer
    {
        /// <summary>
        /// Renders the complete system help display including application configuration and keyboard shortcuts
        /// </summary>
        /// <param name="applicationConfig">Application configuration to display</param>
        /// <param name="consoleWidth">Available console width for formatting</param>
        /// <returns>Formatted help content as a string</returns>
        string RenderSystemHelp(ApplicationConfig applicationConfig, int consoleWidth);

        /// <summary>
        /// Renders just the application configuration section
        /// </summary>
        /// <param name="applicationConfig">Application configuration to display</param>
        /// <returns>Formatted configuration section</returns>
        string RenderApplicationConfiguration(ApplicationConfig applicationConfig);

        /// <summary>
        /// Renders just the keyboard shortcuts section with status information
        /// </summary>
        /// <param name="consoleWidth">Available console width for table formatting</param>
        /// <returns>Formatted shortcuts section</returns>
        string RenderKeyboardShortcuts(int consoleWidth);
    }
}