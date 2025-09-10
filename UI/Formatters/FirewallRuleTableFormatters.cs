using System;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.UI.Formatters;
using SharpBridge.Models;
using SharpBridge.Models.Infrastructure;
using SharpBridge.UI.Utilities;

namespace SharpBridge.UI.Formatters
{
    /// <summary>
    /// Column formatters for displaying firewall rules in a table format
    /// </summary>
    public static class FirewallRuleTableFormatters
    {
        /// <summary>
        /// Creates all column formatters for the firewall rules table
        /// </summary>
        /// <returns>List of column formatters for the firewall rules table</returns>
        public static List<ITableColumnFormatter<FirewallRule>> CreateColumnFormatters()
        {
            return new List<ITableColumnFormatter<FirewallRule>>
            {
                CreateStatusColumn(),
                CreateActionColumn(),
                CreateRuleNameColumn(),
                CreateProtocolColumn(),
                CreatePortColumn(),
                CreateDirectionColumn(),
                CreateScopeColumn()
            };
        }

        /// <summary>
        /// Status column: [Enabled] or [Disabled] with appropriate coloring
        /// </summary>
        private static ITableColumnFormatter<FirewallRule> CreateStatusColumn()
        {
            return new TextColumnFormatter<FirewallRule>(
                header: "Status",
                valueSelector: rule => rule.IsEnabled
                    ? ConsoleColors.Colorize("[Enabled]", ConsoleColors.Success)
                    : ConsoleColors.Colorize("[Disabled]", ConsoleColors.Disabled),
                minWidth: 10,
                maxWidth: 12
            );
        }

        /// <summary>
        /// Action column: Allow (green) or Deny (red)
        /// </summary>
        private static ITableColumnFormatter<FirewallRule> CreateActionColumn()
        {
            return new TextColumnFormatter<FirewallRule>(
                header: "Action",
                valueSelector: rule => rule.Action.Equals("Allow", StringComparison.OrdinalIgnoreCase)
                    ? ConsoleColors.Colorize(rule.Action, ConsoleColors.Success)
                    : ConsoleColors.Colorize(rule.Action, ConsoleColors.Error),
                minWidth: 6,
                maxWidth: 8
            );
        }

        /// <summary>
        /// Rule name column: Rule name or "Unnamed Rule" if empty
        /// </summary>
        private static ITableColumnFormatter<FirewallRule> CreateRuleNameColumn()
        {
            return new TextColumnFormatter<FirewallRule>(
                header: "Rule Name",
                valueSelector: rule => !string.IsNullOrEmpty(rule.Name) ? rule.Name : "Unnamed Rule",
                minWidth: 12,
                maxWidth: 30
            );
        }

        /// <summary>
        /// Protocol column: UDP, TCP, etc.
        /// </summary>
        private static ITableColumnFormatter<FirewallRule> CreateProtocolColumn()
        {
            return new TextColumnFormatter<FirewallRule>(
                header: "Protocol",
                valueSelector: rule => rule.Protocol,
                minWidth: 8,
                maxWidth: 10
            );
        }

        /// <summary>
        /// Port column: Port number, *, or any
        /// </summary>
        private static ITableColumnFormatter<FirewallRule> CreatePortColumn()
        {
            return new TextColumnFormatter<FirewallRule>(
                header: "Port",
                valueSelector: rule => !string.IsNullOrEmpty(rule.LocalPort) ? rule.LocalPort : "*",
                minWidth: 6,
                maxWidth: 8
            );
        }

        /// <summary>
        /// Direction column: (Any → ThisDevice), (ThisDevice → Any), etc.
        /// </summary>
        private static ITableColumnFormatter<FirewallRule> CreateDirectionColumn()
        {
            return new TextColumnFormatter<FirewallRule>(
                header: "Direction",
                valueSelector: FormatRuleDirection,
                minWidth: 20,
                maxWidth: 35
            );
        }

        /// <summary>
        /// Scope column: Application path or "Global"
        /// </summary>
        private static ITableColumnFormatter<FirewallRule> CreateScopeColumn()
        {
            return new TextColumnFormatter<FirewallRule>(
                header: "Scope",
                valueSelector: rule => !string.IsNullOrEmpty(rule.ApplicationName) ? rule.ApplicationName : "Global",
                minWidth: 20,
                maxWidth: 75
            );
        }

        /// <summary>
        /// Formats the direction information for a firewall rule
        /// </summary>
        /// <param name="rule">The firewall rule</param>
        /// <returns>Formatted direction string</returns>
        private static string FormatRuleDirection(FirewallRule rule)
        {
            // Add direction info with arrow
            if (!string.IsNullOrEmpty(rule.RemoteAddress) && rule.RemoteAddress != "*" && !string.Equals(rule.RemoteAddress, "any", StringComparison.OrdinalIgnoreCase))
            {
                var source = rule.RemoteAddress == "0.0.0.0" ? "Any" : rule.RemoteAddress;
                return string.Equals(rule.Direction, "inbound", StringComparison.OrdinalIgnoreCase)
                    ? $"({source} → ThisDevice)"
                    : $"(ThisDevice → {source})";
            }
            else
            {
                return string.Equals(rule.Direction, "inbound", StringComparison.OrdinalIgnoreCase)
                    ? "(Any → ThisDevice)"
                    : "(ThisDevice → Any)";
            }
        }
    }
}
