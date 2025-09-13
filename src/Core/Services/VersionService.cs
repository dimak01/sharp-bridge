using System.Reflection;
using SharpBridge.Interfaces.Core.Services;

namespace SharpBridge.Core.Services
{
    /// <summary>
    /// Service for retrieving application version information from assembly metadata.
    /// </summary>
    public class VersionService : IVersionService
    {
        /// <summary>
        /// Gets the full semantic version string from the assembly's AssemblyInformationalVersion attribute.
        /// </summary>
        /// <returns>The semantic version string (e.g., "0.5.0-beta.1" or "0.5.0-dev").</returns>
        public string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            return attribute?.InformationalVersion ?? "0.0.0-unknown";
        }

        /// <summary>
        /// Gets the formatted display version string for UI display.
        /// </summary>
        /// <returns>The formatted version string (e.g., "Sharp Bridge v0.5.0-beta.1").</returns>
        public string GetDisplayVersion()
        {
            return $"Sharp Bridge v{GetVersion()}";
        }
    }
}
