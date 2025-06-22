using System.Diagnostics;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for launching external processes
    /// </summary>
    public interface IProcessLauncher
    {
        /// <summary>
        /// Attempts to start a process with the specified executable and arguments
        /// </summary>
        /// <param name="executable">The executable to launch</param>
        /// <param name="arguments">Arguments to pass to the executable</param>
        /// <returns>True if the process was started successfully, false otherwise</returns>
        bool TryStartProcess(string executable, string arguments);
    }
} 