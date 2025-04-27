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
        private readonly List<(string Name, Expression Expression, double Min, double Max, double DefaultValue)> _rules = new();
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
                    
                    // All validation passed, add the rule
                    _rules.Add((rule.Name, expression, rule.Min, rule.Max, rule.DefaultValue));
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

            foreach (var (name, expression, min, max, defaultValue) in _rules)
            {
                try
                {
                    // Set parameters from tracking data
                    SetParametersFromTrackingData(expression, trackingData);
                    
                    // Evaluate and clamp value
                    var value = Convert.ToDouble(expression.Evaluate());
                    value = Math.Clamp(value, min, max);
                    
                    paramValues.Add(new TrackingParam
                    {
                        Id = name,
                        Value = value
                    });

                    paramDefinitions.Add(new VTSParameter(name, min, max, defaultValue));
                }
                catch (Exception ex)
                {
                    // Log the error
                    _logger.Error($"Error evaluating expression for parameter '{name}': {ex.Message}", ex);
                    
                    // Instead of silently using default values, throw an exception with context
                    throw new InvalidOperationException(
                        $"Error evaluating expression for parameter '{name}': {ex.Message}", ex);
                }
            }
            
            return new PCTrackingInfo
            {
                FaceFound = trackingData.FaceFound,
                Parameters = paramValues,
                ParameterDefinitions = paramDefinitions.ToDictionary(p => p.Name, p => p)
            };
        }
        
        /// <summary>
        /// Sets parameters on the expression from tracking data
        /// </summary>
        private void SetParametersFromTrackingData(Expression expression, PhoneTrackingInfo trackingData)
        {
            // Add head position
            if (trackingData.Position != null)
            {
                expression.Parameters["HeadPosX"] = trackingData.Position.X;
                expression.Parameters["HeadPosY"] = trackingData.Position.Y;
                expression.Parameters["HeadPosZ"] = trackingData.Position.Z;
            }
            
            // Add head rotation
            if (trackingData.Rotation != null)
            {
                expression.Parameters["HeadRotX"] = trackingData.Rotation.X;
                expression.Parameters["HeadRotY"] = trackingData.Rotation.Y;
                expression.Parameters["HeadRotZ"] = trackingData.Rotation.Z;
            }
            
            // Add eye positions
            if (trackingData.EyeLeft != null)
            {
                expression.Parameters["EyeLeftX"] = trackingData.EyeLeft.X;
                expression.Parameters["EyeLeftY"] = trackingData.EyeLeft.Y;
                expression.Parameters["EyeLeftZ"] = trackingData.EyeLeft.Z;
            }
            
            if (trackingData.EyeRight != null)
            {
                expression.Parameters["EyeRightX"] = trackingData.EyeRight.X;
                expression.Parameters["EyeRightY"] = trackingData.EyeRight.Y;
                expression.Parameters["EyeRightZ"] = trackingData.EyeRight.Z;
            }
            
            // Add blend shapes
            if (trackingData.BlendShapes != null)
            {
                foreach (var shape in trackingData.BlendShapes)
                {
                    if (!string.IsNullOrEmpty(shape.Key))
                    {
                        expression.Parameters[shape.Key] = shape.Value;
                    }
                }
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