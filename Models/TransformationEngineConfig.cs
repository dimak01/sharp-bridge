using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for the transformation engine
    /// </summary>
    public class TransformationEngineConfig
    {
        // ========================================
        // User-Configurable Settings
        // ========================================

        /// <summary>
        /// Path to the transformation rules JSON file
        /// </summary>
        public string ConfigPath { get; set; } = "Configs/vts_transforms.json";

        /// <summary>
        /// Maximum number of evaluation iterations for parameter dependencies
        /// </summary>
        public int MaxEvaluationIterations { get; set; } = 10;
        // ========================================
        // Internal Settings (Not User-Configurable) - add these properties with[JsonIgnore] attribute
        // ========================================

    }
}