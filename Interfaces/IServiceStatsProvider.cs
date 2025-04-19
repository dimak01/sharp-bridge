using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for components that provide service statistics
    /// </summary>
    public interface IServiceStatsProvider
    {
        /// <summary>
        /// Gets the current service statistics
        /// </summary>
        IServiceStats GetServiceStats();
    }
} 