using System.ComponentModel.DataAnnotations;

namespace SharpBridge.Models.Domain
{
    /// <summary>
    /// Represents a 2D point with normalized coordinates (0-1 range)
    /// </summary>
    public class Point
    {
        /// <summary>
        /// X coordinate (must be between 0 and 1)
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "X coordinate must be between 0 and 1")]
        public double X { get; set; }

        /// <summary>
        /// Y coordinate (must be between 0 and 1)
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "Y coordinate must be between 0 and 1")]
        public double Y { get; set; }
    }
}