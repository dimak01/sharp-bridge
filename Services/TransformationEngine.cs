using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NCalc;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Transforms tracking data into VTube Studio parameters according to transformation rules
    /// </summary>
    public class TransformationEngine : ITransformationEngine, IServiceStatsProvider
    {
        private readonly List<(string Name, Expression Expression, string ExpressionString, double Min, double Max, double DefaultValue)> _rules = new();
        private readonly IAppLogger _logger;
        
        // Statistics tracking fields
        private long _totalTransformations = 0;
        private long _successfulTransformations = 0;
        private long _failedTransformations = 0;
        private long _hotReloadAttempts = 0;
        private long _hotReloadSuccesses = 0;
        private DateTime _lastSuccessfulTransformation;
        private DateTime _rulesLoadedTime;
        private string _lastError;
        private string _configFilePath;
        private TransformationEngineStatus _currentStatus = TransformationEngineStatus.NeverLoaded;
        private readonly List<RuleInfo> _invalidRules = new();
        
        public TransformationEngine(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Loads transformation rules from the specified file
        /// </summary>
        /// <param name="filePath">Path to the transformation rules JSON file</param>
        /// <returns>An asynchronous operation that completes when rules are loaded</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified file doesn't exist</exception>
        /// <exception cref="JsonException">Thrown when the file contains invalid JSON</exception>
        /// <exception cref="InvalidOperationException">Thrown when one or more rules fail validation</exception>
        public async Task LoadRulesAsync(string filePath)
        {
            // Track hot reload attempts
            _hotReloadAttempts++;
            
            // Store config file path
            _configFilePath = filePath;
            
            if (!File.Exists(filePath))
            {
                _lastError = $"Transformation rules file not found: {filePath}";
                _currentStatus = TransformationEngineStatus.ConfigMissing;
                _logger.Error(_lastError);
                throw new FileNotFoundException(_lastError);
            }
            
            var json = await File.ReadAllTextAsync(filePath);
            
            var rules = JsonSerializer.Deserialize<List<TransformRule>>(json) ?? 
                throw new JsonException("Failed to deserialize transformation rules");
            
            _rules.Clear();
            _invalidRules.Clear(); // Clear previous invalid rules
            
            int validRules = 0;
            int invalidRules = 0;
            List<string> validationErrors = new List<string>();
            
            foreach (var rule in rules)
            {
                try
                {
                    // Check for null or empty expression
                    if (string.IsNullOrWhiteSpace(rule.Func))
                    {
                        string errorMsg = $"Rule '{rule.Name}' has an empty expression";
                        validationErrors.Add(errorMsg);
                        _invalidRules.Add(new RuleInfo(rule.Name, rule.Func ?? string.Empty, "Empty expression", "Validation"));
                        invalidRules++;
                        continue;
                    }
                    
                    // Create expression with default options
                    var expression = new Expression(rule.Func);
                    
                    // Check for syntax errors
                    if (expression.HasErrors())
                    {
                        string errorMsg = $"Syntax error in rule '{rule.Name}': {expression.Error}";
                        validationErrors.Add(errorMsg);
                        _invalidRules.Add(new RuleInfo(rule.Name, rule.Func, expression.Error?.Message ?? "Syntax error", "Validation"));
                        invalidRules++;
                        continue;
                    }
                    
                    // Check for valid min/max ranges
                    if (rule.Min > rule.Max)
                    {
                        string errorMsg = $"Rule '{rule.Name}' has Min value ({rule.Min}) greater than Max value ({rule.Max})";
                        validationErrors.Add(errorMsg);
                        _invalidRules.Add(new RuleInfo(rule.Name, rule.Func, $"Min ({rule.Min}) > Max ({rule.Max})", "Validation"));
                        invalidRules++;
                        continue; // Skip adding this rule
                    }
                    
                    // All validation passed, add the rule with the original expression string
                    _rules.Add((rule.Name, expression, rule.Func, rule.Min, rule.Max, rule.DefaultValue));
                    validRules++;
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Error parsing expression '{rule.Func}': {ex.Message}";
                    validationErrors.Add(errorMsg);
                    _invalidRules.Add(new RuleInfo(rule.Name, rule.Func, ex.Message, "Validation"));
                    invalidRules++;
                    // Continue with other rules even if one fails
                }
            }
            
            _logger.Info($"Loaded {validRules} valid transformation rules, skipped {invalidRules} invalid rules");
            
            // Update status and timestamps based on results
            if (validRules > 0 && invalidRules == 0)
            {
                _currentStatus = TransformationEngineStatus.Ready;
                _lastError = null; // Clear previous errors on successful load
            }
            else if (validRules > 0 && invalidRules > 0)
            {
                _currentStatus = TransformationEngineStatus.Partial;
                // Don't clear _lastError for partial loads as there are still issues
            }
            else if (validRules == 0 && invalidRules > 0)
            {
                _currentStatus = TransformationEngineStatus.NoValidRules;
            }
            else
            {
                _currentStatus = TransformationEngineStatus.NoValidRules;
            }
            
            // Set timestamps and success tracking for valid rules
            if (validRules > 0)
            {
                _rulesLoadedTime = DateTime.UtcNow;
                _hotReloadSuccesses++;
            }
            
            // Log validation errors but continue with graceful degradation
            if (invalidRules > 0)
            {
                string errorDetails = string.Join($"{Environment.NewLine}- ", validationErrors);
                _lastError = $"Failed to load {invalidRules} transformation rules. Valid rules: {validRules}.";
                _logger.Error($"{_lastError}{Environment.NewLine}Errors:{Environment.NewLine}- {errorDetails}");
                // Continue with valid rules - don't throw exception
            }
            
            // Log if no valid rules but continue operating (graceful degradation)
            if (validRules == 0)
            {
                _lastError = "No valid transformation rules found in the configuration file.";
                _logger.Error(_lastError);
                // Continue operating - transformation will return empty results but app won't crash
            }
        }
        
        /// <summary>
        /// Transforms tracking data into VTube Studio parameters according to loaded rules
        /// </summary>
        /// <param name="trackingData">The tracking data to transform</param>
        /// <returns>Collection of transformed parameters</returns>
        public PCTrackingInfo TransformData(PhoneTrackingInfo trackingData)
        {
            // Track total transformation attempts
            _totalTransformations++;
            
            try
            {
                if (_rules.Count == 0 || !trackingData.FaceFound)
                {
                    _successfulTransformations++; // Count as successful even if no processing needed
                    _lastSuccessfulTransformation = DateTime.UtcNow;
                return new PCTrackingInfo() { FaceFound = trackingData.FaceFound };
            }
            
            var paramValues = new List<TrackingParam>();
            var paramDefinitions = new List<VTSParameter>();
            var paramExpressions = new Dictionary<string, string>();

            // Get parameters from tracking data
            var trackingParameters = GetParametersFromTrackingData(trackingData);

            // Create a working copy of rules that we can modify during evaluation
            var remainingRules = new List<(string Name, Expression Expression, string ExpressionString, double Min, double Max, double DefaultValue)>(_rules);
            
            // Multi-pass evaluation with progress tracking
            const int maxIterations = 10; // Prevent infinite loops
            int currentIteration = 0;
            
            while (remainingRules.Count > 0 && currentIteration < maxIterations)
            {
                currentIteration++;
                int rulesCountAtStart = remainingRules.Count;
                
                // TODO: Add pass-level logging for debugging when needed
                
                for (int i = remainingRules.Count - 1; i >= 0; i--)
                {
                    var (name, expression, expressionString, min, max, defaultValue) = remainingRules[i];
                    expression.Parameters.Clear();
                    // Set parameters from tracking data (and any previously calculated custom parameters)
                    SetParametersOnExpression(expression, trackingParameters);
                    
                    // Test if expression can be evaluated with current parameters
                    if (TryEvaluateExpression(expression, out double evaluatedValue, out Exception evaluationError))
                    {
                        // Expression is valid - evaluate, clamp, and store result
                        var value = Math.Clamp(evaluatedValue, min, max);
                        
                        paramValues.Add(new TrackingParam
                        {
                            Id = name,
                            Value = value
                        });

                        paramDefinitions.Add(new VTSParameter(name, min, max, defaultValue));
                        paramExpressions[name] = expressionString;

                        // Add this parameter to trackingParameters for future rules to reference
                        // Only add if not already present - blend shapes and first-evaluated parameters win
                        if (!trackingParameters.ContainsKey(name))
                        {
                            trackingParameters[name] = value;
                        }
                        
                        // Remove successfully evaluated rule from remaining rules
                        remainingRules.RemoveAt(i);
                        
                        // TODO: Add per-parameter success logging for debugging when needed
                    }
                    // If evaluation failed, it must be a parameter dependency issue since
                    // syntax errors are caught during initialization - keep for next pass
                }
                
                // Check if we made progress in this pass
                if (remainingRules.Count == rulesCountAtStart)
                {
                    // No progress made - stop evaluation
                    // TODO: Add warning logging for unresolved dependencies when needed
                    break;
                }
                
                // TODO: Add pass completion logging when needed
            }
            
                // Track abandoned rules from this transformation by adding them to invalid rules
                // First, remove any previous evaluation failures (keep validation failures)
                _invalidRules.RemoveAll(rule => rule.Type == "Evaluation");
                
                // Add new evaluation failures
                foreach (var (name, _, expressionString, _, _, _) in remainingRules)
                {
                    _invalidRules.Add(new RuleInfo(name, expressionString, "Failed to evaluate - missing dependencies or evaluation error", "Evaluation"));
                }
                
                // Update status based on evaluation results
                if (remainingRules.Count == 0)
                {
                    _currentStatus = TransformationEngineStatus.Ready; // All rules evaluated successfully
                }
                else if (paramValues.Count > 0)
                {
                    _currentStatus = TransformationEngineStatus.Partial; // Some rules worked
                }
                
                _successfulTransformations++;
                _lastSuccessfulTransformation = DateTime.UtcNow;
            
            return new PCTrackingInfo
            {
                FaceFound = trackingData.FaceFound,
                Parameters = paramValues,
                ParameterDefinitions = paramDefinitions.ToDictionary(p => p.Name, p => p),
                ParameterCalculationExpressions = paramExpressions
            };
            }
            catch (Exception ex)
            {
                _failedTransformations++;
                _lastError = $"Transformation failed: {ex.Message}";
                _logger.Error("Error during transformation: {0}", ex.Message);
                
                // Return empty result on failure
                return new PCTrackingInfo() { FaceFound = trackingData.FaceFound };
            }
        }
        
        /// <summary>
        /// Gets parameters from tracking data as a dictionary
        /// </summary>
        private Dictionary<string, object> GetParametersFromTrackingData(PhoneTrackingInfo trackingData)
        {
            var parameters = new Dictionary<string, object>();
            
            // Add head position
            if (trackingData.Position != null)
            {
                parameters["HeadPosX"] = trackingData.Position.X;
                parameters["HeadPosY"] = trackingData.Position.Y;
                parameters["HeadPosZ"] = trackingData.Position.Z;
            }
            
            // Add head rotation
            if (trackingData.Rotation != null)
            {
                parameters["HeadRotX"] = trackingData.Rotation.X;
                parameters["HeadRotY"] = trackingData.Rotation.Y;
                parameters["HeadRotZ"] = trackingData.Rotation.Z;
            }
            
            // Add eye positions
            if (trackingData.EyeLeft != null)
            {
                parameters["EyeLeftX"] = trackingData.EyeLeft.X;
                parameters["EyeLeftY"] = trackingData.EyeLeft.Y;
                parameters["EyeLeftZ"] = trackingData.EyeLeft.Z;
            }
            
            if (trackingData.EyeRight != null)
            {
                parameters["EyeRightX"] = trackingData.EyeRight.X;
                parameters["EyeRightY"] = trackingData.EyeRight.Y;
                parameters["EyeRightZ"] = trackingData.EyeRight.Z;
            }
            
            // Add blend shapes
            if (trackingData.BlendShapes != null)
            {
                foreach (var shape in trackingData.BlendShapes)
                {
                    if (!string.IsNullOrEmpty(shape.Key))
                    {
                        parameters[shape.Key] = shape.Value;
                    }
                }
            }
            
            return parameters;
        }

        /// <summary>
        /// Sets parameters on an expression from a dictionary
        /// </summary>
        private void SetParametersOnExpression(Expression expression, Dictionary<string, object> parameters)
        {
            foreach (var param in parameters)
            {
                expression.Parameters[param.Key] = param.Value;
            }
        }
        
        /// <summary>
        /// Attempts to evaluate an expression, returning success/failure and any error
        /// </summary>
        private bool TryEvaluateExpression(Expression expression, out double result, out Exception error)
        {
            // Check if all required parameters are available before evaluation
            var requiredParameters = expression.GetParameterNames();
            var availableParameters = expression.Parameters.Keys;
            
            foreach (var requiredParam in requiredParameters)
            {
                if (!availableParameters.Contains(requiredParam))
                {
                    // Missing parameter - cannot evaluate yet
                    result = 0;
                    error = new ArgumentException($"Parameter '{requiredParam}' is not defined");
                    return false;
                }
            }
            
            // All parameters are available - safe to evaluate
            try
            {
                result = Convert.ToDouble(expression.Evaluate());
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                // This should be rare now, but handle any other evaluation errors
                result = 0;
                error = ex;
                return false;
            }
        }
        
        /// <summary>
        /// Gets the current service statistics
        /// </summary>
        public IServiceStats GetServiceStats()
        {
            var counters = new Dictionary<string, long>
            {
                ["Total Transformations"] = _totalTransformations,
                ["Successful Transformations"] = _successfulTransformations,
                ["Failed Transformations"] = _failedTransformations,
                ["Hot Reload Attempts"] = _hotReloadAttempts,
                ["Hot Reload Successes"] = _hotReloadSuccesses,
                ["Valid Rules"] = _rules.Count,
                ["Invalid Rules"] = _invalidRules.Count
            };
            
            // Add uptime since rules loaded if applicable
            if (_rulesLoadedTime != default)
            {
                counters["Uptime Since Rules Loaded (seconds)"] = (long)(DateTime.UtcNow - _rulesLoadedTime).TotalSeconds;
            }
            
            var currentEntity = new TransformationEngineInfo(
                configFilePath: _configFilePath ?? string.Empty,
                validRulesCount: _rules.Count,
                invalidRules: _invalidRules.AsReadOnly());
            
            // Determine if service is healthy
            bool isHealthy = _currentStatus == TransformationEngineStatus.Ready || 
                           _currentStatus == TransformationEngineStatus.Partial;
            
            return new ServiceStats(
                serviceName: "Transformation Engine",
                status: _currentStatus.ToString(),
                currentEntity: currentEntity,
                isHealthy: isHealthy,
                lastSuccessfulOperation: _lastSuccessfulTransformation,
                lastError: _lastError,
                counters: counters);
        }
        
        /// <summary>
        /// Gets all parameters defined in the loaded transformation rules
        /// </summary>
        /// <returns>Collection of parameter definitions</returns>
        public IEnumerable<VTSParameter> GetParameterDefinitions()
        {
            return _rules.Select(rule => new VTSParameter(rule.Name, rule.Min, rule.Max, rule.DefaultValue));
        }
    }
} 