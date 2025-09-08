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
    /// Content provider for displaying initialization progress
    /// </summary>
    public class InitializationContentProvider : IConsoleModeContentProvider
    {
        private readonly IAppLogger _logger;
        private readonly IExternalEditorService _externalEditorService;
        private InitializationProgress _progress;

        /// <summary>
        /// Initializes a new instance of the InitializationContentProvider
        /// </summary>
        /// <param name="logger">The logger to use for error reporting</param>
        /// <param name="externalEditorService">The external editor service for opening configuration files</param>
        public InitializationContentProvider(IAppLogger logger, IExternalEditorService externalEditorService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _externalEditorService = externalEditorService ?? throw new ArgumentNullException(nameof(externalEditorService));
            _progress = new InitializationProgress();
        }

        /// <summary>
        /// Gets the console mode for this content provider
        /// </summary>
        public ConsoleMode Mode => ConsoleMode.Initialization;

        /// <summary>
        /// Gets the display name for this content provider
        /// </summary>
        public string DisplayName => "Initialization";

        /// <summary>
        /// Gets the toggle action for this content provider
        /// </summary>
        public ShortcutAction ToggleAction => ShortcutAction.ShowSystemHelp; // Not used for initialization mode

        /// <summary>
        /// Gets the preferred update interval for this content provider
        /// </summary>
        public TimeSpan PreferredUpdateInterval => TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Sets the initialization progress to track
        /// </summary>
        /// <param name="progress">The progress to track</param>
        public virtual void SetProgress(InitializationProgress progress)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        /// <summary>
        /// Enters the initialization mode
        /// </summary>
        /// <param name="console">The console to operate on</param>
        public void Enter(IConsole console)
        {
            // No-op for initialization mode
        }

        /// <summary>
        /// Exits the initialization mode
        /// </summary>
        /// <param name="console">The console to operate on</param>
        public void Exit(IConsole console)
        {
            // No-op for initialization mode
        }

        /// <summary>
        /// Gets the content for the initialization display
        /// </summary>
        /// <param name="context">The context containing the service statistics</param>
        /// <returns>An array of strings representing the initialization display</returns>
        public string[] GetContent(ConsoleRenderContext context)
        {
            try
            {
                return RenderInitializationDisplay(context);
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error rendering initialization display", ex);
                return new[] { "Error displaying initialization progress" };
            }
        }

        /// <summary>
        /// Attempts to open the application configuration in an external editor
        /// </summary>
        /// <returns>True if successfully opened, false otherwise</returns>
        public async Task<bool> TryOpenInExternalEditorAsync()
        {
            try
            {
                return await _externalEditorService.TryOpenApplicationConfigAsync();
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error opening application config in external editor", ex);
                return false;
            }
        }

        /// <summary>
        /// Renders the initialization progress display
        /// </summary>
        /// <param name="context">The rendering context</param>
        /// <returns>Formatted initialization display lines</returns>
        private string[] RenderInitializationDisplay(ConsoleRenderContext context)
        {
            var lines = new List<string>();

            // Header
            lines.Add("Initializing Sharp Bridge...");
            lines.Add($"Elapsed: {FormatElapsedTime(_progress.ElapsedTime)}");
            lines.Add("");

            // Progress steps
            var stepOrder = new[]
            {
                InitializationStep.ConsoleSetup,
                InitializationStep.TransformationEngine,
                InitializationStep.FileWatchers,
                InitializationStep.PCClient,
                InitializationStep.PhoneClient,
                InitializationStep.ParameterSync,
                InitializationStep.FinalSetup
            };

            foreach (var step in stepOrder)
            {
                if (_progress.Steps.TryGetValue(step, out var stepInfo))
                {
                    var stepLine = RenderStepLine(step, stepInfo, context);
                    lines.Add(stepLine);

                    // Add sub-status for PC and Phone clients if they're in progress
                    if ((step == InitializationStep.PCClient || step == InitializationStep.PhoneClient) &&
                        stepInfo.Status == StepStatus.InProgress)
                    {
                        var subStatus = GetSubStatus(step, context);
                        if (!string.IsNullOrEmpty(subStatus))
                        {
                            lines.Add($"       └─ {subStatus}");
                        }
                    }
                }
            }

            lines.Add("");
            lines.Add("Press Ctrl+C to cancel");

            return lines.ToArray();
        }

        /// <summary>
        /// Renders a single step line with status indicator
        /// </summary>
        /// <param name="step">The initialization step</param>
        /// <param name="stepInfo">The step information</param>
        /// <param name="context">The rendering context</param>
        /// <returns>Formatted step line</returns>
        private static string RenderStepLine(InitializationStep step, StepInfo stepInfo, ConsoleRenderContext context)
        {
            var stepName = AttributeHelper.GetDescription(step);
            var statusIndicator = GetStatusIndicator(stepInfo.Status);
            var duration = stepInfo.Duration?.TotalSeconds.ToString("F1") ?? "pending";
            var durationText = stepInfo.Status == StepStatus.Pending ? "pending" : $"{duration}s";

            return $"{statusIndicator} {stepName,-30} ({durationText})";
        }

        /// <summary>
        /// Gets the status indicator for a step status
        /// </summary>
        /// <param name="status">The step status</param>
        /// <returns>ASCII status indicator</returns>
        private static string GetStatusIndicator(StepStatus status)
        {
            return status switch
            {
                StepStatus.Completed => "[OK]",
                StepStatus.InProgress => "[RUN]",
                StepStatus.Pending => "[PEND]",
                StepStatus.Failed => "[FAIL]",
                _ => "[UNK]"
            };
        }

        /// <summary>
        /// Gets the sub-status for PC or Phone client steps
        /// </summary>
        /// <param name="step">The initialization step</param>
        /// <param name="context">The rendering context</param>
        /// <returns>Sub-status description or empty string</returns>
        private string GetSubStatus(InitializationStep step, ConsoleRenderContext context)
        {
            if (context?.ServiceStats == null)
                return string.Empty;

            var stats = context.ServiceStats.ToList();

            if (step == InitializationStep.PCClient)
            {
                var pcStats = stats.FirstOrDefault(s => s.ServiceName.Contains("PC"));
                if (pcStats != null)
                {
                    return GetStatusDescription(pcStats);
                }
            }
            else if (step == InitializationStep.PhoneClient)
            {
                var phoneStats = stats.FirstOrDefault(s => s.ServiceName.Contains("Phone"));
                if (phoneStats != null)
                {
                    return GetStatusDescription(phoneStats);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets a user-friendly status description from service stats
        /// </summary>
        /// <param name="stats">The service statistics</param>
        /// <returns>User-friendly status description</returns>
        private string GetStatusDescription(IServiceStats stats)
        {
            try
            {
                if (stats.CurrentEntity is PCTrackingInfo)
                {
                    if (Enum.TryParse<PCClientStatus>(stats.Status, out var pcStatus))
                    {
                        return AttributeHelper.GetDescription(pcStatus);
                    }
                }
                else if (stats.CurrentEntity is PhoneTrackingInfo)
                {
                    if (Enum.TryParse<PhoneClientStatus>(stats.Status, out var phoneStatus))
                    {
                        return AttributeHelper.GetDescription(phoneStatus);
                    }
                }

                return stats.Status; // Fallback to raw status
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException($"Error getting status description for {stats.ServiceName}", ex);
                return stats.Status;
            }
        }

        /// <summary>
        /// Formats elapsed time as MM:SS.f
        /// </summary>
        /// <param name="elapsed">The elapsed time</param>
        /// <returns>Formatted time string</returns>
        private static string FormatElapsedTime(TimeSpan elapsed)
        {
            return $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds / 100:D1}";
        }
    }
}
