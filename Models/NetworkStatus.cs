using System;
using System.Collections.Generic;

namespace SharpBridge.Models
{
    /// <summary>
    /// Aggregated network status for all monitored connections
    /// </summary>
    public class NetworkStatus
    {
        /// <summary>
        /// Timestamp when this status was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// iPhone connection status
        /// </summary>
        public IPhoneConnectionStatus IPhone { get; set; } = new IPhoneConnectionStatus();

        /// <summary>
        /// PC VTube Studio connection status
        /// </summary>
        public PCConnectionStatus PC { get; set; } = new PCConnectionStatus();
    }

    /// <summary>
    /// iPhone connection status information
    /// </summary>
    public class IPhoneConnectionStatus
    {
        /// <summary>
        /// Whether local UDP port 28964 is open
        /// </summary>
        public bool LocalPortOpen { get; set; }

        /// <summary>
        /// Whether outbound UDP connectivity to iPhone is allowed
        /// </summary>
        public bool OutboundAllowed { get; set; }

        /// <summary>
        /// Firewall analysis result for inbound traffic (local port)
        /// </summary>
        public FirewallAnalysisResult? InboundFirewallAnalysis { get; set; }

        /// <summary>
        /// Firewall analysis result for outbound traffic (remote connection)
        /// </summary>
        public FirewallAnalysisResult? OutboundFirewallAnalysis { get; set; }

        /// <summary>
        /// Timestamp when iPhone status was last checked
        /// </summary>
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// PC VTube Studio connection status information
    /// </summary>
    public class PCConnectionStatus
    {
        /// <summary>
        /// Whether discovery port connectivity is allowed (when discovery enabled)
        /// </summary>
        public bool DiscoveryAllowed { get; set; }

        /// <summary>
        /// Whether WebSocket port connectivity is allowed
        /// </summary>
        public bool WebSocketAllowed { get; set; }

        /// <summary>
        /// Firewall analysis result for discovery port (when applicable)
        /// </summary>
        public FirewallAnalysisResult? DiscoveryFirewallAnalysis { get; set; }

        /// <summary>
        /// Firewall analysis result for WebSocket port
        /// </summary>
        public FirewallAnalysisResult? WebSocketFirewallAnalysis { get; set; }

        /// <summary>
        /// Timestamp when PC status was last checked
        /// </summary>
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }
}