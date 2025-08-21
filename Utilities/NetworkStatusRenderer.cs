using System;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Console renderer for network status and troubleshooting information
    /// </summary>
    public class NetworkStatusRenderer : IConsoleModeRenderer
    {
        private readonly IPortStatusMonitorService _portStatusMonitor;
        private readonly INetworkStatusFormatter _networkStatusFormatter;
        private readonly IExternalEditorService _externalEditorService;
        private readonly IAppLogger _logger;
        private readonly IConsole _console;

        private NetworkStatus? _lastSnapshot;
        private DateTime _lastRefresh = DateTime.MinValue;
        private readonly object _snapshotLock = new object();
        private Task? _refreshTask;

        public ConsoleMode Mode => ConsoleMode.NetworkStatus;
        public string DisplayName => "Network Status";
        public ShortcutAction ToggleAction => ShortcutAction.ShowNetworkStatus;
        public TimeSpan PreferredUpdateInterval => TimeSpan.FromMilliseconds(100); // Hardcoded as per user feedback

        /// <summary>
        /// Initializes a new instance of the NetworkStatusRenderer
        /// </summary>
        /// <param name="portStatusMonitor">Service for monitoring port and network status</param>
        /// <param name="networkStatusFormatter">Formatter for network troubleshooting display</param>
        /// <param name="externalEditorService">Service for opening configuration in external editor</param>
        /// <param name="logger">Application logger</param>
        /// <param name="console">Console interface for output</param>
        public NetworkStatusRenderer(IPortStatusMonitorService portStatusMonitor, INetworkStatusFormatter networkStatusFormatter, IExternalEditorService externalEditorService, IAppLogger logger, IConsole console)
        {
            _portStatusMonitor = portStatusMonitor ?? throw new ArgumentNullException(nameof(portStatusMonitor));
            _networkStatusFormatter = networkStatusFormatter ?? throw new ArgumentNullException(nameof(networkStatusFormatter));
            _externalEditorService = externalEditorService ?? throw new ArgumentNullException(nameof(externalEditorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        /// <summary>
        /// Enters the Network Status mode
        /// </summary>
        /// <param name="console">Console to clear and prepare</param>
        public void Enter(IConsole console)
        {
            console.Clear();
            _logger.Debug("Entered Network Status mode.");

            // Start background refresh if not already running
            EnsureRefreshTaskRunning();
        }

        /// <summary>
        /// Exits the Network Status mode
        /// </summary>
        /// <param name="console">Console to clean up</param>
        public void Exit(IConsole console)
        {
            _logger.Debug("Exited Network Status mode.");
            // No specific cleanup needed - background refresh can continue for next time
        }

        /// <summary>
        /// Renders the current network status to the console
        /// </summary>
        /// <param name="context">Rendering context with console and configuration</param>
        public void Render(ConsoleRenderContext context)
        {
            NetworkStatus? snapshot;
            lock (_snapshotLock)
            {
                snapshot = _lastSnapshot;
            }

            if (snapshot == null)
            {
                // Show loading message if no snapshot available yet
                var loadingLines = new[] { "Loading network status...", "", "Press any key to return to main status." };
                _console.WriteLines(loadingLines);
                return;
            }

            if (context.ApplicationConfig == null)
            {
                var errorLines = new[] { "Error: Application configuration not available.", "", "Press any key to return to main status." };
                _console.WriteLines(errorLines);
                return;
            }

            // Render the network troubleshooting content
            var content = _networkStatusFormatter.RenderNetworkTroubleshooting(snapshot, context.ApplicationConfig);
            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            _console.WriteLines(lines);
        }

        /// <summary>
        /// Opens the Application configuration in an external editor
        /// </summary>
        /// <returns>True if successfully opened, false otherwise</returns>
        public Task<bool> TryOpenInExternalEditorAsync()
        {
            return _externalEditorService.TryOpenApplicationConfigAsync();
        }

        /// <summary>
        /// Ensures the background refresh task is running
        /// </summary>
        private void EnsureRefreshTaskRunning()
        {
            if (_refreshTask == null || _refreshTask.IsCompleted)
            {
                _refreshTask = Task.Run(BackgroundRefreshLoop);
            }
        }

        /// <summary>
        /// Background loop to refresh network status every 1-2 seconds
        /// </summary>
        private async Task BackgroundRefreshLoop()
        {
            const int refreshIntervalMs = 1500; // 1.5 seconds

            while (true)
            {
                try
                {
                    // Check if we need to refresh (avoid over-refreshing)
                    if (DateTime.UtcNow - _lastRefresh < TimeSpan.FromMilliseconds(refreshIntervalMs))
                    {
                        await Task.Delay(100); // Short wait before checking again
                        continue;
                    }

                    var networkStatus = await _portStatusMonitor.GetNetworkStatusAsync();

                    lock (_snapshotLock)
                    {
                        _lastSnapshot = networkStatus;
                        _lastRefresh = DateTime.UtcNow;
                    }

                    _logger.Debug("Network status refreshed successfully");

                    // Wait for the next refresh cycle
                    await Task.Delay(refreshIntervalMs);
                }
                catch (Exception ex)
                {
                    _logger.Warning("Failed to refresh network status: {0}", ex.Message);

                    // Wait a bit longer on error before retrying
                    await Task.Delay(refreshIntervalMs * 2);
                }
            }
        }
    }
}
