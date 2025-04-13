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
        
        /// <summary>
        /// Loads transformation rules from the specified file
        /// </summary>
        /// <param name="filePath">Path to the transformation rules JSON file</param>
        /// <returns>An asynchronous operation that completes when rules are loaded</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified file doesn't exist</exception>
        /// <exception cref="JsonException">Thrown when the file contains invalid JSON</exception>
        public async Task LoadRulesAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Transformation rules file not found: {filePath}");
            }
            
            var json = await File.ReadAllTextAsync(filePath);
            
            var rules = JsonSerializer.Deserialize<List<TransformRule>>(json) ?? 
                throw new JsonException("Failed to deserialize transformation rules");
            
            _rules.Clear();
            
            int validRules = 0;
            int invalidRules = 0;
            
            foreach (var rule in rules)
            {
                try
                {
                    // Check for null or empty expression
                    if (string.IsNullOrWhiteSpace(rule.Func))
                    {
                        Console.WriteLine($"Warning: Rule '{rule.Name}' has an empty expression");
                        invalidRules++;
                        continue;
                    }
                    
                    // Create expression with default options
                    var expression = new Expression(rule.Func);
                    
                    // Check for syntax errors
                    if (expression.HasErrors())
                    {
                        Console.WriteLine($"Syntax error in rule '{rule.Name}': {expression.Error}");
                        invalidRules++;
                        continue;
                    }
                    
                    // Check for valid min/max ranges
                    if (rule.Min > rule.Max)
                    {
                        Console.WriteLine($"Warning: Rule '{rule.Name}' has Min value ({rule.Min}) greater than Max value ({rule.Max})");
                    }
                    
                    // All validation passed, add the rule
                    _rules.Add((rule.Name, expression, rule.Min, rule.Max, rule.DefaultValue));
                    validRules++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing expression '{rule.Func}': {ex.Message}");
                    invalidRules++;
                    // Continue with other rules even if one fails
                }
            }
            
            Console.WriteLine($"Loaded {validRules} valid transformation rules, skipped {invalidRules} invalid rules");
        }
        
        /// <summary>
        /// Transforms tracking data into VTube Studio parameters according to loaded rules
        /// </summary>
        /// <param name="trackingData">The tracking data to transform</param>
        /// <returns>Collection of transformed parameters</returns>
        public IEnumerable<TrackingParam> TransformData(TrackingResponse trackingData)
        {
            if (_rules.Count == 0 || !trackingData.FaceFound)
            {
                return Enumerable.Empty<TrackingParam>();
            }
            
            var results = new List<TrackingParam>();
            
            foreach (var (name, expression, min, max, defaultValue) in _rules)
            {
                try
                {
                    // Set parameters from tracking data
                    SetParametersFromTrackingData(expression, trackingData);
                    
                    // Evaluate and clamp value
                    var value = Convert.ToDouble(expression.Evaluate());
                    value = Math.Clamp(value, min, max);
                    
                    results.Add(new TrackingParam
                    {
                        Id = name,
                        Value = value,
                        Weight = 1.0 // Default weight
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error evaluating expression for parameter '{name}': {ex.Message}");
                    results.Add(new TrackingParam
                    {
                        Id = name,
                        Value = defaultValue,
                        Weight = 1.0 // Default weight
                    });
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Sets parameters on the expression from tracking data
        /// </summary>
        private void SetParametersFromTrackingData(Expression expression, TrackingResponse trackingData)
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
    }
} 