using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Utilities;

namespace SharpBridge.Services
{
    /// <summary>
    /// Service implementation for providing color-coded parameter names and expressions.
    /// Provides color coding to visually distinguish blend shapes (cyan) from calculated parameters (yellow).
    /// </summary>
    public class ParameterColorService : IParameterColorService
    {
        private readonly IAppLogger _logger;
        
        /// <summary>
        /// Set of known blend shape names for expression coloring
        /// </summary>
        private readonly HashSet<string> _blendShapeNames = new HashSet<string>();
        
        /// <summary>
        /// Set of known calculated parameter names for expression coloring
        /// </summary>
        private readonly HashSet<string> _calculatedParameterNames = new HashSet<string>();
        
        /// <summary>
        /// Cache for colored expressions to avoid repeated processing
        /// </summary>
        private readonly Dictionary<string, string> _coloredExpressionCache = new Dictionary<string, string>();
        
        /// <summary>
        /// Initializes a new instance of the ParameterColorService
        /// </summary>
        /// <param name="logger">Logger for debugging and diagnostics</param>
        public ParameterColorService(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Initialize the service with transformation configuration data.
        /// Stores parameter names for expression coloring.
        /// </summary>
        /// <param name="expressions">Dictionary of parameter names to their transformation expressions</param>
        /// <param name="blendShapeNames">Collection of blend shape names from iPhone tracking data</param>
        public void InitializeFromConfiguration(Dictionary<string, string> expressions, IEnumerable<string> blendShapeNames)
        {
            if (expressions == null)
            {
                _logger.Warning("ParameterColorService initialized with null expressions dictionary");
                return;
            }
            
            if (blendShapeNames == null)
            {
                _logger.Warning("ParameterColorService initialized with null blend shape names");
                return;
            }
            
            // Clear existing sets and cache
            _blendShapeNames.Clear();
            _calculatedParameterNames.Clear();
            _coloredExpressionCache.Clear();
            
            var expressionCount = expressions.Count;
            var blendShapeCount = blendShapeNames.Count();
            
            // Store blend shape names for expression coloring
            foreach (var blendShapeName in blendShapeNames)
            {
                if (!string.IsNullOrEmpty(blendShapeName))
                {
                    _blendShapeNames.Add(blendShapeName);
                }
            }
            
            // Store calculated parameter names for expression coloring
            foreach (var parameterName in expressions.Keys)
            {
                if (!string.IsNullOrEmpty(parameterName))
                {
                    _calculatedParameterNames.Add(parameterName);
                }
            }
            
            _logger.Info($"ParameterColorService initialized with {expressionCount} calculated parameters and {blendShapeCount} blend shapes");
            _logger.Debug($"Parameter sets: {_calculatedParameterNames.Count} calculated parameters, {_blendShapeNames.Count} blend shapes");
            
            if (expressionCount > 0)
            {
                _logger.Debug($"Sample calculated parameters: {string.Join(", ", expressions.Keys.Take(3))}");
            }
            
            if (blendShapeCount > 0)
            {
                _logger.Debug($"Sample blend shapes: {string.Join(", ", blendShapeNames.Take(3))}");
            }
        }
        
        /// <summary>
        /// Gets a color-coded version of a transformation expression.
        /// Calculated parameters are colored yellow, blend shapes are colored cyan (with priority).
        /// Results are cached for performance.
        /// </summary>
        /// <param name="expression">The transformation expression to colorize</param>
        /// <returns>Expression with parameter names color-coded using ANSI escape sequences</returns>
        public string GetColoredExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                return string.Empty;
            }
            
            // Check cache first for performance
            if (_coloredExpressionCache.TryGetValue(expression, out var cachedResult))
            {
                return cachedResult;
            }
            
            var coloredExpression = expression;
            
            // Step 1: Color blend shapes first (cyan) - these get priority
            foreach (var blendShapeName in _blendShapeNames)
            {
                coloredExpression = ReplaceParameterInExpression(coloredExpression, blendShapeName, 
                    ConsoleColors.ColorizeBlendShape(blendShapeName));
            }
            
            // Step 2: Color calculated parameters second (yellow) - only affects uncolored parameters
            foreach (var parameterName in _calculatedParameterNames)
            {
                coloredExpression = ReplaceParameterInExpression(coloredExpression, parameterName, 
                    ConsoleColors.ColorizeCalculatedParameter(parameterName));
            }
            
            // Cache the result for future use
            _coloredExpressionCache[expression] = coloredExpression;
            
            return coloredExpression;
        }
        
        /// <summary>
        /// Replaces parameter names in an expression with their colored versions using regex
        /// </summary>
        /// <param name="expression">The expression to process</param>
        /// <param name="parameterName">The parameter name to find and replace</param>
        /// <param name="coloredParameterName">The colored version to replace with</param>
        /// <returns>Expression with the parameter name replaced</returns>
        private string ReplaceParameterInExpression(string expression, string parameterName, string coloredParameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
                return expression;
                
            // Use regex to match parameter names as whole words (not part of other identifiers)
            // This pattern matches the parameter name when it's:
            // - At the start of string or preceded by non-alphanumeric character
            // - At the end of string or followed by non-alphanumeric character
            var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(parameterName)}\b";
            
            return System.Text.RegularExpressions.Regex.Replace(expression, pattern, coloredParameterName);
        }
        
        /// <summary>
        /// Gets a color-coded version of a blend shape name (iPhone source data).
        /// Always returns the name in light cyan color.
        /// </summary>
        /// <param name="blendShapeName">The blend shape name to colorize</param>
        /// <returns>Blend shape name in light cyan with ANSI escape sequences</returns>
        public string GetColoredBlendShapeName(string blendShapeName)
        {
            if (string.IsNullOrEmpty(blendShapeName))
            {
                return string.Empty;
            }
            
            return ConsoleColors.ColorizeBlendShape(blendShapeName);
        }
        
        /// <summary>
        /// Gets a color-coded version of a calculated parameter name (PC derived data).
        /// Always returns the name in light yellow color.
        /// </summary>
        /// <param name="parameterName">The calculated parameter name to colorize</param>
        /// <returns>Parameter name in light yellow with ANSI escape sequences</returns>
        public string GetColoredCalculatedParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                return string.Empty;
            }
            
            return ConsoleColors.ColorizeCalculatedParameter(parameterName);
        }
    }
} 