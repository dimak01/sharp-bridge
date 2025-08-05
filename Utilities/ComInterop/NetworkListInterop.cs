using System;
using System.Runtime.InteropServices;

namespace SharpBridge.Utilities.ComInterop
{
    /// <summary>
    /// Network List Manager COM interface for network profile detection
    /// </summary>
    [ComImport]
    [Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetworkListManager
    {
        // Network enumeration
        object GetNetworks(int Flags);
        object GetNetworkConnections();

        // Note: We don't need the specific network/connection methods for our use case
        // We can use .NET NetworkInterface.GetAllNetworkInterfaces() instead
    }

    // Note: INetwork and INetworkConnection interfaces are not needed for our use case
    // We can use .NET NetworkInterface.GetAllNetworkInterfaces() for interface enumeration
    // and INetworkListManager for network profile detection

    /// <summary>
    /// Network category constants
    /// </summary>
    public static class NLM_NETWORK_CATEGORY
    {
        public const int Public = 0;
        public const int Private = 1;
        public const int Domain = 2;
    }

    /// <summary>
    /// Network connectivity constants
    /// </summary>
    public static class NLM_CONNECTIVITY
    {
        public const int Disconnected = 0;
        public const int IPv4NoTraffic = 1;
        public const int IPv6NoTraffic = 2;
        public const int IPv4Subnet = 16;
        public const int IPv4LocalNetwork = 32;
        public const int IPv4Internet = 64;
        public const int IPv6Subnet = 256;
        public const int IPv6LocalNetwork = 512;
        public const int IPv6Internet = 1024;
    }
}