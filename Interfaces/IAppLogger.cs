using System;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Application-specific logging interface
    /// </summary>
    public interface IAppLogger
    {
        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">The message template</param>
        /// <param name="args">Optional arguments for the message template</param>
        void Debug(string message, params object[] args);
        
        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message template</param>
        /// <param name="args">Optional arguments for the message template</param>
        void Info(string message, params object[] args);
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The message template</param>
        /// <param name="args">Optional arguments for the message template</param>
        void Warning(string message, params object[] args);
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The message template</param>
        /// <param name="args">Optional arguments for the message template</param>
        void Error(string message, params object[] args);
        
        /// <summary>
        /// Logs an error message with an exception
        /// </summary>
        /// <param name="message">The message template</param>
        /// <param name="ex">The exception to include</param>
        /// <param name="args">Optional arguments for the message template</param>
        void ErrorWithException(string message, Exception ex, params object[] args);
    }
} 