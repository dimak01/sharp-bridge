using System.Net.NetworkInformation;
using System.Collections.Generic;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Single facade for Windows-specific interop: Firewall COM, Network List Manager, and Win32 APIs.
    /// Consolidates operations previously split between IWindowsComInterop and IWindowsSystemApi.
    /// </summary>
    public interface IWindowsInterop
    {
        // Firewall COM
        bool TryCreateFirewallPolicy(out dynamic? policy);
        IEnumerable<dynamic> EnumerateFirewallRules(dynamic policy);
        int GetDefaultAction(dynamic policy, int direction, int profile);
        int GetCurrentProfiles(dynamic policy);
        int GetNetworkCategoryForInterface(string interfaceId);
        void ReleaseComObject(object comObject);

        // System / Win32
        bool IsFirewallServiceRunning();
        int GetBestInterface(string targetHost);
        NetworkInterface[] GetAllNetworkInterfaces();
    }
}


