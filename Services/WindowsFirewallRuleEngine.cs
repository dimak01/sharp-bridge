using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities.ComInterop;

namespace SharpBridge.Services
{
    /// <summary>
    /// Windows implementation of firewall rule engine using COM interfaces.
    /// Handles rule enumeration, filtering, and matching for Windows Firewall analysis.
    /// </summary>
    public class WindowsFirewallRuleEngine : IFirewallRuleEngine, IDisposable
    {
        private readonly IAppLogger _logger;
        private dynamic? _firewallPolicy;
        private bool _disposed;

        public WindowsFirewallRuleEngine(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeComObjects();
        }

        private void InitializeComObjects()
        {
            try
            {
                // Create policy instance
                var type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                _firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(type);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8604 // Possible null reference argument.


                if (_firewallPolicy != null)
                {
                    _logger.Info("Windows Firewall COM objects initialized successfully");
                }
                else
                {
                    _logger.Warning("Failed to create Windows Firewall COM object - all methods failed");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to initialize Windows Firewall COM objects: {ex.Message}");
                _firewallPolicy = null;
            }
        }


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

        public bool IsApplicationRule(object rule)
        {
            if (rule is not INetFwRule comRule)
                return false;

            try
            {
                var applicationName = comRule.ApplicationName;
                if (string.IsNullOrEmpty(applicationName))
                    return false;

                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
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

        public bool IsTargetMatch(FirewallRule rule, string targetHost, string targetPort)
        {
            try
            {
                var ruleRemoteAddress = rule.RemoteAddress ?? string.Empty;
                var ruleRemotePort = rule.RemotePort ?? string.Empty;

                // Check if target host matches rule's remote address
                if (!string.IsNullOrEmpty(ruleRemoteAddress) && !string.IsNullOrEmpty(targetHost))
                {
                    var normalizedRuleAddress = NormalizeAddress(ruleRemoteAddress);
                    if (!IsHostInSubnet(targetHost, normalizedRuleAddress))
                        return false;
                }

                // Check if target port matches rule's remote port
                if (!string.IsNullOrEmpty(ruleRemotePort) && !string.IsNullOrEmpty(targetPort))
                {
                    if (!IsPortInRange(targetPort, ruleRemotePort))
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

        public bool IsPortInRange(string port, string rulePort)
        {
            if (string.IsNullOrEmpty(rulePort) || string.IsNullOrEmpty(port))
                return true; // No port restriction means match

            try
            {
                // Handle port ranges (e.g., "28960-28970")
                if (rulePort.Contains("-"))
                {
                    var range = rulePort.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out var min) && int.TryParse(range[1], out var max))
                    {
                        if (int.TryParse(port, out var portNum))
                        {
                            return portNum >= min && portNum <= max;
                        }
                    }
                }

                // Handle wildcard ports
                if (rulePort == "*" || rulePort.ToLower() == "any")
                    return true;

                // Exact port match
                return port == rulePort;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking port range: {ex.Message}");
                return false;
            }
        }

        public bool IsHostInSubnet(string host, string subnet)
        {
            if (string.IsNullOrEmpty(subnet) || string.IsNullOrEmpty(host))
                return true; // No subnet restriction means match

            try
            {
                // Handle wildcard addresses
                if (subnet == "*" || subnet.ToLower() == "any" || subnet == "0.0.0.0")
                    return true;

                // Handle CIDR notation (e.g., "192.168.1.0/24")
                if (subnet.Contains("/"))
                {
                    var parts = subnet.Split('/');
                    if (parts.Length == 2 && int.TryParse(parts[1], out var maskBits))
                    {
                        return IsHostInNetwork(host, parts[0], maskBits);
                    }
                }

                // Exact host match
                return host == subnet;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error checking host in subnet: {ex.Message}");
                return false;
            }
        }

        public string NormalizeAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return address;

            var normalized = address.Trim().ToLower();

            switch (normalized)
            {
                case "*":
                case "any":
                    return "0.0.0.0";
                case "<localsubnet>":
                    return GetLocalSubnet();
                case "<local>":
                    return "127.0.0.1";
                default:
                    return address.Trim();
            }
        }

        private List<FirewallRule> EnumerateAllRules()
        {
            var rules = new List<FirewallRule>();

            if (_firewallPolicy == null)
                return rules;

            try
            {
                dynamic comRules = _firewallPolicy.Rules;

                // For native COM objects, try direct enumeration
                _logger.Debug("Attempting direct COM enumeration");
                try
                {
                    // Try to enumerate directly using foreach on the COM object
                    foreach (dynamic comRule in comRules)
                    {
                        if (comRule != null)
                        {
                            try
                            {
                                var rule = ConvertComRuleToFirewallRule(comRule);
                                if (rule != null)
                                {
                                    rules.Add(rule);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Debug($"Error converting COM rule: {ex.Message}");
                            }
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

            return rules.Where(rule =>
            {
                // Filter by direction
                if (!string.Equals(rule.Direction, desiredDirection, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Filter by protocol
                if (!string.Equals(rule.Protocol, desiredProtocol, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Filter by profile (using the existing helper that handles bitmasks)
                if (!IsProfileRule(rule, profile))
                    return false;

                // Filter by target if specified
                if (!string.IsNullOrEmpty(targetHost) && !string.IsNullOrEmpty(targetPort))
                {
                    if (!IsTargetMatch(rule, targetHost, targetPort))
                        return false;
                }

                // Filter by local port for inbound connections
                if (!string.IsNullOrEmpty(localPort) && !string.IsNullOrEmpty(rule.LocalPort))
                {
                    if (!IsPortInRange(localPort, rule.LocalPort))
                        return false;
                }

                return true;
            }).ToList();
        }

        private FirewallRule? ConvertComRuleToFirewallRule(dynamic comRule)
        {
            try
            {
                return new FirewallRule
                {
                    Name = comRule.Name ?? string.Empty,
                    IsEnabled = comRule.Enabled,
                    Direction = comRule.Direction == 1 ? "Inbound" : "Outbound",
                    Action = comRule.Action == 1 ? "Allow" : "Block",
                    Protocol = GetProtocolName(comRule.Protocol),
                    LocalPort = comRule.LocalPorts,
                    RemotePort = comRule.RemotePorts,
                    RemoteAddress = comRule.RemoteAddresses,
                    ApplicationName = comRule.ApplicationName,
                    Profiles = comRule.Profiles
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

        private static string GetLocalSubnet()
        {
            // Simplified implementation - in practice, this would need to determine the actual local subnet
            // For now, return a placeholder that will be handled by the calling code
            return "192.168.1.0/24"; // Placeholder
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    if (_firewallPolicy != null)
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(_firewallPolicy);
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
    }
}