using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities;

namespace SharpBridge.Services
{
    /// <summary>
    /// Service responsible for handling application initialization
    /// </summary>
    public class ApplicationInitializationService : IApplicationInitializationService
    {
        // Core services
        private readonly IVTubeStudioPCClient _vtubeStudioPCClient;
        private readonly IVTubeStudioPhoneClient _vtubeStudioPhoneClient;
        private readonly ITransformationEngine _transformationEngine;
        private readonly IVTubeStudioPCParameterManager _parameterManager;
        private readonly IConfigManager _configManager;
        private readonly IFileChangeWatcher _appConfigWatcher;
        private readonly IConsoleModeManager _modeManager;
        private readonly IAppLogger _logger;

        // Configuration objects

        // Progress tracking (will be moved from ApplicationOrchestrator)
        private readonly InitializationProgress _initializationProgress;
        private readonly InitializationContentProvider _initializationContentProvider;

        /// <summary>
        /// Initializes a new instance of the ApplicationInitializationService
        /// </summary>
        /// <param name="vtubeStudioPCClient">The VTube Studio PC client</param>
        /// <param name="vtubeStudioPhoneClient">The VTube Studio phone client</param>
        /// <param name="transformationEngine">The transformation engine</param>
        /// <param name="parameterManager">VTube Studio PC parameter manager</param>
        /// <param name="configManager">Configuration manager for saving user preferences</param>
        /// <param name="appConfigWatcher">File change watcher for application configuration</param>
        /// <param name="modeManager">Console mode manager for UI mode switching</param>
        /// <param name="logger">Application logger</param>
        /// <param name="initializationContentProvider">Content provider for initialization progress display</param>
        public ApplicationInitializationService(
            IVTubeStudioPCClient vtubeStudioPCClient,
            IVTubeStudioPhoneClient vtubeStudioPhoneClient,
            ITransformationEngine transformationEngine,
            IVTubeStudioPCParameterManager parameterManager,
            IConfigManager configManager,
            IFileChangeWatcher appConfigWatcher,
            IConsoleModeManager modeManager,
            IAppLogger logger,
            InitializationContentProvider initializationContentProvider)
        {
            _vtubeStudioPCClient = vtubeStudioPCClient ?? throw new ArgumentNullException(nameof(vtubeStudioPCClient));
            _vtubeStudioPhoneClient = vtubeStudioPhoneClient ?? throw new ArgumentNullException(nameof(vtubeStudioPhoneClient));
            _transformationEngine = transformationEngine ?? throw new ArgumentNullException(nameof(transformationEngine));
            _parameterManager = parameterManager ?? throw new ArgumentNullException(nameof(parameterManager));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _appConfigWatcher = appConfigWatcher ?? throw new ArgumentNullException(nameof(appConfigWatcher));
            _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _initializationContentProvider = initializationContentProvider ?? throw new ArgumentNullException(nameof(initializationContentProvider));

            // Initialize progress tracking
            _initializationProgress = new InitializationProgress();
        }

        /// <summary>
        /// Initializes the application by setting up all required components
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="preActions">List of actions to execute after console setup (defaults to empty list)</param>
        /// <param name="postActions">List of actions to execute during final setup phase (defaults to empty list)</param>
        /// <returns>A task that completes when initialization is done</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken,
                                          List<Action>? preActions = null,
                                          List<Action>? postActions = null)
        {
            // Handle null parameters with empty list defaults
            preActions ??= new List<Action>();
            postActions ??= new List<Action>();

            // Switch to initialization mode and set up progress tracking
            _modeManager.SetMode(ConsoleMode.Initialization);
            _initializationContentProvider.SetProgress(_initializationProgress);

            try
            {
                //SetupConsoleWindow(); //TODO: Move to pre-actions (place it before subbing to window size changes)                 

                // Execute pre-actions (e.g., console size change tracking)
                foreach (var action in preActions)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning("Pre-action failed during initialization: {0}", ex.Message);
                    }
                }

                // Step 2: Transformation Engine
                _initializationProgress.UpdateStep(InitializationStep.TransformationEngine, StepStatus.InProgress);
                RenderInitializationProgress();
                await InitializeTransformationEngine();
                _initializationProgress.UpdateStep(InitializationStep.TransformationEngine, StepStatus.Completed);
                RenderInitializationProgress();

                // Step 3: File Watchers
                _initializationProgress.UpdateStep(InitializationStep.FileWatchers, StepStatus.InProgress);
                RenderInitializationProgress();
                _appConfigWatcher.StartWatching(_configManager.ApplicationConfigPath);
                _logger.Info("Started watching application config file for hot reload: {0}", _configManager.ApplicationConfigPath);
                _initializationProgress.UpdateStep(InitializationStep.FileWatchers, StepStatus.Completed);
                RenderInitializationProgress();

                // Step 4: PC Client
                _initializationProgress.UpdateStep(InitializationStep.PCClient, StepStatus.InProgress);
                RenderInitializationProgress();
                _logger.Info("Attempting initial PC client connection...");
                var pcClientSuccess = await _vtubeStudioPCClient.TryInitializeAsync(cancellationToken);
                if (pcClientSuccess)
                {
                    _initializationProgress.UpdateStep(InitializationStep.PCClient, StepStatus.Completed);
                }
                else
                {
                    _initializationProgress.UpdateStep(InitializationStep.PCClient, StepStatus.Failed, "PC client initialization failed");
                }
                RenderInitializationProgress();

                // Step 5: Phone Client
                _initializationProgress.UpdateStep(InitializationStep.PhoneClient, StepStatus.InProgress);
                RenderInitializationProgress();
                _logger.Info("Attempting initial Phone client connection...");
                var phoneClientSuccess = await _vtubeStudioPhoneClient.TryInitializeAsync(cancellationToken);
                if (phoneClientSuccess)
                {
                    _initializationProgress.UpdateStep(InitializationStep.PhoneClient, StepStatus.Completed);
                }
                else
                {
                    _initializationProgress.UpdateStep(InitializationStep.PhoneClient, StepStatus.Failed, "Phone client initialization failed");
                }
                RenderInitializationProgress();

                // Step 6: Parameter Sync
                _initializationProgress.UpdateStep(InitializationStep.ParameterSync, StepStatus.InProgress);
                RenderInitializationProgress();
                var parameterSyncSuccess = await TrySynchronizeParametersAsync(cancellationToken);
                if (parameterSyncSuccess)
                {
                    _initializationProgress.UpdateStep(InitializationStep.ParameterSync, StepStatus.Completed);
                }
                else
                {
                    _initializationProgress.UpdateStep(InitializationStep.ParameterSync, StepStatus.Failed, "Parameter synchronization failed");
                    _logger.Warning("Parameter synchronization failed during initialization, will retry during recovery");
                }
                RenderInitializationProgress();

                // Step 7: Final Setup
                _initializationProgress.UpdateStep(InitializationStep.FinalSetup, StepStatus.InProgress);
                RenderInitializationProgress();

                // Execute external final setup actions
                foreach (var action in postActions)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorWithException("Error executing final setup action", ex);
                    }
                }

                _initializationProgress.UpdateStep(InitializationStep.FinalSetup, StepStatus.Completed);
                RenderInitializationProgress();

                // Mark initialization as complete
                _initializationProgress.MarkComplete();
                RenderInitializationProgress();
                _logger.Info("Application initialized successfully");

                // Switch to main mode
                _modeManager.SetMode(ConsoleMode.Main);
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException("Error during initialization", ex);

                // Mark current step as failed
                _initializationProgress.UpdateStep(_initializationProgress.CurrentStep, StepStatus.Failed, ex.Message);
                RenderInitializationProgress();

                // Switch to main mode even if initialization failed
                _modeManager.SetMode(ConsoleMode.Main);

                throw;
            }
        }


        /// <summary>
        /// Initializes the transformation engine by loading rules
        /// </summary>
        private async Task InitializeTransformationEngine()
        {
            await _transformationEngine.LoadRulesAsync();
        }

        /// <summary>
        /// Attempts to synchronize VTube Studio parameters based on loaded transformation rules
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if synchronization was successful, false otherwise</returns>
        private async Task<bool> TrySynchronizeParametersAsync(CancellationToken cancellationToken)
        {
            var requiredParameters = _transformationEngine.GetParameterDefinitions();
            var success = await _parameterManager.TrySynchronizeParametersAsync(requiredParameters, cancellationToken);

            if (success)
            {
                _logger.Info("VTube Studio parameters synchronized successfully");
            }
            else
            {
                _logger.Warning("Failed to synchronize VTube Studio parameters");
            }

            return success;
        }


        /// <summary>
        /// Manually renders the initialization progress to the console
        /// </summary>
        private void RenderInitializationProgress()
        {
            try
            {
                // Get empty stats since we're in initialization mode
                var emptyStats = Enumerable.Empty<IServiceStats>();

                // Force an immediate update of the console
                _modeManager.Update(emptyStats);
            }
            catch (Exception ex)
            {
                // Don't let rendering errors break initialization
                _logger.Warning("Failed to render initialization progress: {0}", ex.Message);
            }
        }


    }
}
