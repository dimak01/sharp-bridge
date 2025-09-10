using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Services;

namespace SharpBridge.Infrastructure.Services
{
    /// <summary>
    /// Real implementation of IProcessLauncher that launches actual processes
    /// </summary>
    public class ProcessLauncher : IProcessLauncher, IDisposable
    {
        private readonly bool _useShellExecute;
        private readonly bool _createNoWindow;
        private readonly List<Process> _spawnedProcesses = new();
        private bool _disposed = false;

        /// <summary>
        /// Creates a new ProcessLauncher with default settings (visible windows, shell execute)
        /// </summary>
        public ProcessLauncher() : this(useShellExecute: true, createNoWindow: false)
        {
        }

        /// <summary>
        /// Creates a new ProcessLauncher with custom settings
        /// </summary>
        /// <param name="useShellExecute">Whether to use shell execute (default: true)</param>
        /// <param name="createNoWindow">Whether to create no window (default: false)</param>
        public ProcessLauncher(bool useShellExecute = true, bool createNoWindow = false)
        {
            _useShellExecute = useShellExecute;
            _createNoWindow = createNoWindow;
        }

        /// <summary>
        /// Gets the list of spawned processes (for testing purposes)
        /// </summary>
        internal IReadOnlyList<Process> SpawnedProcesses => _spawnedProcesses.AsReadOnly();

        /// <summary>
        /// Gets the last started process (for testing purposes)
        /// </summary>
        internal Process? LastStartedProcess { get; private set; }

        /// <summary>
        /// Attempts to start a process with the specified executable and arguments
        /// </summary>
        /// <param name="executable">The executable to launch</param>
        /// <param name="arguments">Arguments to pass to the executable</param>
        /// <returns>True if the process was started successfully, false otherwise</returns>
        public bool TryStartProcess(string executable, string arguments)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProcessLauncher));

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = _useShellExecute,
                    CreateNoWindow = _createNoWindow
                };

                // Start the process and don't wait for it to complete
                var process = Process.Start(processStartInfo);

                if (process != null)
                {
                    LastStartedProcess = process;
                    _spawnedProcesses.Add(process);
                }

                return process != null;
            }
            catch
            {
                // Return false for any exception during process start
                return false;
            }
        }

        /// <summary>
        /// Kills all spawned processes that are still running
        /// </summary>
        public void KillAllSpawnedProcesses()
        {
            var processesToKill = new List<Process>();

            foreach (var process in _spawnedProcesses)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        processesToKill.Add(process);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process has already exited or been disposed
                }
            }

            foreach (var process in processesToKill)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                    // Ignore exceptions when killing processes
                }
            }
        }

        /// <summary>
        /// Disposes all spawned processes and cleans up resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            KillAllSpawnedProcesses();

            foreach (var process in _spawnedProcesses)
            {
                try
                {
                    process.Dispose();
                }
                catch (Exception)
                {
                    // Ignore disposal exceptions
                }
            }

            _spawnedProcesses.Clear();
            LastStartedProcess = null;
            _disposed = true;
        }
    }
}