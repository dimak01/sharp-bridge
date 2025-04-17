using System.Collections.Generic;
using SharpBridge.Interfaces;

namespace SharpBridge.Models
{
    /// <summary>
    /// Tracking data sent to VTube Studio PC
    /// </summary>
    public class PCTrackingInfo : IFormattableObject
    {
        /// <summary>
        /// Parameters to send to VTube Studio PC
        /// </summary>
        public IEnumerable<TrackingParam> Parameters { get; set; }
        
        /// <summary>
        /// Whether a face is detected
        /// </summary>
        public bool FaceFound { get; set; }
    }
} 