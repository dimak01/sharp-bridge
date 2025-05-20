# Resiliency Improvements

## Overview

This document outlines proposed improvements to enhance the resiliency of the Sharp Bridge application, focusing on essential connection management and observability improvements.

## Current State

### Existing Features

1. **VTubeStudioPCClient**
   - Connection state tracking via WebSocketState
   - Connection attempt and failure counting
   - Uptime and message statistics
   - Token persistence and management
   - Last successful connection tracking

2. **VTubeStudioPhoneClient**
   - Frame counting and FPS calculation
   - Failed frame tracking
   - Uptime monitoring
   - Status tracking
   - Receive timeout handling

3. **ApplicationOrchestrator**
   - Runtime configuration via keyboard shortcuts
   - Hot-reloading of transformation config
   - Basic error handling and logging
   - Main application loop with component coordination

## Current Limitations

1. **Strict Initialization Requirements**
   - Application fails to start if VTube Studio PC client is not found
   - No graceful handling of temporary service unavailability
   - Connection failures are treated as critical errors

2. **Limited Connection Management**
   - No automatic reconnection attempts
   - Basic connection health monitoring exists but could be enhanced
   - Status indicators exist but could be more comprehensive

3. **Basic Error Handling**
   - Limited retry mechanisms
   - Basic error reporting exists but could be enhanced

## Proposed Improvements

### 1. New Interfaces

#### Initialization Interface
```csharp
public interface IInitializable
{
    /// <summary>
    /// Attempts to initialize the component
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if initialization was successful</returns>
    Task<bool> TryInitializeAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets the last error that occurred during initialization
    /// </summary>
    string LastInitializationError { get; }
}
```

#### Enhanced Service Stats
```csharp
public interface IServiceStats
{
    // Existing properties
    string ServiceName { get; }
    string Status { get; }
    Dictionary<string, long> Counters { get; }
    IFormattableObject CurrentEntity { get; }
    
    // New properties
    bool IsHealthy { get; }
    DateTime LastSuccessfulOperation { get; }
    string LastError { get; }
}
```

### 2. Orchestrator-Centric Recovery

#### Simple Recovery Policy
```csharp
public class SimpleRecoveryPolicy
{
    private readonly TimeSpan _retryInterval;
    
    public SimpleRecoveryPolicy(TimeSpan retryInterval)
    {
        _retryInterval = retryInterval;
    }
    
    public TimeSpan GetNextDelay()
    {
        return _retryInterval;
    }
}
```

#### Main Loop Recovery Logic
```csharp
private async Task RunUntilCancelled(CancellationToken cancellationToken)
{
    _status = "Running";
    _logger.Info("Starting main application loop...");
    
    // Initialize timing variables
    var nextRequestTime = DateTime.UtcNow;
    var nextStatusUpdateTime = DateTime.UtcNow;
    var nextRecoveryAttemptTime = DateTime.UtcNow;
    
    // Single recovery policy with consistent 2-second interval
    var recoveryPolicy = new SimpleRecoveryPolicy(TimeSpan.FromSeconds(2));
    
    try
    {
        _consoleRenderer.ClearConsole();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Check if it's time to attempt recovery
                if (DateTime.UtcNow >= nextRecoveryAttemptTime)
                {
                    var needsRecovery = await AttemptRecoveryAsync(cancellationToken);
                    // Always use consistent interval for next attempt
                    nextRecoveryAttemptTime = DateTime.UtcNow.Add(recoveryPolicy.GetNextDelay());
                }
                
                // Normal operation continues...
                // [Existing loop logic]
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _status = $"Error: {ex.Message}";
                _logger.Error("Error in application loop: {0}", ex.Message);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
    catch (OperationCanceledException)
    {
        _status = "Cancellation requested";
        _logger.Info("Operation was canceled, shutting down...");
    }
    finally
    {
        _status = "Stopped";
    }
}

private async Task<bool> AttemptRecoveryAsync(CancellationToken cancellationToken)
{
    bool needsRecovery = false;
    
    // Check PC client health
    var pcStats = _vtubeStudioPCClient.GetServiceStats();
    if (!pcStats.IsHealthy)
    {
        _logger.Info("Attempting to recover PC client...");
        await _vtubeStudioPCClient.TryInitializeAsync(cancellationToken);
        needsRecovery = true;
    }
    
    // Check Phone client health
    var phoneStats = _vtubeStudioPhoneClient.GetServiceStats();
    if (!phoneStats.IsHealthy)
    {
        _logger.Info("Attempting to recover Phone client...");
        await _vtubeStudioPhoneClient.TryInitializeAsync(cancellationToken);
        needsRecovery = true;
    }
    
    // Check transformation engine health
    // [Future: Add transformation engine health check]
    
    return needsRecovery;
}
```

### 3. Initialization Flow Refactoring

#### Simplified Initialization
1. Validate transformation config (critical)
2. Start clients in disconnected state
3. Begin connection attempts in background
4. Allow application to start regardless of client state
5. Show clear status indicators

#### Error Handling
- Only treat transformation config errors as critical
- Handle client connection failures gracefully
- Use orchestrator for recovery coordination
- Support manual reconnection via existing keyboard shortcuts

### 4. Observability Improvements

#### Status Display
- Show clear connection status (Connected/Disconnected)
- Display basic health metrics (uptime, last success)
- Show recovery attempts when applicable
- Use existing console renderer with minimal changes

#### Console UI Updates
- Add simple connection status indicators
- Show basic health metrics
- Display recovery information when relevant
- Keep error messages clear and actionable

## Implementation Details

### Client Changes
- Both clients implement `IInitializable`
- Keep existing tech-specific interfaces (`IVTubeStudioPCClient`, `IVTubeStudioPhoneClient`)
- Enhance `IServiceStats` implementation with new properties
- Clients remain focused on their core responsibilities

### Orchestrator Changes
- Add recovery coordination in main loop
- Track basic client health
- Handle connection failures gracefully
- Provide clear status updates
- Support manual reconnection
- Build on existing keyboard shortcuts

## Benefits

1. **Improved Reliability**
   - Application can start without all services
   - Centralized recovery coordination
   - Clear status visibility
   - Builds on existing monitoring

2. **Better User Experience**
   - Clear connection status
   - Actionable error messages
   - Simple visual indicators
   - Basic health information

3. **Enhanced Maintainability**
   - Simple, understandable code
   - Clear component responsibilities
   - Easy to debug
   - Minimal new complexity

## Implementation Phases

1. **Phase 1: Core Changes**
   - Add new interfaces
   - Update orchestrator main loop
   - Improve status indicators
   - Build on existing features

2. **Phase 2: UI Improvements**
   - Enhance status display
   - Add basic health metrics
   - Improve error messages
   - Build on existing console renderer

## Future Considerations

1. **Simple Monitoring**
   - Basic performance metrics
   - Connection success rate
   - Error frequency
   - Build on existing statistics

2. **Configuration Options**
   - Recovery attempt interval
   - Connection timeout
   - Build on existing config

## Implementation Plan

For a detailed implementation checklist and progress tracking, please refer to `ResiliencyImplementationChecklist.md`. 