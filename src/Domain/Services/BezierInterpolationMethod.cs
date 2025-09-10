using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Core.Services;
using SharpBridge.Models;
using SharpBridge.Models.Domain;

namespace SharpBridge.Domain.Services
{
    /// <summary>
    /// Bezier curve interpolation method using De Casteljau's algorithm
    /// </summary>
    public class BezierInterpolationMethod : IInterpolationMethod
    {
        private readonly Point[] _controlPoints;

        /// <summary>
        /// Initializes a new instance of the BezierInterpolationMethod
        /// </summary>
        /// <param name="controlPoints">Control points for the Bezier curve (2-8 points)</param>
        /// <exception cref="ArgumentNullException">Thrown when controlPoints is null</exception>
        /// <exception cref="ArgumentException">Thrown when controlPoints has less than 2 or more than 8 points</exception>
        public BezierInterpolationMethod(IEnumerable<Point> controlPoints)
        {
            _controlPoints = controlPoints?.ToArray() ?? throw new ArgumentNullException(nameof(controlPoints));

            if (_controlPoints.Length < 2)
                throw new ArgumentException("Bezier interpolation requires at least 2 control points", nameof(controlPoints));

            if (_controlPoints.Length > 8)
                throw new ArgumentException("Bezier interpolation supports maximum 8 control points for performance", nameof(controlPoints));
        }

        /// <summary>
        /// Evaluates the Bezier curve at parameter t using De Casteljau's algorithm
        /// </summary>
        /// <param name="t">Normalized parameter value between 0 and 1</param>
        /// <returns>Y coordinate of the curve at parameter t</returns>
        public double Interpolate(double t)
        {
            if (t < 0 || t > 1)
                throw new ArgumentOutOfRangeException(nameof(t), "Parameter t must be between 0 and 1");

            // For 2 control points, this is linear interpolation
            if (_controlPoints.Length == 2)
            {
                return LinearInterpolate(_controlPoints[0].Y, _controlPoints[1].Y, t);
            }

            // Use De Casteljau's algorithm for higher-order curves
            return EvaluateBezierCurve(t);
        }

        /// <summary>
        /// Gets the display name for Bezier interpolation
        /// </summary>
        /// <returns>"Bezier (N points)" where N is the number of control points</returns>
        public string GetDisplayName()
        {
            return $"Bezier ({_controlPoints.Length} points)";
        }

        /// <summary>
        /// Evaluates the Bezier curve using De Casteljau's algorithm
        /// </summary>
        /// <param name="t">Parameter value between 0 and 1</param>
        /// <returns>Y coordinate of the curve at parameter t</returns>
        private double EvaluateBezierCurve(double t)
        {
            var points = new double[_controlPoints.Length];

            // Initialize with Y coordinates of control points
            for (int i = 0; i < _controlPoints.Length; i++)
            {
                points[i] = _controlPoints[i].Y;
            }

            // Apply De Casteljau's algorithm
            for (int level = 1; level < _controlPoints.Length; level++)
            {
                for (int i = 0; i < _controlPoints.Length - level; i++)
                {
                    points[i] = LinearInterpolate(points[i], points[i + 1], t);
                }
            }

            return points[0];
        }

        /// <summary>
        /// Performs linear interpolation between two values
        /// </summary>
        /// <param name="a">First value</param>
        /// <param name="b">Second value</param>
        /// <param name="t">Interpolation parameter (0-1)</param>
        /// <returns>Interpolated value</returns>
        private static double LinearInterpolate(double a, double b, double t)
        {
            return a + (b - a) * t;
        }
    }
}