using System;
using System.Net.Sockets;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of IUdpClientWrapperFactory that creates configured UDP client wrappers
    /// </summary>
    public class UdpClientWrapperFactory : IUdpClientWrapperFactory
    {
        private readonly VTubeStudioPhoneClientConfig _phoneConfig;
        private const int PortDiscoveryPort = 47779;
        private const int PortDiscoveryTimeoutMs = 2000;

        public UdpClientWrapperFactory(VTubeStudioPhoneClientConfig phoneConfig)
        {
            _phoneConfig = phoneConfig ?? throw new ArgumentNullException(nameof(phoneConfig));
        }

        public IUdpClientWrapper CreateForPhoneClient()
        {
            return new UdpClientWrapper(new UdpClient(_phoneConfig.LocalPort));
        }

        public IUdpClientWrapper CreateForPortDiscovery()
        {
            var client = new UdpClient(PortDiscoveryPort);
            client.Client.ReceiveTimeout = PortDiscoveryTimeoutMs;
            return new UdpClientWrapper(client);
        }
    }
} 