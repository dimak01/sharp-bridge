using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using SharpBridge.Interfaces;
using SharpBridge.Models;

[assembly: InternalsVisibleTo("Tests")]

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Formatter for PhoneTrackingInfo objects with service statistics
    /// </summary>
    public class PhoneTrackingInfoFormatter : IFormatter
    {
        private const int TARGET_COLUMN_COUNT = 4;
        private const int TARGET_ROWS_NORMAL = 13;

        // Service Display Constants
        private const string SERVICE_NAME = "Phone Client";

        private readonly IConsole _console;
        private readonly ITableFormatter _tableFormatter;
        private readonly IParameterColorService _colorService;
        private readonly IShortcutConfigurationManager _shortcutManager;

        /// <summary>
        /// Gets or sets the current time for testing purposes. If null, uses DateTime.UtcNow.
        /// </summary>
        public DateTime? CurrentTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the PhoneTrackingInfoFormatter
        /// </summary>
        /// <param name="console">Console abstraction for getting window dimensions</param>
        /// <param name="tableFormatter">Table formatter for generating tables</param>
        /// <param name="colorService">Parameter color service for colored display</param>
        /// <param name="shortcutManager">Shortcut configuration manager for dynamic shortcuts</param>
        /// <param name="userPreferences">User preferences for initial verbosity level</param>
        public PhoneTrackingInfoFormatter(IConsole console, ITableFormatter tableFormatter, IParameterColorService colorService, IShortcutConfigurationManager shortcutManager, UserPreferences userPreferences)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
            _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
            _shortcutManager = shortcutManager ?? throw new ArgumentNullException(nameof(shortcutManager));
            var preferences = userPreferences ?? throw new ArgumentNullException(nameof(userPreferences));

            // Initialize verbosity from user preferences
            CurrentVerbosity = preferences.PhoneClientVerbosity;
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
        /// Formats a PhoneTrackingInfo object with service statistics into a display string
        /// </summary>
        public string Format(IServiceStats stats)
        {
            if (stats == null)
                return "No service data available";

            var builder = new StringBuilder();

            // Get dynamic shortcut instead of hardcoded "Alt+O"
            var verbosityShortcut = _shortcutManager.GetDisplayString(ShortcutAction.CyclePhoneClientVerbosity);

            // Header with service status
            builder.AppendLine(FormatServiceHeader(SERVICE_NAME, stats.Status, verbosityShortcut));

            // Tracking data details
            if (stats.CurrentEntity is PhoneTrackingInfo phoneTrackingInfo)
            {
                AppendTrackingDetails(builder, phoneTrackingInfo);
            }
            else if (stats.CurrentEntity != null)
            {
                throw new ArgumentException("CurrentEntity must be of type PhoneTrackingInfo or null");
            }
            else
            {
                builder.AppendLine();
                builder.AppendLine("No current tracking data available");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Appends tracking data details to the string builder
        /// </summary>
        private void AppendTrackingDetails(StringBuilder builder, PhoneTrackingInfo phoneTrackingInfo)
        {
            AppendFaceStatus(builder, phoneTrackingInfo);
            AppendPositionAndRotation(builder, phoneTrackingInfo);
            AppendBlendShapes(builder, phoneTrackingInfo);
        }

        /// <summary>
        /// Appends face detection status to the string builder
        /// </summary>
        private static void AppendFaceStatus(StringBuilder builder, PhoneTrackingInfo phoneTrackingInfo)
        {
            var faceIcon = phoneTrackingInfo.FaceFound ? "√" : "X";
            var faceColor = phoneTrackingInfo.FaceFound ? ConsoleColors.Success : ConsoleColors.Warning;
            var faceText = phoneTrackingInfo.FaceFound ? "Detected" : "Not Found";

            builder.AppendLine($"Face Status: {ConsoleColors.Colorize($"{faceIcon} {faceText}", faceColor)}");
        }

        /// <summary>
        /// Appends position and rotation data if face is detected and verbosity allows
        /// </summary>
        private void AppendPositionAndRotation(StringBuilder builder, PhoneTrackingInfo phoneTrackingInfo)
        {
            if (!ShouldShowPositionAndRotation(phoneTrackingInfo))
                return;

            AppendRotationData(builder, phoneTrackingInfo.Rotation);
            AppendPositionData(builder, phoneTrackingInfo.Position);
        }

        /// <summary>
        /// Determines if position and rotation data should be shown
        /// </summary>
        private bool ShouldShowPositionAndRotation(PhoneTrackingInfo phoneTrackingInfo)
        {
            return phoneTrackingInfo.FaceFound && CurrentVerbosity >= VerbosityLevel.Normal;
        }

        /// <summary>
        /// Appends rotation data if available
        /// </summary>
        private static void AppendRotationData(StringBuilder builder, Coordinates rotation)
        {
            if (rotation != null)
            {
                builder.AppendLine($"Head Rotation (X,Y,Z): " +
                    $"{rotation.X:F1}°, " +
                    $"{rotation.Y:F1}°, " +
                    $"{rotation.Z:F1}°");
            }
        }

        /// <summary>
        /// Appends position data if available
        /// </summary>
        private static void AppendPositionData(StringBuilder builder, Coordinates position)
        {
            if (position != null)
            {
                builder.AppendLine($"Head Position (X,Y,Z): " +
                    $"{position.X:F1}, " +
                    $"{position.Y:F1}, " +
                    $"{position.Z:F1}");
            }
        }

        /// <summary>
        /// Appends blend shapes data in detailed mode
        /// </summary>
        private void AppendBlendShapes(StringBuilder builder, PhoneTrackingInfo phoneTrackingInfo)
        {
            if (CurrentVerbosity < VerbosityLevel.Normal)
                return;

            builder.AppendLine();

            if (HasNoBlendShapes(phoneTrackingInfo))
            {
                builder.AppendLine("No blend shapes");
                return;
            }

            AppendBlendShapesTable(builder, phoneTrackingInfo);
        }

        /// <summary>
        /// Checks if there are no blend shapes available
        /// </summary>
        private static bool HasNoBlendShapes(PhoneTrackingInfo phoneTrackingInfo)
        {
            return phoneTrackingInfo.BlendShapes == null || phoneTrackingInfo.BlendShapes.Count == 0;
        }

        /// <summary>
        /// Appends the blend shapes table to the string builder
        /// </summary>
        private void AppendBlendShapesTable(StringBuilder builder, PhoneTrackingInfo phoneTrackingInfo)
        {
            var sortedShapes = GetSortedBlendShapes(phoneTrackingInfo);
            var columns = CreateBlendShapeColumns();
            var singleColumnLimit = GetBlendShapeDisplayLimit();

            _tableFormatter.AppendTable(builder, "=== BlendShapes ===", sortedShapes, columns,
                TARGET_COLUMN_COUNT, _console.WindowWidth, 20, singleColumnLimit);

            builder.AppendLine();
            builder.AppendLine($"Total Blend Shapes: {phoneTrackingInfo.BlendShapes.Count}");
        }

        /// <summary>
        /// Gets sorted blend shapes for display
        /// </summary>
        private static List<BlendShape> GetSortedBlendShapes(PhoneTrackingInfo phoneTrackingInfo)
        {
            return phoneTrackingInfo.BlendShapes
                .Where(s => s != null)
                .OrderBy(s => s.Key)
                .ToList();
        }

        /// <summary>
        /// Creates columns for the blend shapes table
        /// </summary>
        private List<ITableColumnFormatter<BlendShape>> CreateBlendShapeColumns()
        {
            return new List<ITableColumnFormatter<BlendShape>>
            {
                new TextColumnFormatter<BlendShape>("Expression", shape => _colorService.GetColoredBlendShapeName(shape.Key), minWidth: 10, maxWidth: 20),
                new ProgressBarColumnFormatter<BlendShape>("", shape => shape.Value, minWidth: 6, maxWidth: 15, _tableFormatter),
                new NumericColumnFormatter<BlendShape>("Value", shape => shape.Value, "F2", minWidth: 6, padLeft: true)
            };
        }

        /// <summary>
        /// Gets the display limit for blend shapes based on verbosity
        /// </summary>
        private int? GetBlendShapeDisplayLimit()
        {
            return CurrentVerbosity == VerbosityLevel.Detailed ? (int?)null : TARGET_ROWS_NORMAL;
        }

        /// <summary>
        /// Formats a service header with status and color coding
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="status">Current status</param>
        /// <param name="shortcut">Keyboard shortcut (e.g., "Alt+O")</param>
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