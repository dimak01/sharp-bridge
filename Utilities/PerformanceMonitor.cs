using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Tracks and displays performance metrics for the tracking data
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        // Performance tracking variables
        private readonly Queue<DateTime> _frameTimestamps = new Queue<DateTime>();
        private readonly int _maxFrameHistory;
        private int _totalFramesReceived = 0;
        private readonly Stopwatch _uptimeStopwatch = new Stopwatch();
        private DateTime _lastUiUpdate = DateTime.MinValue;
        private readonly object _lockObject = new object();
        private TrackingResponse _lastFrame = null;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly int _uiUpdateIntervalMs;
        private Task _uiTask;
        private readonly Action<string> _displayAction;
        
        /// <summary>
        /// Get the current performance metrics
        /// </summary>
        public PerformanceMetrics CurrentMetrics 
        { 
            get
            {
                lock (_lockObject)
                {
                    return new PerformanceMetrics
                    {
                        TotalFrames = _totalFramesReceived,
                        Uptime = _uptimeStopwatch.Elapsed,
                        CurrentFps = CalculateCurrentFps(),
                        AverageFps = CalculateAverageFps(),
                        LastFrame = _lastFrame
                    };
                }
            }
        }
        
        /// <summary>
        /// Creates a new instance of the PerformanceMonitor with a custom display action
        /// </summary>
        /// <param name="displayAction">Action to call with the display output</param>
        /// <param name="maxFrameHistory">Maximum number of frames to keep in history for FPS calculation</param>
        /// <param name="uiUpdateIntervalMs">How often to update the UI in milliseconds</param>
        public PerformanceMonitor(Action<string> displayAction, int maxFrameHistory = 100, int uiUpdateIntervalMs = 250)
        {
            _displayAction = displayAction ?? throw new ArgumentNullException(nameof(displayAction));
            _maxFrameHistory = maxFrameHistory;
            _uiUpdateIntervalMs = uiUpdateIntervalMs;
        }
        
        /// <summary>
        /// Creates a new instance of the PerformanceMonitor with built-in console display
        /// </summary>
        /// <param name="maxFrameHistory">Maximum number of frames to keep in history for FPS calculation</param>
        /// <param name="uiUpdateIntervalMs">How often to update the UI in milliseconds</param>
        public PerformanceMonitor(int maxFrameHistory = 100, int uiUpdateIntervalMs = 250)
            : this(ConsoleDisplayAction, maxFrameHistory, uiUpdateIntervalMs)
        {
        }
        
        /// <summary>
        /// Built-in console display action that updates the console in a flicker-free way
        /// </summary>
        private static void ConsoleDisplayAction(string output)
        {
            try
            {
                // Position cursor at the beginning
                Console.SetCursorPosition(0, 0);
                
                // Write output
                Console.Write(output);
                
                // Get current cursor position after writing the output
                int currentLine = Console.CursorTop;
                int currentCol = Console.CursorLeft;
                
                // Clear any remaining content from previous outputs by writing spaces
                // Make sure we clear at least to the end of the console window
                int windowHeight = Console.WindowHeight;
                for (int i = currentLine; i < windowHeight - 1; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }
                
                // Reset cursor position
                Console.SetCursorPosition(currentCol, currentLine);
            }
            catch (Exception ex)
            {
                // If console operations fail, fallback to a simpler approach
                // This can happen if the console window is resized or other issues
                try
                {
                    Console.Clear();
                    Console.Write(output);
                }
                catch
                {
                    // Last resort, just try to display without positioning
                    // Do nothing more if this fails too
                }
            }
        }
        
        /// <summary>
        /// Start monitoring performance
        /// </summary>
        public void Start()
        {
            _uptimeStopwatch.Start();
            
            // Start UI update task
            _uiTask = Task.Run(UpdateUiLoop);
        }
        
        /// <summary>
        /// Process a new frame of tracking data
        /// </summary>
        /// <param name="frame">The tracking data frame</param>
        public void ProcessFrame(TrackingResponse frame)
        {
            if (frame == null) return;
            
            lock (_lockObject)
            {
                _totalFramesReceived++;
                _lastFrame = frame;
                
                // Record the timestamp for FPS calculation
                var now = DateTime.UtcNow;
                _frameTimestamps.Enqueue(now);
                
                // Keep the queue at a reasonable size
                while (_frameTimestamps.Count > _maxFrameHistory)
                {
                    _frameTimestamps.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// Disposes the monitor and stops the UI update task
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _uiTask?.Wait(1000);
            }
            catch (AggregateException)
            {
                // Task was canceled, this is expected
            }
            _cts.Dispose();
        }
        
        private async Task UpdateUiLoop()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    UpdateConsoleOutput();
                    await Task.Delay(_uiUpdateIntervalMs, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                    break;
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    Console.WriteLine($"Error updating UI: {ex.Message}");
                    await Task.Delay(1000, _cts.Token);
                }
            }
        }
        
        private double CalculateCurrentFps()
        {
            if (_frameTimestamps.Count < 2)
                return 0;
                
            var oldestTimestamp = _frameTimestamps.Peek();
            var newestTimestamp = _frameTimestamps.Last();
            var timeSpan = newestTimestamp - oldestTimestamp;
            
            // Avoid division by zero
            if (timeSpan.TotalSeconds > 0)
            {
                // Subtract 1 because we're measuring the time between frames
                return (_frameTimestamps.Count - 1) / timeSpan.TotalSeconds;
            }
            
            return 0;
        }
        
        private double CalculateAverageFps()
        {
            var uptime = _uptimeStopwatch.Elapsed;
            return uptime.TotalSeconds > 0 ? _totalFramesReceived / uptime.TotalSeconds : 0;
        }
        
        private void UpdateConsoleOutput()
        {
            // Only update the UI if we have new data
            var now = DateTime.UtcNow;
            if (now - _lastUiUpdate < TimeSpan.FromMilliseconds(100))
                return;

            _lastUiUpdate = now;

            var metrics = CurrentMetrics;
            if (metrics.LastFrame == null)
                return;

            var frameCopy = metrics.LastFrame;
            var uptime = metrics.Uptime;
            var totalFrames = metrics.TotalFrames;
            var currentFps = metrics.CurrentFps;
            var averageFps = metrics.AverageFps;
                
            var output = new System.Text.StringBuilder();
            output.AppendLine("=== Sharp Bridge Performance Monitor ===");
            output.AppendLine($"Connected to iPhone: {frameCopy.FaceFound}");
            output.AppendLine($"Uptime: {uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}");
            output.AppendLine($"Total Frames: {totalFrames}");
            output.AppendLine($"Current FPS: {currentFps:F1}");
            output.AppendLine($"Average FPS: {averageFps:F1}");
            output.AppendLine($"Tracking Frequency: {currentFps:F1} Hz");
            
            // Display key frame data
            output.AppendLine("\n=== Latest Frame Data ===");
            
            // Head movement
            output.AppendLine($"Head Rotation (X,Y,Z): " +
                $"{frameCopy.Rotation?.X:F1}°, " +
                $"{frameCopy.Rotation?.Y:F1}°, " +
                $"{frameCopy.Rotation?.Z:F1}°");
            
            // Show a few key blend shapes if available
            if (frameCopy.BlendShapes != null && frameCopy.BlendShapes.Count > 0)
            {
                var expressions = new[] { "JawOpen", "EyeBlinkLeft", "EyeBlinkRight", "BrowInnerUp", "MouthSmile" };
                output.AppendLine("\nKey Expressions:");
                
                foreach (var expression in expressions)
                {
                    var shape = frameCopy.BlendShapes.FirstOrDefault(s => s.Key == expression);
                    if (shape != null)
                    {
                        var barLength = (int)(shape.Value * 20); // Scale to 0-20 characters
                        var bar = new string('█', barLength) + new string('░', 20 - barLength);
                        output.AppendLine($"{expression.PadRight(15)}: {bar} {shape.Value:F2}");
                    }
                }
                
                // Show total blend shapes count
                output.AppendLine($"\nTotal Blend Shapes: {frameCopy.BlendShapes.Count}");
            }
            
            output.AppendLine("\nPress Ctrl+C to exit...");
            
            _displayAction(output.ToString());
        }
    }
    
    /// <summary>
    /// Contains performance metrics data
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>Total number of frames received</summary>
        public int TotalFrames { get; set; }
        
        /// <summary>Time elapsed since monitoring started</summary>
        public TimeSpan Uptime { get; set; }
        
        /// <summary>Current frames per second based on recent history</summary>
        public double CurrentFps { get; set; }
        
        /// <summary>Average frames per second since monitoring started</summary>
        public double AverageFps { get; set; }
        
        /// <summary>The most recent tracking data frame</summary>
        public TrackingResponse LastFrame { get; set; }
    }
} 