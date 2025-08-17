using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities.ComInterop
{
    /// <summary>
    /// Windows implementation of COM interop operations for Windows Firewall.
    /// Handles all COM object creation and interaction with Windows Firewall APIs.
    /// </summary>
    public class WindowsComInterop : IWindowsComInterop
    {
        private readonly IAppLogger _logger;

        public WindowsComInterop(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

        public IEnumerable<dynamic> EnumerateFirewallRules(dynamic policy)
        {
            if (policy == null)
                return Enumerable.Empty<dynamic>();

            var rules = new List<dynamic>();

            try
            {
                dynamic comRules = policy.Rules;

                // For native COM objects, try direct enumeration
                _logger.Debug("Attempting direct COM enumeration");
                try
                {
                    // Try to enumerate directly using foreach on the COM object
                    foreach (dynamic comRule in comRules)
                    {
                        if (comRule != null)
                        {
                            rules.Add(comRule);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Direct COM enumeration failed: {ex.Message}");
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                _logger.Error($"COM error enumerating firewall rules: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})");
                if (comEx.HResult == unchecked((int)0x80070057)) // E_INVALIDARG
                {
                    _logger.Error("This may indicate an issue with firewall service or permissions");
                }
            }
            catch (System.ArgumentException argEx)
            {
                _logger.Error($"Argument error enumerating firewall rules: {argEx.Message}");
                _logger.Error("This typically indicates 'Value does not fall within the expected range'");
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error enumerating firewall rules: {ex.Message}");
                _logger.Debug($"Exception type: {ex.GetType().Name}");
            }

            return rules;
        }

        public int GetDefaultAction(dynamic policy, int direction, int profile)
        {
            if (policy == null)
            {
                _logger.Warning("Firewall policy not available, defaulting to block");
                return 0; // Default to block for safety
            }

            try
            {
                // Debug: Let's see what methods are available on the dynamic object
                _logger.Debug($"Firewall policy type: {policy?.GetType().Name ?? "null"}");

                int defaultAction = 0; // Initialize with safe default
                if (direction == 1) // Inbound
                {
                    defaultAction = policy?.DefaultInboundAction(profile) ?? 0;
                    _logger.Debug($"Successfully called get_DefaultInboundAction for profile {profile}");
                }
                else // Outbound
                {
                    defaultAction = policy?.DefaultOutboundAction(profile) ?? 0;
                    _logger.Debug($"Successfully called get_DefaultOutboundAction for profile {profile}");
                }

                var directionName = direction == 1 ? "inbound" : "outbound";
                var actionName = defaultAction == 1 ? "Allow" : "Block";
                _logger.Debug($"Default {directionName} action for profile {profile}: {actionName}");
                return defaultAction;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting default firewall action: {ex.Message}");
                return 0; // Default to block for safety
            }
        }

        public int GetCurrentProfiles(dynamic policy)
        {
            if (policy == null)
            {
                _logger.Warning("Firewall policy not initialized - defaulting to Private profile");
                return NetFwProfile2.Private; // Fail-safe: assume Private
            }

            try
            {
                var firewallPolicy = (INetFwPolicy2)policy;
                var currentProfiles = firewallPolicy.CurrentProfileTypes;

                _logger.Debug($"Current active profiles: {currentProfiles} " +
                             $"(Domain={((currentProfiles & NetFwProfile2.Domain) != 0)}, " +
                             $"Private={((currentProfiles & NetFwProfile2.Private) != 0)}, " +
                             $"Public={((currentProfiles & NetFwProfile2.Public) != 0)})");

                return currentProfiles;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error getting current profiles: {ex.Message} - defaulting to Private");
                return NetFwProfile2.Private; // Fail-safe: assume Private
            }
        }

        public int GetNetworkCategoryForInterface(string interfaceId)
        {
            try
            {
                // Create Network List Manager COM object
                var networkListManager = new NetworkListManagerComObject() as INetworkListManager;
                if (networkListManager == null)
                {
                    _logger.Debug("Failed to create NetworkListManager COM object - defaulting to Private category");
                    return NLM_NETWORK_CATEGORY.Private;
                }

                // Get all network connections
                dynamic connections = networkListManager.GetNetworkConnections();

                // Enumerate connections to find one matching our interface
                foreach (dynamic connection in connections)
                {
                    try
                    {
                        var connectionInterface = connection as INetworkConnection;
                        if (connectionInterface == null) continue;

                        // Get the network for this connection
                        dynamic networkObj = connectionInterface.GetNetwork();
                        var network = networkObj as INetwork;
                        if (network == null) continue;

                        // Get network category
                        var category = network.GetCategory();

                        _logger.Debug($"Found network connection with category {category} for interface");

                        // For simplicity, return the first connected network's category
                        // In practice, we might need more sophisticated matching
                        if (connectionInterface.IsConnected)
                        {
                            return category;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Error processing network connection: {ex.Message}");
                    }
                }

                _logger.Debug("No matching network connection found - defaulting to Private category");
                return NLM_NETWORK_CATEGORY.Private;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error getting network category via NLM: {ex.Message} - defaulting to Private");
                return NLM_NETWORK_CATEGORY.Private;
            }
        }

        public void ReleaseComObject(object comObject)
        {
            if (comObject != null)
            {
                try
                {
                    Marshal.ReleaseComObject(comObject);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error releasing COM object: {ex.Message}");
                }
            }
        }
    }
}

