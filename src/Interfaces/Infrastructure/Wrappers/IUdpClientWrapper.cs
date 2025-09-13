// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces.Infrastructure.Wrappers
{
    /// <summary>
    /// Interface for UDP client operations providing async UDP communication capabilities
    /// </summary>
    public interface IUdpClientWrapper : IDisposable
    {
        /// <summary>
        /// Sends UDP data asynchronously to the specified endpoint
        /// </summary>
        /// <param name="datagram">The data to send</param>
        /// <param name="bytes">The number of bytes to send</param>
        /// <param name="hostname">The target hostname or IP address</param>
        /// <param name="port">The target port number</param>
        /// <returns>The number of bytes sent</returns>
        Task<int> SendAsync(byte[] datagram, int bytes, string hostname, int port);
        
        /// <summary>
        /// Receives UDP data asynchronously
        /// </summary>
        /// <param name="token">Cancellation token to cancel the operation</param>
        /// <returns>The received UDP data and sender information</returns>
        Task<UdpReceiveResult> ReceiveAsync(CancellationToken token);
        
        /// <summary>
        /// Gets the number of bytes available for reading
        /// </summary>
        int Available { get; }
        
        /// <summary>
        /// Polls the UDP client for data availability
        /// </summary>
        /// <param name="microseconds">Timeout in microseconds</param>
        /// <param name="mode">The polling mode to use</param>
        /// <returns>True if data is available or the operation completed</returns>
        bool Poll(int microseconds, SelectMode mode);
    }
} 