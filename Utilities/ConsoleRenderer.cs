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
    public static class ConsoleRenderer
    {
        private static readonly Dictionary<Type, object> _formatters = new Dictionary<Type, object>();
        private static DateTime _lastUpdate = DateTime.MinValue;
        private static readonly object _lock = new object();
        private static VerbosityLevel _currentVerbosity = VerbosityLevel.Normal;
        
        /// <summary>
        /// Current verbosity level
        /// </summary>
        public static VerbosityLevel CurrentVerbosity
        {
            get => _currentVerbosity;
            set => _currentVerbosity = value;
        }
        
        static ConsoleRenderer()
        {
            // Register formatters for known types
            RegisterFormatter(new PhoneTrackingInfoFormatter());
            RegisterFormatter(new PCTrackingInfoFormatter());
        }
        
        /// <summary>
        /// Registers a formatter for a specific entity type
        /// </summary>
        public static void RegisterFormatter<T>(IFormatter<T> formatter) where T : IFormattableObject
        {
            _formatters[typeof(T)] = formatter;
        }
        
        /// <summary>
        /// Updates the console display with service statistics
        /// </summary>
        public static void Update<T>(IEnumerable<IServiceStats<T>> stats, VerbosityLevel? verbosity = null) 
            where T : IFormattableObject
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastUpdate < TimeSpan.FromMilliseconds(100))
                    return;
                
                _lastUpdate = now;
                var actualVerbosity = verbosity ?? _currentVerbosity;
                
                var lines = new List<string>();
                lines.Add($"=== SharpBridge Status (Verbosity: {actualVerbosity}) ===");
                lines.Add($"Current Time: {DateTime.Now:HH:mm:ss}");
                lines.Add(string.Empty);
                
                foreach (var stat in stats.Where(s => s != null))
                {
                    lines.Add($"=== {stat.ServiceName} ({stat.Status}) ===");
                    
                    if (actualVerbosity >= VerbosityLevel.Normal && stat.Counters.Any())
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
                        if (_formatters.TryGetValue(entityType, out var formatter))
                        {
                            var formatterMethod = formatter.GetType().GetMethod("Format");
                            if (formatterMethod != null)
                            {
                                string formattedOutput = (string)formatterMethod.Invoke(
                                    formatter, 
                                    new object[] { stat.CurrentEntity, actualVerbosity });
                                
                                // Split formatted output into lines and add each one
                                foreach (var line in formattedOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                                {
                                    lines.Add(line);
                                }
                            }
                        }
                        else
                        {
                            lines.Add($"[No formatter registered for {entityType.Name}]");
                        }
                    }
                    
                    lines.Add(string.Empty);
                }
                
                lines.Add("Press Ctrl+C to exit");
                
                ConsoleDisplayAction(lines.ToArray());
            }
        }
        
        /// <summary>
        /// Cycles to the next verbosity level
        /// </summary>
        public static void CycleVerbosity()
        {
            _currentVerbosity = _currentVerbosity switch
            {
                VerbosityLevel.Basic => VerbosityLevel.Normal,
                VerbosityLevel.Normal => VerbosityLevel.Detailed,
                VerbosityLevel.Detailed => VerbosityLevel.Basic,
                _ => VerbosityLevel.Normal
            };
        }
        
        // Reusing PerformanceMonitor's console display technique
        private static void ConsoleDisplayAction(string[] outputLines)
        {
            try
            {
                Console.SetCursorPosition(0, 0);
                
                int currentLine = 0;
                int windowWidth = Console.WindowWidth - 1;
                
                // Write each line and clear the remainder of each line
                foreach (var line in outputLines)
                {
                    Console.SetCursorPosition(0, currentLine);
                    Console.Write(line);
                    
                    // Clear the rest of this line (in case previous content was longer)
                    int remainingSpace = windowWidth - line.Length;
                    if (remainingSpace > 0)
                    {
                        Console.Write(new string(' ', remainingSpace));
                    }
                    
                    currentLine++;
                    
                    // Ensure we don't exceed console boundaries
                    if (currentLine >= Console.WindowHeight - 1)
                        break;
                }
                
                // Clear any remaining lines that might have had content before
                int windowHeight = Console.WindowHeight;
                for (int i = currentLine; i < windowHeight - 1; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', windowWidth));
                }
                
                // Reset cursor position to the end of our content
                Console.SetCursorPosition(0, currentLine);
            }
            catch (Exception)
            {
                try
                {
                    Console.Clear();
                    foreach (var line in outputLines)
                    {
                        Console.WriteLine(line);
                    }
                }
                catch
                {
                    // Last resort fallback
                }
            }
        }
    }
} 