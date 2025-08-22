using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Manages console UI modes and delegates rendering to the appropriate mode renderer
    /// </summary>
    public class ConsoleModeManager : IConsoleModeManager
    {
        private readonly IConsole _console;
        private readonly IConfigManager _configManager;
        private readonly IAppLogger _logger;
        private readonly IShortcutConfigurationManager _shortcutManager;
        private readonly Dictionary<ConsoleMode, IConsoleModeContentProvider> _renderers;

        private ConsoleMode _currentMode = ConsoleMode.Main;
        private DateTime _lastUpdate = DateTime.MinValue;
        private IConsoleModeContentProvider? _activeRenderer;

        /// <summary>
        /// Gets the currently active console mode
        /// </summary>
        public ConsoleMode CurrentMode => _currentMode;

        /// <summary>
        /// Gets the main status renderer for accessing formatters (temporary compatibility)
        /// </summary>
        public IMainStatusRenderer MainStatusRenderer => (IMainStatusRenderer)_renderers[ConsoleMode.Main];

        /// <summary>
        /// Initializes a new instance of the ConsoleModeManager
        /// </summary>
        /// <param name="console">Console for display operations</param>
        /// <param name="configManager">Configuration manager for loading configs</param>
        /// <param name="logger">Application logger</param>
        /// <param name="shortcutManager">Shortcut configuration manager for footer generation</param>
        /// <param name="renderers">Collection of all available mode renderers</param>
        public ConsoleModeManager(IConsole console, IConfigManager configManager, IAppLogger logger, IShortcutConfigurationManager shortcutManager, IEnumerable<IConsoleModeContentProvider> renderers)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shortcutManager = shortcutManager ?? throw new ArgumentNullException(nameof(shortcutManager));

            if (renderers == null)
                throw new ArgumentNullException(nameof(renderers));

            // Build renderer lookup dictionary
            _renderers = renderers.ToDictionary(r => r.Mode, r => r);

            // Validate we have all required renderers
            ValidateRequiredRenderers();

            // Set initial active renderer to Main mode
            _activeRenderer = _renderers[ConsoleMode.Main];

            _logger.Debug("ConsoleModeManager initialized with {0} renderers. Current mode: {1}", _renderers.Count, _currentMode);
        }

        /// <summary>
        /// Toggles the specified mode. If the mode is already active, returns to Main mode.
        /// If the mode is not active, switches to that mode.
        /// </summary>
        /// <param name="mode">The mode to toggle</param>
        public void Toggle(ConsoleMode mode)
        {
            if (_currentMode == mode)
            {
                // Toggle back to Main mode
                SetMode(ConsoleMode.Main);
            }
            else
            {
                // Switch to the requested mode
                SetMode(mode);
            }
        }

        /// <summary>
        /// Forces the console to the specified mode
        /// </summary>
        /// <param name="mode">The mode to set as active</param>
        public void SetMode(ConsoleMode mode)
        {
            if (!_renderers.ContainsKey(mode))
            {
                _logger.Warning("Attempted to set unknown console mode: {0}", mode);
                return;
            }

            if (_currentMode == mode)
            {
                _logger.Debug("Already in mode {0}, no change needed", mode);
                return;
            }

            var previousMode = _currentMode;
            var previousRenderer = _activeRenderer;
            var newRenderer = _renderers[mode];

            // Exit the current mode
            previousRenderer?.Exit(_console);
            _logger.Debug("Exited console mode: {0}", previousMode);

            // Switch to new mode
            _currentMode = mode;
            _activeRenderer = newRenderer;

            // Enter the new mode
            _activeRenderer.Enter(_console);
            _logger.Debug("Entered console mode: {0} ({1})", mode, _activeRenderer.DisplayName);

            // Reset update timing to ensure immediate render
            _lastUpdate = DateTime.MinValue;
        }

        /// <summary>
        /// Updates the active mode renderer with current service statistics and configuration.
        /// Respects each renderer's preferred update interval to avoid over-rendering.
        /// </summary>
        /// <param name="stats">Current service statistics</param>
        public void Update(IEnumerable<IServiceStats> stats)
        {
            if (_activeRenderer == null)
            {
                _logger.Warning("No active renderer available for update");
                return;
            }

            // Check if we should update based on the renderer's preferred interval
            var now = DateTime.UtcNow;
            var timeSinceLastUpdate = now - _lastUpdate;

            if (timeSinceLastUpdate < _activeRenderer.PreferredUpdateInterval)
            {
                // Too soon to update, skip this cycle
                return;
            }

            try
            {
                // Load current configuration
                var applicationConfig = _configManager.LoadApplicationConfigAsync().GetAwaiter().GetResult();
                var userPreferences = _configManager.LoadUserPreferencesAsync().GetAwaiter().GetResult();

                // Build render context
                var context = new ConsoleRenderContext
                {
                    ServiceStats = stats,
                    ApplicationConfig = applicationConfig,
                    UserPreferences = userPreferences,
                    ConsoleSize = (_console.WindowWidth, _console.WindowHeight),
                    CancellationToken = default // TODO: Consider adding cancellation support
                };

                // Get content from active provider
                var content = _activeRenderer.GetContent(context);

                // Wrap content with header and footer
                var wrappedContent = WrapContentWithHeaderAndFooter(content);

                // Display final content
                _console.WriteLines(wrappedContent);
                _lastUpdate = now;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to update console mode {0}: {1}", _currentMode, ex.Message);

                // On error, try to fall back to main mode if we're not already there
                if (_currentMode != ConsoleMode.Main)
                {
                    _logger.Info("Falling back to Main mode due to render error");
                    SetMode(ConsoleMode.Main);
                }
            }
        }

        /// <summary>
        /// Clears the console display
        /// </summary>
        public void Clear()
        {
            _console.Clear();
            _logger.Debug("Console cleared");
        }

        /// <summary>
        /// Forwards the "open in external editor" request to the currently active mode renderer
        /// </summary>
        /// <returns>True if the editor was successfully opened, false otherwise</returns>
        public async Task<bool> TryOpenActiveModeInEditorAsync()
        {
            if (_activeRenderer == null)
            {
                _logger.Warning("No active renderer available for external editor request");
                return false;
            }

            try
            {
                _logger.Debug("Forwarding external editor request to {0} mode renderer", _currentMode);
                var result = await _activeRenderer.TryOpenInExternalEditorAsync();

                if (result)
                {
                    _logger.Debug("External editor opened successfully from {0} mode", _currentMode);
                }
                else
                {
                    _logger.Warning("External editor failed to open from {0} mode", _currentMode);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Error opening external editor from {0} mode: {1}", _currentMode, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Validates that all required console mode renderers are available
        /// </summary>
        private void ValidateRequiredRenderers()
        {
            var requiredModes = new[] { ConsoleMode.Main, ConsoleMode.SystemHelp, ConsoleMode.NetworkStatus };
            var missingModes = requiredModes.Where(mode => !_renderers.ContainsKey(mode)).ToArray();

            if (missingModes.Any())
            {
                var missingModeNames = string.Join(", ", missingModes);
                throw new InvalidOperationException($"Missing required console mode renderers: {missingModeNames}");
            }

            _logger.Debug("All required console mode renderers are available: {0}", string.Join(", ", requiredModes));
        }

        /// <summary>
        /// Wraps content with header and footer for consistent mode navigation
        /// </summary>
        /// <param name="content">Original content from the active provider</param>
        /// <returns>Content wrapped with header and footer</returns>
        private string[] WrapContentWithHeaderAndFooter(string[] content)
        {
            var result = new List<string>();

            // Add header
            var header = GenerateHeader();
            if (!string.IsNullOrEmpty(header))
            {
                result.Add(header);
                result.Add(""); // Empty line separator
            }

            // Add original content
            result.AddRange(content);

            // Add footer
            var footer = GenerateFooter();
            if (!string.IsNullOrEmpty(footer))
            {
                result.Add(""); // Empty line separator
                result.Add(footer);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Generates the header showing current mode
        /// </summary>
        /// <returns>Header string or empty if no header needed</returns>
        private string GenerateHeader()
        {
            var activeRenderer = _renderers[_currentMode];
            return $"=== {activeRenderer.DisplayName.ToUpper()} ===";
        }

        /// <summary>
        /// Generates the footer showing available mode navigation shortcuts
        /// </summary>
        /// <returns>Footer string or empty if no footer needed</returns>
        private string GenerateFooter()
        {
            var footerParts = new List<string>();

            // Get shortcut display strings
            var helpShortcut = _shortcutManager.GetDisplayString(ShortcutAction.ShowSystemHelp);
            var networkShortcut = _shortcutManager.GetDisplayString(ShortcutAction.ShowNetworkStatus);

            // Generate footer based on current mode
            switch (_currentMode)
            {
                case ConsoleMode.Main:
                    // Main: F1: System Help | F2: Network Status
                    footerParts.Add($"{helpShortcut}: {_renderers[ConsoleMode.SystemHelp].DisplayName}");
                    footerParts.Add($"{networkShortcut}: {_renderers[ConsoleMode.NetworkStatus].DisplayName}");
                    break;

                case ConsoleMode.SystemHelp:
                    // System Help: F1: Return to Main | F2: Network Status  
                    footerParts.Add($"{helpShortcut}: Return to Main");
                    footerParts.Add($"{networkShortcut}: {_renderers[ConsoleMode.NetworkStatus].DisplayName}");
                    break;

                case ConsoleMode.NetworkStatus:
                    // Network Status: F1: System Help | F2: Return to Main
                    footerParts.Add($"{helpShortcut}: {_renderers[ConsoleMode.SystemHelp].DisplayName}");
                    footerParts.Add($"{networkShortcut}: Return to Main");
                    break;
            }

            // Always add the exit option at the end
            footerParts.Add("Ctrl+C: Exit");

            return string.Join(" | ", footerParts);
        }
    }
}
