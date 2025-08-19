using System;
using System.Collections;
using System.Runtime.InteropServices;

// Intentionally keep the legacy namespace so existing code using these types does not break.
namespace SharpBridge.Utilities.ComInterop
{
    /// <summary>
    /// Windows Firewall profile constants
    /// </summary>
    public static class NetFwProfile2
    {
        /// <summary>Domain profile bitmask value (1).</summary>
        public const int Domain = 1;
        /// <summary>Private profile bitmask value (2).</summary>
        public const int Private = 2;
        /// <summary>Public profile bitmask value (4).</summary>
        public const int Public = 4;
        /// <summary>All profiles bitmask value (7) – equivalent to NET_FW_PROFILE2_ALL.</summary>
        public const int All = 7; // NET_FW_PROFILE2_ALL
    }

    /// <summary>
    /// Windows Firewall action constants
    /// </summary>
    public static class NetFwAction
    {
        /// <summary>Block action (0).</summary>
        public const int Block = 0;
        /// <summary>Allow action (1).</summary>
        public const int Allow = 1;
    }

    /// <summary>
    /// COM class for Windows Firewall policy (NetFwPolicy2).
    /// Used to instantiate <see cref="INetFwPolicy2"/> via COM.
    /// </summary>
    [ComImport]
    [Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD")] // CLSID for NetFwPolicy2
    [ClassInterface(ClassInterfaceType.None)]
    public class NetFwPolicy2ComObject { }

    /// <summary>
    /// Windows Firewall policy COM interface (INetFwPolicy2).
    /// </summary>
    [ComImport]
    [Guid("98325047-C671-4174-8D81-DEFCD3F03186")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwPolicy2
    {
        /// <summary>Gets the bitmask of currently active firewall profiles.</summary>
        int CurrentProfileTypes { get; }
        /// <summary>Gets the rules collection for this policy.</summary>
        INetFwRules Rules { get; }

        /// <summary>Returns the default inbound action for the specified profile.</summary>
        /// <param name="profileType">Profile bitmask.</param>
        /// <returns>Action value from <see cref="NetFwAction"/>.</returns>
        int DefaultInboundAction(int profileType);
        /// <summary>Returns the default outbound action for the specified profile.</summary>
        /// <param name="profileType">Profile bitmask.</param>
        /// <returns>Action value from <see cref="NetFwAction"/>.</returns>
        int DefaultOutboundAction(int profileType);
        /// <summary>Gets whether firewall is enabled for the specified profile.</summary>
        /// <param name="profileType">Profile bitmask.</param>
        bool FirewallEnabled(int profileType);
        /// <summary>Gets whether block-all-inbound is enabled for the specified profile.</summary>
        /// <param name="profileType">Profile bitmask.</param>
        bool BlockAllInboundTraffic(int profileType);
    }

    /// <summary>
    /// Firewall rules COM collection interface (INetFwRules).
    /// </summary>
    [ComImport]
    [Guid("9C4C6277-5027-441E-ABB1-58F24D10A9E3")] // IID for INetFwRules
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwRules : IEnumerable
    {
        /// <summary>Gets the number of rules in the collection.</summary>
        int Count { get; }
        /// <summary>Adds a rule to the collection.</summary>
        /// <param name="rule">Rule to add.</param>
        void Add([In] INetFwRule rule);
        /// <summary>Removes a rule by name.</summary>
        /// <param name="name">Rule name.</param>
        void Remove([In] string name);
        /// <summary>Gets a rule by name.</summary>
        /// <param name="name">Rule name.</param>
        /// <returns>The rule instance.</returns>
        INetFwRule Item(string name);
        /// <summary>Returns an enumerator over the rules.</summary>
        new IEnumerator GetEnumerator();
    }

    /// <summary>
    /// Windows Firewall rule COM interface (INetFwRule).
    /// </summary>
    [ComImport]
    [Guid("AF230D27-BABA-4E42-ACED-F524F22CFCE2")] // IID for INetFwRule
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwRule
    {
        /// <summary>Gets or sets the rule name.</summary>
        string Name { get; set; }
        /// <summary>Gets or sets the rule description.</summary>
        string Description { get; set; }
        /// <summary>Gets or sets the application path associated with the rule.</summary>
        string ApplicationName { get; set; }
        /// <summary>Gets or sets the service name associated with the rule.</summary>
        string ServiceName { get; set; }
        /// <summary>Gets or sets the protocol number (e.g., 6 = TCP, 17 = UDP).</summary>
        int Protocol { get; set; }
        /// <summary>Gets or sets the local port specification.</summary>
        string LocalPorts { get; set; }
        /// <summary>Gets or sets the remote port specification.</summary>
        string RemotePorts { get; set; }
        /// <summary>Gets or sets the local address specification.</summary>
        string LocalAddresses { get; set; }
        /// <summary>Gets or sets the remote address specification.</summary>
        string RemoteAddresses { get; set; }
        /// <summary>Gets or sets the direction (1 = Inbound, 2 = Outbound).</summary>
        int Direction { get; set; }
        /// <summary>Gets or sets whether the rule is enabled.</summary>
        bool Enabled { get; set; }
        /// <summary>Gets or sets the grouping string.</summary>
        string Grouping { get; set; }
        /// <summary>Gets or sets ICMP types and codes.</summary>
        string IcmpTypesAndCodes { get; set; }
        /// <summary>Gets or sets the interface identifiers.</summary>
        string Interfaces { get; set; }
        /// <summary>Gets or sets the interface types.</summary>
        int InterfaceTypes { get; set; }
        /// <summary>Gets or sets the profile bitmask the rule applies to.</summary>
        int Profiles { get; set; }
        /// <summary>Gets or sets the rule action (see <see cref="NetFwAction"/>).</summary>
        int Action { get; set; }
        /// <summary>Gets or sets whether edge traversal is enabled.</summary>
        bool EdgeTraversal { get; set; }
    }

    /// <summary>
    /// Network List Manager COM interface for network profile detection
    /// </summary>
    [ComImport]
    [Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetworkListManager
    {
        /// <summary>Gets the networks collection.</summary>
        /// <param name="Flags">NLM flags.</param>
        object GetNetworks(int Flags);
        /// <summary>Gets the network connections collection.</summary>
        object GetNetworkConnections();
        /// <summary>Gets a network by identifier.</summary>
        /// <param name="networkId">Network GUID.</param>
        object GetNetwork(Guid networkId);
        /// <summary>Gets a network connection by identifier.</summary>
        /// <param name="networkConnectionId">Network connection GUID.</param>
        object GetNetworkConnection(Guid networkConnectionId);
        /// <summary>Gets whether the machine is connected to the Internet.</summary>
        bool IsConnectedToInternet { get; }
        /// <summary>Gets whether the machine is connected to any network.</summary>
        bool IsConnected { get; }
        /// <summary>Gets connectivity flags.</summary>
        /// <param name="Flags">NLM flags.</param>
        int GetConnectivity(int Flags);
    }

    /// <summary>
    /// COM class for Network List Manager used to create <see cref="INetworkListManager"/> instances.
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
        /// <summary>Gets the associated network object.</summary>
        object GetNetwork();
        /// <summary>Gets whether the connection has Internet access.</summary>
        bool IsConnectedToInternet { get; }
        /// <summary>Gets whether the connection is connected.</summary>
        bool IsConnected { get; }
        /// <summary>Gets connectivity flags for this connection.</summary>
        int GetConnectivity();
        /// <summary>Gets the connection identifier.</summary>
        Guid GetConnectionId();
        /// <summary>Gets the adapter identifier.</summary>
        Guid GetAdapterId();
        /// <summary>Gets the domain type identifier.</summary>
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
        /// <summary>Gets the network name.</summary>
        string GetName();
        /// <summary>Sets the network name.</summary>
        /// <param name="szNetworkNewName">New name.</param>
        void SetName(string szNetworkNewName);
        /// <summary>Gets the network description.</summary>
        string GetDescription();
        /// <summary>Sets the network description.</summary>
        /// <param name="szDescription">New description.</param>
        void SetDescription(string szDescription);
        /// <summary>Gets the network identifier.</summary>
        Guid GetNetworkId();
        /// <summary>Gets the domain type identifier.</summary>
        Guid GetDomainType();
        /// <summary>Gets the network connections associated with this network.</summary>
        object GetNetworkConnections();
        /// <summary>Gets timestamps for creation and connection.</summary>
        /// <param name="pdwLowDateTimeCreated">Low creation time.</param>
        /// <param name="pdwHighDateTimeCreated">High creation time.</param>
        /// <param name="pdwLowDateTimeConnected">Low connected time.</param>
        /// <param name="pdwHighDateTimeConnected">High connected time.</param>
        void GetTimeCreatedAndConnected(out uint pdwLowDateTimeCreated, out uint pdwHighDateTimeCreated, out uint pdwLowDateTimeConnected, out uint pdwHighDateTimeConnected);
        /// <summary>Gets whether the network has Internet connectivity.</summary>
        bool IsConnectedToInternet { get; }
        /// <summary>Gets whether the network is connected.</summary>
        bool IsConnected { get; }
        /// <summary>Gets connectivity flags for this network.</summary>
        int GetConnectivity();
        /// <summary>Gets the current network category.</summary>
        int GetCategory();
        /// <summary>Sets the network category.</summary>
        /// <param name="NewCategory">New category value.</param>
        void SetCategory(int NewCategory);
    }

    /// <summary>
    /// Network category constants
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Keeps parity with Windows COM definitions.")]
    public static class NLM_NETWORK_CATEGORY
    {
        /// <summary>Public network category (0).</summary>
        public const int Public = 0;
        /// <summary>Private network category (1).</summary>
        public const int Private = 1;
        /// <summary>Domain network category (2).</summary>
        public const int Domain = 2;
    }

    /// <summary>
    /// Network connectivity constants
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Keeps parity with Windows COM definitions.")]
    public static class NLM_CONNECTIVITY
    {
        /// <summary>No connectivity.</summary>
        public const int Disconnected = 0;
        /// <summary>IPv4 stack present but no traffic.</summary>
        public const int IPv4NoTraffic = 1;
        /// <summary>IPv6 stack present but no traffic.</summary>
        public const int IPv6NoTraffic = 2;
        /// <summary>Connectivity limited to IPv4 subnet.</summary>
        public const int IPv4Subnet = 16;
        /// <summary>Connectivity to IPv4 local network.</summary>
        public const int IPv4LocalNetwork = 32;
        /// <summary>Connectivity to IPv4 Internet.</summary>
        public const int IPv4Internet = 64;
        /// <summary>Connectivity limited to IPv6 subnet.</summary>
        public const int IPv6Subnet = 256;
        /// <summary>Connectivity to IPv6 local network.</summary>
        public const int IPv6LocalNetwork = 512;
        /// <summary>Connectivity to IPv6 Internet.</summary>
        public const int IPv6Internet = 1024;
    }
}


