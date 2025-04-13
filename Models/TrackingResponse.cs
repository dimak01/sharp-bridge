using System;
using System.Collections.Generic;

namespace SharpBridge.Models
{
    /// <summary>
    /// Tracking data response from iPhone VTube Studio
    /// </summary>
    public class TrackingResponse
    {
        /// <summary>Timestamp of the tracking data</summary>
        public ulong Timestamp { get; set; }
        
        /// <summary>Hotkey value</summary>
        public short Hotkey { get; set; }
        
        /// <summary>Whether a face is detected</summary>
        public bool FaceFound { get; set; }
        
        /// <summary>Head rotation in 3D space</summary>
        public Coordinates Rotation { get; set; }
        
        /// <summary>Head position in 3D space</summary>
        public Coordinates Position { get; set; }
        
        /// <summary>Left eye position</summary>
        public Coordinates EyeLeft { get; set; }
        
        /// <summary>Right eye position</summary>
        public Coordinates EyeRight { get; set; }
        
        /// <summary>Collection of blend shapes representing facial expressions</summary>
        public List<BlendShape> BlendShapes { get; set; }
    }
} 