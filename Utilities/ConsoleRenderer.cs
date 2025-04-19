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
        private readonly Dictionary<Type, object> _formatters = new Dictionary<Type, object>();
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly object _lock = new object();
        private readonly IConsole _console;
        private readonly IAppLogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the ConsoleRenderer class
        /// </summary>
        /// <param name="console">The console implementation to use for output</param>
        /// <param name="logger">The logger to use for error reporting</param>
        public ConsoleRenderer(IConsole console, IAppLogger logger)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Register formatters for known types
            RegisterFormatter(new PhoneTrackingInfoFormatter());
            RegisterFormatter(new PCTrackingInfoFormatter());
        }
        
        /// <summary>
        /// Registers a formatter for a specific entity type
        /// </summary>
        public void RegisterFormatter<T>(IFormatter<T> formatter) where T : IFormattableObject
        {
            _formatters[typeof(T)] = formatter;
        }
        
        /// <summary>
        /// Gets a formatter for the specified type
        /// </summary>
        public IFormatter<T> GetFormatter<T>() where T : IFormattableObject
        {
            if (_formatters.TryGetValue(typeof(T), out var formatter))
            {
                return (IFormatter<T>)formatter;
            }
            return null;
        }
        
        /// <summary>
        /// Updates the console display with service statistics
        /// </summary>
        public void Update<T>(IEnumerable<IServiceStats<T>> stats) 
            where T : IFormattableObject
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastUpdate < TimeSpan.FromMilliseconds(100))
                    return;
                
                _lastUpdate = now;
                
                var lines = new List<string>();
                lines.Add($"=== SharpBridge Status ===");
                lines.Add($"Current Time: {DateTime.Now:HH:mm:ss}");
                lines.Add(string.Empty);
                
                foreach (var stat in stats.Where(s => s != null))
                {
                    lines.Add($"=== {stat.ServiceName} ({stat.Status}) ===");
                    
                    // Show counters for normal and detailed verbosity
                    if (stat.Counters.Any())
                    {
                        lines.Add("Metrics:");
                        foreach (var counter in stat.Counters)
                        {
                            lines.Add($"  {counter.Key}: {counter.Value}");
                        }
                        lines.Add(string.Empty);
                    }
                    
                    if (stat.CurrentEntity != null)
                    {
                        var entityType = stat.CurrentEntity.GetType();
                        if (_formatters.TryGetValue(entityType, out var formatterObj))
                        {
                            // We know it must be IFormatter<T> based on how we put it in the dictionary
                            var typedFormatter = (IFormatter<T>)formatterObj;
                            var formattedOutput = typedFormatter.Format(stat.CurrentEntity);
                            
                            // Split formatted output into lines and add each one
                            foreach (var line in formattedOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                            {
                                lines.Add(line);
                            }
                        }
                        else
                        {
                            lines.Add($"[No formatter registered for {entityType.Name}]");
                        }
                    }
                    
                    lines.Add(string.Empty);
                }
                
                lines.Add("Press Ctrl+C to exit | Alt+P for PC client verbosity | Alt+O for Phone client verbosity");
                
                ConsoleDisplayAction(lines.ToArray());
            }
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