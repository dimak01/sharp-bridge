# Application Orchestration

## Introduction

This document outlines the orchestration approach for the SharpBridge application, which serves as a bridge between iPhone tracking data and VTube Studio on PC. Rather than having individual components manage their own lifecycles, we've adopted a centralized orchestration approach where a dedicated component coordinates the initialization, operation, and cleanup of all application components.

## Orchestrator Responsibilities

The application orchestrator (previously referred to as "Bridge Service") is responsible for:

1. **Configuration Validation** - Validating user inputs and configuration settings
2. **Component Initialization** - Creating and initializing all required components
3. **Connection Establishment** - Establishing and verifying connections to external systems
4. **Lifecycle Management** - Coordinating startup, operation, and shutdown sequences
5. **Error Handling** - Implementing retry strategies and graceful degradation
6. **Resource Cleanup** - Ensuring proper disposal of resources

## Application Lifecycle Flow

### Initialization Phase

1. **Configuration Validation**
   - Validate command-line arguments and configuration files
   - Ensure required files exist and have proper permissions
   - Fail fast with clear error messages if configuration is invalid

2. **Component Initialization**
   - Create instances of required components (TrackingReceiver, TransformationEngine, VTubeStudioClient)
   - Load transformation rules from configuration
   - Subscribe to required events

### Connection Phase

1. **VTube Studio Connection**
   - Attempt port discovery if enabled
   - Establish WebSocket connection to VTube Studio
   - Handle authentication with token management
   - Implement retry logic with exponential backoff
   - Provide user feedback during authentication process

2. **iPhone Tracking Connection**
   - Establish UDP connection to iPhone
   - Verify data reception
   - Implement timeout handling for connection verification

### Operation Phase

1. **Event-Based Processing**
   - Process tracking data events from iPhone
   - Transform tracking data using the transformation engine
   - Forward transformed parameters to VTube Studio
   - Handle and recover from transient errors

2. **Error Recovery**
   - Implement retries for recoverable errors
   - Provide clear feedback on connection status
   - Attempt to reconnect when connections are lost

### Shutdown Phase

1. **Graceful Termination**
   - Unsubscribe from events
   - Close connections properly
   - Send appropriate close messages

2. **Resource Cleanup**
   - Dispose all disposable resources
   - Ensure no lingering connections or resources

## Error Handling Strategy

### Categorization of Errors

1. **Configuration Errors** (Non-recoverable)
   - Missing or invalid files
   - Invalid network settings
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

### Orchestrator → VTubeStudioClient

1. **Initialization**
   ```
   Create VTubeStudioClient with configuration
   ```

2. **Connection Management**
   ```
   Discover port (optional)
   Connect to VTube Studio
   Authenticate with token or request new token
   ```

3. **Operation**
   ```
   Monitor connection state
   Handle reconnection when needed
   ```

4. **Shutdown**
   ```
   Close connection gracefully
   Dispose resources
   ```

### Orchestrator → TrackingReceiver

1. **Initialization**
   ```
   Create TrackingReceiver with configuration
   Subscribe to tracking data events
   ```

2. **Connection Management**
   ```
   Start receiver
   Verify data reception
   ```

3. **Shutdown**
   ```
   Unsubscribe from events
   Stop receiver
   Dispose resources
   ```

### TrackingReceiver → Orchestrator → VTubeStudioClient

The data flow follows an event-based pattern:

```
TrackingReceiver
    raises TrackingDataReceived event
        → Orchestrator handles event
            → Transforms data using TransformationEngine
                → Sends parameters to VTubeStudioClient
```

## Initialization and Connection Sequence

```
┌───────────────┐
│ Load Config   │
└───────┬───────┘
        │
        ▼
┌───────────────┐    No     ┌────────────────┐
│ Config Valid? ├──────────►│ Error and Exit │
└───────┬───────┘           └────────────────┘
        │ Yes
        ▼
┌───────────────┐
│ Initialize    │
│ Components    │
└───────┬───────┘
        │
        ▼
┌───────────────┐    No     ┌────────────────┐
│ Connect VTS?  ├──────────►│ Retry Logic    │
└───────┬───────┘           └───────┬────────┘
        │ Yes                       │
        │                           │ Max Retries
        │                           ▼
        │                  ┌────────────────┐
        │                  │ Error and Exit │
        │                  └────────────────┘
        ▼
┌───────────────┐    No     ┌────────────────┐
│ Authenticate? ├──────────►│ Wait for User  │
└───────┬───────┘           │ Approval       │
        │ Yes               └────────┬───────┘
        │                            │
        │                            │ Timeout
        │                            ▼
        │                   ┌────────────────┐
        │                   │ Error and Exit │
        │                   └────────────────┘
        ▼
┌───────────────┐    No     ┌────────────────┐
│ Connect       ├──────────►│ Retry Logic    │
│ Tracking?     │           └───────┬────────┘
└───────┬───────┘                   │
        │ Yes                       │ Max Retries
        │                           ▼
        │                  ┌────────────────┐
        │                  │ Error and Exit │
        │                  └────────────────┘
        ▼
┌───────────────┐
│ Start Event   │
│ Processing    │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ Wait for      │
│ Cancellation  │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ Cleanup       │
│ Resources     │
└───────────────┘
```

## Renaming Considerations

The current "Bridge Service" name doesn't fully capture the component's orchestration responsibilities. Alternative names that better reflect its role include:

- **ApplicationOrchestrator**
- **SharpBridgeOrchestrator**
- **TrackingOrchestrator**
- **TrackingPipeline**

We recommend renaming the component to **ApplicationOrchestrator** to clearly communicate its primary responsibility of coordinating the application lifecycle.

## Future Considerations

1. **UI Integration**
   - When a UI is added, the orchestrator will need to provide status updates to the UI
   - Connection state changes should be observable by the UI
   - The UI may need to trigger reconnection or parameter creation

2. **Parameter Creation**
   - Future support for creating VTube Studio parameters
   - These operations should be exposed through the orchestrator
   - Will require additional error handling and user feedback

3. **Configuration Updates**
   - Support for runtime configuration changes
   - May require component reinitialization
   - Should maintain state where possible

## Conclusion

This orchestration approach provides a clear separation of concerns, with individual components handling their specific tasks while the orchestrator manages the overall application lifecycle. By centralizing connection management, error handling, and lifecycle coordination, we create a more maintainable and robust application that can gracefully handle various error conditions while providing clear feedback to users. 