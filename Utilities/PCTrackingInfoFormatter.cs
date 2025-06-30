using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Formatter for PC tracking information with dynamic shortcut support
    /// </summary>
    public class PCTrackingInfoFormatter : IFormatter
    {
        // Counter Keys
        private const string TOTAL_MESSAGES_KEY = "Total Messages";
        private const string UPTIME_SECONDS_KEY = "Uptime (seconds)";

        // Display Limits
        private const int PARAMETER_DISPLAY_COUNT_NORMAL = 15;

        // Column Width Constants
        private const int PARAMETER_NAME_COLUMN_MIN_WIDTH = 8;
        private const int PARAMETER_NAME_COLUMN_MAX_WIDTH = 20;
        private const int PARAMETER_VALUE_COLUMN_MIN_WIDTH = 8;
        private const int PARAMETER_VALUE_COLUMN_MAX_WIDTH = 15;

        // Table Formatting Constants
        private const int TABLE_MINIMUM_ROWS = 1;
        private const int TABLE_MINIMUM_WIDTH = 20;

        // Service Display Constants
        private const string SERVICE_NAME = "PC Client";

        private readonly IConsole _console;
        private readonly ITableFormatter _tableFormatter;
        private readonly IParameterColorService _colorService;
        private readonly IShortcutConfigurationManager _shortcutManager;

        /// <summary>
        /// Initializes a new instance of the PCTrackingInfoFormatter
        /// </summary>
        /// <param name="console">Console abstraction for getting window dimensions</param>
        /// <param name="tableFormatter">Table formatter for generating tables</param>
        /// <param name="colorService">Parameter color service for colored display</param>
        /// <param name="shortcutManager">Shortcut configuration manager for dynamic shortcuts</param>
        public PCTrackingInfoFormatter(IConsole console, ITableFormatter tableFormatter, IParameterColorService colorService, IShortcutConfigurationManager shortcutManager)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
            _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
            _shortcutManager = shortcutManager ?? throw new ArgumentNullException(nameof(shortcutManager));
        }

        /// <summary>
        /// Current verbosity level for this formatter
        /// </summary>
        public VerbosityLevel CurrentVerbosity { get; private set; } = VerbosityLevel.Normal;

        /// <summary>
        /// Cycles to the next verbosity level
        /// </summary>
        /// <returns>The new verbosity level after cycling</returns>
        public VerbosityLevel CycleVerbosity()
        {
            CurrentVerbosity = CurrentVerbosity switch
            {
                VerbosityLevel.Basic => VerbosityLevel.Normal,
                VerbosityLevel.Normal => VerbosityLevel.Detailed,
                VerbosityLevel.Detailed => VerbosityLevel.Basic,
                _ => VerbosityLevel.Normal
            };
            return CurrentVerbosity;
        }

        /// <summary>
        /// Formats a PCTrackingInfo object with service statistics into a display string
        /// </summary>
        public string Format(IServiceStats stats)
        {
            if (stats == null)
                return "No service data available";

            var builder = new StringBuilder();

            // Get dynamic shortcut instead of hardcoded "Alt+P"
            var verbosityShortcut = _shortcutManager.GetDisplayString(ShortcutAction.CyclePCClientVerbosity);

            // Header with service status
            builder.AppendLine(FormatServiceHeader(SERVICE_NAME, stats.Status, verbosityShortcut));

            // Tracking data details
            if (stats.CurrentEntity is PCTrackingInfo pcTrackingInfo)
            {
                // Face detection status
                var faceIcon = pcTrackingInfo.FaceFound ? "√" : "X";
                var faceColor = pcTrackingInfo.FaceFound ? ConsoleColors.Success : ConsoleColors.Warning;
                builder.AppendLine($"Face Status: {ConsoleColors.Colorize($"{faceIcon} {(pcTrackingInfo.FaceFound ? "Detected" : "Not Found")}", faceColor)}");

                builder.AppendLine();

                if (CurrentVerbosity >= VerbosityLevel.Normal)
                {
                    AppendParameters(builder, pcTrackingInfo);
                }
            }
            else if (stats.CurrentEntity != null)
            {
                throw new ArgumentException("CurrentEntity must be of type PCTrackingInfo or null");
            }
            else
            {
                builder.AppendLine();
                builder.AppendLine("No current tracking data available");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Appends the parameter information to the string builder using the new table format
        /// </summary>
        private void AppendParameters(StringBuilder builder, PCTrackingInfo trackingInfo)
        {
            var parameters = trackingInfo.Parameters.ToList();

            // Sort parameters by ID - let TableFormatter handle display limits
            var parametersToShow = parameters
                .OrderBy(p => p.Id)
                .ToList();

            // Define columns for the generic table
            var columns = new List<ITableColumn<TrackingParam>>
            {
                new TextColumn<TrackingParam>("Parameter", param => _colorService.GetColoredCalculatedParameterName(param.Id), minWidth: 8),
                new ProgressBarColumn<TrackingParam>("", param => CalculateNormalizedValue(param, trackingInfo), minWidth: 6, maxWidth: 20, _tableFormatter),
                new NumericColumn<TrackingParam>("Value", param => param.Value, "0.##", minWidth: 6, padLeft: true),
                new TextColumn<TrackingParam>("Width x Range", param => FormatCompactRange(param, trackingInfo), minWidth: 12, maxWidth: 25),
                new TextColumn<TrackingParam>("Expression", param => _colorService.GetColoredExpression(FormatExpression(param, trackingInfo)), minWidth: 15, maxWidth: 90)
            };

            // Use the new generic table formatter - let it handle display limits
            var singleColumnLimit = CurrentVerbosity == VerbosityLevel.Detailed ? (int?)null : PARAMETER_DISPLAY_COUNT_NORMAL;
            _tableFormatter.AppendTable(builder, "=== Parameters ===", parametersToShow, columns, 2, _console.WindowWidth, 20, singleColumnLimit);

            builder.AppendLine();
            builder.AppendLine($"Total Parameters: {parameters.Count}");
        }

        /// <summary>
        /// Calculates the normalized value (0.0 to 1.0) for a parameter based on its definition
        /// </summary>
        private static double CalculateNormalizedValue(TrackingParam param, PCTrackingInfo trackingInfo)
        {
            if (trackingInfo.ParameterDefinitions?.TryGetValue(param.Id, out var definition) == true)
            {
                double range = definition.Max - definition.Min;
                if (Math.Abs(range) > double.Epsilon)
                {
                    return Math.Max(0, Math.Min(1, (param.Value - definition.Min) / range));
                }
            }

            // Fallback: assume range of -1 to 1
            return Math.Max(0, Math.Min(1, (param.Value + 1) / 2));
        }

        /// <summary>
        /// Formats the compact range information for a parameter
        /// </summary>
        private static string FormatCompactRange(TrackingParam param, PCTrackingInfo trackingInfo)
        {
            var weight = param.Weight?.ToString("0.##") ?? "1";

            if (trackingInfo.ParameterDefinitions?.TryGetValue(param.Id, out var definition) == true)
            {
                var min = definition.Min.ToString("0.##");
                var defaultVal = definition.DefaultValue.ToString("0.##");
                var max = definition.Max.ToString("0.##");
                return $"{weight} x [{min}; {defaultVal}; {max}]";
            }

            // Fallback for parameters without definitions
            return $"{weight} x [no definition]";
        }

        /// <summary>
        /// Formats the transformation expression for a parameter
        /// </summary>
        private static string FormatExpression(TrackingParam param, PCTrackingInfo trackingInfo)
        {
            if (trackingInfo?.ParameterCalculationExpressions == null)
                return "[no expression]";

            if (trackingInfo.ParameterCalculationExpressions.TryGetValue(param.Id, out var expression))
            {
                if (string.IsNullOrEmpty(expression))
                    return "[no expression]";

                // Truncate long expressions for display
                const int maxLength = 90;
                if (expression.Length > maxLength)
                {
                    return expression.Substring(0, maxLength - 3) + "...";
                }
                return expression;
            }

            // Fallback for parameters without expressions
            return "[no expression]";
        }

        /// <summary>
        /// Formats a service header with status and color coding
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="status">Current status</param>
        /// <param name="shortcut">Keyboard shortcut (e.g., "Alt+P")</param>
        /// <returns>Formatted header string</returns>
        private string FormatServiceHeader(string serviceName, string status, string shortcut)
        {
            var verbosity = CurrentVerbosity switch
            {
                VerbosityLevel.Basic => "[BASIC]",
                VerbosityLevel.Normal => "[INFO]",
                VerbosityLevel.Detailed => "[DEBUG]",
                _ => "[INFO]"
            };
            var statusColor = ConsoleColors.GetStatusColor(status);
            var colorizedStatus = ConsoleColors.Colorize(status, statusColor);
            return $"=== {verbosity} {serviceName} ({colorizedStatus}) === [{shortcut}]";
        }
    }
}