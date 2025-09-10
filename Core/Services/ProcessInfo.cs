using System;
using System.Diagnostics;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Core.Services;
using SharpBridge.Interfaces.Infrastructure.Services;

namespace SharpBridge.Core.Services
{
    /// <summary>
    /// Implementation of process information provider.
    /// Provides information about the current running process.
    /// </summary>
    public class ProcessInfo : IProcessInfo
    {
        private readonly IAppLogger _logger;

        /// <summary>
        /// Creates a new <see cref="ProcessInfo"/>.
        /// </summary>
        /// <param name="logger">Application logger for diagnostics.</param>
        public ProcessInfo(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the full path of the current process executable.
        /// </summary>
        /// <returns>Absolute path to the executable, or null if unavailable.</returns>
        public string? GetCurrentExecutablePath()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var path = currentProcess.MainModule?.FileName;

                if (string.IsNullOrEmpty(path))
                {
                    _logger.Debug("Unable to get current process executable path");
                    return null;
                }

                _logger.Debug($"Current executable path: {path}");
                return path;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error getting current executable path: {ex.Message}");
                return null;
            }
        }
    }
}

