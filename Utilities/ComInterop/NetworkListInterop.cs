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

        // Additional methods we need
        object GetNetwork(Guid networkId);
        object GetNetworkConnection(Guid networkConnectionId);
        bool IsConnectedToInternet { get; }
        bool IsConnected { get; }
        int GetConnectivity(int Flags);
    }

    /// <summary>
    /// Network List Manager COM class
    /// </summary>
    [ComImport]
    [Guid("DCB00000-570F-4A9B-8D69-199FDBA5723B")] // CLSID for NetworkListManager
    [ClassInterface(ClassInterfaceType.None)]
    public class NetworkListManagerComObject { }

    /// <summary>
    /// Network connection COM interface
    /// </summary>
    [ComImport]
    [Guid("DCB00005-570F-4A9B-8D69-199FDBA5723B")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetworkConnection
    {
        object GetNetwork();
        bool IsConnectedToInternet { get; }
        bool IsConnected { get; }
        int GetConnectivity();
        Guid GetConnectionId();
        Guid GetAdapterId();
        Guid GetDomainType();
    }

    /// <summary>
    /// Network COM interface  
    /// </summary>
    [ComImport]
    [Guid("DCB00002-570F-4A9B-8D69-199FDBA5723B")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetwork
    {
        string GetName();
        void SetName(string szNetworkNewName);
        string GetDescription();
        void SetDescription(string szDescription);
        Guid GetNetworkId();
        Guid GetDomainType();
        object GetNetworkConnections();
        void GetTimeCreatedAndConnected(out uint pdwLowDateTimeCreated, out uint pdwHighDateTimeCreated, out uint pdwLowDateTimeConnected, out uint pdwHighDateTimeConnected);
        bool IsConnectedToInternet { get; }
        bool IsConnected { get; }
        int GetConnectivity();
        int GetCategory();
        void SetCategory(int NewCategory);
    }

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