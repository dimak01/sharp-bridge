namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for the transformation engine
    /// </summary>
    public class TransformationEngineConfig
    {
        /// <summary>
        /// Path to the transformation rules JSON file
        /// </summary>
        public string ConfigPath { get; set; } = "Configs/vts_transforms.json";

        /// <summary>
        /// Maximum number of evaluation iterations for parameter dependencies
        /// </summary>
        public int MaxEvaluationIterations { get; set; } = 10;
    }
}