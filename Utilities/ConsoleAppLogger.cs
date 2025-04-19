using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Basic console implementation of IAppLogger for initial integration
    /// </summary>
    /// <remarks>
    /// This is a temporary implementation that will be replaced with Serilog later
    /// </remarks>
    public class ConsoleAppLogger : IAppLogger
    {
        /// <summary>
        /// Logs a debug message to the console
        /// </summary>
        public void Debug(string message, params object[] args)
        {
            WriteLine("DEBUG", string.Format(message, args));
        }
        
        /// <summary>
        /// Logs an informational message to the console
        /// </summary>
        public void Info(string message, params object[] args)
        {
            WriteLine("INFO", string.Format(message, args));
        }
        
        /// <summary>
        /// Logs a warning message to the console
        /// </summary>
        public void Warning(string message, params object[] args)
        {
            WriteLine("WARN", string.Format(message, args));
        }
        
        /// <summary>
        /// Logs an error message to the console
        /// </summary>
        public void Error(string message, params object[] args)
        {
            WriteLine("ERROR", string.Format(message, args));
        }
        
        /// <summary>
        /// Logs an error message with an exception to the console
        /// </summary>
        public void ErrorWithException(string message, Exception ex, params object[] args)
        {
            string formattedMessage = string.Format(message, args);
            
            if (ex != null)
            {
                formattedMessage += $" Exception: {ex.Message}";
                // Optionally include stack trace for detailed logging
                // formattedMessage += $"\nStack Trace: {ex.StackTrace}";
            }
            
            WriteLine("ERROR", formattedMessage);
        }
        
        /// <summary>
        /// Writes a formatted log message to the console
        /// </summary>
        private void WriteLine(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"[{timestamp}] [{level}] {message}");
        }
    }
} 