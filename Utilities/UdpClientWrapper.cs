using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Wrapper around UdpClient providing an abstraction layer for UDP operations
    /// </summary>
    public class UdpClientWrapper : IUdpClientWrapper
    {
        private readonly UdpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpClientWrapper"/> class
        /// </summary>
        /// <param name="client">The underlying UdpClient to wrap</param>
        /// <exception cref="ArgumentNullException">Thrown when client is null</exception>
        public UdpClientWrapper(UdpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            
            // Set read timeout to match Rust implementation's 2-second timeout
            _client.Client.ReceiveTimeout = 2000;
        }

        /// <summary>
        /// Gets the number of bytes available for reading
        /// </summary>
        public int Available => _client.Available;

        /// <summary>
        /// Releases all resources used by the UDP client
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
        }

        /// <summary>
        /// Polls the UDP client for data availability
        /// </summary>
        /// <param name="microseconds">Timeout in microseconds</param>
        /// <param name="mode">The polling mode to use</param>
        /// <returns>True if data is available or the operation completed</returns>
        public bool Poll(int microseconds, SelectMode mode)
        {
            return _client.Client.Poll(microseconds, mode);
        }

        /// <summary>
        /// Receives UDP data asynchronously
        /// </summary>
        /// <param name="token">Cancellation token to cancel the operation</param>
        /// <returns>The received UDP data and sender information</returns>
        public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken token)
        {
            return await _client.ReceiveAsync(token);
        }

        /// <summary>
        /// Sends UDP data asynchronously to the specified endpoint
        /// </summary>
        /// <param name="datagram">The data to send</param>
        /// <param name="bytes">The number of bytes to send</param>
        /// <param name="hostname">The target hostname or IP address</param>
        /// <param name="port">The target port number</param>
        /// <returns>The number of bytes sent</returns>
        public Task<int> SendAsync(byte[] datagram, int bytes, string hostname, int port)
        {
            return _client.SendAsync(datagram, bytes, hostname, port);
        }
    }
} 