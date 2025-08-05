using System.Runtime.InteropServices;

namespace SharpBridge.Utilities.ComInterop
{
    /// <summary>
    /// Windows Firewall Policy COM interface for accessing firewall rules and state
    /// </summary>
    [ComImport]
    [Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwPolicy2
    {
        // Firewall state and profile methods
        bool get_FirewallEnabled(int profile);
        bool get_BlockAllInboundTraffic(int profile);
        int get_CurrentProfileTypes();

        // Rules collection
        INetFwRules get_Rules();
    }

    /// <summary>
    /// Windows Firewall Rules collection COM interface
    /// </summary>
    [ComImport]
    [Guid("9C4C6277-5027-441E-AFA0-E31DBB6F9F06")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwRules
    {
        // Rule enumeration
        int get_Count();
        INetFwRule Item(int index);
        object get__NewEnum();
    }

    /// <summary>
    /// Windows Firewall Rule COM interface for individual rule properties
    /// </summary>
    [ComImport]
    [Guid("F7898AF5-DAC5-4C35-A934-86F1F5CC9C4F")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwRule
    {
        // Rule properties
        string get_Name();
        bool get_Enabled();
        int get_Direction();
        int get_Protocol();
        string get_LocalPorts();
        string get_RemotePorts();
        string get_LocalAddresses();
        string get_RemoteAddresses();
        string get_ApplicationName();
        int get_Profiles();
        int get_Action();
    }

    /// <summary>
    /// Windows Firewall profile constants
    /// </summary>
    public static class NetFwProfile2
    {
        public const int Domain = 1;
        public const int Private = 2;
        public const int Public = 4;
        public const int All = 7; // NET_FW_PROFILE2_ALL
    }

    /// <summary>
    /// Windows Firewall direction constants
    /// </summary>
    public static class NetFwRuleDirection
    {
        public const int Inbound = 1;
        public const int Outbound = 2;
    }

    /// <summary>
    /// Windows Firewall action constants
    /// </summary>
    public static class NetFwAction
    {
        public const int Block = 0;
        public const int Allow = 1;
    }

    /// <summary>
    /// Windows Firewall protocol constants
    /// </summary>
    public static class NetFwProtocol
    {
        public const int Any = -1;
        public const int TCP = 6;
        public const int UDP = 17;
    }
}