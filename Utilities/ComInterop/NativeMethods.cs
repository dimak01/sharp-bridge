using System.Runtime.InteropServices;

namespace SharpBridge.Utilities.ComInterop
{
    /// <summary>
    /// Native Windows API methods for networking
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Gets the best interface for routing to a destination address
        /// </summary>
        /// <param name="destAddr">Destination IP address in network byte order</param>
        /// <param name="bestIfIndex">Output parameter for the best interface index</param>
        /// <returns>0 on success, error code on failure</returns>
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetBestInterface(uint destAddr, out uint bestIfIndex);

        /// <summary>
        /// Gets the best interface for routing to a destination address (IPv6 version)
        /// </summary>
        /// <param name="destAddr">Destination IPv6 address</param>
        /// <param name="bestIfIndex">Output parameter for the best interface index</param>
        /// <returns>0 on success, error code on failure</returns>
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetBestInterfaceEx(byte[] destAddr, out uint bestIfIndex);

        /// <summary>
        /// Gets the adapter index from adapter name
        /// </summary>
        /// <param name="adapterName">Adapter name</param>
        /// <param name="ifIndex">Output parameter for adapter index</param>
        /// <returns>0 on success, error code on failure</returns>
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetAdapterIndex(string adapterName, out uint ifIndex);

        /// <summary>
        /// Gets the adapter name from adapter index
        /// </summary>
        /// <param name="ifIndex">Adapter index</param>
        /// <param name="adapterName">Output parameter for adapter name</param>
        /// <param name="size">Size of adapter name buffer</param>
        /// <returns>0 on success, error code on failure</returns>
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetAdapterName(uint ifIndex, System.Text.StringBuilder adapterName, ref uint size);

        /// <summary>
        /// Windows error codes
        /// </summary>
        public static class ErrorCodes
        {
            public const int NO_ERROR = 0;
            public const int ERROR_INVALID_PARAMETER = 87;
            public const int ERROR_NOT_FOUND = 1168;
            public const int ERROR_NO_DATA = 232;
        }
    }
}