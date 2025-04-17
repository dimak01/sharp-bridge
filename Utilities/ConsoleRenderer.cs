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
        public static void Update<T>(IEnumerable<ServiceStats<T>> stats, VerbosityLevel? verbosity = null) 
            where T : IFormattableObject
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastUpdate < TimeSpan.FromMilliseconds(100))
                    return;
                
                _lastUpdate = now;
                var actualVerbosity = verbosity ?? _currentVerbosity;
                
                var sb = new StringBuilder();
                sb.AppendLine($"=== SharpBridge Status (Verbosity: {actualVerbosity}) ===");
                sb.AppendLine($"Current Time: {DateTime.Now:HH:mm:ss}");
                sb.AppendLine();
                
                foreach (var stat in stats.Where(s => s != null))
                {
                    sb.AppendLine($"=== {stat.ServiceName} ({stat.Status}) ===");
                    
                    if (actualVerbosity >= VerbosityLevel.Normal && stat.Counters.Any())
                    {
                        sb.AppendLine("Metrics:");
                        foreach (var counter in stat.Counters)
                        {
                            sb.AppendLine($"  {counter.Key}: {counter.Value}");
                        }
                        sb.AppendLine();
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
                                sb.AppendLine(formattedOutput);
                            }
                        }
                        else
                        {
                            sb.AppendLine($"[No formatter registered for {entityType.Name}]");
                        }
                    }
                    
                    sb.AppendLine();
                }
                
                sb.AppendLine("Press Ctrl+C to exit");
                
                ConsoleDisplayAction(sb.ToString());
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
        private static void ConsoleDisplayAction(string output)
        {
            try
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(output);
                
                int currentLine = Console.CursorTop;
                int currentCol = Console.CursorLeft;
                
                int windowHeight = Console.WindowHeight;
                for (int i = currentLine; i < windowHeight - 1; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }
                
                Console.SetCursorPosition(currentCol, currentLine);
            }
            catch (Exception)
            {
                try
                {
                    Console.Clear();
                    Console.Write(output);
                }
                catch
                {
                    // Last resort fallback
                }
            }
        }
    }
} 