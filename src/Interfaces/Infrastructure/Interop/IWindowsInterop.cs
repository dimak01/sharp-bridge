// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Net.NetworkInformation;
using System.Collections.Generic;

namespace SharpBridge.Interfaces.Infrastructure.Interop
{
    /// <summary>
    /// Single facade for Windows-specific interop: Firewall COM, Network List Manager, and Win32 APIs.
    /// Consolidates operations previously split between <c>IWindowsComInterop</c> and <c>IWindowsSystemApi</c>.
    /// </summary>
    public interface IWindowsInterop
    {
        // Firewall COM
        /// <summary>
        /// Attempts to create the Windows Firewall policy COM object.
        /// </summary>
        /// <param name="policy">When successful, receives the initialized COM policy object; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the policy object was created; otherwise <c>false</c>.</returns>
        bool TryCreateFirewallPolicy(out dynamic? policy);

        /// <summary>
        /// Enumerates raw COM firewall rules for the provided policy.
        /// </summary>
        /// <param name="policy">Firewall policy COM object.</param>
        /// <returns>An enumerable of dynamic COM rule objects. Never <c>null</c>.</returns>
        IEnumerable<dynamic> EnumerateFirewallRules(dynamic policy);

        /// <summary>
        /// Gets the default firewall action for a direction/profile combination.
        /// </summary>
        /// <param name="policy">Firewall policy COM object.</param>
        /// <param name="direction">1 for inbound, 2 for outbound.</param>
        /// <param name="profile">Firewall profile bitmask.</param>
        /// <returns>The underlying <c>NetFwAction</c> value (Allow/Block).</returns>
        int GetDefaultAction(dynamic policy, int direction, int profile);

        /// <summary>
        /// Gets the current active firewall profiles from the policy.
        /// </summary>
        /// <param name="policy">Firewall policy COM object.</param>
        /// <returns>Profile bitmask corresponding to <c>NetFwProfile2</c>.</returns>
        int GetCurrentProfiles(dynamic policy);

        /// <summary>
        /// Gets the Network List Manager category for the network associated with the given interface identifier.
        /// </summary>
        /// <param name="interfaceId">The network interface GUID as a string.</param>
        /// <returns>NLM network category value.</returns>
        int GetNetworkCategoryForInterface(string interfaceId);

        /// <summary>
        /// Releases a COM object instance safely.
        /// </summary>
        /// <param name="comObject">COM object to release.</param>
        void ReleaseComObject(object comObject);

        // System / Win32
        /// <summary>
        /// Determines whether the Windows Firewall (MpsSvc) service is running.
        /// </summary>
        /// <returns><c>true</c> if running; otherwise <c>false</c>.</returns>
        bool IsFirewallServiceRunning();

        /// <summary>
        /// Uses the Win32 <c>GetBestInterface</c> API to determine the best interface for reaching a target host.
        /// </summary>
        /// <param name="targetHost">IPv4 address or hostname.</param>
        /// <returns>Windows interface index; 0 if unknown; 1 for loopback.</returns>
        int GetBestInterface(string targetHost);

        /// <summary>
        /// Returns all network interfaces available on the system.
        /// </summary>
        /// <returns>Array of <see cref="NetworkInterface"/>. Never <c>null</c>.</returns>
        NetworkInterface[] GetAllNetworkInterfaces();
    }
}


