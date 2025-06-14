using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Centralized console rendering utility
    /// </summary>
    public class ConsoleRenderer: IConsoleRenderer
    {
        private readonly Dictionary<Type, IFormatter> _formatters = new Dictionary<Type, IFormatter>();
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly object _lock = new object();
        private readonly IConsole _console;
        private readonly IAppLogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the ConsoleRenderer class
        /// </summary>
        /// <param name="console">The console implementation to use for output</param>
        /// <param name="logger">The logger to use for error reporting</param>
        /// <param name="transformationFormatter">The transformation engine info formatter</param>
        /// <param name="phoneFormatter">The phone tracking info formatter</param>
        /// <param name="pcFormatter">The PC tracking info formatter</param>
        public ConsoleRenderer(IConsole console, IAppLogger logger, TransformationEngineInfoFormatter transformationFormatter, PhoneTrackingInfoFormatter phoneFormatter, PCTrackingInfoFormatter pcFormatter)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Register formatters for known types - order determines display order
            RegisterFormatter<TransformationEngineInfo>(transformationFormatter ?? throw new ArgumentNullException(nameof(transformationFormatter)));
            RegisterFormatter<PhoneTrackingInfo>(phoneFormatter ?? throw new ArgumentNullException(nameof(phoneFormatter)));
            RegisterFormatter<PCTrackingInfo>(pcFormatter ?? throw new ArgumentNullException(nameof(pcFormatter)));
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
        
        /// <summary>
        /// Updates the console display with service statistics
        /// </summary>
        public void Update(IEnumerable<IServiceStats> stats)
        {
            lock (_lock)
            {
                if (!ShouldUpdate())
                    return;
                
                _lastUpdate = DateTime.UtcNow;
                
                var lines = BuildDisplayLines(stats);
                ConsoleDisplayAction(lines.ToArray());
            }
        }

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
        private List<string> BuildDisplayLines(IEnumerable<IServiceStats> stats)
        {
            var lines = new List<string>();
            
            AddHeaderLines(lines);
            AddServiceLines(lines, stats);
            AddFooterLines(lines);
            
            return lines;
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
                return GetFormattedOutputFromFormatter(formatter, stat);
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
                return GetFormattedOutputFromFormatter(formatter, stat);
            }
            else
            {
                return CreateNoDataOutput(stat);
            }
        }

        /// <summary>
        /// Gets formatted output from a specific formatter
        /// </summary>
        private string GetFormattedOutputFromFormatter(IFormatter formatter, IServiceStats stat)
        {
            return formatter switch
            {
                PhoneTrackingInfoFormatter phoneFormatter => phoneFormatter.Format(stat),
                PCTrackingInfoFormatter pcFormatter => pcFormatter.Format(stat),
                TransformationEngineInfoFormatter transformationFormatter => transformationFormatter.Format(stat),
                _ => CreateBasicFormatterOutput(formatter, stat)
            };
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
        /// Creates output for basic formatters that don't support service stats
        /// </summary>
        private static string CreateBasicFormatterOutput(IFormatter formatter, IServiceStats stat)
        {
            var verbosity = formatter.CurrentVerbosity switch
            {
                VerbosityLevel.Basic => "[BASIC]",
                VerbosityLevel.Normal => "[INFO]",
                VerbosityLevel.Detailed => "[DEBUG]",
                _ => "[INFO]"
            };
            var header = $"=== {stat.ServiceName} {verbosity} ({stat.Status}) ==={Environment.NewLine}";
            var content = formatter.Format(stat);
            return header + content;
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
        private static void AddFooterLines(List<string> lines)
        {
            lines.Add("Press Ctrl+C to exit | Alt+T for Transformation Engine verbosity | Alt+P for PC client verbosity | Alt+O for Phone client verbosity");
        }

        // Reusing PerformanceMonitor's console display technique
        private void ConsoleDisplayAction(string[] outputLines)
        {
            try
            {
                _console.SetCursorPosition(0, 0);
                
                int currentLine = 0;
                int windowWidth = _console.WindowWidth - 1;
                
                // Write each line and clear the remainder of each line
                foreach (var line in outputLines)
                {
                    _console.SetCursorPosition(0, currentLine);
                    _console.Write(line);
                    
                    // Clear the rest of this line (in case previous content was longer)
                    int remainingSpace = windowWidth - line.Length;
                    if (remainingSpace > 0)
                    {
                        _console.Write(new string(' ', remainingSpace));
                    }
                    
                    currentLine++;
                    
                    // Ensure we don't exceed console boundaries
                    if (currentLine >= _console.WindowHeight - 1)
                        break;
                }
                
                // Clear any remaining lines that might have had content before
                int windowHeight = _console.WindowHeight;
                for (int i = currentLine; i < windowHeight - 1; i++)
                {
                    _console.SetCursorPosition(0, i);
                    _console.Write(new string(' ', windowWidth));
                }
                
                // Reset cursor position to the end of our content
                _console.SetCursorPosition(0, currentLine);
            }
            catch (Exception ex)
            {
                // Log the error and rethrow - let the application crash rather than attempting to recover
                _logger.ErrorWithException("Console rendering failed", ex);
                throw; // Rethrow the original exception
            }
        }

        /// <summary>
        /// Clears the console
        /// </summary>
        public void ClearConsole()
        {
            _console.Clear();
        }
    }
} 