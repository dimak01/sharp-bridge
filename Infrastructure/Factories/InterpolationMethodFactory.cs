using System;
using System.Collections.Generic;
using SharpBridge.Domain.Services;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Core.Services;
using SharpBridge.Models;
using SharpBridge.Models.Domain;

namespace SharpBridge.Infrastructure.Factories
{
    /// <summary>
    /// Factory for creating interpolation method instances from definitions
    /// </summary>
    public static class InterpolationMethodFactory
    {
        /// <summary>
        /// Creates an interpolation method from a definition
        /// </summary>
        /// <param name="definition">The interpolation definition</param>
        /// <returns>An interpolation method instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when definition is null</exception>
        /// <exception cref="ArgumentException">Thrown when definition type is not supported</exception>
        public static IInterpolationMethod CreateFromDefinition(IInterpolationDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return definition switch
            {
                LinearInterpolation => new LinearInterpolationMethod(),
                BezierInterpolation bezier => new BezierInterpolationMethod(bezier.ControlPoints),
                _ => throw new ArgumentException($"Unsupported interpolation type: {definition.GetType().Name}", nameof(definition))
            };
        }

        /// <summary>
        /// Validates an interpolation definition
        /// </summary>
        /// <param name="definition">The interpolation definition to validate</param>
        /// <returns>True if the definition is valid, false otherwise</returns>
        public static bool ValidateDefinition(IInterpolationDefinition definition)
        {
            if (definition == null)
                return false;

            try
            {
                return definition switch
                {
                    LinearInterpolation => true,
                    BezierInterpolation bezier => ValidateBezierDefinition(bezier),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a Bezier interpolation definition
        /// </summary>
        /// <param name="bezier">The Bezier interpolation definition</param>
        /// <returns>True if the definition is valid, false otherwise</returns>
        private static bool ValidateBezierDefinition(BezierInterpolation bezier)
        {
            if (bezier.ControlPoints == null)
                return false;

            if (bezier.ControlPoints.Count < 2 || bezier.ControlPoints.Count > 8)
                return false;

            // Validate that control points are in 0-1 range
            foreach (var point in bezier.ControlPoints)
            {
                if (point.X < 0 || point.X > 1 || point.Y < 0 || point.Y > 1)
                    return false;
            }

            // Validate that first point is (0,0) and last point is (1,1)
            if (bezier.ControlPoints.Count > 0)
            {
                var first = bezier.ControlPoints[0];
                var last = bezier.ControlPoints[^1];

                if (Math.Abs(first.X) > 1e-6 || Math.Abs(first.Y) > 1e-6)
                    return false;

                if (Math.Abs(last.X - 1.0) > 1e-6 || Math.Abs(last.Y - 1.0) > 1e-6)
                    return false;
            }

            return true;
        }
    }
}