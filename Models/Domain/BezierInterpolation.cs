using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SharpBridge.Models.Domain
{
    /// <summary>
    /// Represents Bezier curve interpolation with variable control points
    /// </summary>
    public class BezierInterpolation : IInterpolationDefinition
    {
        /// <summary>
        /// Control points for the Bezier curve (2-8 points)
        /// </summary>
        [MinLength(2, ErrorMessage = "Bezier interpolation requires at least 2 control points")]
        [MaxLength(8, ErrorMessage = "Bezier interpolation supports maximum 8 control points for performance")]
        public List<Point> ControlPoints { get; set; } = new();
    }
}