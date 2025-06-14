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
    /// Represents a single transformation rule with its expression and constraints
    /// </summary>
    public class TransformationRule
    {
        /// <summary>
        /// Gets the name of the transformation rule
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets the compiled mathematical expression for this rule
        /// </summary>
        public Expression Expression { get; }
        
        /// <summary>
        /// Gets the original string representation of the expression
        /// </summary>
        public string ExpressionString { get; }
        
        /// <summary>
        /// Gets the minimum allowed value for the transformation result
        /// </summary>
        public double Min { get; }
        
        /// <summary>
        /// Gets the maximum allowed value for the transformation result
        /// </summary>
        public double Max { get; }
        
        /// <summary>
        /// Gets the default value to use when the transformation fails
        /// </summary>
        public double DefaultValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationRule"/> class
        /// </summary>
        /// <param name="name">The name of the transformation rule</param>
        /// <param name="expression">The compiled mathematical expression</param>
        /// <param name="expressionString">The original string representation of the expression</param>
        /// <param name="min">The minimum allowed value for the transformation result</param>
        /// <param name="max">The maximum allowed value for the transformation result</param>
        /// <param name="defaultValue">The default value to use when the transformation fails</param>
        public TransformationRule(string name, Expression expression, string expressionString, 
            double min, double max, double defaultValue)
        {
            Name = name;
            Expression = expression;
            ExpressionString = expressionString;
            Min = min;
            Max = max;
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// Transforms tracking data into VTube Studio parameters according to transformation rules
    /// </summary>
    public class TransformationEngine : ITransformationEngine, IServiceStatsProvider
    {
        private const int MAX_EVALUATION_ITERATIONS = 10;
        private const string EVALUATION_ERROR_MESSAGE = "Failed to evaluate - missing dependencies or evaluation error";
        private const string EVALUATION_ERROR_TYPE = "Evaluation";
        private const string VALIDATION_ERROR_TYPE = "Validation";
        
        private readonly List<TransformationRule> _rules = new();
        private readonly IAppLogger _logger;
        
        // Statistics tracking fields
        private long _totalTransformations = 0;
        private long _successfulTransformations = 0;
        private long _failedTransformations = 0;
        private long _hotReloadAttempts = 0;
        private long _hotReloadSuccesses = 0;
        private DateTime _lastSuccessfulTransformation;
        private DateTime _rulesLoadedTime;
        private string _lastError = string.Empty;
        private string _configFilePath = string.Empty;
        private TransformationEngineStatus _currentStatus = TransformationEngineStatus.NeverLoaded;
        private readonly List<RuleInfo> _invalidRules = new();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationEngine"/> class
        /// </summary>
        /// <param name="logger">The logger instance for logging transformation operations</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
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
            
            var (validRules, invalidRules, validationErrors) = ValidateAndCreateRules(rules);
            
            UpdateRulesLoadedStatus(validRules, invalidRules, validationErrors);
        }
        
        /// <summary>
        /// Transforms tracking data into VTube Studio parameters according to loaded rules
        /// </summary>
        /// <param name="trackingData">The tracking data to transform</param>
        /// <returns>Collection of transformed parameters</returns>
        public PCTrackingInfo TransformData(PhoneTrackingInfo trackingData)
        {
            _totalTransformations++;
            
            try
            {
                if (_rules.Count == 0 || !trackingData.FaceFound)
                {
                    return HandleEmptyTransformation(trackingData);
                }
                
                return ProcessTransformation(trackingData);
            }
            catch (Exception ex)
            {
                return HandleTransformationError(ex, trackingData);
            }
        }
        
        private PCTrackingInfo HandleEmptyTransformation(PhoneTrackingInfo trackingData)
        {
            _successfulTransformations++;
            _lastSuccessfulTransformation = DateTime.UtcNow;
            return new PCTrackingInfo() { FaceFound = trackingData.FaceFound };
        }
        
        private PCTrackingInfo ProcessTransformation(PhoneTrackingInfo trackingData)
        {
            var trackingParameters = GetParametersFromTrackingData(trackingData);
            var (successfulRules, abandonedRules) = EvaluateRulesMultiPass(trackingParameters);
            
            UpdateTransformationStatus(successfulRules, abandonedRules);
            return BuildTransformationResult(trackingData, successfulRules);
        }
        
        private (List<TransformationRule> successful, List<TransformationRule> abandoned) 
            EvaluateRulesMultiPass(Dictionary<string, object> trackingParameters)
        {
            var remainingRules = new List<TransformationRule>(_rules);
            var successfulRules = new List<TransformationRule>();
            int currentIteration = 0;
            
            while (remainingRules.Count > 0 && currentIteration < MAX_EVALUATION_ITERATIONS)
            {
                currentIteration++;
                int rulesCountAtStart = remainingRules.Count;
                
                ProcessSingleEvaluationPass(remainingRules, successfulRules, trackingParameters);
                
                if (remainingRules.Count == rulesCountAtStart)
                {
                    break; // No progress made
                }
            }
            
            return (successfulRules, remainingRules);
        }
        
        private void ProcessSingleEvaluationPass(List<TransformationRule> remainingRules, 
            List<TransformationRule> successfulRules, Dictionary<string, object> trackingParameters)
        {
            for (int i = remainingRules.Count - 1; i >= 0; i--)
            {
                var rule = remainingRules[i];
                rule.Expression.Parameters.Clear();
                SetParametersOnExpression(rule.Expression, trackingParameters);
                
                if (TryEvaluateExpression(rule.Expression, out double evaluatedValue, out Exception evaluationError))
                {
                    var value = Math.Clamp(evaluatedValue, rule.Min, rule.Max);
                    
                    if (!trackingParameters.ContainsKey(rule.Name))
                    {
                        trackingParameters[rule.Name] = value;
                    }
                    
                    successfulRules.Add(rule);
                    remainingRules.RemoveAt(i);
                }
            }
        }
        
        private void UpdateTransformationStatus(List<TransformationRule> successfulRules, List<TransformationRule> abandonedRules)
        {
            _invalidRules.RemoveAll(rule => rule.Type == EVALUATION_ERROR_TYPE);
            
            foreach (var rule in abandonedRules)
            {
                _invalidRules.Add(new RuleInfo(rule.Name, rule.ExpressionString, EVALUATION_ERROR_MESSAGE, EVALUATION_ERROR_TYPE));
            }
            
            if (abandonedRules.Count == 0)
            {
                _currentStatus = TransformationEngineStatus.AllRulesValid;
            }
            else if (successfulRules.Count > 0)
            {
                _currentStatus = TransformationEngineStatus.RulesPartiallyValid;
            }
            
            _successfulTransformations++;
            _lastSuccessfulTransformation = DateTime.UtcNow;
        }
        
        private PCTrackingInfo BuildTransformationResult(PhoneTrackingInfo trackingData, List<TransformationRule> successfulRules)
        {
            var paramValues = new List<TrackingParam>();
            var paramDefinitions = new List<VTSParameter>();
            var paramExpressions = new Dictionary<string, string>();
            
            foreach (var rule in successfulRules)
            {
                paramValues.Add(new TrackingParam { Id = rule.Name, Value = GetRuleValue(rule) });
                paramDefinitions.Add(new VTSParameter(rule.Name, rule.Min, rule.Max, rule.DefaultValue));
                paramExpressions[rule.Name] = rule.ExpressionString;
            }
            
            return new PCTrackingInfo
            {
                FaceFound = trackingData.FaceFound,
                Parameters = paramValues,
                ParameterDefinitions = paramDefinitions.ToDictionary(p => p.Name, p => p),
                ParameterCalculationExpressions = paramExpressions
            };
        }
        
        private double GetRuleValue(TransformationRule rule)
        {
            var evaluatedValue = Convert.ToDouble(rule.Expression.Evaluate());
            return Math.Clamp(evaluatedValue, rule.Min, rule.Max);
        }
        
        private PCTrackingInfo HandleTransformationError(Exception ex, PhoneTrackingInfo trackingData)
        {
            _failedTransformations++;
            _lastError = $"Transformation failed: {ex.Message}";
            _logger.Error("Error during transformation: {0}", ex.Message);
            return new PCTrackingInfo() { FaceFound = trackingData.FaceFound };
        }
        
        private (int validRules, int invalidRules, List<string> validationErrors) 
            ValidateAndCreateRules(List<TransformRule> rules)
        {
            _rules.Clear();
            _invalidRules.Clear();
            
            int validRules = 0;
            int invalidRules = 0;
            var validationErrors = new List<string>();
            
            foreach (var rule in rules)
            {
                if (TryCreateTransformationRule(rule, out TransformationRule transformationRule, out string error))
                {
                    _rules.Add(transformationRule);
                    validRules++;
                }
                else
                {
                    validationErrors.Add(error);
                    _invalidRules.Add(new RuleInfo(rule.Name, rule.Func ?? string.Empty, error, VALIDATION_ERROR_TYPE));
                    invalidRules++;
                }
            }
            
            _logger.Info($"Loaded {validRules} valid transformation rules, skipped {invalidRules} invalid rules");
            return (validRules, invalidRules, validationErrors);
        }
        
        private bool TryCreateTransformationRule(TransformRule rule, out TransformationRule transformationRule, out string error)
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
                
                transformationRule = new TransformationRule(rule.Name, expression, rule.Func, rule.Min, rule.Max, rule.DefaultValue);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Error parsing expression '{rule.Func}': {ex.Message}";
                return false;
            }
        }
        
        private void UpdateRulesLoadedStatus(int validRules, int invalidRules, List<string> validationErrors)
        {
            if (validRules > 0 && invalidRules == 0)
            {
                _currentStatus = TransformationEngineStatus.AllRulesValid;
                _lastError = string.Empty;
            }
            else if (validRules > 0 && invalidRules > 0)
            {
                _currentStatus = TransformationEngineStatus.RulesPartiallyValid;
                _lastError = string.Empty;
            }
            else
            {
                _currentStatus = TransformationEngineStatus.NoValidRules;
                _lastError = "No valid transformation rules found in the configuration file.";
            }
            
            if (validRules > 0)
            {
                _rulesLoadedTime = DateTime.UtcNow;
                _hotReloadSuccesses++;
            }
            
            if (invalidRules > 0)
            {
                LogValidationErrors(validRules, invalidRules, validationErrors);
            }
        }
        

        
        private void LogValidationErrors(int validRules, int invalidRules, List<string> validationErrors)
        {
            string errorDetails = string.Join($"{Environment.NewLine}- ", validationErrors);
            var logMessage = $"Failed to load {invalidRules} transformation rules. Valid rules: {validRules}.";
            _logger.Error($"{logMessage}{Environment.NewLine}Errors:{Environment.NewLine}- {errorDetails}");
        }
        
        /// <summary>
        /// Gets parameters from tracking data as a dictionary
        /// </summary>
        private Dictionary<string, object> GetParametersFromTrackingData(PhoneTrackingInfo trackingData)
        {
            var parameters = new Dictionary<string, object>();
            
            AddPositionParameters(parameters, trackingData.Position);
            AddRotationParameters(parameters, trackingData.Rotation);
            AddEyeParameters(parameters, trackingData.EyeLeft, trackingData.EyeRight);
            AddBlendShapeParameters(parameters, trackingData.BlendShapes);
            
            return parameters;
        }
        
        private void AddPositionParameters(Dictionary<string, object> parameters, Coordinates position)
        {
            if (position != null)
            {
                parameters["HeadPosX"] = position.X;
                parameters["HeadPosY"] = position.Y;
                parameters["HeadPosZ"] = position.Z;
            }
        }
        
        private void AddRotationParameters(Dictionary<string, object> parameters, Coordinates rotation)
        {
            if (rotation != null)
            {
                parameters["HeadRotX"] = rotation.X;
                parameters["HeadRotY"] = rotation.Y;
                parameters["HeadRotZ"] = rotation.Z;
            }
        }
        
        private void AddEyeParameters(Dictionary<string, object> parameters, Coordinates eyeLeft, Coordinates eyeRight)
        {
            if (eyeLeft != null)
            {
                parameters["EyeLeftX"] = eyeLeft.X;
                parameters["EyeLeftY"] = eyeLeft.Y;
                parameters["EyeLeftZ"] = eyeLeft.Z;
            }
            
            if (eyeRight != null)
            {
                parameters["EyeRightX"] = eyeRight.X;
                parameters["EyeRightY"] = eyeRight.Y;
                parameters["EyeRightZ"] = eyeRight.Z;
            }
        }
        
        private void AddBlendShapeParameters(Dictionary<string, object> parameters, List<BlendShape> blendShapes)
        {
            if (blendShapes != null)
            {
                foreach (var shape in blendShapes)
                {
                    if (!string.IsNullOrEmpty(shape.Key))
                    {
                        parameters[shape.Key] = shape.Value;
                    }
                }
            }
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
            var requiredParameters = expression.GetParameterNames();
            var availableParameters = expression.Parameters.Keys;
            
            foreach (var requiredParam in requiredParameters)
            {
                if (!availableParameters.Contains(requiredParam))
                {
                    result = 0;
                    error = new ArgumentException($"Parameter '{requiredParam}' is not defined");
                    return false;
                }
            }
            
            try
            {
                result = Convert.ToDouble(expression.Evaluate());
                error = null!;
                return true;
            }
            catch (Exception ex)
            {
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
            
            if (_rulesLoadedTime != default)
            {
                counters["Uptime Since Rules Loaded (seconds)"] = (long)(DateTime.UtcNow - _rulesLoadedTime).TotalSeconds;
            }
            
            var currentEntity = new TransformationEngineInfo(
                configFilePath: _configFilePath ?? string.Empty,
                validRulesCount: _rules.Count,
                invalidRules: _invalidRules.AsReadOnly());
            
            bool isHealthy = _currentStatus == TransformationEngineStatus.AllRulesValid || 
                           _currentStatus == TransformationEngineStatus.RulesPartiallyValid;
            
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