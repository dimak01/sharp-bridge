using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for components that provide service statistics
    /// </summary>
    public interface IServiceStatsProvider<T> where T : IFormattableObject
    {
        /// <summary>
        /// Gets the current service statistics
        /// </summary>
        ServiceStats<T> GetServiceStats();
    }
} 