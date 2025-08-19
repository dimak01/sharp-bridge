using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities.ComInterop;

namespace SharpBridge.Services
{
    /// <summary>
    /// Windows implementation of firewall engine using COM interfaces and Windows APIs.
    /// Handles firewall rules, network interface detection, and Windows-specific firewall operations.
    /// </summary>
    public class WindowsFirewallEngine : IFirewallEngine, IDisposable
    {
        private readonly IAppLogger _logger;
        private readonly IWindowsInterop _interop;
        private readonly IProcessInfo _processInfo;
        private dynamic? _firewallPolicy;
        private bool _disposed;

        /// <summary>
        /// Creates a new instance of the Windows firewall analysis engine.
        /// </summary>
        /// <param name="logger">Application logger used for diagnostics and troubleshooting output.</param>
        /// <param name="interop">Windows interop facade that abstracts COM/NLM/Win32 calls.</param>
        /// <param name="processInfo">Provides current process information (executable path).</param>
        public WindowsFirewallEngine(
            IAppLogger logger,
            IWindowsInterop interop,
            IProcessInfo processInfo)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
            _processInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));

            InitializeComObjects();
        }

        private void InitializeComObjects()
        {
            if (_interop.TryCreateFirewallPolicy(out _firewallPolicy))
            {
                _logger.Debug("Successfully initialized Windows Firewall COM objects");
            }
            else
            {
                _logger.Warning("Failed to initialize Windows Firewall COM objects");
                _firewallPolicy = null;
            }
        }


        /// <inheritdoc />
        public List<FirewallRule> GetRelevantRules(
            int direction,
            int protocol,
            int profile,
            string? targetHost = null,
            string? targetPort = null,
            string? localPort = null)
        {
            if (_firewallPolicy == null)
            {
                _logger.Warning("Firewall policy not available, returning empty rule list");
                return new List<FirewallRule>();
            }

            try
            {
                var allRules = EnumerateAllRules();
                var filteredRules = FilterRules(allRules, direction, protocol, profile, targetHost, targetPort, localPort);

                _logger.Debug($"Found {filteredRules.Count} relevant rules for direction={direction}, protocol={protocol}, profile={profile}");
                return filteredRules;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting relevant rules: {ex.Message}");
                return new List<FirewallRule>();
            }
        }

        /// <summary>
        /// Gets the default firewall action for a specific direction and profile
        /// </summary>
        /// <param name="direction">1 for Inbound, 2 for Outbound</param>
        /// <param name="profile">Profile type (Domain/Private/Public)</param>
        /// <returns>True if default action is Allow, false if Block</returns>
        /// <inheritdoc />
        public bool GetDefaultAction(int direction, int profile)
        {
            if (_firewallPolicy == null)
            {
                _logger.Warning("Firewall policy not available, defaulting to block");
                return false; // Default to block for safety
            }

            try
            {
                var defaultAction = _interop.GetDefaultAction(_firewallPolicy, direction, profile);

                // NetFwAction.Allow = 1, NetFwAction.Block = 0
                var isAllowed = defaultAction == Utilities.ComInterop.NetFwAction.Allow;

                var directionName = direction == 1 ? "inbound" : "outbound";
                var actionName = isAllowed ? "Allow" : "Block";
                _logger.Debug($"Default {directionName} action for profile {profile}: {actionName}");
                return isAllowed;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting default firewall action: {ex.Message}");
                return false; // Default to block for safety
            }
        }

        /// <inheritdoc />
        public bool IsApplicationRule(object rule)
        {
            if (rule is not INetFwRule comRule)
                return false;

            try
            {
                var applicationName = comRule.ApplicationName;
                if (string.IsNullOrEmpty(applicationName))
                    return false;

                var currentExePath = _processInfo.GetCurrentExecutablePath();
                if (string.IsNullOrEmpty(currentExePath))
                    return false;

                var ruleAppPath = CleanApplicationPath(applicationName);
                return string.Equals(ruleAppPath, currentExePath, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking application rule: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool IsRuleEnabled(object rule)
        {
            if (rule is not INetFwRule comRule)
                return false;

            try
            {
                return comRule.Enabled;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking rule enabled status: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool IsProfileRule(FirewallRule rule, int profile)
        {
            try
            {
                var ruleProfiles = rule.Profiles;

                // 7 means NET_FW_PROFILE2_ALL – applies to all profiles
                if ((ruleProfiles & 7) == 7)
                    return true;

                return (ruleProfiles & profile) != 0;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking profile rule: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool IsProtocolRule(FirewallRule rule, int protocol)
        {
            try
            {
                var ruleProtocol = rule.Protocol;

                if (ruleProtocol == "Any")
                    return true;

                var desiredProtocolName = GetProtocolName(protocol);
                return string.Equals(ruleProtocol, desiredProtocolName, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking protocol rule: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool IsTargetMatch(FirewallRule rule, string targetHost, string targetPort)
        {
            try
            {
                var ruleRemoteAddress = rule.RemoteAddress ?? string.Empty;
                var ruleRemotePort = rule.RemotePort ?? string.Empty;

                // Check if target host matches rule's remote address
                if (!string.IsNullOrEmpty(ruleRemoteAddress) && !string.IsNullOrEmpty(targetHost)
                    && !IsHostInSubnet(targetHost, NormalizeAddress(ruleRemoteAddress)))
                {
                    return false;
                }

                // Check if target port matches rule's remote port
                if (!string.IsNullOrEmpty(ruleRemotePort) && !string.IsNullOrEmpty(targetPort)
                    && !IsPortInRange(targetPort, ruleRemotePort))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking target match: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool IsPortInRange(string port, string rulePort)
        {
            if (string.IsNullOrEmpty(rulePort) || string.IsNullOrEmpty(port))
                return true; // No port restriction means match

            // Handle port ranges (e.g., "28960-28970")
            var range = rulePort.Split('-');
            if (range.Length == 2
                && int.TryParse(range[0], out var min)
                && int.TryParse(range[1], out var max)
                && int.TryParse(port, out var portNum))
            {
                return portNum >= min && portNum <= max;
            }

            // Handle wildcard ports
            if (rulePort == "*" || rulePort.Equals("any", StringComparison.OrdinalIgnoreCase))
                return true;

            // Exact port match
            return port == rulePort;
        }

        /// <inheritdoc />
        public bool IsHostInSubnet(string host, string subnet)
        {
            if (string.IsNullOrEmpty(subnet) || string.IsNullOrEmpty(host))
                return true; // No subnet restriction means match

            // Handle wildcard addresses
            if (subnet == "*" || subnet.Equals("any", StringComparison.OrdinalIgnoreCase) || subnet == "0.0.0.0")
                return true;

            // Handle CIDR notation (e.g., "192.168.1.0/24")
            if (subnet.Contains('/'))
            {
                var parts = subnet.Split('/', 2);
                if (parts.Length == 2 && int.TryParse(parts[1], out var maskBits))
                {
                    return IsHostInNetwork(host, parts[0], maskBits);
                }
            }

            // Exact host match
            return host == subnet;
        }

        /// <inheritdoc />
        public string NormalizeAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return address;

            var trimmed = address.Trim();

            if (trimmed.Equals("*", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                return "0.0.0.0";
            }
            if (trimmed.Equals("<localsubnet>", StringComparison.OrdinalIgnoreCase))
            {
                return GetLocalSubnet();
            }
            if (trimmed.Equals("<local>", StringComparison.OrdinalIgnoreCase))
            {
                return "127.0.0.1";
            }

            return trimmed;
        }

        private List<FirewallRule> EnumerateAllRules()
        {
            var rules = new List<FirewallRule>();

            try
            {
                var comRules = _interop.EnumerateFirewallRules(_firewallPolicy);
                if (comRules != null)
                {
                    foreach (var comRule in comRules)
                    {
                        var rule = ConvertComRuleToFirewallRule(comRule);
                        if (rule != null)
                        {
                            rules.Add(rule);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error enumerating firewall rules: {ex.Message}");
            }

            return rules;
        }

        private List<FirewallRule> FilterRules(
    List<FirewallRule> rules,
    int direction,
    int protocol,
    int profile,
    string? targetHost,
    string? targetPort,
    string? localPort)
        {
            // Convert magic integers to human-readable strings
            var desiredDirection = direction == 1 ? "Inbound" : "Outbound";
            var desiredProtocol = GetProtocolName(protocol);

            // Get current application path for comparison
            var currentExePath = _processInfo.GetCurrentExecutablePath();

            var hasTarget = !string.IsNullOrEmpty(targetHost) && !string.IsNullOrEmpty(targetPort);
            var hasLocal = !string.IsNullOrEmpty(localPort);

            IEnumerable<FirewallRule> query = rules
                .Where(r => MatchesDirection(r, desiredDirection))
                .Where(r => MatchesProtocol(r, desiredProtocol))
                .Where(r => IsProfileRule(r, profile))
                .Where(r => MatchesApplicationScope(r, currentExePath));

            if (hasTarget)
            {
                query = query.Where(r => MatchesTarget(r, targetHost!, targetPort!));
            }

            if (hasLocal)
            {
                query = query.Where(r => MatchesLocalPort(r, localPort!));
            }

            return query.ToList();
        }

        private static bool MatchesDirection(FirewallRule rule, string desiredDirection)
        {
            return string.Equals(rule.Direction, desiredDirection, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesProtocol(FirewallRule rule, string desiredProtocol)
        {
            return string.Equals(rule.Protocol, desiredProtocol, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesApplicationScope(FirewallRule rule, string? currentExePath)
        {
            // If rule.ApplicationName is null/empty, it's a global rule - include it
            if (string.IsNullOrEmpty(rule.ApplicationName))
                return true;

            // Rule has an application specified - check if it's our app
            var ruleAppPath = CleanApplicationPath(rule.ApplicationName);
            return string.Equals(ruleAppPath, currentExePath, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesTarget(FirewallRule rule, string targetHost, string targetPort)
        {
            return IsTargetMatch(rule, targetHost, targetPort);
        }

        private bool MatchesLocalPort(FirewallRule rule, string localPort)
        {
            // If the rule does not constrain local port, include it
            if (string.IsNullOrEmpty(rule.LocalPort))
                return true;

            return IsPortInRange(localPort, rule.LocalPort);
        }

        private FirewallRule? ConvertComRuleToFirewallRule(dynamic comRule)
        {
            try
            {
                // Read COM properties first to keep object construction simple
                var name = (string?)comRule.Name ?? string.Empty;
                bool isEnabled = comRule.Enabled;
                var direction = comRule.Direction == 1 ? "Inbound" : "Outbound";
                var action = comRule.Action == 1 ? "Allow" : "Block";
                var protocol = GetProtocolName(comRule.Protocol);
                string? localPort = comRule.LocalPorts;
                string? remotePort = comRule.RemotePorts;
                string? remoteAddress = comRule.RemoteAddresses;
                string? applicationName = comRule.ApplicationName;
                int profiles = comRule.Profiles;

                return new FirewallRule
                {
                    Name = name,
                    IsEnabled = isEnabled,
                    Direction = direction,
                    Action = action,
                    Protocol = protocol,
                    LocalPort = localPort,
                    RemotePort = remotePort,
                    RemoteAddress = remoteAddress,
                    ApplicationName = applicationName,
                    Profiles = profiles
                };
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error converting COM rule to FirewallRule: {ex.Message}");
                return null;
            }
        }




        private static string GetProtocolName(int protocol)
        {
            return protocol switch
            {
                6 => "TCP",
                17 => "UDP",
                256 => "Any",
                _ => $"Unknown({protocol})"
            };
        }

        /// <summary>
        /// Cleans application path by stripping quotes and normalizing for comparison
        /// </summary>
        private static string CleanApplicationPath(string input)
        {
            return input.Trim().Trim('"').Trim('\'');
        }

        private bool IsHostInNetwork(string host, string network, int maskBits)
        {
            try
            {
                if (!IPAddress.TryParse(host, out var hostIP) || !IPAddress.TryParse(network, out var networkIP))
                    return false;

                var hostBytes = hostIP.GetAddressBytes();
                var networkBytes = networkIP.GetAddressBytes();
                var maskBytes = GetSubnetMask(maskBits);

                for (int i = 0; i < 4; i++)
                {
                    if ((hostBytes[i] & maskBytes[i]) != (networkBytes[i] & maskBytes[i]))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking host in network: {ex.Message}");
                return false;
            }
        }

        private static byte[] GetSubnetMask(int maskBits)
        {
            var mask = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (maskBits >= 8)
                {
                    mask[i] = 255;
                    maskBits -= 8;
                }
                else
                {
                    mask[i] = (byte)(255 << (8 - maskBits));
                    break;
                }
            }
            return mask;
        }

        private const string LocalSubnetPlaceholder = "192.168.1.0/24";

        private static string GetLocalSubnet()
        {
            // Simplified implementation - in practice, this would need to determine the actual local subnet
            // For now, return a placeholder that will be handled by the calling code
            return LocalSubnetPlaceholder;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources and, optionally, managed resources used by this instance.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    if (_firewallPolicy != null)
                    {
                        _interop.ReleaseComObject(_firewallPolicy);
                        _firewallPolicy = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error disposing COM objects: {ex.Message}");
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Gets the current Windows Firewall state by checking if the service is running.
        /// This approach works without elevation and is more reliable than COM calls.
        /// </summary>
        /// <returns>True if firewall service is running, false if stopped</returns>
        public bool GetFirewallState()
        {
            return _interop.IsFirewallServiceRunning();
        }

        /// <summary>
        /// Gets the current active network profiles.
        /// </summary>
        /// <returns>Bitwise combination of active profiles (Domain=1, Private=2, Public=4)</returns>
        public int GetCurrentProfiles()
        {
            if (_firewallPolicy == null)
            {
                _logger.Warning("Firewall policy not initialized - defaulting to Private profile");
                return NetFwProfile2.Private; // Fail-safe: assume Private
            }

            return _interop.GetCurrentProfiles(_firewallPolicy);
        }

        /// <summary>
        /// Gets the network profile for a specific network interface.
        /// Maps Windows interface index to firewall profile using Network List Manager.
        /// </summary>
        /// <param name="interfaceIndex">Windows interface index from GetBestInterface()</param>
        /// <returns>Network profile (Domain=1, Private=2, Public=4)</returns>
        public int GetInterfaceProfile(int interfaceIndex)
        {
            try
            {
                // Handle loopback special case
                if (interfaceIndex == 0 || interfaceIndex == 1)
                {
                    _logger.Debug("Loopback interface detected - using Private profile");
                    return NetFwProfile2.Private; // Loopback is always Private
                }

                // Step 1: Find the NetworkInterface that matches our interface index
                var targetInterface = FindNetworkInterfaceByIndex(interfaceIndex);
                if (targetInterface == null)
                {
                    _logger.Debug($"Interface {interfaceIndex} not found - defaulting to Private profile");
                    return NetFwProfile2.Private;
                }

                _logger.Debug($"Found interface {interfaceIndex}: {targetInterface.Name} ({targetInterface.Description})");

                // Step 2: Use Network List Manager to get the network category
                var networkCategory = _interop.GetNetworkCategoryForInterface(targetInterface.Id);

                // Step 3: Map NLM category to firewall profile
                var firewallProfile = MapNetworkCategoryToFirewallProfile(networkCategory);

                _logger.Debug($"Interface {interfaceIndex} mapped to category {networkCategory}, firewall profile {firewallProfile}");
                return firewallProfile;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error detecting interface profile for index {interfaceIndex}: {ex.Message} - defaulting to Private");
                return NetFwProfile2.Private; // Fail-safe: assume Private
            }
        }

        /// <summary>
        /// Finds a NetworkInterface by its Windows interface index
        /// </summary>
        private NetworkInterface? FindNetworkInterfaceByIndex(int interfaceIndex)
        {
            try
            {
                var networkInterfaces = _interop.GetAllNetworkInterfaces();

                foreach (var ni in networkInterfaces)
                {
                    // Skip non-operational interfaces
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;

                    var props = ni.GetIPProperties();

                    // Try to match by IPv4 interface index
                    try
                    {
                        var ipv4Props = props.GetIPv4Properties();
                        if (ipv4Props?.Index == interfaceIndex)
                        {
                            _logger.Debug($"Matched interface index {interfaceIndex} to {ni.Name} via IPv4 properties");
                            return ni;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Error getting IPv4 properties for {ni.Name}: {ex.Message}");
                    }
                }

                _logger.Debug($"No matching NetworkInterface found for index {interfaceIndex}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error finding NetworkInterface by index: {ex.Message}");
                return null;
            }
        }



        /// <summary>
        /// Maps Network List Manager category to Windows Firewall profile
        /// </summary>
        private static int MapNetworkCategoryToFirewallProfile(int nlmCategory)
        {
            return nlmCategory switch
            {
                NLM_NETWORK_CATEGORY.Domain => NetFwProfile2.Domain,   // Domain (2) -> Domain (1)
                NLM_NETWORK_CATEGORY.Private => NetFwProfile2.Private, // Private (1) -> Private (2) 
                NLM_NETWORK_CATEGORY.Public => NetFwProfile2.Public,   // Public (0) -> Public (4)
                _ => NetFwProfile2.Private // Default to Private for unknown categories
            };
        }

        /// <summary>
        /// Gets the best network interface for reaching a target host using Windows GetBestInterface API.
        /// Encapsulates the P/Invoke call to keep it isolated from business logic.
        /// </summary>
        /// <param name="targetHost">Target IP address or hostname</param>
        /// <returns>Windows interface index, or 0 if unable to determine</returns>
        public int GetBestInterface(string targetHost)
        {
            return _interop.GetBestInterface(targetHost);
        }
    }
}