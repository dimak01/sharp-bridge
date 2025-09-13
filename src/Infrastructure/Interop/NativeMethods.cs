// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;

// Keep namespace for minimal churn; callers already use SharpBridge.Utilities.ComInterop.NativeMethods
namespace SharpBridge.Utilities.ComInterop
{
    /// <summary>
    /// Native Windows API methods for networking
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetBestInterface(uint destAddr, out uint bestIfIndex);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetBestInterfaceEx(byte[] destAddr, out uint bestIfIndex);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetAdapterIndex(string adapterName, out uint ifIndex);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetAdapterName(uint ifIndex, System.Text.StringBuilder adapterName, ref uint size);

        [DllImport("ole32.dll", SetLastError = true)]
        public static extern int CoCreateInstance(
            ref System.Guid clsid,
            System.IntPtr pUnkOuter,
            uint dwClsContext,
            ref System.Guid riid,
            out object ppv);

        public static class ErrorCodes
        {
            public const int NO_ERROR = 0;
            public const int ERROR_INVALID_PARAMETER = 87;
            public const int ERROR_NOT_FOUND = 1168;
            public const int ERROR_NO_DATA = 232;
        }
    }
}












