using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpBridge.Models
{
    /// <summary>
    /// Tracks the progress of application initialization
    /// </summary>
    public class InitializationProgress
    {
        /// <summary>
        /// Gets or sets the current initialization step
        /// </summary>
        public InitializationStep CurrentStep { get; set; } = InitializationStep.ConsoleSetup;

        /// <summary>
        /// Gets or sets the status of the current step
        /// </summary>
        public StepStatus Status { get; set; } = StepStatus.Pending;

        /// <summary>
        /// Gets or sets when initialization started
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the elapsed time since initialization started
        /// </summary>
        public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;

        /// <summary>
        /// Gets or sets the details for each initialization step
        /// </summary>
        public Dictionary<InitializationStep, StepInfo> Steps { get; set; } = new Dictionary<InitializationStep, StepInfo>();

        /// <summary>
        /// Gets or sets whether initialization is complete
        /// </summary>
        public bool IsComplete { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of InitializationProgress
        /// </summary>
        public InitializationProgress()
        {
            // Initialize all steps as pending
            foreach (InitializationStep step in Enum.GetValues<InitializationStep>())
            {
                Steps[step] = new StepInfo
                {
                    Status = StepStatus.Pending
                };
            }
        }

        /// <summary>
        /// Updates the status of a specific step
        /// </summary>
        /// <param name="step">The step to update</param>
        /// <param name="status">The new status</param>
        /// <param name="errorMessage">Optional error message if status is Failed</param>
        public void UpdateStep(InitializationStep step, StepStatus status, string? errorMessage = null)
        {
            if (Steps.TryGetValue(step, out var stepInfo))
            {
                stepInfo.Status = status;

                if (status == StepStatus.InProgress)
                {
                    stepInfo.StartTime = DateTime.UtcNow;
                }
                else if (status == StepStatus.Completed || status == StepStatus.Failed)
                {
                    stepInfo.EndTime = DateTime.UtcNow;
                }

                if (status == StepStatus.Failed && !string.IsNullOrEmpty(errorMessage))
                {
                    stepInfo.ErrorMessage = errorMessage;
                }
            }

            // Update current step and status
            if (status == StepStatus.InProgress)
            {
                CurrentStep = step;
                Status = status;
            }
        }

        /// <summary>
        /// Gets the next pending step
        /// </summary>
        /// <returns>The next step that needs to be executed, or null if all steps are complete</returns>
        public InitializationStep? GetNextPendingStep()
        {
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

            return stepOrder.FirstOrDefault(step =>
                Steps.TryGetValue(step, out var stepInfo) &&
                stepInfo.Status == StepStatus.Pending);
        }

        /// <summary>
        /// Marks initialization as complete
        /// </summary>
        public void MarkComplete()
        {
            IsComplete = true;
            Status = StepStatus.Completed;
        }
    }
}
