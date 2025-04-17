# Console Status Display Implementation

## Overview

This document outlines the implementation plan for the console status display system in SharpBridge. The system will consolidate status reporting from all components and present them in a unified, non-flickering console interface similar to the existing `PerformanceMonitor`.

## Current Architecture Analysis

### Issues with Current Implementation

1. The `VTubeStudioPhoneClient` includes the main application loop in its `RunAsync()` method, which is a responsibility that should belong to the `ApplicationOrchestrator`.

2. Console output is scattered throughout the application with no unified approach:
   - Ad-hoc `Console.WriteLine()` calls in various components
   - Redundant status reporting
   - No consistent formatting or organization

3. The existing `PerformanceMonitor` provides a solid foundation for console rendering, but is not integrated with other components.

4. The `TrackingResponse` class from the iPhone and transformed parameters for the PC don't have a consistent naming convention, making the data flow less clear.

## Refactoring Plan

### Pre-Implementation Tasks

1. **Move Application Loop Logic**:
   - Refactor `VTubeStudioPhoneClient.RunAsync()` to focus only on receiving data from the iPhone
   - Enhance `ApplicationOrchestrator.RunUntilCancelled()` to include all application loop logic

2. **Rename & Create Domain Models**:
   - Rename `TrackingResponse` to `PhoneTrackingInfo` for clarity
   - Create a new `PCTrackingInfo` class to encapsulate tracking data sent to VTube Studio PC
   - Both classes should implement a marker interface for consistent handling

3. **Define Service Statistics Interface**:
   - Create interfaces for retrieving service statistics from components
   - Implement a standard container for service statistics

### Domain Models

```csharp
// In Interfaces/IFormattableObject.cs
namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Marker interface for objects that can be formatted for display
    /// </summary>
    public interface IFormattableObject
    {
    }
}

// In Models/PhoneTrackingInfo.cs (renamed from TrackingResponse.cs)
namespace SharpBridge.Models
{
    /// <summary>
    /// Tracking data received from iPhone VTube Studio
    /// </summary>
    public class PhoneTrackingInfo : IFormattableObject
    {
        /// <summary>Timestamp of the tracking data</summary>
        public ulong Timestamp { get; set; }
        
        /// <summary>Hotkey value</summary>
        public short Hotkey { get; set; }
        
        /// <summary>Whether a face is detected</summary>
        public bool FaceFound { get; set; }
        
        /// <summary>Head rotation in 3D space</summary>
        public Coordinates Rotation { get; set; }
        
        /// <summary>Head position in 3D space</summary>
        public Coordinates Position { get; set; }
        
        /// <summary>Left eye position</summary>
        public Coordinates EyeLeft { get; set; }
        
        /// <summary>Right eye position</summary>
        public Coordinates EyeRight { get; set; }
        
        /// <summary>Collection of blend shapes representing facial expressions</summary>
        public List<BlendShape> BlendShapes { get; set; }
    }
}

// In Models/PCTrackingInfo.cs
namespace SharpBridge.Models
{
    /// <summary>
    /// Tracking data sent to VTube Studio PC
    /// </summary>
    public class PCTrackingInfo : IFormattableObject
    {
        /// <summary>
        /// Parameters to send to VTube Studio PC
        /// </summary>
        public IEnumerable<TrackingParam> Parameters { get; set; }
        
        /// <summary>
        /// Whether a face is detected
        /// </summary>
        public bool FaceFound { get; set; }
    }
}
```

### Service Statistics

```csharp
// In Models/ServiceStats.cs
namespace SharpBridge.Models
{
    /// <summary>
    /// Container for service statistics
    /// </summary>
    public class ServiceStats<T> where T : IFormattableObject
    {
        /// <summary>
        /// The name of the service
        /// </summary>
        public string ServiceName { get; }
        
        /// <summary>
        /// The current status of the service
        /// </summary>
        public string Status { get; }
        
        /// <summary>
        /// Service-specific counters and metrics
        /// </summary>
        public Dictionary<string, long> Counters { get; }
        
        /// <summary>
        /// The current entity being processed by the service
        /// </summary>
        public T CurrentEntity { get; }
        
        /// <summary>
        /// Creates a new instance of ServiceStats
        /// </summary>
        public ServiceStats(string serviceName, string status, T currentEntity, Dictionary<string, long> counters = null)
        {
            ServiceName = serviceName;
            Status = status;
            CurrentEntity = currentEntity;
            Counters = counters ?? new Dictionary<string, long>();
        }
    }
}

// In Interfaces/IServiceStatsProvider.cs
namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for components that provide service statistics
    /// </summary>
    public interface IServiceStatsProvider<T> where T : IFormattableObject
    {
        /// <summary>
        /// Gets the current service statistics
        /// </summary>
        ServiceStats<T> GetServiceStats();
    }
}
```

### Formatters

```csharp
// In Interfaces/IFormatter.cs
namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Defines verbosity levels for formatting
    /// </summary>
    public enum VerbosityLevel
    {
        Basic,
        Normal,
        Detailed
    }
    
    /// <summary>
    /// Interface for formatters that convert entities to display strings
    /// </summary>
    public interface IFormatter<T> where T : IFormattableObject
    {
        /// <summary>
        /// Formats an entity into a display string
        /// </summary>
        string Format(T entity, VerbosityLevel verbosity);
    }
}

// In Utilities/PhoneTrackingInfoFormatter.cs
namespace SharpBridge.Utilities
{
    /// <summary>
    /// Formatter for PhoneTrackingInfo objects
    /// </summary>
    public class PhoneTrackingInfoFormatter : IFormatter<PhoneTrackingInfo>
    {
        // Reuse most of PerformanceMonitor.cs code for formatting
    }
    
    // Implement similar formatters for PCTrackingInfo
}
```

### Console Renderer

```csharp
// In Utilities/ConsoleRenderer.cs
namespace SharpBridge.Utilities
{
    /// <summary>
    /// Centralized console rendering utility
    /// </summary>
    public static class ConsoleRenderer
    {
        private static readonly Dictionary<Type, object> _formatters = new();
        private static DateTime _lastUpdate = DateTime.MinValue;
        private static readonly object _lock = new();
        
        static ConsoleRenderer()
        {
            // Register formatters for known types
            RegisterFormatter(new PhoneTrackingInfoFormatter());
            RegisterFormatter(new PCTrackingInfoFormatter());
        }
        
        /// <summary>
        /// Registers a formatter for a specific entity type
        /// </summary>
        public static void RegisterFormatter<T>(IFormatter<T> formatter) where T : IFormattableObject
        {
            _formatters[typeof(T)] = formatter;
        }
        
        /// <summary>
        /// Updates the console display with service statistics
        /// </summary>
        public static void Update<T>(IEnumerable<ServiceStats<T>> stats, VerbosityLevel verbosity = VerbosityLevel.Normal) 
            where T : IFormattableObject
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastUpdate < TimeSpan.FromMilliseconds(100))
                    return;
                
                _lastUpdate = now;
                
                var sb = new StringBuilder();
                
                foreach (var stat in stats.Where(s => s != null))
                {
                    sb.AppendLine($"=== {stat.ServiceName} ({stat.Status}) ===");
                    
                    if (verbosity >= VerbosityLevel.Normal && stat.Counters.Any())
                    {
                        sb.AppendLine("Metrics:");
                        foreach (var counter in stat.Counters)
                        {
                            sb.AppendLine($"  {counter.Key}: {counter.Value}");
                        }
                    }
                    
                    if (stat.CurrentEntity != null)
                    {
                        var entityType = stat.CurrentEntity.GetType();
                        if (_formatters.TryGetValue(entityType, out var formatter))
                        {
                            sb.AppendLine();
                            sb.Append(((IFormatter<T>)formatter).Format(stat.CurrentEntity, verbosity));
                        }
                    }
                    
                    sb.AppendLine();
                }
                
                ConsoleDisplayAction(sb.ToString());
            }
        }
        
        // Reusing PerformanceMonitor's console display technique
        private static void ConsoleDisplayAction(string output)
        {
            try
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(output);
                
                int currentLine = Console.CursorTop;
                int currentCol = Console.CursorLeft;
                
                int windowHeight = Console.WindowHeight;
                for (int i = currentLine; i < windowHeight - 1; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }
                
                Console.SetCursorPosition(currentCol, currentLine);
            }
            catch
            {
                try
                {
                    Console.Clear();
                    Console.Write(output);
                }
                catch
                {
                    // Last resort fallback
                }
            }
        }
    }
}
```

## Implementation Strategy

### Phase 1: Domain Model Refactoring

1. Implement `IFormattableObject` interface
2. Rename `TrackingResponse` to `PhoneTrackingInfo`
3. Create `PCTrackingInfo` class
4. Update all references accordingly

### Phase 2: Service Statistics

1. Implement `ServiceStats<T>` class
2. Implement `IServiceStatsProvider<T>` interface
3. Update `VTubeStudioPhoneClient`, `VTubeStudioPCClient` to implement the interface

### Phase 3: Formatters and Renderer

1. Implement formatters for each entity type
2. Implement the `ConsoleRenderer` class
3. Integrate with `ApplicationOrchestrator`

### Phase 4: Application Loop Refactoring

1. Move loop logic from `VTubeStudioPhoneClient` to `ApplicationOrchestrator`
2. Update `OnTrackingDataReceived` to collect stats and update the console

## Integration with ApplicationOrchestrator

```csharp
// In OnTrackingDataReceived method
private async void OnTrackingDataReceived(object sender, PhoneTrackingInfo trackingData)
{
    try
    {
        if (trackingData == null) return;
        
        // Create a list to collect service stats
        var serviceStats = new List<object>();
                
        // Add phone client stats
        serviceStats.Add(_vtubeStudioPhoneClient.GetServiceStats());
        
        // Transform data
        IEnumerable<TrackingParam> parameters = _transformationEngine.TransformData(trackingData);
        var pcTrackingInfo = new PCTrackingInfo { Parameters = parameters, FaceFound = trackingData.FaceFound };
        
        
        // Send data to VTube Studio if connection is open
        if (_vtubeStudioPCClient.State == WebSocketState.Open)
        {
            await _vtubeStudioPCClient.SendTrackingAsync(
                parameters, 
                trackingData.FaceFound, 
                CancellationToken.None);
        }
        
        // Add PC client stats
        serviceStats.Add(_vtubeStudioPCClient.GetServiceStats());
        
        // Update console display
        ConsoleRenderer.Update(serviceStats, _currentVerbosity);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing tracking data: {ex.Message}");
    }
}
```

## Benefits of This Approach

1. **Clear Separation of Concerns**:
   - Each component focuses on its core functionality
   - Display logic is centralized in dedicated formatters
   - Statistics gathering is standardized across components

2. **Consistent Naming**:
   - Renamed models clearly indicate the data flow direction
   - Interfaces establish clear contracts for behavior

3. **Extensibility**:
   - New components can easily integrate by implementing `IServiceStatsProvider`
   - New entity types can be supported by implementing `IFormattableObject` and registering a formatter
   - Verbosity levels allow for different detail levels based on user preference

4. **Improved User Experience**:
   - Consolidated display eliminates scattered console output
   - Non-flickering updates provide a more professional appearance
   - Organized sections make information easier to find 