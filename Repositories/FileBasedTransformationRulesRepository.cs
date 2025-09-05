using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NCalc;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Services;

namespace SharpBridge.Repositories
{
    /// <summary>
    /// File-based implementation of transformation rules repository with caching and graceful error handling
    /// </summary>
    public sealed class FileBasedTransformationRulesRepository : ITransformationRulesRepository
    {
        private const string VALIDATION_ERROR_TYPE = "Validation";

        private readonly IAppLogger _logger;
        private readonly IFileChangeWatcher _fileWatcher;
        private readonly IFileChangeWatcher _appConfigWatcher;
        private readonly IConfigManager _configManager;
        private bool _isUpToDate = true;
        private string _currentFilePath = string.Empty;
        private DateTime _lastLoadTime;
        private bool _disposed = false;

        // Cache for graceful fallback
        private List<ParameterTransformation> _cachedValidRules = new();
        private List<RuleInfo> _cachedInvalidRules = new();
        private List<string> _cachedValidationErrors = new();
        private bool _hasCachedRules = false;

        /// <summary>
        /// Event raised when the rules file changes on disk
        /// </summary>
        public event EventHandler<RulesChangedEventArgs>? RulesChanged;

        /// <summary>
        /// Gets whether the currently loaded rules are up to date with the source
        /// </summary>
        public bool IsUpToDate => _isUpToDate;

        /// <summary>
        /// Gets the path to the currently loaded rules file
        /// </summary>
        public string TransformationRulesPath => _currentFilePath;

        /// <summary>
        /// Gets the timestamp when rules were last successfully loaded
        /// </summary>
        public DateTime LastLoadTime => _lastLoadTime;

        /// <summary>
        /// Initializes a new instance of the FileBasedTransformationRulesRepository
        /// </summary>
        /// <param name="logger">Logger for recording operations and errors</param>
        /// <param name="fileWatcher">File watcher for monitoring transformation config file changes</param>
        /// <param name="appConfigWatcher">File watcher for monitoring application config changes</param>
        /// <param name="configManager">Configuration manager for loading application config</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        public FileBasedTransformationRulesRepository(IAppLogger logger, IFileChangeWatcher fileWatcher, IFileChangeWatcher appConfigWatcher, IConfigManager configManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _appConfigWatcher = appConfigWatcher ?? throw new ArgumentNullException(nameof(appConfigWatcher));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));

            _fileWatcher.FileChanged += OnFileChanged;
            _appConfigWatcher.FileChanged += OnApplicationConfigChanged;
        }

        /// <summary>
        /// Loads transformation rules from the configured path in application config
        /// </summary>
        /// <returns>Result containing loaded rules, validation errors, and cache information</returns>
        public async Task<RulesLoadResult> LoadRulesAsync()
        {
            ThrowIfDisposed();

            try
            {
                var appConfig = await _configManager.LoadApplicationConfigAsync();
                var configPath = appConfig.TransformationEngine.ConfigPath;

                if (string.IsNullOrEmpty(configPath))
                {
                    return HandleCriticalError("Transformation engine config path is not specified in application config");
                }

                return await LoadRulesFromPathAsync(configPath);
            }
            catch (Exception ex)
            {
                return HandleCriticalError($"Failed to load application config: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads transformation rules from the specified file path (interface compatibility)
        /// </summary>
        /// <param name="filePath">Path to the transformation rules file</param>
        /// <returns>Result containing loaded rules, validation errors, and cache information</returns>
        public async Task<RulesLoadResult> LoadRulesAsync(string filePath)
        {
            return await LoadRulesFromPathAsync(filePath);
        }

        /// <summary>
        /// Loads transformation rules from the specified file path
        /// </summary>
        /// <param name="filePath">Path to the transformation rules file</param>
        /// <returns>Result containing loaded rules, validation errors, and cache information</returns>
        public async Task<RulesLoadResult> LoadRulesFromPathAsync(string filePath)
        {
            ThrowIfDisposed();

            // Stop watching previous file if any
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                _fileWatcher.StopWatching();
            }

            // Update file path tracking
            _currentFilePath = Path.GetFullPath(filePath);

            try
            {
                // Validate file exists
                if (!File.Exists(_currentFilePath))
                {
                    return HandleCriticalError($"Rules file not found: {_currentFilePath}");
                }

                // Load and parse JSON
                var json = await File.ReadAllTextAsync(_currentFilePath);
                var ruleDefinitions = JsonSerializer.Deserialize<List<ParameterRuleDefinition>>(json);

                if (ruleDefinitions == null)
                {
                    return HandleCriticalError("Failed to deserialize transformation rules - file returned null");
                }

                // Validate and create rules
                var result = ValidateAndCreateRules(ruleDefinitions);

                // SUCCESS: Update cache and status
                _cachedValidRules = new List<ParameterTransformation>(result.ValidRules);
                _cachedInvalidRules = new List<RuleInfo>(result.InvalidRules);
                _cachedValidationErrors = new List<string>(result.ValidationErrors);
                _hasCachedRules = true;
                _isUpToDate = true;
                _lastLoadTime = DateTime.UtcNow;

                // Start watching the file
                _fileWatcher.StartWatching(_currentFilePath);

                _logger.Info($"Successfully loaded {result.ValidRules.Count} valid rules, {result.InvalidRules.Count} invalid rules from {_currentFilePath}");

                return result;
            }
            catch (JsonException ex)
            {
                return HandleCriticalError($"JSON parsing error: {ex.Message}");
            }
            catch (IOException ex)
            {
                return HandleCriticalError($"File access error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return HandleCriticalError($"Unexpected error loading rules: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles critical loading errors with graceful fallback to cached rules
        /// </summary>
        private RulesLoadResult HandleCriticalError(string errorMessage)
        {
            _logger.Error($"Critical config loading error: {errorMessage}");
            _isUpToDate = false;  // Mark as out of date due to error

            if (_hasCachedRules)
            {
                _logger.Warning("Returning cached rules due to loading error");
                return new RulesLoadResult(
                    validRules: new List<ParameterTransformation>(_cachedValidRules),
                    invalidRules: new List<RuleInfo>(_cachedInvalidRules),
                    validationErrors: new List<string>(_cachedValidationErrors),
                    loadedFromCache: true,
                    loadError: errorMessage);
            }
            else
            {
                _logger.Error("No cached rules available, returning empty rule set");
                return new RulesLoadResult(
                    validRules: new List<ParameterTransformation>(),
                    invalidRules: new List<RuleInfo>(),
                    validationErrors: new List<string> { errorMessage },
                    loadedFromCache: false,
                    loadError: errorMessage);
            }
        }

        /// <summary>
        /// Validates rule definitions and creates transformation rules
        /// </summary>
        private RulesLoadResult ValidateAndCreateRules(List<ParameterRuleDefinition> ruleDefinitions)
        {
            var validRules = new List<ParameterTransformation>();
            var invalidRules = new List<RuleInfo>();
            var validationErrors = new List<string>();

            foreach (var rule in ruleDefinitions)
            {
                if (TryCreateTransformationRule(rule, out ParameterTransformation transformationRule, out string error))
                {
                    validRules.Add(transformationRule);
                }
                else
                {
                    validationErrors.Add(error);
                    invalidRules.Add(new RuleInfo(rule.Name, rule.Func ?? string.Empty, error, VALIDATION_ERROR_TYPE));
                }
            }

            _logger.Info($"Validated {validRules.Count} valid transformation rules, {invalidRules.Count} invalid rules");

            return new RulesLoadResult(validRules, invalidRules, validationErrors);
        }

        /// <summary>
        /// Attempts to create a transformation rule from a rule definition
        /// </summary>
        private static bool TryCreateTransformationRule(ParameterRuleDefinition rule, out ParameterTransformation transformationRule, out string error)
        {
            transformationRule = null!;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(rule.Func))
            {
                error = $"Rule '{rule.Name}' has an empty expression";
                return false;
            }

            try
            {
                var expression = new Expression(rule.Func);

                if (expression.HasErrors())
                {
                    error = $"Syntax error in rule '{rule.Name}': {expression.Error}";
                    return false;
                }

                if (rule.Min > rule.Max)
                {
                    error = $"Rule '{rule.Name}' has Min value ({rule.Min}) greater than Max value ({rule.Max})";
                    return false;
                }

                // Validate interpolation configuration if present
                if (rule.Interpolation != null)
                {
                    if (!InterpolationMethodFactory.ValidateDefinition(rule.Interpolation))
                    {
                        error = $"Rule '{rule.Name}' has invalid interpolation configuration";
                        return false;
                    }
                }

                transformationRule = new ParameterTransformation(rule.Name, expression, rule.Func, rule.Min, rule.Max, rule.DefaultValue, rule.Interpolation);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Error parsing expression '{rule.Func}': {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Compares two file paths for equality after normalizing them
        /// </summary>
        /// <param name="path1">First path to compare</param>
        /// <param name="path2">Second path to compare</param>
        /// <returns>True if the paths refer to the same file, false otherwise</returns>
        private static bool ArePathsEqual(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
                return path1 == path2;

            return Path.GetFullPath(path1).Equals(Path.GetFullPath(path2), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Handles file change events from the transformation rules file watcher
        /// </summary>
        private void OnFileChanged(object? sender, FileChangeEventArgs e)
        {
            if (ArePathsEqual(e.FilePath, _currentFilePath))
            {
                _isUpToDate = false;
                _logger.Debug($"Rules file changed: {e.FilePath}");
                RulesChanged?.Invoke(this, new RulesChangedEventArgs(e.FilePath));
            }
        }

        /// <summary>
        /// Handles application configuration file changes
        /// </summary>
        private async void OnApplicationConfigChanged(object? sender, FileChangeEventArgs e)
        {
            try
            {
                _logger.Debug("Application config changed, checking if transformation engine config path was affected");

                // Load new app config and check if transformation engine config path changed
                var newAppConfig = await _configManager.LoadApplicationConfigAsync();
                var newConfigPath = newAppConfig.TransformationEngine.ConfigPath;

                if (string.IsNullOrEmpty(newConfigPath))
                {
                    _logger.Warning("Transformation engine config path is empty in application config");
                    return;
                }

                if (!ArePathsEqual(newConfigPath, _currentFilePath))
                {
                    _logger.Info($"Transformation engine config path changed from '{_currentFilePath}' to '{newConfigPath}', reloading rules");
                    _isUpToDate = false;

                    // Notify subscribers that rules have changed
                    RulesChanged?.Invoke(this, new RulesChangedEventArgs(newConfigPath));
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error handling application config change", ex);
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the repository has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        /// <summary>
        /// Disposes the repository and releases resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _fileWatcher.FileChanged -= OnFileChanged;
                _appConfigWatcher.FileChanged -= OnApplicationConfigChanged;
                _fileWatcher.Dispose();
                _appConfigWatcher.Dispose();
                _disposed = true;
            }
        }
    }
}