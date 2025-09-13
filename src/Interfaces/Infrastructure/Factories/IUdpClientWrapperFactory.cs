// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Wrappers;

namespace SharpBridge.Interfaces.Infrastructure.Factories
{
    /// <summary>
    /// Factory for creating IUdpClientWrapper instances with specific configurations
    /// </summary>
    public interface IUdpClientWrapperFactory
    {
        /// <summary>
        /// Creates a UDP client wrapper configured for the phone client
        /// </summary>
        IUdpClientWrapper CreateForPhoneClient();

        /// <summary>
        /// Creates a UDP client wrapper configured for port discovery
        /// </summary>
        IUdpClientWrapper CreateForPortDiscovery();
    }
}