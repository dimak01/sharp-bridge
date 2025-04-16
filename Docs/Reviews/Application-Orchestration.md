# Application Orchestration

## Introduction

This document outlines the orchestration approach for the SharpBridge application, which serves as a bridge between iPhone tracking data and VTube Studio on PC. Rather than having individual components manage their own lifecycles, we've adopted a centralized orchestration approach where a dedicated component coordinates the initialization, operation, and cleanup of all application components.

## Orchestrator Responsibilities

The `ApplicationOrchestrator` is responsible for:

1. **Configuration Validation** - Validating user inputs and configuration settings
2. **Component Initialization** - Creating and initializing all required components
3. **Connection Establishment** - Establishing and verifying connections to external systems
4. **Lifecycle Management** - Coordinating startup, operation, and shutdown sequences
5. **Error Handling** - Implementing retry strategies and graceful degradation
6. **Resource Cleanup** - Ensuring proper disposal of resources

## Application Lifecycle Flow

The application lifecycle is now separated into distinct phases:

### Initialization Phase (InitializeAsync)

1. **Configuration Validation**
   - Validate input parameters
   - Ensure required files exist and have proper permissions
   - Fail fast with clear error messages if configuration is invalid

2. **Transformation Engine Initialization**
   - Load transformation rules from configuration

3. **VTube Studio Connection**
   - Discover VTube Studio port
   - Establish WebSocket connection
   - Handle authentication with token management
   - Provide user feedback during the process

The initialization phase is explicitly separated from the operation phase to:
- Ensure proper setup before starting any data flow
- Handle initialization errors before operation begins
- Allow for testing of initialization in isolation
- Enable clean up if initialization fails

### Operation Phase (RunAsync)

1. **Event Subscription**
   - Subscribe to tracking data events from the VTubeStudioPhoneClient
   - Only done during active operation, not during initialization

2. **Data Processing**
   - Start the iPhone tracking client
   - Process tracking data events
   - Transform and forward data to VTube Studio
   - Handle operational errors appropriately

3. **Cleanup on Exit**
   - Unsubscribe from events
   - Close connections gracefully
   - Dispose of resources properly

### Error Handling Strategy

#### Categorization of Errors

1. **Initialization Errors** (Non-recoverable)
   - Missing or invalid files
   - Invalid network settings
   - VTube Studio connection failures
   - Action: Fail fast with clear user guidance

2. **Connection Errors** (Potentially recoverable)
   - Network unavailable
   - Service not found
   - Authentication failures
   - Action: Implement retry with exponential backoff

3. **Transient Errors** (Recoverable)
   - Temporary network disruptions
   - Rate limiting
   - Action: Retry with appropriate backoff

4. **Operational Errors** (Context-dependent)
   - Data transformation errors
   - Protocol violations
   - Action: Log, possibly retry, potentially skip problematic data

### Retry Strategy

For recoverable errors, we implement a retry strategy with the following characteristics:

1. **Limited Retry Count** - Maximum of 5 retries to avoid infinite loops
2. **Exponential Backoff** - Increasing delays between retries (1s, 2s, 4s, 8s, 16s)
3. **Maximum Backoff** - Cap maximum delay at 30 seconds
4. **Cancellation Support** - Honor cancellation requests during retries

### User Feedback

During connection and authentication processes:
1. Provide clear status messages about current operations
2. Indicate when user action is required (e.g., VTube Studio authentication prompt)
3. Show progress during multi-step operations
4. Present clear error messages with suggested actions

## Component Interactions

### ApplicationOrchestrator → VTubeStudioPCClient

1. **Initialization**
   ```csharp
   // Discover port
   int port = await _vtubeStudioPCClient.DiscoverPortAsync(cancellationToken);
   
   // Connect to VTube Studio
   await _vtubeStudioPCClient.ConnectAsync(cancellationToken);
   
   // Authenticate with token or request new token
   bool authenticated = await _vtubeStudioPCClient.AuthenticateAsync(cancellationToken);
   ```

2. **Operation**
   ```csharp
   // Send tracking data if connection is open
   if (_vtubeStudioPCClient.State == WebSocketState.Open)
   {
       await _vtubeStudioPCClient.SendTrackingAsync(
           parameters, 
           trackingData.FaceFound, 
           CancellationToken.None);
   }
   ```

3. **Shutdown**
   ```csharp
   // Close connection gracefully
   await _vtubeStudioPCClient.CloseAsync(
       WebSocketCloseStatus.NormalClosure,
       "Application shutting down",
       CancellationToken.None);
   
   // Dispose resources
   _vtubeStudioPCClient?.Dispose();
   ```

### ApplicationOrchestrator → VTubeStudioPhoneClient

1. **Subscription (during RunAsync)**
   ```csharp
   // Subscribe to tracking data events
   _vtubeStudioPhoneClient.TrackingDataReceived += OnTrackingDataReceived;
   ```

2. **Operation**
   ```csharp
   // Start receiver
   await _vtubeStudioPhoneClient.RunAsync(_iphoneIp, cancellationToken);
   ```

3. **Shutdown**
   ```csharp
   // Unsubscribe from events
   _vtubeStudioPhoneClient.TrackingDataReceived -= OnTrackingDataReceived;
   
   // Dispose resources
   _vtubeStudioPhoneClient?.Dispose();
   ```

### Data Flow

The data flow follows an event-based pattern:

```
VTubeStudioPhoneClient
    raises TrackingDataReceived event
        → ApplicationOrchestrator handles event
            → Transforms data using TransformationEngine
                → Sends parameters to VTubeStudioPCClient
```

## Dependency Injection

The application now uses dependency injection to:
1. **Simplify Component Creation** - Components are instantiated by the DI container
2. **Improve Testability** - Dependencies can be easily mocked in tests
3. **Centralize Configuration** - Component configurations are managed centrally
4. **Ensure Proper Lifecycle** - Components are disposed properly by the container

Registration is handled through extension methods:

```csharp
services.AddSharpBridgeServices();
```

## Next Steps

1. **Error Handling Refinement** - Implement retry strategies for recoverable errors
2. **UI Integration** - Prepare for eventual UI integration with observable state
3. **Telemetry and Monitoring** - Add performance monitoring and diagnostics
4. **Comprehensive Testing** - Create unit and integration tests for all components

## Conclusion

The application orchestration approach provides a clear separation of concerns, with the `ApplicationOrchestrator` managing the overall application lifecycle while individual components focus on their core responsibilities. By separating initialization from operation and using dependency injection, we've created a more maintainable, testable, and robust architecture that handles errors gracefully and provides clear user feedback. 