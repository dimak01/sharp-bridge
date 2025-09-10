namespace SharpBridge.Models.Infrastructure
{
    /// <summary>
    /// Simplified firewall rule information for display and analysis
    /// </summary>
    public class FirewallRule
    {
        /// <summary>
        /// Name of the firewall rule
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Direction of the rule (Inbound/Outbound)
        /// </summary>
        public string Direction { get; set; } = string.Empty;

        /// <summary>
        /// Action of the rule (Allow/Block)
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Protocol (UDP/TCP/Any)
        /// </summary>
        public string Protocol { get; set; } = string.Empty;

        /// <summary>
        /// Local port (if specified)
        /// </summary>
        public string? LocalPort { get; set; }

        /// <summary>
        /// Remote port (if specified)
        /// </summary>
        public string? RemotePort { get; set; }

        /// <summary>
        /// Remote address (if specified)
        /// </summary>
        public string? RemoteAddress { get; set; }

        /// <summary>
        /// Application name (if specified)
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Network profiles this rule applies to
        /// </summary>
        public int Profiles { get; set; }
    }
}