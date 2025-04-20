using System;
using Serilog;
using Serilog.Events;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Serilog implementation of IAppLogger
    /// </summary>
    public class SerilogAppLogger : IAppLogger
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the SerilogAppLogger class
        /// </summary>
        /// <param name="logger">The Serilog logger instance to use</param>
        public SerilogAppLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        public void Debug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }
        
        /// <summary>
        /// Logs an informational message
        /// </summary>
        public void Info(string message, params object[] args)
        {
            _logger.Information(message, args);
        }
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        public void Warning(string message, params object[] args)
        {
            _logger.Warning(message, args);
        }
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        public void Error(string message, params object[] args)
        {
            _logger.Error(message, args);
        }
        
        /// <summary>
        /// Logs an error message with an exception
        /// </summary>
        public void ErrorWithException(string message, Exception ex, params object[] args)
        {
            _logger.Error(ex, message, args);
        }
    }
} 