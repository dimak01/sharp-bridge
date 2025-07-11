using System;
using System.IO;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpBridge.Services
{
    /// <summary>
    /// Service for opening files in external editors
    /// </summary>
    public class ExternalEditorService : IExternalEditorService, IDisposable
    {
        private readonly IAppLogger _logger;
        private readonly IProcessLauncher _processLauncher;
        private readonly IConfigManager _configManager;
        private readonly IFileChangeWatcher _configWatcher;
        private GeneralSettingsConfig _generalSettingsConfig;
        private TransformationEngineConfig _transformationConfig;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the ExternalEditorService
        /// </summary>
        /// <param name="configManager">Configuration manager for accessing application configuration</param>
        /// <param name="logger">Logger for recording operations and errors</param>
        /// <param name="processLauncher">Process launcher for starting external processes</param>
        /// <param name="configWatcher">File change watcher for application configuration</param>
        public ExternalEditorService(IConfigManager configManager, IAppLogger logger, IProcessLauncher processLauncher, IFileChangeWatcher configWatcher)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
            _configWatcher = configWatcher ?? throw new ArgumentNullException(nameof(configWatcher));

            // Load initial configuration
            LoadConfiguration();

            // Subscribe to configuration changes
            _configWatcher.FileChanged += OnApplicationConfigChanged;
        }

        /// <summary>
        /// Releases all resources used by the external editor service
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the external editor service
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _logger.Debug("Disposing ExternalEditorService");

                // Unsubscribe from file watcher events
                _configWatcher.FileChanged -= OnApplicationConfigChanged;

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Loads and stores all configuration data
        /// </summary>
        private void LoadConfiguration()
        {
            _generalSettingsConfig = _configManager.LoadGeneralSettingsConfigAsync().GetAwaiter().GetResult();
            _transformationConfig = _configManager.LoadTransformationConfigAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles application configuration changes
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">File change event arguments</param>
        private async void OnApplicationConfigChanged(object? sender, FileChangeEventArgs e)
        {
            try
            {
                _logger.Debug("Application config changed, checking if general settings were affected");

                // Load new config and compare general settings section
                var newConfig = await _configManager.LoadApplicationConfigAsync();
                if (!ConfigComparers.GeneralSettingsEqual(_generalSettingsConfig, newConfig.GeneralSettings))
                {
                    _logger.Info("General settings changed, updating external editor service");
                    LoadConfiguration();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error handling application config change: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Attempts to open the transformation configuration file in the configured external editor
        /// </summary>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        public Task<bool> TryOpenTransformationConfigAsync()
        {
            return TryOpenFileInEditorAsync(_transformationConfig.ConfigPath, "transformation config");
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
                if (string.IsNullOrWhiteSpace(_generalSettingsConfig.EditorCommand))
                {
                    _logger.Warning("Cannot open {0} in editor: editor command is not configured", configType);
                    return Task.FromResult(false);
                }

                // Replace %f placeholder with actual file path
                var commandWithFile = _generalSettingsConfig.EditorCommand.Replace("%f", filePath);

                _logger.Debug("Executing editor command: {0}", commandWithFile);

                // Parse command and arguments
                if (!TryParseCommand(commandWithFile, out var parseResult))
                {
                    _logger.Warning("Malformed editor command: {0}", commandWithFile);
                    return Task.FromResult(false);
                }

                var (executable, arguments) = parseResult;

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
        /// Attempts to parse a command line into executable and arguments
        /// </summary>
        /// <param name="commandLine">Full command line to parse</param>
        /// <param name="result">Tuple containing executable path and arguments</param>
        /// <returns>True if parsing succeeded, false if the command line is malformed</returns>
        private static bool TryParseCommand(string commandLine, out (string executable, string arguments) result)
        {
            result = (string.Empty, string.Empty);

            var trimmedCommand = commandLine.Trim();

            // Pattern: mandatory executable followed by optional space-delimited arguments
            var pattern = @"^(?<executable>(""[^""]+""|[^\s""']+))(?:\s+(?<argument>(""[^""]*""|[^""']+)))*$";

            var match = Regex.Match(trimmedCommand, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            if (!match.Success)
                return false;

            var executable = match.Groups["executable"].Value;
            var arguments = new List<string>();

            // Extract all arguments
            foreach (Capture capture in match.Groups["argument"].Captures)
            {
                arguments.Add(capture.Value);
            }

            // Remove quotes from executable if present
            executable = executable.Trim('"');

            // Remove quotes from arguments if present
            arguments = arguments.Select(arg => arg.Trim('"')).ToList();

            result = (executable, string.Join(" ", arguments));
            return true;
        }
    }
}