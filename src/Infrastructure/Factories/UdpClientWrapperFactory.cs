// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Net.Sockets;
using SharpBridge.Infrastructure.Wrappers;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Factories;
using SharpBridge.Interfaces.Infrastructure.Wrappers;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Infrastructure.Factories
{
    /// <summary>
    /// Implementation of IUdpClientWrapperFactory that creates configured UDP client wrappers
    /// </summary>
    public class UdpClientWrapperFactory : IUdpClientWrapperFactory
    {
        private readonly VTubeStudioPhoneClientConfig _phoneConfig;
        private const int PortDiscoveryPort = 47779;
        private const int PortDiscoveryTimeoutMs = 2000;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpClientWrapperFactory"/> class
        /// </summary>
        /// <param name="phoneConfig">The phone client configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when phoneConfig is null</exception>
        public UdpClientWrapperFactory(VTubeStudioPhoneClientConfig phoneConfig)
        {
            _phoneConfig = phoneConfig ?? throw new ArgumentNullException(nameof(phoneConfig));
        }

        /// <summary>
        /// Creates a UDP client wrapper configured for phone client operations
        /// </summary>
        /// <returns>A configured UDP client wrapper for phone communication</returns>
        public IUdpClientWrapper CreateForPhoneClient()
        {
            return new UdpClientWrapper(new UdpClient(_phoneConfig.LocalPort));
        }

        /// <summary>
        /// Creates a UDP client wrapper configured for port discovery operations
        /// </summary>
        /// <returns>A configured UDP client wrapper for port discovery</returns>
        public IUdpClientWrapper CreateForPortDiscovery()
        {
            var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, PortDiscoveryPort));
            client.Client.ReceiveTimeout = PortDiscoveryTimeoutMs;
            return new UdpClientWrapper(client);
        }
    }
}