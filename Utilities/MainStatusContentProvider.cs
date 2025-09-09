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
        private readonly IAppLogger _logger;
        private readonly IExternalEditorService _externalEditorService;

        /// <summary>
        /// Initializes a new instance of the MainStatusRenderer class
        /// </summary>
        /// <param name="logger">The logger to use for error reporting</param>
        /// <param name="transformationFormatter">The transformation engine info formatter</param>
        /// <param name="phoneFormatter">The phone tracking info formatter</param>
        /// <param name="pcFormatter">The PC tracking info formatter</param>
        /// <param name="externalEditorService">The external editor service for opening configuration files</param>
        public MainStatusContentProvider(IAppLogger logger, IFormatter transformationFormatter, IFormatter phoneFormatter, IFormatter pcFormatter, IExternalEditorService externalEditorService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _externalEditorService = externalEditorService ?? throw new ArgumentNullException(nameof(externalEditorService));

            // Register formatters for known types - order determines display order
            RegisterFormatter<TransformationEngineInfo>(transformationFormatter ?? throw new ArgumentNullException(nameof(transformationFormatter)));
            RegisterFormatter<PhoneTrackingInfo>(phoneFormatter ?? throw new ArgumentNullException(nameof(phoneFormatter)));
            RegisterFormatter<PCTrackingInfo>(pcFormatter ?? throw new ArgumentNullException(nameof(pcFormatter)));
        }

        /// <summary>
        /// Registers a formatter for a specific entity type
        /// </summary>
        /// <typeparam name="T">The type of entity to format</typeparam>
        /// <param name="formatter">The formatter to register</param>
        public void RegisterFormatter<T>(IFormatter formatter) where T : IFormattableObject
        {
            _formatters[typeof(T)] = formatter;
        }

        /// <summary>
        /// Gets a formatter for the specified type
        /// </summary>
        /// <typeparam name="T">The type of entity to get a formatter for</typeparam>
        /// <returns>The formatter for the specified type, or null if not found</returns>
        public IFormatter? GetFormatter<T>() where T : IFormattableObject
        {
            if (_formatters.TryGetValue(typeof(T), out var formatter))
            {
                return formatter;
            }
            return null;
        }

        // IConsoleModeRenderer implementation

        /// <summary>
        /// Gets the console mode for this content provider
        /// </summary>
        public ConsoleMode Mode => ConsoleMode.Main;

        /// <summary>
        /// Gets the display name for this content provider
        /// </summary>
        public string DisplayName => "Application Status";

        /// <summary>
        /// Gets the toggle action for this content provider
        /// </summary>
        /// <remarks>Main mode doesn't need a dedicated toggle action; manager can return to Main when toggling the same mode</remarks>
        public ShortcutAction ToggleAction => ShortcutAction.ShowSystemHelp; // placeholder, not used for Main mode

        /// <summary>
        /// Attempts to open the transformation configuration in an external editor
        /// </summary>
        /// <returns>True if successfully opened, false otherwise</returns>
        public async Task<bool> TryOpenInExternalEditorAsync()
        {
            try
            {
                return await _externalEditorService.TryOpenTransformationConfigAsync();
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error opening transformation config in external editor", ex);
                return false;
            }
        }

        /// <summary>
        /// Enters the main status mode
        /// </summary>
        /// <param name="console">The console to operate on</param>
        public void Enter(IConsole console)
        {
            // No-op for now; keep console as-is or clear if needed
        }

        /// <summary>
        /// Exits the main status mode
        /// </summary>
        /// <param name="console">The console to operate on</param>
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

        /// <summary>
        /// Gets the preferred update interval for this content provider
        /// </summary>
        public TimeSpan PreferredUpdateInterval => TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Clears the console
        /// </summary>
        /// <remarks>No-op for now; keep console as-is or clear if needed</remarks>
        public void ClearConsole()
        {
            // No-op for now; keep console as-is or clear if needed
        }

        private string[] BuildDisplayLines(IEnumerable<IServiceStats> stats)
        {
            var lines = new List<string>();

            AddServiceLines(lines, stats);

            return lines.ToArray();
        }

        private void AddServiceLines(List<string> lines, IEnumerable<IServiceStats> stats)
        {
            foreach (var stat in stats.Where(s => s != null))
            {
                AddSingleServiceLines(lines, stat);
                lines.Add(string.Empty);
            }
        }

        private void AddSingleServiceLines(List<string> lines, IServiceStats stat)
        {
            var formattedOutput = FormatServiceOutput(stat);
            AddFormattedOutputLines(lines, formattedOutput);
        }

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

        private string FormatServiceWithEntity(IServiceStats stat)
        {
            var entityType = stat.CurrentEntity!.GetType();

            if (_formatters.TryGetValue(entityType, out var formatter))
            {
                try
                {
                    return formatter.Format(stat);
                }
                catch (Exception ex)
                {
                    _logger.ErrorWithException($"Error formatting {entityType.Name} for service {stat.ServiceName}", ex);
                    return CreateNoFormatterOutput(stat, entityType);
                }
            }
            else
            {
                return CreateNoFormatterOutput(stat, entityType);
            }
        }

        private string FormatServiceWithoutEntity(IServiceStats stat)
        {
            var formatter = FindFormatterForServiceWithoutEntity(stat);

            if (formatter != null)
            {
                try
                {
                    return formatter.Format(stat);
                }
                catch (Exception ex)
                {
                    _logger.ErrorWithException($"Error formatting service {stat.ServiceName} without entity", ex);
                    return CreateNoDataOutput(stat);
                }
            }
            else
            {
                return CreateNoDataOutput(stat);
            }
        }

        private IFormatter? FindFormatterForServiceWithoutEntity(IServiceStats stat)
        {
            if (stat.ServiceName.Contains("Phone") &&
                _formatters.TryGetValue(typeof(PhoneTrackingInfo), out var phoneFormatter))
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

        private static string CreateNoFormatterOutput(IServiceStats stat, Type entityType)
        {
            return $"=== {stat.ServiceName} ({stat.Status}) ==={Environment.NewLine}" +
                   $"[No formatter registered for {entityType.Name}]";
        }

        private static string CreateNoDataOutput(IServiceStats stat)
        {
            return $"=== {stat.ServiceName} ({stat.Status}) ==={Environment.NewLine}" +
                   "No current data available";
        }

        private static void AddFormattedOutputLines(List<string> lines, string formattedOutput)
        {
            foreach (var line in formattedOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                lines.Add(line);
            }
        }
    }
}