# Console Status Display Implementation Checklist

## Overview
This checklist tracks the implementation status of the console status display system as outlined in the `ConsoleStatusDisplay.md` document. It breaks down each task into concrete steps and serves as a reference during implementation.

## Phase 1: Domain Model Refactoring

### Create Marker Interface
- [x] Create `Interfaces/IFormattableObject.cs`
  ```csharp
  namespace SharpBridge.Interfaces
  {
      /// <summary>
      /// Marker interface for objects that can be formatted for display
      /// </summary>
      public interface IFormattableObject
      {
      }
  }
  ```

### Rename TrackingResponse to PhoneTrackingInfo
- [x] Rename `Models/TrackingResponse` into `Models/PhoneTrackingInfo.cs` and make sure it implements `IFormattableObject`
  ```csharp
  namespace SharpBridge.Models
  {
      /// <summary>
      /// Tracking data received from iPhone VTube Studio
      /// </summary>
      public class PhoneTrackingInfo : IFormattableObject
      {
          // properties from TrackingResponse remains untouched
      }
  }
  ```
- [x] Update all references to `TrackingResponse` to use `PhoneTrackingInfo`
  - [x] Application Orchestrator
  - [x] Tests
  - [x] Any other references

### Create PCTrackingInfo Class
- [x] Create `Models/PCTrackingInfo.cs` implementing `IFormattableObject`
  ```csharp
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

- [x] Refactor current logic that collects TrackingParams and FaceFound separately to use this new file  

## Phase 2: Service Statistics Implementation

### Create ServiceStats Container
- [x] Create `Models/ServiceStats.cs`
  ```csharp
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
  ```

### Create Service Stats Provider Interface
- [x] Create `Interfaces/IServiceStatsProvider.cs`
  ```csharp
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

### Update Service Classes to Implement IServiceStatsProvider
- [x] Update `VTubeStudioPhoneClient` to implement `IServiceStatsProvider<PhoneTrackingInfo>`
- [x] Update `VTubeStudioPCClient` to implement `IServiceStatsProvider<PCTrackingInfo>`

## Phase 3: Formatters and Renderer Implementation

### Create Formatter Interface and Verbosity Enum
- [x] Create `Interfaces/IFormatter.cs`
  ```csharp
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
  ```

### Create Concrete Formatters
- [x] Create `Utilities/PhoneTrackingInfoFormatter.cs`
  ```csharp
  namespace SharpBridge.Utilities
  {
      /// <summary>
      /// Formatter for PhoneTrackingInfo objects
      /// </summary>
      public class PhoneTrackingInfoFormatter : IFormatter<PhoneTrackingInfo>
      {
          // Implementation
      }
  }
  ```
- [x] Create `Utilities/PCTrackingInfoFormatter.cs`

### Create Console Renderer
- [x] Create `Utilities/ConsoleRenderer.cs`
  ```csharp
  namespace SharpBridge.Utilities
  {
      /// <summary>
      /// Centralized console rendering utility
      /// </summary>
      public class ConsoleRenderer : IConsoleRenderer
      {
          // Implementation
      }
  }
  ```
- [x] Create `Interfaces/IConsoleRenderer.cs` for decoupling
- [x] Convert ConsoleRenderer to non-static class
- [x] Register IConsoleRenderer in DI container

## Phase 4: Application Logic Refactoring

### Move Application Loop Logic
- [x] Update `VTubeStudioPhoneClient.RunAsync()` to focus on receiving data
- [x] Enhance `ApplicationOrchestrator.RunUntilCancelled()` to handle all application loop logic

### Update OnTrackingDataReceived to Use Console Renderer
- [x] Modify `ApplicationOrchestrator.OnTrackingDataReceived()` method to collect stats and update the console

### Additional Enhancements
- [x] Add hot-key reload for transformation configuration (Alt+K)
- [x] Enhance tracking parameter visualization with progress bars

## Testing and Integration

### Add Unit Tests
- [ ] Test `ServiceStats<T>` class
- [ ] Test formatters
- [ ] Test `ConsoleRenderer` class
- [ ] Test updated application orchestrator logic

### Manual Testing
- [x] Test with different verbosity levels
- [x] Test with different tracking data scenarios
- [x] Test error handling

## Final Cleanup and Documentation

- [x] Update XML documentation comments
- [x] Remove old console output code
- [x] Update any remaining references to old class names
- [x] Add keyboard command to cycle through verbosity levels (Alt+P and Alt+O) 