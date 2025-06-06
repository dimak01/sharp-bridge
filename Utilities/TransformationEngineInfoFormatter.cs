using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Formatter for TransformationEngineInfo objects
    /// </summary>
    public class TransformationEngineInfoFormatter : IFormatter
    {
        private const int RULE_DISPLAY_COUNT_NORMAL = 15;
        
        private readonly IConsole _console;
        private readonly ITableFormatter _tableFormatter;
        
        /// <summary>
        /// Initializes a new instance of the TransformationEngineInfoFormatter
        /// </summary>
        /// <param name="console">Console abstraction for getting window dimensions</param>
        /// <param name="tableFormatter">Table formatter for generating tables</param>
        public TransformationEngineInfoFormatter(IConsole console, ITableFormatter tableFormatter)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
        }
        
        /// <summary>
        /// Current verbosity level for this formatter
        /// </summary>
        public VerbosityLevel CurrentVerbosity { get; private set; } = VerbosityLevel.Normal;
        
        /// <summary>
        /// Cycles to the next verbosity level
        /// </summary>
        public void CycleVerbosity()
        {
            CurrentVerbosity = CurrentVerbosity switch
            {
                VerbosityLevel.Basic => VerbosityLevel.Normal,
                VerbosityLevel.Normal => VerbosityLevel.Detailed,
                VerbosityLevel.Detailed => VerbosityLevel.Basic,
                _ => VerbosityLevel.Normal
            };
        }
        
        /// <summary>
        /// Formats a TransformationEngineInfo object with service statistics into a display string
        /// </summary>
        public string Format(IServiceStats serviceStats)
        {
            if (serviceStats == null) 
                return "No service data available";
            
            var builder = new StringBuilder();
            
            // Header with service status
            AppendServiceHeader(builder, serviceStats);
            
            // Transformation engine details
            if (serviceStats.CurrentEntity is TransformationEngineInfo engineInfo)
            {
                if (CurrentVerbosity >= VerbosityLevel.Normal)
                {
                    // Show invalid rules if any
                    if (engineInfo.InvalidRules.Count > 0)
                    {
                        AppendInvalidRules(builder, engineInfo.InvalidRules);
                    }
                    
                    // Show abandoned rules if any
                    if (engineInfo.AbandonedRules.Count > 0)
                    {
                        AppendAbandonedRules(builder, engineInfo.AbandonedRules);
                    }
                }
            }
            else if (serviceStats.CurrentEntity != null)
            {
                throw new ArgumentException("CurrentEntity must be of type TransformationEngineInfo or null");
            }
            else
            {
                builder.AppendLine();
                builder.AppendLine("No transformation engine data available");
            }
            
            return builder.ToString();
        }
        
        /// <summary>
        /// Appends the service header information to the string builder
        /// </summary>
        private void AppendServiceHeader(StringBuilder builder, IServiceStats serviceStats)
        {
            // Header with service status
            builder.AppendLine(FormatServiceHeader("Transformation Engine", serviceStats.Status, "Alt+T"));
            builder.AppendLine($"Verbosity: {CurrentVerbosity}");
            builder.AppendLine();
            
            // Health status
            builder.AppendLine(FormatHealthStatus(
                serviceStats.IsHealthy, 
                serviceStats.LastSuccessfulOperation, 
                serviceStats.LastError));
            
            // Core rule stats
            if (serviceStats.Counters.ContainsKey("Valid Rules") && serviceStats.Counters.ContainsKey("Invalid Rules"))
            {
                var validRules = serviceStats.Counters["Valid Rules"];
                var invalidRules = serviceStats.Counters["Invalid Rules"];
                var totalRules = validRules + invalidRules;
                
                var uptimeText = string.Empty;
                if (serviceStats.Counters.ContainsKey("Uptime Since Rules Loaded (seconds)"))
                {
                    var uptimeSeconds = serviceStats.Counters["Uptime Since Rules Loaded (seconds)"];
                    uptimeText = $", Uptime: {FormatUptime(uptimeSeconds)}";
                }
                
                builder.AppendLine($"Rules Loaded: Total: {totalRules}, Valid: {validRules}, Invalid: {invalidRules}{uptimeText}");
            }
            
            // Config file info
            if (serviceStats.CurrentEntity is TransformationEngineInfo engineInfo)
            {
                var configStatus = DetermineConfigStatus(serviceStats.Status, engineInfo.ConfigFilePath);
                builder.AppendLine($"Config File: Path: {engineInfo.ConfigFilePath}, Status: {configStatus}");
            }
            
            // Hot reload stats
            if (serviceStats.Counters.ContainsKey("Hot Reload Attempts") && serviceStats.Counters.ContainsKey("Hot Reload Successes"))
            {
                var attempts = serviceStats.Counters["Hot Reload Attempts"];
                var successes = serviceStats.Counters["Hot Reload Successes"];
                builder.AppendLine($"Hot Reloads: Attempts: {attempts}, Successful: {successes}");
            }
            
            // Transformation metrics
            if (serviceStats.Counters.ContainsKey("Total Transformations"))
            {
                var total = serviceStats.Counters["Total Transformations"];
                var successful = serviceStats.Counters.ContainsKey("Successful Transformations") ? serviceStats.Counters["Successful Transformations"] : 0;
                var failed = serviceStats.Counters.ContainsKey("Failed Transformations") ? serviceStats.Counters["Failed Transformations"] : 0;
                
                if (total > 0)
                {
                    builder.AppendLine($"Transformations: Total: {total}, Successful: {successful}, Failed: {failed}");
                }
            }
            
            builder.AppendLine();
        }
        
        /// <summary>
        /// Appends invalid rules table to the string builder
        /// </summary>
        private void AppendInvalidRules(StringBuilder builder, IReadOnlyList<RuleInfo> invalidRules)
        {
            var rulesToShow = invalidRules.ToList();
            
            // Define columns for invalid rules table
            var columns = new List<ITableColumn<RuleInfo>>
            {
                new TextColumn<RuleInfo>("Rule Name", rule => rule.Name, minWidth: 8, maxWidth: 20),
                new TextColumn<RuleInfo>("Function", rule => TruncateExpression(rule.Func, 80), minWidth: 12, maxWidth: 80),
                new TextColumn<RuleInfo>("Error", rule => TruncateError(rule.Error, 80), minWidth: 15, maxWidth: 80)
            };
            
            var singleColumnLimit = CurrentVerbosity == VerbosityLevel.Detailed ? (int?)null : RULE_DISPLAY_COUNT_NORMAL;
            _tableFormatter.AppendTable(builder, "Invalid Rules", rulesToShow, columns, 1, _console.WindowWidth, 20, singleColumnLimit);
        }
        
        /// <summary>
        /// Appends abandoned rules table to the string builder
        /// </summary>
        private void AppendAbandonedRules(StringBuilder builder, IReadOnlyList<RuleInfo> abandonedRules)
        {
            var rulesToShow = abandonedRules.ToList();
            
            // Define columns for abandoned rules table
            var columns = new List<ITableColumn<RuleInfo>>
            {
                new TextColumn<RuleInfo>("Rule Name", rule => rule.Name, minWidth: 8, maxWidth: 20),
                new TextColumn<RuleInfo>("Function", rule => TruncateExpression(rule.Func, 80), minWidth: 12, maxWidth: 80),
                new TextColumn<RuleInfo>("Reason", rule => TruncateError(rule.Error, 80), minWidth: 15, maxWidth: 80)
            };
            
            var singleColumnLimit = CurrentVerbosity == VerbosityLevel.Detailed ? (int?)null : RULE_DISPLAY_COUNT_NORMAL;
            _tableFormatter.AppendTable(builder, "Abandoned Rules", rulesToShow, columns, 1, _console.WindowWidth, 20, singleColumnLimit);
        }
        
        /// <summary>
        /// Determines the config file status based on engine status and file path
        /// </summary>
        private string DetermineConfigStatus(string engineStatus, string configFilePath)
        {
            return engineStatus switch
            {
                "Ready" => "Loaded",
                "Partial" => "Loaded",
                "ConfigErrorCached" => "Error (Using Cached)",
                "NoValidRules" => "Loaded (No Valid Rules)",
                "ConfigMissing" => "Not Found",
                "NeverLoaded" => "Never Loaded",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Truncates an expression string for display in tables
        /// </summary>
        private string TruncateExpression(string expression, int maxLength)
        {
            if (string.IsNullOrEmpty(expression))
                return "[empty]";
                
            if (expression.Length <= maxLength)
                return expression;
                
            return expression.Substring(0, maxLength - 3) + "...";
        }
        
        /// <summary>
        /// Truncates an error message for display in tables
        /// </summary>
        private string TruncateError(string error, int maxLength)
        {
            if (string.IsNullOrEmpty(error))
                return "[no error]";
                
            if (error.Length <= maxLength)
                return error;
                
            return error.Substring(0, maxLength - 3) + "...";
        }
        
        /// <summary>
        /// Formats uptime into a human-readable string
        /// </summary>
        private string FormatUptime(long uptimeSeconds)
        {
            return DisplayFormatting.FormatDuration(TimeSpan.FromSeconds(uptimeSeconds));
        }
        
        /// <summary>
        /// Formats a time span into a human-readable string
        /// </summary>
        private string FormatTimeAgo(TimeSpan timeSpan)
        {
            return DisplayFormatting.FormatDuration(timeSpan);
        }
        
        /// <summary>
        /// Formats a health status with color coding
        /// </summary>
        private string FormatHealthStatus(bool isHealthy, DateTime lastSuccess, string lastError = null)
        {
            var healthIcon = isHealthy ? "√" : "X";
            var healthColor = isHealthy ? ConsoleColors.Success : ConsoleColors.Error;
            var healthText = ConsoleColors.Colorize($"{healthIcon} {(isHealthy ? "Healthy" : "Unhealthy")}", healthColor);
            
            var lastSuccessText = lastSuccess == default ? "Never" : FormatTimeAgo(DateTime.UtcNow - lastSuccess);
            
            var errorText = string.Empty;
            if (!string.IsNullOrEmpty(lastError))
            {
                // Truncate long error messages
                var truncatedError = lastError.Length > 50 ? lastError.Substring(0, 47) + "..." : lastError;
                errorText = $" | Error: {truncatedError}";
            }
            
            return $"Health: {healthText} | Last Success: {lastSuccessText}{errorText}";
        }
        
        /// <summary>
        /// Formats a service header with status and color coding
        /// </summary>
        private string FormatServiceHeader(string serviceName, string status, string shortcut)
        {
            var statusColor = ConsoleColors.GetStatusColor(status);
            var colorizedStatus = ConsoleColors.Colorize(status, statusColor);
            return $"=== {serviceName} ({colorizedStatus}) === [{shortcut}]";
        }
    }
} 