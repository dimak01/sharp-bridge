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
    public class TransformationEngine : ITransformationEngine
    {
        private readonly List<(string Name, Expression Expression, string ExpressionString, double Min, double Max, double DefaultValue)> _rules = new();
        private readonly IAppLogger _logger;
        
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
            if (!File.Exists(filePath))
            {
                _logger.Error($"Transformation rules file not found: {filePath}");
                throw new FileNotFoundException($"Transformation rules file not found: {filePath}");
            }
            
            var json = await File.ReadAllTextAsync(filePath);
            
            var rules = JsonSerializer.Deserialize<List<TransformRule>>(json) ?? 
                throw new JsonException("Failed to deserialize transformation rules");
            
            _rules.Clear();
            
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
                        invalidRules++;
                        continue;
                    }
                    
                    // Check for valid min/max ranges
                    if (rule.Min > rule.Max)
                    {
                        string errorMsg = $"Rule '{rule.Name}' has Min value ({rule.Min}) greater than Max value ({rule.Max})";
                        validationErrors.Add(errorMsg);
                        // Still add the rule but count it as invalid
                        invalidRules++;
                    }
                    
                    // All validation passed, add the rule with the original expression string
                    _rules.Add((rule.Name, expression, rule.Func, rule.Min, rule.Max, rule.DefaultValue));
                    validRules++;
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Error parsing expression '{rule.Func}': {ex.Message}";
                    validationErrors.Add(errorMsg);
                    invalidRules++;
                    // Continue with other rules even if one fails
                }
            }
            
            _logger.Info($"Loaded {validRules} valid transformation rules, skipped {invalidRules} invalid rules");
            
            // Throw an exception if any rules failed validation
            if (invalidRules > 0)
            {
                string errorDetails = string.Join($"{Environment.NewLine}- ", validationErrors);
                _logger.Error($"Failed to load {invalidRules} transformation rules. Valid rules: {validRules}.{Environment.NewLine}Errors:{Environment.NewLine}- {errorDetails}");
                throw new InvalidOperationException(
                    $"Failed to load {invalidRules} transformation rules. Valid rules: {validRules}.{Environment.NewLine}" +
                    $"Errors:{Environment.NewLine}- {errorDetails}");
            }
            
            // Check if we have at least one valid rule
            if (validRules == 0)
            {
                _logger.Error("No valid transformation rules found in the configuration file.");
                throw new InvalidOperationException("No valid transformation rules found in the configuration file.");
            }
        }
        
        /// <summary>
        /// Transforms tracking data into VTube Studio parameters according to loaded rules
        /// </summary>
        /// <param name="trackingData">The tracking data to transform</param>
        /// <returns>Collection of transformed parameters</returns>
        public PCTrackingInfo TransformData(PhoneTrackingInfo trackingData)
        {
            if (_rules.Count == 0 || !trackingData.FaceFound)
            {
                return new PCTrackingInfo() { FaceFound = trackingData.FaceFound };
            }
            
            var paramValues = new List<TrackingParam>();
            var paramDefinitions = new List<VTSParameter>();
            var paramExpressions = new Dictionary<string, string>();

            // Get parameters from tracking data
            var trackingParameters = GetParametersFromTrackingData(trackingData);

            // Create a working copy of rules that we can modify during evaluation
            var remainingRules = new List<(string Name, Expression Expression, string ExpressionString, double Min, double Max, double DefaultValue)>(_rules);
            
            for (int i = remainingRules.Count - 1; i >= 0; i--)
            {
                var (name, expression, expressionString, min, max, defaultValue) = remainingRules[i];
                
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
                    trackingParameters[name] = value;
                    
                    // Remove successfully evaluated rule from remaining rules
                    remainingRules.RemoveAt(i);
                }
            }
            
            return new PCTrackingInfo
            {
                FaceFound = trackingData.FaceFound,
                Parameters = paramValues,
                ParameterDefinitions = paramDefinitions.ToDictionary(p => p.Name, p => p),
                ParameterCalculationExpressions = paramExpressions
            };
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
        /// Gets all parameters defined in the loaded transformation rules
        /// </summary>
        /// <returns>Collection of parameter definitions</returns>
        public IEnumerable<VTSParameter> GetParameterDefinitions()
        {
            return _rules.Select(rule => new VTSParameter(rule.Name, rule.Min, rule.Max, rule.DefaultValue));
        }
    }
} 