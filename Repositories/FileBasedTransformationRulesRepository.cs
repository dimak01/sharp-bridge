using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NCalc;
using SharpBridge.Interfaces;
using SharpBridge.Models;

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
        public string CurrentFilePath => _currentFilePath;
        
        /// <summary>
        /// Gets the timestamp when rules were last successfully loaded
        /// </summary>
        public DateTime LastLoadTime => _lastLoadTime;
        
        /// <summary>
        /// Initializes a new instance of the FileBasedTransformationRulesRepository
        /// </summary>
        /// <param name="logger">Logger for recording operations and errors</param>
        /// <param name="fileWatcher">File watcher for monitoring config file changes</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or fileWatcher is null</exception>
        public FileBasedTransformationRulesRepository(IAppLogger logger, IFileChangeWatcher fileWatcher)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _fileWatcher.FileChanged += OnFileChanged;
        }
        
        /// <summary>
        /// Loads transformation rules from the specified file path
        /// </summary>
        /// <param name="filePath">Path to the transformation rules file</param>
        /// <returns>Result containing loaded rules, validation errors, and cache information</returns>
        public async Task<RulesLoadResult> LoadRulesAsync(string filePath)
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
                
                transformationRule = new ParameterTransformation(rule.Name, expression, rule.Func, rule.Min, rule.Max, rule.DefaultValue);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Error parsing expression '{rule.Func}': {ex.Message}";
                return false;
            }
        }
        
        /// <summary>
        /// Handles file change events from the file watcher
        /// </summary>
        private void OnFileChanged(object? sender, FileChangeEventArgs e)
        {
            if (e.FilePath == _currentFilePath)
            {
                _isUpToDate = false;
                _logger.Debug($"Rules file changed: {e.FilePath}");
                RulesChanged?.Invoke(this, new RulesChangedEventArgs(e.FilePath));
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
                _fileWatcher.Dispose();
                _disposed = true;
            }
        }
    }
} 