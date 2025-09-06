using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Column formatters for FirewallRule table display
    /// </summary>
    public static class FirewallRuleColumnFormatters
    {
        private const string AllowLowerText = "allow";

        /// <summary>
        /// Status column formatter - shows [Enabled]/[Disabled] with colors
        /// </summary>
        public class StatusColumnFormatter : ITableColumnFormatter<FirewallRule>
        {
            public string Header { get; } = "Status";
            public int MinWidth { get; } = 8; // "[Enabled]" = 9 chars
            public int? MaxWidth { get; } = 10; // Keep it compact

            public Func<FirewallRule, string> ValueFormatter { get; }

            public StatusColumnFormatter()
            {
                ValueFormatter = rule => rule.IsEnabled
                    ? ConsoleColors.Colorize("[Enabled]", ConsoleColors.Success)
                    : ConsoleColors.Colorize("[Disabled]", ConsoleColors.Disabled);
            }

            public string FormatCell(FirewallRule item, int width)
            {
                var content = ValueFormatter(item);
                var visualLength = ConsoleColors.GetVisualLength(content);
                var paddingNeeded = width - visualLength;

                if (paddingNeeded > 0)
                {
                    content += new string(' ', paddingNeeded);
                }

                return content;
            }

            public string FormatHeader(int width)
            {
                return Header.PadRight(width);
            }
        }

        /// <summary>
        /// Action column formatter - shows Allow/Block with colors
        /// </summary>
        public class ActionColumnFormatter : ITableColumnFormatter<FirewallRule>
        {
            public string Header { get; } = "Action";
            public int MinWidth { get; } = 5; // "Allow" = 5 chars
            public int? MaxWidth { get; } = 6; // Keep it compact

            public Func<FirewallRule, string> ValueFormatter { get; }

            public ActionColumnFormatter()
            {
                ValueFormatter = rule =>
                {
                    var actionColor = rule.Action.Equals(AllowLowerText, StringComparison.OrdinalIgnoreCase)
                        ? ConsoleColors.Success
                        : ConsoleColors.Error;
                    return ConsoleColors.Colorize(rule.Action, actionColor);
                };
            }

            public string FormatCell(FirewallRule item, int width)
            {
                var content = ValueFormatter(item);
                var visualLength = ConsoleColors.GetVisualLength(content);
                var paddingNeeded = width - visualLength;

                if (paddingNeeded > 0)
                {
                    content += new string(' ', paddingNeeded);
                }

                return content;
            }

            public string FormatHeader(int width)
            {
                return Header.PadRight(width);
            }
        }

        /// <summary>
        /// Rule name column formatter - shows rule name or "Unnamed Rule"
        /// </summary>
        public class RuleNameColumnFormatter : ITableColumnFormatter<FirewallRule>
        {
            public string Header { get; } = "Rule Name";
            public int MinWidth { get; } = 9; // "Rule Name" = 9 chars
            public int? MaxWidth { get; } = 25; // Reasonable limit for rule names

            public Func<FirewallRule, string> ValueFormatter { get; }

            public RuleNameColumnFormatter()
            {
                ValueFormatter = rule => !string.IsNullOrEmpty(rule.Name) ? rule.Name : "Unnamed Rule";
            }

            public string FormatCell(FirewallRule item, int width)
            {
                var content = ValueFormatter(item);
                var visualLength = ConsoleColors.GetVisualLength(content);

                // Truncate if too long
                if (visualLength > width)
                {
                    if (width <= 3)
                    {
                        content = TruncateToVisualLength(content, width);
                    }
                    else
                    {
                        content = TruncateToVisualLength(content, width - 3) + "...";
                    }
                }

                var paddingNeeded = width - ConsoleColors.GetVisualLength(content);
                if (paddingNeeded > 0)
                {
                    content += new string(' ', paddingNeeded);
                }

                return content;
            }

            public string FormatHeader(int width)
            {
                return Header.PadRight(width);
            }

            private static string TruncateToVisualLength(string content, int targetVisualLength)
            {
                if (targetVisualLength <= 0) return "";

                var result = new System.Text.StringBuilder();
                var visualCharCount = 0;
                var i = 0;

                while (i < content.Length && visualCharCount < targetVisualLength)
                {
                    if (IsAnsiEscapeStart(content, i))
                    {
                        var ansiSequence = ExtractAnsiSequence(content, i);
                        result.Append(ansiSequence);
                        i += ansiSequence.Length;
                    }
                    else
                    {
                        result.Append(content[i]);
                        visualCharCount++;
                        i++;
                    }
                }

                return result.ToString();
            }

            private static bool IsAnsiEscapeStart(string content, int position)
            {
                return position < content.Length - 1 &&
                       content[position] == '\u001b' &&
                       content[position + 1] == '[';
            }

            private static string ExtractAnsiSequence(string content, int startPosition)
            {
                var i = startPosition + 2;
                while (i < content.Length && (char.IsDigit(content[i]) || content[i] == ';'))
                {
                    i++;
                }
                if (i < content.Length && content[i] == 'm')
                {
                    i++;
                }
                return content.Substring(startPosition, i - startPosition);
            }
        }

        /// <summary>
        /// Protocol column formatter - shows UDP/TCP/Any
        /// </summary>
        public class ProtocolColumnFormatter : ITableColumnFormatter<FirewallRule>
        {
            public string Header { get; } = "Protocol";
            public int MinWidth { get; } = 8; // "Protocol" = 8 chars
            public int? MaxWidth { get; } = 8; // Keep it compact

            public Func<FirewallRule, string> ValueFormatter { get; }

            public ProtocolColumnFormatter()
            {
                ValueFormatter = rule => rule.Protocol;
            }

            public string FormatCell(FirewallRule item, int width)
            {
                var content = ValueFormatter(item);
                var paddingNeeded = width - content.Length;

                if (paddingNeeded > 0)
                {
                    content += new string(' ', paddingNeeded);
                }

                return content;
            }

            public string FormatHeader(int width)
            {
                return Header.PadRight(width);
            }
        }

        /// <summary>
        /// Port column formatter - shows local port or wildcard
        /// </summary>
        public class PortColumnFormatter : ITableColumnFormatter<FirewallRule>
        {
            public string Header { get; } = "Port";
            public int MinWidth { get; } = 4; // "Port" = 4 chars
            public int? MaxWidth { get; } = 8; // Keep it compact

            public Func<FirewallRule, string> ValueFormatter { get; }

            public PortColumnFormatter()
            {
                ValueFormatter = rule => !string.IsNullOrEmpty(rule.LocalPort) &&
                    rule.LocalPort != "*" &&
                    !string.Equals(rule.LocalPort, "any", StringComparison.OrdinalIgnoreCase)
                    ? rule.LocalPort
                    : "*";
            }

            public string FormatCell(FirewallRule item, int width)
            {
                var content = ValueFormatter(item);
                var paddingNeeded = width - content.Length;

                if (paddingNeeded > 0)
                {
                    content += new string(' ', paddingNeeded);
                }

                return content;
            }

            public string FormatHeader(int width)
            {
                return Header.PadRight(width);
            }
        }

        /// <summary>
        /// Direction column formatter - shows (Any → ThisDevice) format
        /// </summary>
        public class DirectionColumnFormatter : ITableColumnFormatter<FirewallRule>
        {
            public string Header { get; } = "Direction";
            public int MinWidth { get; } = 8; // "Direction" = 9 chars
            public int? MaxWidth { get; } = 20; // Reasonable limit for direction arrows

            public Func<FirewallRule, string> ValueFormatter { get; }

            public DirectionColumnFormatter()
            {
                ValueFormatter = rule =>
                {
                    if (!string.IsNullOrEmpty(rule.RemoteAddress) &&
                        rule.RemoteAddress != "*" &&
                        !string.Equals(rule.RemoteAddress, "any", StringComparison.OrdinalIgnoreCase))
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
                };
            }

            public string FormatCell(FirewallRule item, int width)
            {
                var content = ValueFormatter(item);
                var visualLength = ConsoleColors.GetVisualLength(content);

                // Truncate if too long
                if (visualLength > width)
                {
                    if (width <= 3)
                    {
                        content = TruncateToVisualLength(content, width);
                    }
                    else
                    {
                        content = TruncateToVisualLength(content, width - 3) + "...";
                    }
                }

                var paddingNeeded = width - ConsoleColors.GetVisualLength(content);
                if (paddingNeeded > 0)
                {
                    content += new string(' ', paddingNeeded);
                }

                return content;
            }

            public string FormatHeader(int width)
            {
                return Header.PadRight(width);
            }

            private static string TruncateToVisualLength(string content, int targetVisualLength)
            {
                if (targetVisualLength <= 0) return "";

                var result = new System.Text.StringBuilder();
                var visualCharCount = 0;
                var i = 0;

                while (i < content.Length && visualCharCount < targetVisualLength)
                {
                    if (IsAnsiEscapeStart(content, i))
                    {
                        var ansiSequence = ExtractAnsiSequence(content, i);
                        result.Append(ansiSequence);
                        i += ansiSequence.Length;
                    }
                    else
                    {
                        result.Append(content[i]);
                        visualCharCount++;
                        i++;
                    }
                }

                return result.ToString();
            }

            private static bool IsAnsiEscapeStart(string content, int position)
            {
                return position < content.Length - 1 &&
                       content[position] == '\u001b' &&
                       content[position + 1] == '[';
            }

            private static string ExtractAnsiSequence(string content, int startPosition)
            {
                var i = startPosition + 2;
                while (i < content.Length && (char.IsDigit(content[i]) || content[i] == ';'))
                {
                    i++;
                }
                if (i < content.Length && content[i] == 'm')
                {
                    i++;
                }
                return content.Substring(startPosition, i - startPosition);
            }
        }

        /// <summary>
        /// Scope column formatter - shows app path or "Global"
        /// </summary>
        public class ScopeColumnFormatter : ITableColumnFormatter<FirewallRule>
        {
            public string Header { get; } = "Scope";
            public int MinWidth { get; } = 5; // "Scope" = 5 chars
            public int? MaxWidth { get; } = 30; // Allow for app paths

            public Func<FirewallRule, string> ValueFormatter { get; }

            public ScopeColumnFormatter()
            {
                ValueFormatter = rule => !string.IsNullOrEmpty(rule.ApplicationName)
                    ? $"App: {rule.ApplicationName}"
                    : "Global";
            }

            public string FormatCell(FirewallRule item, int width)
            {
                var content = ValueFormatter(item);
                var visualLength = ConsoleColors.GetVisualLength(content);

                // Truncate if too long
                if (visualLength > width)
                {
                    if (width <= 3)
                    {
                        content = TruncateToVisualLength(content, width);
                    }
                    else
                    {
                        content = TruncateToVisualLength(content, width - 3) + "...";
                    }
                }

                var paddingNeeded = width - ConsoleColors.GetVisualLength(content);
                if (paddingNeeded > 0)
                {
                    content += new string(' ', paddingNeeded);
                }

                return content;
            }

            public string FormatHeader(int width)
            {
                return Header.PadRight(width);
            }

            private static string TruncateToVisualLength(string content, int targetVisualLength)
            {
                if (targetVisualLength <= 0) return "";

                var result = new System.Text.StringBuilder();
                var visualCharCount = 0;
                var i = 0;

                while (i < content.Length && visualCharCount < targetVisualLength)
                {
                    if (IsAnsiEscapeStart(content, i))
                    {
                        var ansiSequence = ExtractAnsiSequence(content, i);
                        result.Append(ansiSequence);
                        i += ansiSequence.Length;
                    }
                    else
                    {
                        result.Append(content[i]);
                        visualCharCount++;
                        i++;
                    }
                }

                return result.ToString();
            }

            private static bool IsAnsiEscapeStart(string content, int position)
            {
                return position < content.Length - 1 &&
                       content[position] == '\u001b' &&
                       content[position + 1] == '[';
            }

            private static string ExtractAnsiSequence(string content, int startPosition)
            {
                var i = startPosition + 2;
                while (i < content.Length && (char.IsDigit(content[i]) || content[i] == ';'))
                {
                    i++;
                }
                if (i < content.Length && content[i] == 'm')
                {
                    i++;
                }
                return content.Substring(startPosition, i - startPosition);
            }
        }

        /// <summary>
        /// Gets all column formatters for FirewallRule table display
        /// </summary>
        /// <returns>List of column formatters in display order</returns>
        public static IList<ITableColumnFormatter<FirewallRule>> GetAllColumns()
        {
            return new List<ITableColumnFormatter<FirewallRule>>
            {
                new StatusColumnFormatter(),
                new ActionColumnFormatter(),
                new RuleNameColumnFormatter(),
                new ProtocolColumnFormatter(),
                new PortColumnFormatter(),
                new DirectionColumnFormatter(),
                new ScopeColumnFormatter()
            };
        }
    }
}
