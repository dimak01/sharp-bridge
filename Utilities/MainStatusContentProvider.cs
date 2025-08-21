using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Main status dashboard renderer
    /// </summary>
    public class MainStatusContentProvider : IMainStatusRenderer, IConsoleModeContentProvider
    {
        private readonly Dictionary<Type, IFormatter> _formatters = new Dictionary<Type, IFormatter>();
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly object _lock = new object();
        private readonly IAppLogger _logger;
        private readonly IShortcutConfigurationManager _shortcutManager;
        private readonly IExternalEditorService? _externalEditorService;

        /// <summary>
        /// Initializes a new instance of the MainStatusRenderer class
        /// </summary>
        /// <param name="console">The console implementation to use for output</param>
        /// <param name="logger">The logger to use for error reporting</param>
        /// <param name="transformationFormatter">The transformation engine info formatter</param>
        /// <param name="phoneFormatter">The phone tracking info formatter</param>
        /// <param name="pcFormatter">The PC tracking info formatter</param>
        /// <param name="shortcutManager">Shortcut configuration manager for dynamic shortcuts</param>
        public MainStatusContentProvider(IAppLogger logger, TransformationEngineInfoFormatter transformationFormatter, PhoneTrackingInfoFormatter phoneFormatter, PCTrackingInfoFormatter pcFormatter, IShortcutConfigurationManager shortcutManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shortcutManager = shortcutManager ?? throw new ArgumentNullException(nameof(shortcutManager));

            // Register formatters for known types - order determines display order
            RegisterFormatter<TransformationEngineInfo>(transformationFormatter ?? throw new ArgumentNullException(nameof(transformationFormatter)));
            RegisterFormatter<PhoneTrackingInfo>(phoneFormatter ?? throw new ArgumentNullException(nameof(phoneFormatter)));
            RegisterFormatter<PCTrackingInfo>(pcFormatter ?? throw new ArgumentNullException(nameof(pcFormatter)));
        }

        /// <summary>
        /// Initializes a new instance of the MainStatusRenderer class with external editor support
        /// </summary>
        public MainStatusContentProvider(IAppLogger logger, TransformationEngineInfoFormatter transformationFormatter, PhoneTrackingInfoFormatter phoneFormatter, PCTrackingInfoFormatter pcFormatter, IShortcutConfigurationManager shortcutManager, IExternalEditorService externalEditorService)
            : this(logger, transformationFormatter, phoneFormatter, pcFormatter, shortcutManager)
        {
            _externalEditorService = externalEditorService ?? throw new ArgumentNullException(nameof(externalEditorService));
        }

        /// <summary>
        /// Registers a formatter for a specific entity type
        /// </summary>
        public void RegisterFormatter<T>(IFormatter formatter) where T : IFormattableObject
        {
            _formatters[typeof(T)] = formatter;
        }

        /// <summary>
        /// Gets a formatter for the specified type
        /// </summary>
        public IFormatter? GetFormatter<T>() where T : IFormattableObject
        {
            if (_formatters.TryGetValue(typeof(T), out var formatter))
            {
                return formatter;
            }
            return null;
        }

        // IConsoleModeRenderer implementation

        public ConsoleMode Mode => ConsoleMode.Main;

        public string DisplayName => "Main Status";

        // Main mode doesn't need a dedicated toggle action; manager can return to Main when toggling the same mode
        public ShortcutAction ToggleAction => ShortcutAction.ShowSystemHelp; // placeholder, not used for Main mode

        public async Task<bool> TryOpenInExternalEditorAsync()
        {
            try
            {
                if (_externalEditorService == null)
                {
                    return false;
                }
                return await _externalEditorService.TryOpenTransformationConfigAsync();
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error opening transformation config in external editor", ex);
                return false;
            }
        }

        public void Enter(IConsole console)
        {
            // No-op for now; keep console as-is or clear if needed
        }

        public void Exit(IConsole console)
        {
            // No-op for now; keep console as-is or clear if needed
        }

        /// <summary>
        /// Gets the content for the main status display
        /// </summary>
        /// <param name="context">The context containing the service statistics</param>
        /// <returns>An array of strings representing the main status display</returns>
        public string[] GetContent(ConsoleRenderContext context)
        {
            var stats = context?.ServiceStats ?? Array.Empty<IServiceStats>();
            return BuildDisplayLines(stats);
        }

        public TimeSpan PreferredUpdateInterval => TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Determines if the console should be updated based on timing
        /// </summary>
        private bool ShouldUpdate()
        {
            var now = DateTime.UtcNow;
            return now - _lastUpdate >= TimeSpan.FromMilliseconds(100);
        }

        /// <summary>
        /// Builds the complete list of display lines from service statistics
        /// </summary>
        private string[] BuildDisplayLines(IEnumerable<IServiceStats> stats)
        {
            var lines = new List<string>();

            AddHeaderLines(lines);
            AddServiceLines(lines, stats);
            AddFooterLines(lines);

            return lines.ToArray();
        }

        /// <summary>
        /// Adds header lines to the display
        /// </summary>
        private static void AddHeaderLines(List<string> lines)
        {
            lines.Add($"=== SharpBridge Status at {DateTime.Now:HH:mm:ss} ===");
            lines.Add(string.Empty);
        }

        /// <summary>
        /// Adds service status lines to the display
        /// </summary>
        private void AddServiceLines(List<string> lines, IEnumerable<IServiceStats> stats)
        {
            foreach (var stat in stats.Where(s => s != null))
            {
                AddSingleServiceLines(lines, stat);
                lines.Add(string.Empty);
            }
        }

        /// <summary>
        /// Adds lines for a single service to the display
        /// </summary>
        private void AddSingleServiceLines(List<string> lines, IServiceStats stat)
        {
            var formattedOutput = FormatServiceOutput(stat);
            AddFormattedOutputLines(lines, formattedOutput);
        }

        /// <summary>
        /// Formats the output for a single service
        /// </summary>
        private string FormatServiceOutput(IServiceStats stat)
        {
            if (stat.CurrentEntity != null)
            {
                return FormatServiceWithEntity(stat);
            }
            else
            {
                return FormatServiceWithoutEntity(stat);
            }
        }

        /// <summary>
        /// Formats service output when it has a current entity
        /// </summary>
        private string FormatServiceWithEntity(IServiceStats stat)
        {
            var entityType = stat.CurrentEntity!.GetType();

            if (_formatters.TryGetValue(entityType, out var formatter))
            {
                return formatter.Format(stat);
            }
            else
            {
                return CreateNoFormatterOutput(stat, entityType);
            }
        }

        /// <summary>
        /// Formats service output when it has no current entity
        /// </summary>
        private string FormatServiceWithoutEntity(IServiceStats stat)
        {
            var formatter = FindFormatterForServiceWithoutEntity(stat);

            if (formatter != null)
            {
                return formatter.Format(stat);
            }
            else
            {
                return CreateNoDataOutput(stat);
            }
        }

        /// <summary>
        /// Finds a formatter for a service that has no current entity
        /// </summary>
        private IFormatter? FindFormatterForServiceWithoutEntity(IServiceStats stat)
        {
            if (stat.ServiceName.Contains("Phone") &&
                _formatters.TryGetValue(typeof(PhoneTrackingInfo), out var phoneFormatter) &&
                phoneFormatter is PhoneTrackingInfoFormatter)
            {
                return phoneFormatter;
            }

            if (stat.ServiceName.Contains("PC") &&
                _formatters.TryGetValue(typeof(PCTrackingInfo), out var pcFormatter))
            {
                return pcFormatter;
            }

            return null;
        }

        /// <summary>
        /// Creates output when no formatter is registered
        /// </summary>
        private static string CreateNoFormatterOutput(IServiceStats stat, Type entityType)
        {
            return $"=== {stat.ServiceName} ({stat.Status}) ==={Environment.NewLine}" +
                   $"[No formatter registered for {entityType.Name}]";
        }

        /// <summary>
        /// Creates output when no data is available
        /// </summary>
        private static string CreateNoDataOutput(IServiceStats stat)
        {
            return $"=== {stat.ServiceName} ({stat.Status}) ==={Environment.NewLine}" +
                   "No current data available";
        }

        /// <summary>
        /// Adds formatted output lines to the display list
        /// </summary>
        private static void AddFormattedOutputLines(List<string> lines, string formattedOutput)
        {
            foreach (var line in formattedOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                lines.Add(line);
            }
        }

        /// <summary>
        /// Adds footer lines to the display
        /// </summary>
        private void AddFooterLines(List<string> lines)
        {
            var transformationShortcut = _shortcutManager.GetDisplayString(ShortcutAction.CycleTransformationEngineVerbosity);
            var pcShortcut = _shortcutManager.GetDisplayString(ShortcutAction.CyclePCClientVerbosity);
            var phoneShortcut = _shortcutManager.GetDisplayString(ShortcutAction.CyclePhoneClientVerbosity);

            lines.Add($"Press Ctrl+C to exit | {transformationShortcut} for Transformation Engine verbosity | {pcShortcut} for PC client verbosity | {phoneShortcut} for Phone client verbosity");
        }



        /// <summary>
        /// Clears the console
        /// </summary>
        public void ClearConsole()
        {
            // No-op for now; keep console as-is or clear if needed
        }


    }
}