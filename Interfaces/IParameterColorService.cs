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
        /// Initialize color mappings from transformation configuration data.
        /// This method is called once when configuration is loaded to set up
        /// color assignments for blend shapes and calculated parameters.
        /// </summary>
        /// <param name="expressions">Dictionary of parameter names to their transformation expressions</param>
        /// <param name="blendShapeNames">Collection of blend shape names from iPhone tracking data</param>
        void InitializeFromConfiguration(Dictionary<string, string> expressions, IEnumerable<string> blendShapeNames);
        
        /// <summary>
        /// Gets a color-coded version of a transformation expression.
        /// Parameter names within the expression are colored according to their type
        /// (blend shapes vs calculated parameters). Results are cached for performance.
        /// </summary>
        /// <param name="expression">The transformation expression to colorize</param>
        /// <returns>Expression with parameter names color-coded using ANSI escape sequences</returns>
        string GetColoredExpression(string expression);
        
        /// <summary>
        /// Gets a color-coded version of a parameter name.
        /// Colors are assigned based on parameter type: blend shapes (light cyan) 
        /// vs calculated parameters (light yellow).
        /// </summary>
        /// <param name="parameterName">The parameter name to colorize</param>
        /// <returns>Parameter name with appropriate color coding using ANSI escape sequences</returns>
        string GetColoredParameterName(string parameterName);
    }
} 