using System;
using System.IO;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Service for opening files in external editors
    /// </summary>
    public class ExternalEditorService : IExternalEditorService
    {
        private readonly GeneralSettingsConfig _config;
        private readonly IAppLogger _logger;
        private readonly IProcessLauncher _processLauncher;
        private readonly TransformationEngineConfig _transformationEngineConfig;
        private readonly IConfigManager _configManager;

        /// <summary>
        /// Initializes a new instance of the ExternalEditorService
        /// </summary>
        /// <param name="config">Application configuration containing editor command</param>
        /// <param name="logger">Logger for recording operations and errors</param>
        /// <param name="processLauncher">Process launcher for starting external processes</param>
        /// <param name="transformationEngineConfig">Configuration for the transformation engine</param>
        /// <param name="configManager">Configuration manager for accessing file paths</param>
        public ExternalEditorService(GeneralSettingsConfig config, IAppLogger logger, IProcessLauncher processLauncher, TransformationEngineConfig transformationEngineConfig, IConfigManager configManager)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
            _transformationEngineConfig = transformationEngineConfig ?? throw new ArgumentNullException(nameof(transformationEngineConfig));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        }

        /// <summary>
        /// Attempts to open the transformation configuration file in the configured external editor
        /// </summary>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        public Task<bool> TryOpenTransformationConfigAsync()
        {
            return TryOpenFileInEditorAsync(_transformationEngineConfig.ConfigPath, "transformation config");
        }

        /// <summary>
        /// Attempts to open the application configuration file in the configured external editor
        /// </summary>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        public Task<bool> TryOpenApplicationConfigAsync()
        {
            return TryOpenFileInEditorAsync(_configManager.ApplicationConfigPath, "application config");
        }

        /// <summary>
        /// Attempts to open the specified file in the configured external editor
        /// </summary>
        /// <param name="filePath">Path to the file to open</param>
        /// <param name="configType">Type of configuration for logging purposes</param>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        private Task<bool> TryOpenFileInEditorAsync(string filePath, string configType)
        {
            try
            {
                // Validate file path
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logger.Warning("Cannot open {0} in editor: file path is null or empty", configType);
                    return Task.FromResult(false);
                }

                if (!File.Exists(filePath))
                {
                    _logger.Warning("Cannot open {0} in editor: file does not exist: {1}", configType, filePath);
                    return Task.FromResult(false);
                }

                // Validate editor command
                if (string.IsNullOrWhiteSpace(_config.EditorCommand))
                {
                    _logger.Warning("Cannot open {0} in editor: editor command is not configured", configType);
                    return Task.FromResult(false);
                }

                // Replace %f placeholder with actual file path
                var commandWithFile = _config.EditorCommand.Replace("%f", filePath);

                _logger.Debug("Executing editor command: {0}", commandWithFile);

                // Parse command and arguments
                var (executable, arguments) = ParseCommand(commandWithFile);

                // Execute the command using the process launcher
                if (_processLauncher.TryStartProcess(executable, arguments))
                {
                    _logger.Info("External editor launched successfully: {0}", executable);
                    return Task.FromResult(true);
                }
                else
                {
                    _logger.Warning("Failed to start external editor process");
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error launching external editor for {0}", ex, configType);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Parses a command line into executable and arguments
        /// </summary>
        /// <param name="commandLine">Full command line to parse</param>
        /// <returns>Tuple containing executable path and arguments</returns>
        private static (string executable, string arguments) ParseCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return (string.Empty, string.Empty);
            }

            var trimmedCommand = commandLine.Trim();

            // Handle quoted executables
            if (trimmedCommand.StartsWith('"'))
            {
                var closingQuoteIndex = trimmedCommand.IndexOf('"', 1);
                if (closingQuoteIndex > 0)
                {
                    var executable = trimmedCommand.Substring(1, closingQuoteIndex - 1);
                    var arguments = trimmedCommand.Length > closingQuoteIndex + 1
                        ? trimmedCommand.Substring(closingQuoteIndex + 1).Trim()
                        : string.Empty;
                    return (executable, arguments);
                }
            }

            // Handle unquoted executables
            var spaceIndex = trimmedCommand.IndexOf(' ');
            if (spaceIndex > 0)
            {
                var executable = trimmedCommand.Substring(0, spaceIndex);
                var arguments = trimmedCommand.Substring(spaceIndex + 1).Trim();
                return (executable, arguments);
            }

            // Command has no arguments
            return (trimmedCommand, string.Empty);
        }
    }
}