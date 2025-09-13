// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using System.Security.Principal;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Interop;
using SharpBridge.Interfaces.Infrastructure.Services;

// Keep namespace to avoid broad refactors while introducing a single facade
namespace SharpBridge.Utilities.ComInterop
{
    /// <summary>
    /// Unified Windows interop facade that consolidates Firewall COM, Network List Manager and Win32 operations.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WindowsInterop : IWindowsInterop
    {
        private readonly IAppLogger _logger;

        /// <summary>
        /// Creates a new instance of <see cref="WindowsInterop"/>.
        /// </summary>
        /// <param name="logger">Application logger used for diagnostics and error reporting.</param>
        public WindowsInterop(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Firewall COM
        /// <inheritdoc />
        public bool TryCreateFirewallPolicy(out dynamic? policy)
        {
            policy = null;
            try
            {
                policy = (INetFwPolicy2)new NetFwPolicy2ComObject();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to initialize Windows Firewall COM objects: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public IEnumerable<dynamic> EnumerateFirewallRules(dynamic policy)
        {
            if (policy == null) return Enumerable.Empty<dynamic>();
            var rules = new List<dynamic>();
            try
            {
                dynamic comRules = policy.Rules;
                foreach (dynamic comRule in comRules)
                {
                    if (comRule != null) rules.Add(comRule);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error enumerating firewall rules: {ex.Message}");
            }
            return rules;
        }

        /// <inheritdoc />
        public int GetDefaultAction(dynamic policy, int direction, int profile)
        {
            if (policy == null)
            {
                _logger.Warning("Firewall policy not available, defaulting to block");
                return NetFwAction.Block;
            }
            try
            {
                int defaultAction = direction == 1
                    ? policy?.DefaultInboundAction(profile) ?? NetFwAction.Block
                    : policy?.DefaultOutboundAction(profile) ?? NetFwAction.Block;
                return defaultAction;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting default firewall action: {ex.Message}");
                return NetFwAction.Block;
            }
        }

        /// <inheritdoc />
        public int GetCurrentProfiles(dynamic policy)
        {
            if (policy == null)
            {
                _logger.Warning("Firewall policy not initialized - defaulting to Private profile");
                return NetFwProfile2.Private;
            }
            try
            {
                var firewallPolicy = (INetFwPolicy2)policy;
                return firewallPolicy.CurrentProfileTypes;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error getting current profiles: {ex.Message} - defaulting to Private");
                return NetFwProfile2.Private;
            }
        }

        /// <inheritdoc />
        public int GetNetworkCategoryForInterface(string interfaceId)
        {
            // Check if running with elevated permissions
            if (!IsRunningAsAdministrator())
            {
                _logger.Debug("Application not running as administrator - defaulting to Private network category");
                return NLM_NETWORK_CATEGORY.Private;
            }

            try
            {
                var networkListManager = new NetworkListManagerComObject() as INetworkListManager;
                if (networkListManager == null) return NLM_NETWORK_CATEGORY.Private;
                dynamic connections = networkListManager.GetNetworkConnections();
                foreach (dynamic connection in connections)
                {
                    var connectionInterface = connection as INetworkConnection;
                    if (connectionInterface == null) continue;
                    var networkObj = connectionInterface.GetNetwork();
                    var network = networkObj as INetwork;
                    if (network == null) continue;
                    if (connectionInterface.IsConnected) return network.GetCategory();
                }

                return NLM_NETWORK_CATEGORY.Private;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error getting network category via NLM: {ex.Message} - defaulting to Private");
                return NLM_NETWORK_CATEGORY.Private;
            }
        }

        /// <summary>
        /// Checks if the current application is running with administrator privileges
        /// </summary>
        /// <returns>True if running as administrator, false otherwise</returns>
        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                // If we can't determine elevation status, assume not elevated
                return false;
            }
        }

        /// <inheritdoc />
        public void ReleaseComObject(object comObject)
        {
            if (comObject == null) return;
            try { System.Runtime.InteropServices.Marshal.ReleaseComObject(comObject); }
            catch (Exception ex) { _logger.Debug($"Error releasing COM object: {ex.Message}"); }
        }

        // System / Win32
        /// <inheritdoc />
        public bool IsFirewallServiceRunning()
        {
            try
            {
                using var serviceController = new System.ServiceProcess.ServiceController("mpssvc");
                return serviceController.Status == System.ServiceProcess.ServiceControllerStatus.Running;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error checking firewall service status: {ex.Message} - assuming enabled");
                return true;
            }
        }

        /// <inheritdoc />
        public int GetBestInterface(string targetHost)
        {
            try
            {
                if (string.IsNullOrEmpty(targetHost) || targetHost == "localhost" || targetHost == "127.0.0.1")
                    return 1; // loopback
                if (!IPAddress.TryParse(targetHost, out var targetAddr)) return 0;
                var targetBytes = targetAddr.GetAddressBytes();
                var targetInt = BitConverter.ToUInt32(targetBytes, 0);
                var result = NativeMethods.GetBestInterface(targetInt, out uint bestInterface);
                return result == 0 ? (int)bestInterface : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public NetworkInterface[] GetAllNetworkInterfaces()
        {
            try { return NetworkInterface.GetAllNetworkInterfaces(); }
            catch { return Array.Empty<NetworkInterface>(); }
        }
    }
}


