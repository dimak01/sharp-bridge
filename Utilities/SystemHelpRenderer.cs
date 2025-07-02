using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of ISystemHelpRenderer for rendering the F2 help system display
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
        /// Renders the complete system help display including all application configuration sections and keyboard shortcuts
        /// </summary>
        /// <param name="applicationConfig">Complete application configuration to display</param>
        /// <param name="consoleWidth">Available console width for formatting</param>
        /// <returns>Formatted help content as a string</returns>
        public string RenderSystemHelp(ApplicationConfig applicationConfig, int consoleWidth)
        {
            var builder = new StringBuilder();

            // Create separator line based on console width
            var separatorLine = new string('═', Math.Max(consoleWidth, 80));

            // Header
            builder.AppendLine(separatorLine);
            builder.AppendLine(CenterText("SHARP BRIDGE - SYSTEM HELP (F2)", consoleWidth));
            builder.AppendLine(separatorLine);
            builder.AppendLine();

            // Application Configuration sections
            builder.AppendLine(RenderApplicationConfiguration(applicationConfig));
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
        /// Renders all application configuration sections
        /// </summary>
        /// <param name="applicationConfig">Complete application configuration to display</param>
        /// <returns>Formatted configuration sections</returns>
        public string RenderApplicationConfiguration(ApplicationConfig applicationConfig)
        {
            var builder = new StringBuilder();

            builder.AppendLine("APPLICATION CONFIGURATION:");
            builder.AppendLine("═════════════════════════");

            if (applicationConfig == null)
            {
                builder.AppendLine("  No configuration loaded");
                return builder.ToString();
            }

            // General Settings Section
            builder.AppendLine();
            builder.AppendLine("GENERAL SETTINGS:");
            builder.AppendLine("─────────────────");
            RenderConfigSection(builder, applicationConfig.GeneralSettings, skipProperties: new[] { nameof(GeneralSettingsConfig.Shortcuts) });

            // Phone Client Section  
            builder.AppendLine();
            builder.AppendLine("PHONE CLIENT:");
            builder.AppendLine("─────────────");
            RenderConfigSection(builder, applicationConfig.PhoneClient);

            // PC Client Section
            builder.AppendLine();
            builder.AppendLine("PC CLIENT:");
            builder.AppendLine("──────────");
            RenderConfigSection(builder, applicationConfig.PCClient);

            // Transformation Engine Section
            builder.AppendLine();
            builder.AppendLine("TRANSFORMATION ENGINE:");
            builder.AppendLine("──────────────────────");
            RenderConfigSection(builder, applicationConfig.TransformationEngine);

            return builder.ToString();
        }

        /// <summary>
        /// Renders a configuration section using reflection
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="configSection">Configuration section object to render</param>
        /// <param name="skipProperties">Properties to skip (e.g., Shortcuts which are displayed separately)</param>
        private static void RenderConfigSection(StringBuilder builder, object? configSection, string[]? skipProperties = null)
        {
            if (configSection == null)
            {
                builder.AppendLine("  Not configured");
                return;
            }

            skipProperties ??= Array.Empty<string>();

            // Use reflection to display all properties with their descriptions
            var properties = configSection.GetType().GetProperties();
            foreach (var property in properties)
            {
                // Skip properties that should not be displayed
                if (skipProperties.Contains(property.Name))
                    continue;

                // Skip properties marked with JsonIgnore (internal settings)
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                var displayName = AttributeHelper.GetPropertyDescription(configSection.GetType(), property.Name);
                var value = property.GetValue(configSection);
                var displayValue = FormatPropertyValue(value);

                builder.AppendLine($"  {displayName}: {displayValue}");
            }
        }

        /// <summary>
        /// Formats property values for display
        /// </summary>
        /// <param name="value">Property value to format</param>
        /// <returns>Formatted display string</returns>
        private static string FormatPropertyValue(object? value)
        {
            return value switch
            {
                null => "Not set",
                string str when string.IsNullOrWhiteSpace(str) => "Not set",
                bool b => b ? "Yes" : "No",
                _ => value.ToString() ?? "Not set"
            };
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