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
                return RenderInitializationDisplay();
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
        /// <returns>Formatted initialization display lines</returns>
        protected virtual string[] RenderInitializationDisplay()
        {
            var lines = new List<string>();

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
                    var stepLine = RenderStepLine(step, stepInfo);
                    lines.Add(stepLine);
                }
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Renders a single step line with status indicator
        /// </summary>
        /// <param name="step">The initialization step</param>
        /// <param name="stepInfo">The step information</param>
        /// <returns>Formatted step line</returns>
        private static string RenderStepLine(InitializationStep step, StepInfo stepInfo)
        {
            var stepName = AttributeHelper.GetDescription(step);

            var statusIndicator = GetStatusIndicator(stepInfo.Status);

            // Fix the duration text logic
            string durationText;
            if (stepInfo.Duration.HasValue)
            {
                durationText = $"{stepInfo.Duration.Value.TotalSeconds:F1}s";
            }
            else
            {
                durationText = AttributeHelper.GetDescription(stepInfo.Status);
            }

            // Concatenate status and step name first, then pad as a single unit
            var statusAndStep = $"{statusIndicator} {stepName}";
            return $"{statusAndStep,-45} ({durationText})";
        }

        /// <summary>
        /// Gets the status indicator for a step status with color coding
        /// </summary>
        /// <param name="status">The step status</param>
        /// <returns>Colored ASCII status indicator</returns>
        private static string GetStatusIndicator(StepStatus status)
        {
            return status switch
            {
                StepStatus.Completed => $"[{ConsoleColors.Colorize("OK", ConsoleColors.Success)}]",
                StepStatus.InProgress => $"[{ConsoleColors.Colorize("RUN", ConsoleColors.Warning)}]",
                StepStatus.Pending => $"[{ConsoleColors.Colorize("PEND", ConsoleColors.Disabled)}]",
                StepStatus.Failed => $"[{ConsoleColors.Colorize("FAIL", ConsoleColors.Error)}]",
                _ => $"[{ConsoleColors.Colorize("UNK", ConsoleColors.Disabled)}]"
            };
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
