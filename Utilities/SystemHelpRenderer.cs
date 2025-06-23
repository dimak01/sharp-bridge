using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of ISystemHelpRenderer for rendering the F1 help system display
    /// </summary>
    public class SystemHelpRenderer : ISystemHelpRenderer
    {
        private readonly IShortcutConfigurationManager _shortcutConfigurationManager;
        private readonly IShortcutParser _shortcutParser;
        private readonly ITableFormatter _tableFormatter;

        /// <summary>
        /// Initializes a new instance of the SystemHelpRenderer
        /// </summary>
        /// <param name="shortcutConfigurationManager">Manager for shortcut configurations</param>
        /// <param name="shortcutParser">Parser for formatting shortcut strings</param>
        /// <param name="tableFormatter">Formatter for creating tables</param>
        public SystemHelpRenderer(
            IShortcutConfigurationManager shortcutConfigurationManager,
            IShortcutParser shortcutParser,
            ITableFormatter tableFormatter)
        {
            _shortcutConfigurationManager = shortcutConfigurationManager ?? throw new ArgumentNullException(nameof(shortcutConfigurationManager));
            _shortcutParser = shortcutParser ?? throw new ArgumentNullException(nameof(shortcutParser));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
        }

        /// <summary>
        /// Renders the complete system help display including application configuration and keyboard shortcuts
        /// </summary>
        /// <param name="applicationConfig">Application configuration to display</param>
        /// <param name="consoleWidth">Available console width for formatting</param>
        /// <returns>Formatted help content as a string</returns>
        public string RenderSystemHelp(ApplicationConfig applicationConfig, int consoleWidth)
        {
            var builder = new StringBuilder();

            // Create separator line based on console width
            var separatorLine = new string('═', Math.Max(consoleWidth, 80));

            // Header
            builder.AppendLine(separatorLine);
            builder.AppendLine(CenterText("SHARP BRIDGE - SYSTEM HELP (F1)", consoleWidth));
            builder.AppendLine(separatorLine);
            builder.AppendLine();

            // Application Configuration section
            builder.AppendLine(RenderApplicationConfiguration(applicationConfig));
            builder.AppendLine();

            // Keyboard Shortcuts section
            builder.AppendLine(RenderKeyboardShortcuts(consoleWidth));

            // Footer
            builder.AppendLine();
            builder.AppendLine(separatorLine);
            builder.AppendLine(CenterText("Press any key to return to main display", consoleWidth));
            builder.AppendLine(separatorLine);

            return builder.ToString();
        }

        /// <summary>
        /// Renders just the application configuration section
        /// </summary>
        /// <param name="applicationConfig">Application configuration to display</param>
        /// <returns>Formatted configuration section</returns>
        public string RenderApplicationConfiguration(ApplicationConfig applicationConfig)
        {
            var builder = new StringBuilder();

            builder.AppendLine("APPLICATION CONFIGURATION:");
            builder.AppendLine("─────────────────────────");

            if (applicationConfig == null)
            {
                builder.AppendLine("  No configuration loaded");
                return builder.ToString();
            }

            // Use reflection to display all properties with their descriptions
            var properties = typeof(ApplicationConfig).GetProperties();
            foreach (var property in properties)
            {
                // Skip the Shortcuts property as it's displayed separately
                if (property.Name == nameof(ApplicationConfig.Shortcuts))
                    continue;

                var displayName = AttributeHelper.GetPropertyDescription(typeof(ApplicationConfig), property.Name);
                var value = property.GetValue(applicationConfig);
                var displayValue = value?.ToString() ?? "Not set";

                builder.AppendLine($"  {displayName}: {displayValue}");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Renders just the keyboard shortcuts section with status information
        /// </summary>
        /// <param name="consoleWidth">Available console width for table formatting</param>
        /// <returns>Formatted shortcuts section</returns>
        public string RenderKeyboardShortcuts(int consoleWidth)
        {
            var builder = new StringBuilder();

            var mappedShortcuts = _shortcutConfigurationManager.GetMappedShortcuts();
            var configurationIssues = _shortcutConfigurationManager.GetConfigurationIssues();

            // Create shortcut display data
            var shortcutRows = new List<ShortcutDisplayRow>();

            foreach (var (action, keyMapping) in mappedShortcuts)
            {
                var row = new ShortcutDisplayRow
                {
                    Action = GetActionDisplayName(action),
                    Shortcut = keyMapping.HasValue ? _shortcutParser.FormatShortcut(keyMapping.Value.Key, keyMapping.Value.Modifiers) : "None",
                    Status = GetStatusDisplay(action, keyMapping, configurationIssues)
                };
                shortcutRows.Add(row);
            }

            // Sort by action name for consistent display
            shortcutRows = shortcutRows.OrderBy(r => r.Action).ToList();

            // Create table columns
            var columns = new List<ITableColumn<ShortcutDisplayRow>>
            {
                new TextColumn<ShortcutDisplayRow>("Action", r => r.Action, 30, 50),
                new TextColumn<ShortcutDisplayRow>("Shortcut", r => r.Shortcut, 12, 20),
                new TextColumn<ShortcutDisplayRow>("Status", r => r.Status, 15, 40)
            };

            // Use TableFormatter to create the shortcuts table
            _tableFormatter.AppendTable(
                builder,
                "KEYBOARD SHORTCUTS:",
                shortcutRows,
                columns,
                targetColumnCount: 1,
                consoleWidth: consoleWidth,
                singleColumnBarWidth: 20
            );

            return builder.ToString();
        }

        /// <summary>
        /// Gets a human-readable display name for a shortcut action using Description attributes
        /// </summary>
        private static string GetActionDisplayName(ShortcutAction action)
        {
            return AttributeHelper.GetDescription(action);
        }

        /// <summary>
        /// Centers text within the specified width
        /// </summary>
        /// <param name="text">Text to center</param>
        /// <param name="width">Total width to center within</param>
        /// <returns>Centered text with appropriate padding</returns>
        private static string CenterText(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || width <= text.Length)
                return text;

            var padding = (width - text.Length) / 2;
            return new string(' ', padding) + text;
        }

        /// <summary>
        /// Gets the status display text for a shortcut, including any configuration issues
        /// </summary>
        private static string GetStatusDisplay(ShortcutAction action, (ConsoleKey Key, ConsoleModifiers Modifiers)? keyMapping, List<string> configurationIssues)
        {
            if (keyMapping == null)
            {
                // Find the specific issue for this action
                var actionDisplayName = GetActionDisplayName(action);
                var issue = configurationIssues.FirstOrDefault(i => i.StartsWith(actionDisplayName + ":"));

                if (issue != null)
                {
                    // Extract the issue description after the colon
                    var issueDescription = issue.Substring(actionDisplayName.Length + 1).Trim();
                    return $"✗ Disabled ({issueDescription})";
                }

                return "✗ Disabled";
            }

            return "✓ Active";
        }

        /// <summary>
        /// Data class for shortcut display rows
        /// </summary>
        private class ShortcutDisplayRow
        {
            public string Action { get; set; } = string.Empty;
            public string Shortcut { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }


}