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
        private readonly IParameterTableConfigurationManager _parameterTableConfigurationManager;
        private readonly ITableFormatter _tableFormatter;
        private readonly INetworkStatusFormatter _networkStatusFormatter;

        /// <summary>
        /// Initializes a new instance of the SystemHelpRenderer
        /// </summary>
        /// <param name="shortcutConfigurationManager">Configuration manager for shortcut information</param>
        /// <param name="parameterTableConfigurationManager">Configuration manager for parameter table columns</param>
        /// <param name="tableFormatter">Table formatter for creating formatted tables</param>
        /// <param name="networkStatusFormatter">Network status formatter for troubleshooting section</param>
        public SystemHelpRenderer(IShortcutConfigurationManager shortcutConfigurationManager, IParameterTableConfigurationManager parameterTableConfigurationManager, ITableFormatter tableFormatter, INetworkStatusFormatter networkStatusFormatter)
        {
            _shortcutConfigurationManager = shortcutConfigurationManager ?? throw new ArgumentNullException(nameof(shortcutConfigurationManager));
            _parameterTableConfigurationManager = parameterTableConfigurationManager ?? throw new ArgumentNullException(nameof(parameterTableConfigurationManager));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
            _networkStatusFormatter = networkStatusFormatter ?? throw new ArgumentNullException(nameof(networkStatusFormatter));
        }

        /// <summary>
        /// Renders the complete system help display including all application configuration sections and keyboard shortcuts
        /// </summary>
        /// <param name="applicationConfig">Complete application configuration to display</param>
        /// <param name="consoleWidth">Available console width for formatting</param>
        /// <param name="networkStatus">Optional network status to include in troubleshooting section</param>
        /// <returns>Formatted help content as a string</returns>
        public string RenderSystemHelp(ApplicationConfig applicationConfig, int consoleWidth, NetworkStatus? networkStatus = null)
        {
            var builder = new StringBuilder();

            var systemHelpScreenWidth = Math.Min(consoleWidth, 80);
            // Create separator line based on console width
            var separatorLine = new string('═', systemHelpScreenWidth);

            // Get the help shortcut dynamically
            var helpShortcut = _shortcutConfigurationManager.GetDisplayString(ShortcutAction.ShowSystemHelp);

            // Header
            builder.AppendLine(separatorLine);
            builder.AppendLine(CenterText($"SHARP BRIDGE - SYSTEM HELP ({helpShortcut})", systemHelpScreenWidth));
            builder.AppendLine(separatorLine);
            builder.AppendLine();

            // Application Configuration sections
            builder.AppendLine(RenderApplicationConfiguration(applicationConfig));

            builder.AppendLine(RenderKeyboardShortcuts(consoleWidth));

            builder.AppendLine(RenderParameterTableColumns(consoleWidth));

            // Network Troubleshooting section (if network status provided)
            if (networkStatus != null)
            {
                builder.AppendLine();
                builder.AppendLine(_networkStatusFormatter.RenderNetworkTroubleshooting(networkStatus, applicationConfig));
            }

            // Footer
            builder.AppendLine();
            builder.AppendLine(separatorLine);
            builder.AppendLine(CenterText($"Press {helpShortcut} again to return to main display", systemHelpScreenWidth));
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

            if (applicationConfig == null)
            {
                builder.AppendLine("  No configuration loaded");
                return builder.ToString();
            }

            // General Settings Section
            builder.AppendLine();
            builder.AppendLine(CreateSectionHeader("GENERAL SETTINGS"));
            RenderConfigSection(builder, applicationConfig.GeneralSettings, skipProperties: new[] { nameof(GeneralSettingsConfig.Shortcuts) });

            // Phone Client Section  
            builder.AppendLine();
            builder.AppendLine(CreateSectionHeader("PHONE CLIENT"));
            RenderConfigSection(builder, applicationConfig.PhoneClient);

            // PC Client Section
            builder.AppendLine();
            builder.AppendLine(CreateSectionHeader("PC CLIENT"));
            RenderConfigSection(builder, applicationConfig.PCClient);

            // Transformation Engine Section
            builder.AppendLine();
            builder.AppendLine(CreateSectionHeader("TRANSFORMATION ENGINE"));
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
                var displayValue = ConsoleColors.ColorizeBasicType(value);

                builder.AppendLine($"  {ConsoleColors.Colorize(displayName, ConsoleColors.ConfigPropertyName)}: {displayValue}");
            }
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
                    Action = ConsoleColors.Colorize(AttributeHelper.GetDescription(action), ConsoleColors.ShortcutActionColor),
                    Shortcut = ConsoleColors.Colorize(_shortcutConfigurationManager.GetDisplayString(action), ConsoleColors.ShortcutKeyColor),
                    Status = GetStatusDisplay(action)
                };
                shortcutRows.Add(row);
            }

            // Sort by action name for consistent display
            shortcutRows = shortcutRows.OrderBy(r => ConsoleColors.RemoveAnsiEscapeCodes(r.Action)).ToList();

            // Create table columns
            var columns = new List<ITableColumnFormatter<ShortcutDisplayRow>>
            {
                new TextColumnFormatter<ShortcutDisplayRow>("Action", r => r.Action, 30, 50),
                new TextColumnFormatter<ShortcutDisplayRow>("Shortcut", r => r.Shortcut, 12, 20),
                new TextColumnFormatter<ShortcutDisplayRow>("Status", r => r.Status, 15, 40)
            };

            // Add the header with underline manually to match other sections
            builder.AppendLine(CreateSectionHeader("KEYBOARD SHORTCUTS"));

            // Use TableFormatter to create the shortcuts table (without title since we added it manually)
            _tableFormatter.AppendTable(
                builder,
                "", // Empty title since we added it manually above
                shortcutRows,
                columns,
                targetColumnCount: 1,
                consoleWidth: consoleWidth,
                singleColumnBarWidth: 20
            );

            return builder.ToString();
        }

        /// <summary>
        /// Renders the parameter table column configuration section
        /// </summary>
        /// <param name="consoleWidth">Available console width for table formatting</param>
        /// <returns>Formatted parameter table column configuration section</returns>
        public string RenderParameterTableColumns(int consoleWidth)
        {
            var builder = new StringBuilder();
            var columnRows = new List<ParameterTableColumnDisplayRow>();

            var currentColumns = _parameterTableConfigurationManager.GetParameterTableColumns();

            // Create rows for each column
            foreach (var column in currentColumns)
            {
                var row = new ParameterTableColumnDisplayRow
                {
                    ColumnName = ConsoleColors.Colorize(_parameterTableConfigurationManager.GetColumnDisplayName(column), ConsoleColors.ConfigPropertyName),
                    Order = Array.IndexOf(currentColumns, column) + 1
                };
                columnRows.Add(row);
            }

            // Sort by order for consistent display
            columnRows = columnRows.OrderBy(r => r.Order).ToList();

            // Create table columns
            var columns = new List<ITableColumnFormatter<ParameterTableColumnDisplayRow>>
                    {
                        new TextColumnFormatter<ParameterTableColumnDisplayRow>("Order", r => r.Order.ToString(), 5, 8),
                        new TextColumnFormatter<ParameterTableColumnDisplayRow>("Column", r => r.ColumnName, 20, 40)
                    };

            // Add the header with underline manually to match other sections
            builder.AppendLine(CreateSectionHeader("PC PARAMETER TABLE COLUMNS"));

            // Use TableFormatter to create the columns table (without title since we added it manually)
            _tableFormatter.AppendTable(
                builder,
                "", // Empty title since we added it manually above
                columnRows,
                columns,
                targetColumnCount: 1,
                consoleWidth: consoleWidth,
                singleColumnBarWidth: 20
            );

            return builder.ToString();
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
        /// Creates a section header with consistent formatting
        /// </summary>
        /// <param name="title">Section title</param>
        /// <param name="maxWidth">Maximum width for the header (default 60)</param>
        /// <returns>Formatted section header</returns>
        private static string CreateSectionHeader(string title, int maxWidth = 60)
        {
            var headerWidth = Math.Min(title.Length + 2, maxWidth);
            var underline = new string('─', headerWidth);
            return $"{title}:\n{underline}";
        }

        /// <summary>
        /// Gets the status display text for a shortcut using the new status system
        /// </summary>
        private string GetStatusDisplay(ShortcutAction action)
        {
            var status = _shortcutConfigurationManager.GetShortcutStatus(action);

            return status switch
            {
                ShortcutStatus.Active => ConsoleColors.Colorize("✓ Active", ConsoleColors.Success),
                ShortcutStatus.Invalid => ConsoleColors.Colorize("✗ Invalid Format", ConsoleColors.Error),
                ShortcutStatus.ExplicitlyDisabled => ConsoleColors.Colorize("✗ Disabled", ConsoleColors.Warning),
                _ => ConsoleColors.Colorize("✗ Unknown", ConsoleColors.Error)
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

        /// <summary>
        /// Data class for parameter table column display rows
        /// </summary>
        private class ParameterTableColumnDisplayRow
        {
            public string ColumnName { get; set; } = string.Empty;
            public int Order { get; set; }
        }
    }
}