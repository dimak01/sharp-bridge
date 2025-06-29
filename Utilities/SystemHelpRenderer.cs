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
        private readonly ITableFormatter _tableFormatter;

        /// <summary>
        /// Initializes a new instance of the SystemHelpRenderer
        /// </summary>
        /// <param name="shortcutConfigurationManager">Configuration manager for shortcut information</param>
        /// <param name="tableFormatter">Table formatter for creating formatted tables</param>
        public SystemHelpRenderer(IShortcutConfigurationManager shortcutConfigurationManager, ITableFormatter tableFormatter)
        {
            _shortcutConfigurationManager = shortcutConfigurationManager ?? throw new ArgumentNullException(nameof(shortcutConfigurationManager));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
        }

        /// <summary>
        /// Renders the complete system help display including application configuration and keyboard shortcuts
        /// </summary>
        /// <param name="generalSettingsConfig">Application configuration to display</param>
        /// <param name="consoleWidth">Available console width for formatting</param>
        /// <returns>Formatted help content as a string</returns>
        public string RenderSystemHelp(GeneralSettingsConfig generalSettingsConfig, int consoleWidth)
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
            builder.AppendLine(RenderApplicationConfiguration(generalSettingsConfig));
            builder.AppendLine();

            // Keyboard Shortcuts section
            builder.AppendLine(RenderShortcutsTable(consoleWidth));

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
        /// <param name="generalSettingsConfig">Application configuration to display</param>
        /// <returns>Formatted configuration section</returns>
        public string RenderApplicationConfiguration(GeneralSettingsConfig generalSettingsConfig)
        {
            var builder = new StringBuilder();

            builder.AppendLine("APPLICATION CONFIGURATION:");
            builder.AppendLine("─────────────────────────");

            if (generalSettingsConfig == null)
            {
                builder.AppendLine("  No configuration loaded");
                return builder.ToString();
            }

            // Use reflection to display all properties with their descriptions
            var properties = typeof(GeneralSettingsConfig).GetProperties();
            foreach (var property in properties)
            {
                // Skip the Shortcuts property as it's displayed separately
                if (property.Name == nameof(GeneralSettingsConfig.Shortcuts))
                    continue;

                var displayName = AttributeHelper.GetPropertyDescription(typeof(GeneralSettingsConfig), property.Name);
                var value = property.GetValue(generalSettingsConfig);
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
            var shortcutRows = new List<ShortcutDisplayRow>();

            var mappedShortcuts = _shortcutConfigurationManager.GetMappedShortcuts();

            foreach (var action in mappedShortcuts.Keys)
            {
                var row = new ShortcutDisplayRow
                {
                    Action = GetActionDisplayName(action),
                    Shortcut = _shortcutConfigurationManager.GetDisplayString(action),
                    Status = GetStatusDisplay(action)
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
        /// Renders the shortcuts table section of the help display
        /// </summary>
        private string RenderShortcutsTable(int consoleWidth)
        {
            return RenderKeyboardShortcuts(consoleWidth);
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
        /// Gets the status display text for a shortcut using the new status system
        /// </summary>
        private string GetStatusDisplay(ShortcutAction action)
        {
            var status = _shortcutConfigurationManager.GetShortcutStatus(action);

            return status switch
            {
                ShortcutStatus.Active => "✓ Active",
                ShortcutStatus.Invalid => "✗ Invalid Format",
                ShortcutStatus.ExplicitlyDisabled => "✗ Disabled",
                _ => "✗ Unknown"
            };
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