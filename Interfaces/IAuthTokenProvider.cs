using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Provides authentication token management functionality
    /// </summary>
    public interface IAuthTokenProvider
    {
        /// <summary>
        /// Gets the current authentication token
        /// </summary>
        string Token { get; }
        
        /// <summary>
        /// Gets the authentication token, requesting a new one if needed
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The authentication token</returns>
        Task<string> GetTokenAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Saves the authentication token
        /// </summary>
        /// <param name="token">The token to save</param>
        Task SaveTokenAsync(string token);
        
        /// <summary>
        /// Clears the current authentication token
        /// </summary>
        Task ClearTokenAsync();

        /// <summary>
        /// Loads the authentication token from the configured file path
        /// </summary>
        void LoadAuthToken();
    }
} 