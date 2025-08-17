using System;
using System.Collections.Generic;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Abstraction for Windows COM interop operations, primarily for Windows Firewall COM objects.
    /// This interface allows for testing without actual COM dependencies.
    /// </summary>
    public interface IWindowsComInterop
    {
        /// <summary>
        /// Attempts to create a Windows Firewall Policy COM object.
        /// </summary>
        /// <param name="policy">The created policy object if successful, null otherwise</param>
        /// <returns>True if policy was created successfully, false otherwise</returns>
        bool TryCreateFirewallPolicy(out dynamic? policy);

        /// <summary>
        /// Enumerates all firewall rules from the given policy object.
        /// </summary>
        /// <param name="policy">The firewall policy COM object</param>
        /// <returns>Enumerable of dynamic COM rule objects</returns>
        IEnumerable<dynamic> EnumerateFirewallRules(dynamic policy);

        /// <summary>
        /// Gets the default firewall action for a specific direction and profile.
        /// </summary>
        /// <param name="policy">The firewall policy COM object</param>
        /// <param name="direction">1 for Inbound, 2 for Outbound</param>
        /// <param name="profile">Profile type (Domain/Private/Public)</param>
        /// <returns>Default action value (0=Block, 1=Allow)</returns>
        int GetDefaultAction(dynamic policy, int direction, int profile);

        /// <summary>
        /// Gets the current active network profiles from the firewall policy.
        /// </summary>
        /// <param name="policy">The firewall policy COM object</param>
        /// <returns>Bitwise combination of active profiles</returns>
        int GetCurrentProfiles(dynamic policy);

        /// <summary>
        /// Gets the network category for a network interface using Network List Manager.
        /// </summary>
        /// <param name="interfaceId">The network interface identifier</param>
        /// <returns>Network category (Domain=2, Private=1, Public=0)</returns>
        int GetNetworkCategoryForInterface(string interfaceId);

        /// <summary>
        /// Releases COM objects to prevent memory leaks.
        /// </summary>
        /// <param name="comObject">The COM object to release</param>
        void ReleaseComObject(object comObject);
    }
}

