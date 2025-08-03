using System.Collections.Generic;

namespace SharpBridge.Models
{
    /// <summary>
    /// Result of firewall rule analysis
    /// </summary>
    public class FirewallAnalysisResult
    {
        /// <summary>
        /// Whether outbound connectivity is allowed by firewall rules
        /// </summary>
        public bool IsAllowed { get; set; }

        /// <summary>
        /// List of relevant firewall rules that affect this connection
        /// </summary>
        public List<FirewallRule> RelevantRules { get; set; } = new List<FirewallRule>();
    }
}