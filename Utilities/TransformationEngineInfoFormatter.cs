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
        // Counter Keys
        private const string VALID_RULES_KEY = "Valid Rules";
        private const string INVALID_RULES_KEY = "Invalid Rules";
        private const string UPTIME_SINCE_RULES_LOADED_KEY = "Uptime Since Rules Loaded (seconds)";
        private const string HOT_RELOAD_ATTEMPTS_KEY = "Hot Reload Attempts";
        private const string HOT_RELOAD_SUCCESSES_KEY = "Hot Reload Successes";

        
        // Display Limits
        private const int RULE_DISPLAY_COUNT_NORMAL = 15;
        
        // Column Width Constants
        private const int RULE_NAME_COLUMN_MIN_WIDTH = 8;
        private const int RULE_NAME_COLUMN_MAX_WIDTH = 20;
        private const int FUNCTION_COLUMN_MIN_WIDTH = 12;
        private const int FUNCTION_COLUMN_MAX_WIDTH = 80;
        private const int ERROR_COLUMN_MIN_WIDTH = 15;
        private const int ERROR_COLUMN_MAX_WIDTH = 80;
        
        // Text Truncation Constants
        private const int DEFAULT_TEXT_TRUNCATION_LENGTH = 80;
        private const int ELLIPSIS_LENGTH = 3;
        
        // Table Formatting Constants
        private const int TABLE_MINIMUM_ROWS = 1;
        private const int TABLE_MINIMUM_WIDTH = 20;
        
        // Service Display Constants
        private const string SERVICE_NAME = "Transformation Engine";
        private const string KEYBOARD_SHORTCUT = "Alt+T";
        
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
        public string Format(IServiceStats stats)
        {
            if (stats == null) 
                return "No service data available";
            
            var builder = new StringBuilder();
            
            // Header with service status
            AppendServiceHeader(builder, stats);
            
            // Transformation engine details
            if (stats.CurrentEntity is TransformationEngineInfo engineInfo)
            {
                if (CurrentVerbosity >= VerbosityLevel.Normal && engineInfo.InvalidRules.Any())
                {
                    AppendFailedRules(builder, engineInfo.InvalidRules);
                }
            }
            else if (stats.CurrentEntity != null)
            {
                throw new ArgumentException(
                    "CurrentEntity must be of type TransformationEngineInfo or null");
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
            builder.AppendLine(FormatServiceHeader(SERVICE_NAME, serviceStats.Status, KEYBOARD_SHORTCUT));

            AppendRulesOverview(builder, serviceStats);
            AppendConfigurationInfo(builder, serviceStats);
        }
        
        /// <summary>
        /// Appends the rules overview section to the string builder
        /// </summary>
        private void AppendRulesOverview(StringBuilder builder, IServiceStats serviceStats)
        {
            var validRules = GetCounterValue(serviceStats.Counters, VALID_RULES_KEY);
            var invalidRules = GetCounterValue(serviceStats.Counters, INVALID_RULES_KEY);
            
            if (validRules > 0 || invalidRules > 0)
            {
                var totalRules = validRules + invalidRules;
                var uptimeText = GetUptimeText(serviceStats.Counters);
                
                builder.AppendLine(
                    $"Rules Loaded - Total: {totalRules}, Valid: {validRules}, " +
                    $"Invalid: {invalidRules}{uptimeText}");
            }
        }
        
        /// <summary>
        /// Appends the configuration information section to the string builder
        /// </summary>
        private static void AppendConfigurationInfo(StringBuilder builder, IServiceStats serviceStats)
        {
            if (serviceStats.CurrentEntity is TransformationEngineInfo engineInfo)
            {
                var colorizedStatus = DetermineConfigStatus(serviceStats.Status);
                var colorized_config_path = ConsoleColors.Colorize(engineInfo.ConfigFilePath, ConsoleColors.ConfigPathColor);
                builder.AppendLine($"Config File Path: {colorized_config_path}");
                builder.AppendLine($"Up to Date: {colorizedStatus} | Load Attempts: {serviceStats.Counters[HOT_RELOAD_ATTEMPTS_KEY]}, Successful: {serviceStats.Counters[HOT_RELOAD_SUCCESSES_KEY]}");
            }
        }
        
        /// <summary>
        /// Appends failed rules table to the string builder (includes validation failures and evaluation failures)
        /// </summary>
        private void AppendFailedRules(StringBuilder builder, IReadOnlyList<RuleInfo> failedRules)
        {
            var rulesToShow = failedRules.ToList();

            if (!rulesToShow.Any()) return;

            // Define columns for failed rules table
            var columns = new List<ITableColumn<RuleInfo>>
            {
                new TextColumn<RuleInfo>("Rule Name", rule => ConsoleColors.ColorizeRuleErrorName(rule.Name), 
                    minWidth: RULE_NAME_COLUMN_MIN_WIDTH, maxWidth: RULE_NAME_COLUMN_MAX_WIDTH),
                new TextColumn<RuleInfo>("Function", 
                    rule => TruncateText(rule.Func, DEFAULT_TEXT_TRUNCATION_LENGTH, "[empty]"), 
                    minWidth: FUNCTION_COLUMN_MIN_WIDTH, maxWidth: FUNCTION_COLUMN_MAX_WIDTH),
                new TextColumn<RuleInfo>("Error", 
                    rule => TruncateText(rule.Error, DEFAULT_TEXT_TRUNCATION_LENGTH, "[no error]"), 
                    minWidth: ERROR_COLUMN_MIN_WIDTH, maxWidth: ERROR_COLUMN_MAX_WIDTH)
            };
            
            var singleColumnLimit = CurrentVerbosity == VerbosityLevel.Detailed ? 
                (int?)null : RULE_DISPLAY_COUNT_NORMAL;

            builder.AppendLine();
            _tableFormatter.AppendTable(builder, "=== Failed Rules ===", rulesToShow, columns, 
                                        TABLE_MINIMUM_ROWS, _console.WindowWidth, TABLE_MINIMUM_WIDTH, singleColumnLimit);
        }
        
        /// <summary>
        /// Safely gets a counter value with a default fallback
        /// </summary>
        private long GetCounterValue(IReadOnlyDictionary<string, long> counters, string key, long defaultValue = 0)
        {
            return counters.TryGetValue(key, out var value) ? value : defaultValue;
        }
        
        /// <summary>
        /// Gets the uptime text from counters if available
        /// </summary>
        private string GetUptimeText(IReadOnlyDictionary<string, long> counters)
        {
            var uptimeSeconds = GetCounterValue(counters, UPTIME_SINCE_RULES_LOADED_KEY);
            return uptimeSeconds > 0 ? $", Uptime: {FormatUptime(uptimeSeconds)}" : string.Empty;
        }
        
        /// <summary>
        /// Determines the config file status based on service status and file path
        /// </summary>
        private static string DetermineConfigStatus(string serviceStatus)
        {
            var status = serviceStatus switch
            {
                "AllRulesValid" => "Yes",
                "RulesPartiallyValid" => "Yes",
                "ConfigErrorCached" => "No",
                "NoValidRules" => "Yes",
                "ConfigMissing" => "No",
                "NeverLoaded" => "No",
                _ => "Unknown"
            };
            return status == "Yes" ? ConsoleColors.Colorize(status, ConsoleColors.Success) : ConsoleColors.Colorize(status, ConsoleColors.Warning);
        }
        
        /// <summary>
        /// Truncates text for display in tables with customizable empty placeholder
        /// </summary>
        private string TruncateText(string text, int maxLength, string emptyPlaceholder = "[empty]")
        {
            if (string.IsNullOrEmpty(text))
                return emptyPlaceholder;
                
            if (text.Length <= maxLength)
                return text;
                
            // Guard against maxLength being too small for ellipsis
            if (maxLength <= ELLIPSIS_LENGTH)
                return text.Substring(0, Math.Max(0, maxLength));
                
            return text.Substring(0, maxLength - ELLIPSIS_LENGTH) + "...";
        }
        
        /// <summary>
        /// Formats uptime into a human-readable string
        /// </summary>
        private string FormatUptime(long uptimeSeconds)
        {
            return DisplayFormatting.FormatDuration(TimeSpan.FromSeconds(uptimeSeconds));
        }
        
        /// <summary>
        /// Formats a service header with status and color coding
        /// </summary>
        private string FormatServiceHeader(string serviceName, string status, string shortcut)
        {
            var statusColor = GetStatusColor(status);
            var colorizedStatus = ConsoleColors.Colorize(status, statusColor);
            var verbosity = CurrentVerbosity switch
            {
                VerbosityLevel.Basic => "[BASIC]",
                VerbosityLevel.Normal => "[INFO]",
                VerbosityLevel.Detailed => "[DEBUG]",
                _ => "[INFO]"
            };
            return $"=== {verbosity} {serviceName} ({colorizedStatus}) === [{shortcut}]";
        }
        
        private static string GetStatusColor(string status)
        {
            return status switch
            {
                "AllRulesValid" => ConsoleColors.Success,
                "RulesPartiallyValid" or "NoValidRules" or "NeverLoaded" => ConsoleColors.Warning,
                _ => ConsoleColors.Error
            };
        }
    }
} 