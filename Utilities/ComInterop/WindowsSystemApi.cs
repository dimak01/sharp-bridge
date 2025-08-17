using System;
using System.Net;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities.ComInterop
{
    /// <summary>
    /// Windows implementation of system API calls.
    /// Handles Windows-specific system operations and API calls.
    /// </summary>
    public class WindowsSystemApi : IWindowsSystemApi
    {
        private readonly IAppLogger _logger;

        public WindowsSystemApi(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsFirewallServiceRunning()
        {
            try
            {
                // Check if Windows Firewall service (mpssvc) is running
                // This works without elevation and is a good indicator of firewall state
                using var serviceController = new ServiceController("mpssvc");
                var isRunning = serviceController.Status == ServiceControllerStatus.Running;

                _logger.Debug($"Windows Firewall service (mpssvc) status: {serviceController.Status}, running: {isRunning}");
                return isRunning;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error checking firewall service status: {ex.Message} - assuming enabled");
                return true; // Fail-safe: assume enabled
            }
        }

        public int GetBestInterface(string targetHost)
        {
            try
            {
                // Handle special cases
                if (string.IsNullOrEmpty(targetHost) || targetHost == "localhost" || targetHost == "127.0.0.1")
                {
                    _logger.Debug("Localhost target detected - using loopback interface (index 1)");
                    return 1; // Loopback interface
                }

                // Parse target IP address
                if (!IPAddress.TryParse(targetHost, out var targetAddr))
                {
                    _logger.Debug($"Unable to parse target host '{targetHost}' - defaulting to interface 0");
                    return 0; // Default interface
                }

                // Call Windows GetBestInterface API
                var targetBytes = targetAddr.GetAddressBytes();
                var targetInt = BitConverter.ToUInt32(targetBytes, 0);

                var result = NativeMethods.GetBestInterface(targetInt, out uint bestInterface);
                if (result == 0) // NO_ERROR
                {
                    _logger.Debug($"GetBestInterface for {targetHost} returned interface {bestInterface}");
                    return (int)bestInterface;
                }
                else
                {
                    _logger.Warning($"GetBestInterface failed for {targetHost} with error code {result} - defaulting to interface 0");
                    return 0; // Default interface
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error in GetBestInterface for {targetHost}: {ex.Message} - defaulting to interface 0");
                return 0; // Default interface
            }
        }

        public NetworkInterface[] GetAllNetworkInterfaces()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error getting network interfaces: {ex.Message} - returning empty array");
                return Array.Empty<NetworkInterface>();
            }
        }
    }
}
