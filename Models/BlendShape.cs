using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Represents a blend shape with a key-value pair
    /// </summary>
    public class BlendShape
    {
        /// <summary>The name of the blend shape</summary>
        public string Key { get; set; }
        
        /// <summary>The value of the blend shape (0-1)</summary>
        public double Value { get; set; }
    }
} 