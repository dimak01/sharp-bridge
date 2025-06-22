using System;
using System.Diagnostics;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Real implementation of IProcessLauncher that launches actual processes
    /// </summary>
    public class ProcessLauncher : IProcessLauncher
    {
        /// <summary>
        /// Attempts to start a process with the specified executable and arguments
        /// </summary>
        /// <param name="executable">The executable to launch</param>
        /// <param name="arguments">Arguments to pass to the executable</param>
        /// <returns>True if the process was started successfully, false otherwise</returns>
        public bool TryStartProcess(string executable, string arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                // Start the process and don't wait for it to complete
                using var process = Process.Start(processStartInfo);
                
                return process != null;
            }
            catch
            {
                // Return false for any exception during process start
                return false;
            }
        }
    }
} 