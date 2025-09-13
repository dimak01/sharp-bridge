// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using NCalc;

namespace SharpBridge.Models.Domain
{

    /// <summary>
    /// Represents a single transformation rule with its expression and constraints
    /// </summary>
    public class ParameterTransformation
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
        /// Gets the interpolation method for this transformation (null for linear)
        /// </summary>
        public IInterpolationDefinition? Interpolation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterTransformation"/> class
        /// </summary>
        /// <param name="name">The name of the transformation rule</param>
        /// <param name="expression">The compiled mathematical expression</param>
        /// <param name="expressionString">The original string representation of the expression</param>
        /// <param name="min">The minimum allowed value for the transformation result</param>
        /// <param name="max">The maximum allowed value for the transformation result</param>
        /// <param name="defaultValue">The default value to use when the transformation fails</param>
        /// <param name="interpolation">The interpolation method for this transformation (null for linear)</param>
        public ParameterTransformation(string name, Expression expression, string expressionString,
            double min, double max, double defaultValue, IInterpolationDefinition? interpolation = null)
        {
            Name = name;
            Expression = expression;
            ExpressionString = expressionString;
            Min = min;
            Max = max;
            DefaultValue = defaultValue;
            Interpolation = interpolation;
        }
    }
}