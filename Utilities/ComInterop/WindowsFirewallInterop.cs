using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace SharpBridge.Utilities.ComInterop
{

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
    /// Windows Firewall action constants
    /// </summary>
    public static class NetFwAction
    {
        public const int Block = 0;
        public const int Allow = 1;
    }

    [ComImport]
    [Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD")] // CLSID for NetFwPolicy2
    [ClassInterface(ClassInterfaceType.None)]
    public class NetFwPolicy2ComObject { }


    [ComImport]
    [Guid("98325047-C671-4174-8D81-DEFCD3F03186")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwPolicy2
    {
        int CurrentProfileTypes { get; }
        INetFwRules Rules { get; }

        int DefaultInboundAction(int profileType);
        int DefaultOutboundAction(int profileType);
        bool FirewallEnabled(int profileType);
        bool BlockAllInboundTraffic(int profileType);
    }

    [ComImport]
    [Guid("9C4C6277-5027-441E-ABB1-58F24D10A9E3")] // IID for INetFwRules
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwRules : IEnumerable
    {
        int Count { get; }
        void Add([In] INetFwRule rule);
        void Remove([In] string name);
        INetFwRule Item(string name);
        new IEnumerator GetEnumerator();
    }

    [ComImport]
    [Guid("AF230D27-BABA-4E42-ACED-F524F22CFCE2")] // IID for INetFwRule
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface INetFwRule
    {
        string Name { get; set; }
        string Description { get; set; }
        string ApplicationName { get; set; }
        string ServiceName { get; set; }
        int Protocol { get; set; }
        string LocalPorts { get; set; }
        string RemotePorts { get; set; }
        string LocalAddresses { get; set; }
        string RemoteAddresses { get; set; }
        int Direction { get; set; }
        bool Enabled { get; set; }
        string Grouping { get; set; }
        string IcmpTypesAndCodes { get; set; }
        string Interfaces { get; set; }
        int InterfaceTypes { get; set; }
        int Profiles { get; set; }
        int Action { get; set; }
        bool EdgeTraversal { get; set; }
    }

}