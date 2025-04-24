using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Manages authentication with VTube Studio PC
    /// </summary>
    public interface IVTubeStudioPCAuthManager
    {
        /// <summary>
        /// Authenticates with VTube Studio, handling retries and token management
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task that completes when authentication is successful</returns>
        /// <exception cref="InvalidOperationException">Thrown when authentication fails after all retries</exception>
        Task AuthenticateAsync(CancellationToken cancellationToken);
    }
} 