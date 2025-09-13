// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.UI.Components;

namespace SharpBridge.Models.Domain
{
    /// <summary>
    /// Tracking data received from iPhone VTube Studio
    /// </summary>
    public class PhoneTrackingInfo : IFormattableObject
    {
        /// <summary>Timestamp of the tracking data</summary>
        public ulong Timestamp { get; set; }

        /// <summary>Hotkey value</summary>
        public short Hotkey { get; set; }

        /// <summary>Whether a face is detected</summary>
        public bool FaceFound { get; set; }

        /// <summary>Head rotation in 3D space</summary>
        public Coordinates Rotation { get; set; } = new Coordinates();

        /// <summary>Head position in 3D space</summary>
        public Coordinates Position { get; set; } = new Coordinates();

        /// <summary>Left eye position</summary>
        public Coordinates EyeLeft { get; set; } = new Coordinates();

        /// <summary>Right eye position</summary>
        public Coordinates EyeRight { get; set; } = new Coordinates();

        /// <summary>Collection of blend shapes representing facial expressions</summary>
        public List<BlendShape> BlendShapes { get; set; } = new List<BlendShape>();
    }
}