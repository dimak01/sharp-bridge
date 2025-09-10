using System.ComponentModel;
using System.Text.Json.Serialization;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Configuration;

namespace SharpBridge.Models.Configuration
{
    /// <summary>
    /// Configuration for the transformation engine
    /// </summary>
    public class TransformationEngineConfig : IConfigSection
    {
        // ========================================
        // User-Configurable Settings
        // ========================================

        /// <summary>
        /// Path to the transformation rules JSON file
        /// </summary>
        [Description("Path to Transformation Rules JSON File")]
        public string ConfigPath { get; set; } = "configs/vts_transforms.json";

        /// <summary>
        /// Maximum number of evaluation iterations for parameter dependencies
        /// </summary>
        [Description("Maximum Evaluation Iterations for Parameter Dependencies")]
        public int MaxEvaluationIterations { get; set; } = 10;
        // ========================================
        // Internal Settings (Not User-Configurable) - add these properties with[JsonIgnore] attribute
        // ========================================

    }
}