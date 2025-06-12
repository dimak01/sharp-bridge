using System;
using System.Collections.Generic;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Service responsible for providing color-coded parameter names and expressions
    /// to visually connect the iPhone → PC transformation pipeline in console UI
    /// </summary>
    public interface IParameterColorService
    {
        /// <summary>
        /// Initialize the service with transformation configuration data.
        /// This method is called once when configuration is loaded to set up
        /// the known parameter names for expression coloring.
        /// </summary>
        /// <param name="expressions">Dictionary of parameter names to their transformation expressions</param>
        /// <param name="blendShapeNames">Collection of blend shape names from iPhone tracking data</param>
        void InitializeFromConfiguration(Dictionary<string, string> expressions, IEnumerable<string> blendShapeNames);
        
        /// <summary>
        /// Gets a color-coded version of a transformation expression.
        /// Calculated parameters are colored yellow, blend shapes are colored cyan (with priority).
        /// Results are cached for performance.
        /// </summary>
        /// <param name="expression">The transformation expression to colorize</param>
        /// <returns>Expression with parameter names color-coded using ANSI escape sequences</returns>
        string GetColoredExpression(string expression);
        
        /// <summary>
        /// Gets a color-coded version of a blend shape name (iPhone source data).
        /// Always returns the name in light cyan color.
        /// </summary>
        /// <param name="blendShapeName">The blend shape name to colorize</param>
        /// <returns>Blend shape name in light cyan with ANSI escape sequences</returns>
        string GetColoredBlendShapeName(string blendShapeName);
        
        /// <summary>
        /// Gets a color-coded version of a calculated parameter name (PC derived data).
        /// Always returns the name in light yellow color.
        /// </summary>
        /// <param name="parameterName">The calculated parameter name to colorize</param>
        /// <returns>Parameter name in light yellow with ANSI escape sequences</returns>
        string GetColoredCalculatedParameterName(string parameterName);
    }
} 